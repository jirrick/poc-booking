using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PocBooking.BookingSimulator.Data;
using PocBooking.BookingSimulator.Services;

namespace PocBooking.BookingSimulator.Endpoints;

public static class MessagingEndpoints
{
    private const string Base = "/messaging";
    private const int DefaultPageSize = 50;
    private static readonly TimeSpan SearchJobTtl = TimeSpan.FromHours(48);

    public static void MapMessagingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(Base);

        group.MapGet("/properties/{propertyId}/conversations", GetConversations)
            .WithName("GetConversations");
        group.MapGet("/properties/{propertyId}/conversations/{conversationId}", GetConversation)
            .WithName("GetConversation");
        group.MapPost("/properties/{propertyId}/conversations/{conversationId}", PostMessage)
            .WithName("PostMessage");
        group.MapGet("/messages/search", CreateMessageSearchJob)
            .WithName("CreateMessageSearchJob");
        group.MapGet("/messages/search/result/{jobId}", GetMessageSearchResult)
            .WithName("GetMessageSearchResult");
    }

    private static async Task<IResult> GetConversations(
        string propertyId,
        [FromQuery] string? page_id,
        HttpContext context,
        SimulatorDbContext db,
        IConfiguration config,
        CancellationToken ct)
    {
        if (!Authorize(config, context)) return Results.Unauthorized();

        var property = await db.Properties.FirstOrDefaultAsync(p => p.PropertyId == propertyId, ct);
        if (property == null) return Results.NotFound();

        var query = db.Conversations
            .Where(c => c.PropertyId == property.Id)
            .OrderByDescending(c => c.Id);

        var pageSize = DefaultPageSize;
        var offset = 0;
        if (!string.IsNullOrEmpty(page_id) && int.TryParse(page_id, out var parsedOffset))
            offset = parsedOffset;

        var conversations = await query.Skip(offset).Take(pageSize + 1).ToListAsync(ct);
        var hasMore = conversations.Count > pageSize;
        if (hasMore) conversations = conversations.Take(pageSize).ToList();

        var nextPageId = hasMore ? (offset + pageSize).ToString() : null;

        var list = new List<object>();
        foreach (var c in conversations)
        {
            var latestMessage = await db.Messages
                .Where(m => m.ConversationId == c.Id)
                .OrderByDescending(m => m.TimestampUtc)
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(ct);
            var participants = await db.Participants
                .Where(p => db.Messages.Any(m => m.ConversationId == c.Id && m.SenderParticipantId == p.Id))
                .Select(p => new { participant_id = p.ParticipantId, metadata = new { name = p.Name, participant_type = p.ParticipantType } })
                .Distinct()
                .ToListAsync(ct);
            list.Add(new
            {
                conversation_id = c.ConversationId,
                conversation_reference = c.ConversationReference,
                conversation_type = c.ConversationType,
                messages = latestMessage == null ? Array.Empty<object>() : new[] { MapMessage(latestMessage) },
                participants
            });
        }

        return Results.Json(new
        {
            data = new
            {
                conversations = list,
                next_page_id = nextPageId
            }
        }, options: JsonOptions);
    }

    private static async Task<IResult> GetConversation(
        string propertyId,
        string conversationId,
        HttpContext context,
        SimulatorDbContext db,
        IConfiguration config,
        CancellationToken ct)
    {
        if (!Authorize(config, context)) return Results.Unauthorized();

        var property = await db.Properties.FirstOrDefaultAsync(p => p.PropertyId == propertyId, ct);
        if (property == null) return Results.NotFound();

        var conv = await db.Conversations
            .Include(c => c.Messages.OrderBy(m => m.TimestampUtc)).ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.PropertyId == property.Id && c.ConversationId == conversationId, ct);
        if (conv == null) return Results.NotFound();

        var participants = await db.Participants
            .Where(p => db.Messages.Any(m => m.ConversationId == conv.Id && m.SenderParticipantId == p.Id))
            .Select(p => new { participant_id = p.ParticipantId, metadata = new { name = p.Name, participant_type = p.ParticipantType } })
            .Distinct()
            .ToListAsync(ct);

        var messages = conv.Messages.Select(MapMessage).ToList();

        return Results.Json(new
        {
            data = new
            {
                conversation_id = conv.ConversationId,
                conversation_reference = conv.ConversationReference,
                conversation_type = conv.ConversationType,
                messages,
                participants
            }
        }, options: JsonOptions);
    }

    private static async Task<IResult> PostMessage(
        string propertyId,
        string conversationId,
        HttpContext context,
        SimulatorDbContext db,
        IPocWebhookSender webhookSender,
        IConfiguration config,
        CancellationToken ct)
    {
        if (!Authorize(config, context)) return Results.Unauthorized();

        var property = await db.Properties.FirstOrDefaultAsync(p => p.PropertyId == propertyId, ct);
        if (property == null) return Results.NotFound();

        var conv = await db.Conversations
            .Include(c => c.Property)
            .FirstOrDefaultAsync(c => c.PropertyId == property.Id && c.ConversationId == conversationId, ct);
        if (conv == null) return Results.NotFound();

        var body = await context.Request.ReadFromJsonAsync<PostMessageBody>(ct);
        var content = body?.Message?.Content?.Trim();
        if (string.IsNullOrEmpty(content))
            return Results.BadRequest(new { error = "message.content is required" });

        var participants = await db.Participants.Where(p => p.PropertyId == property.Id).ToListAsync(ct);
        var senderParticipant = participants.FirstOrDefault(p => p.ParticipantType == "hotel")
            ?? participants.First();

        var message = new Message
        {
            MessageId = Guid.NewGuid().ToString(),
            Content = content,
            MessageType = "free_text",
            TimestampUtc = DateTime.UtcNow,
            ConversationId = conv.Id,
            SenderParticipantId = senderParticipant.Id
        };
        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);

        await webhookSender.SendNewMessageNotificationAsync(message, ct);

        return Results.Json(new
        {
            message_id = message.MessageId,
            ok = true,
            guest_has_account = true
        }, statusCode: 200, options: JsonOptions);
    }

    private static async Task<IResult> CreateMessageSearchJob(
        [FromQuery] string? after,
        [FromQuery] string? before,
        [FromQuery] string? property_id,
        [FromQuery] string? order_by,
        HttpContext context,
        SimulatorDbContext db,
        IConfiguration config,
        CancellationToken ct)
    {
        if (!Authorize(config, context)) return Results.Unauthorized();

        DateTime? afterUtc = null, beforeUtc = null;
        if (!string.IsNullOrEmpty(after) && DateTime.TryParse(after, null, System.Globalization.DateTimeStyles.RoundtripKind, out var a))
            afterUtc = a.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(a, DateTimeKind.Utc) : a.ToUniversalTime();
        if (!string.IsNullOrEmpty(before) && DateTime.TryParse(before, null, System.Globalization.DateTimeStyles.RoundtripKind, out var b))
            beforeUtc = b.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(b, DateTimeKind.Utc) : b.ToUniversalTime();

        var order = string.Equals(order_by, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
        var jobId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        db.MessageSearchJobs.Add(new MessageSearchJob
        {
            JobId = jobId,
            AfterUtc = afterUtc,
            BeforeUtc = beforeUtc,
            PropertyId = property_id,
            OrderBy = order,
            ExpiresAtUtc = now.Add(SearchJobTtl),
            CreatedAtUtc = now
        });
        await db.SaveChangesAsync(ct);

        return Results.Json(new
        {
            data = new
            {
                job_id = jobId,
                expires_at = now.Add(SearchJobTtl).ToString("O"),
                ok = true
            }
        }, options: JsonOptions);
    }

    private static async Task<IResult> GetMessageSearchResult(
        string jobId,
        [FromQuery] string? page_id,
        HttpContext context,
        SimulatorDbContext db,
        IConfiguration config,
        CancellationToken ct)
    {
        if (!Authorize(config, context)) return Results.Unauthorized();

        var job = await db.MessageSearchJobs.FirstOrDefaultAsync(j => j.JobId == jobId, ct);
        if (job == null) return Results.NotFound();
        if (job.ExpiresAtUtc < DateTime.UtcNow) return Results.Json(new { error = "job expired" }, statusCode: 410);

        var query = db.Messages.AsQueryable();
        if (job.AfterUtc.HasValue) query = query.Where(m => m.TimestampUtc >= job.AfterUtc.Value);
        if (job.BeforeUtc.HasValue) query = query.Where(m => m.TimestampUtc <= job.BeforeUtc.Value);
        if (!string.IsNullOrEmpty(job.PropertyId))
        {
            var prop = await db.Properties.FirstOrDefaultAsync(p => p.PropertyId == job.PropertyId, ct);
            if (prop != null)
                query = query.Where(m => m.Conversation.PropertyId == prop.Id);
        }
        query = job.OrderBy == "asc"
            ? query.OrderBy(m => m.TimestampUtc)
            : query.OrderByDescending(m => m.TimestampUtc);

        var offset = 0;
        if (!string.IsNullOrEmpty(page_id) && int.TryParse(page_id, out var o)) offset = o;
        var messages = await query
            .Skip(offset)
            .Take(DefaultPageSize + 1)
            .Include(m => m.Sender)
            .Include(m => m.Conversation).ThenInclude(c => c!.Property)
            .ToListAsync(ct);
        var hasMore = messages.Count > DefaultPageSize;
        if (hasMore) messages = messages.Take(DefaultPageSize).ToList();
        var nextPageId = hasMore ? (offset + DefaultPageSize).ToString() : null;

        var list = messages.Select(m => new
        {
            message_id = m.MessageId,
            message_type = m.MessageType,
            timestamp = m.TimestampUtc.ToString("O"),
            content = m.Content,
            sender = new
            {
                participant_id = m.Sender.ParticipantId,
                metadata = new { name = m.Sender.Name, participant_type = m.Sender.ParticipantType }
            },
            conversation = new
            {
                property_id = m.Conversation.Property.PropertyId,
                conversation_id = m.Conversation.ConversationId,
                conversation_reference = m.Conversation.ConversationReference,
                conversation_type = m.Conversation.ConversationType
            }
        }).ToList();

        return Results.Json(new
        {
            data = new
            {
                messages = list,
                next_page_id = nextPageId
            }
        }, options: JsonOptions);
    }

    private static object MapMessage(Message m)
    {
        return new
        {
            message_id = m.MessageId,
            message_type = m.MessageType,
            timestamp = m.TimestampUtc.ToString("O"),
            content = m.Content,
            sender = new
            {
                participant_id = m.Sender.ParticipantId,
                metadata = new { name = m.Sender.Name, participant_type = m.Sender.ParticipantType }
            }
        };
    }

    private static bool Authorize(IConfiguration config, HttpContext? context = null)
    {
        var apiKey = config["BookingSimulator:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return true;
        if (context == null) return false;
        var auth = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;
        var token = auth["Bearer ".Length..].Trim();
        return token == apiKey;
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class PostMessageBody
{
    [JsonPropertyName("message")]
    public PostMessageInner? Message { get; set; }
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class PostMessageInner
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("attachment_ids")]
    public string[]? AttachmentIds { get; set; }
}
