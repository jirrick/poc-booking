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
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

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
        // no_reply_needed: PUT to set, DELETE to unset (no request body)
        group.MapPut("/properties/{propertyId}/conversations/{conversationId}/tags/no_reply_needed", SetNoReplyNeeded)
            .WithName("SetNoReplyNeeded");
        group.MapDelete("/properties/{propertyId}/conversations/{conversationId}/tags/no_reply_needed", RemoveNoReplyNeeded)
            .WithName("RemoveNoReplyNeeded");
        // message_read: PUT to mark read, DELETE to unmark — body: { message_ids, participant_id }
        group.MapPut("/properties/{propertyId}/conversations/{conversationId}/tags/message_read", SetMessageRead)
            .WithName("SetMessageRead");
        group.MapDelete("/properties/{propertyId}/conversations/{conversationId}/tags/message_read", RemoveMessageRead)
            .WithName("RemoveMessageRead");
    }

    // ── Response helpers ──────────────────────────────────────────────────────

    private static IResult Envelope(object data, int statusCode = 200) =>
        Results.Json(new
        {
            meta = new { ruid = GenerateRuid() },
            data,
            errors = Array.Empty<object>(),
            warnings = Array.Empty<object>()
        }, statusCode: statusCode, options: JsonOptions);

    private static string GenerateRuid()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(72);
        return "UmFuZG9tSVYkc2RlIyh9Y" + Convert.ToBase64String(bytes);
    }

    private static object MapParticipant(Participant p, string propertyExternalId)
    {
        if (p.ParticipantType is "hotel" or "property")
            return new { participant_id = p.ParticipantId, metadata = new { type = "property", id = propertyExternalId } };
        return new { participant_id = p.ParticipantId, metadata = new { type = "guest" } };
    }

    // List endpoint: sender_id string, attachment_files_uuid, no tags, no message_type
    private static object MapMessageForList(Message m) => new
    {
        message_id = m.MessageId,
        timestamp = m.TimestampUtc.ToString("O"),
        sender_id = m.Sender.ParticipantId,
        content = m.Content,
        attachment_files_uuid = Array.Empty<string>()
    };

    // Detail endpoint: sender_id string, attachment_ids, tags.read, no message_type
    private static object MapMessageForDetail(Message m) => new
    {
        message_id = m.MessageId,
        timestamp = m.TimestampUtc.ToString("O"),
        sender_id = m.Sender.ParticipantId,
        content = m.Content,
        attachment_ids = Array.Empty<string>(),
        tags = new { read = new { set = m.IsRead } }
    };

    // ── Endpoint handlers ─────────────────────────────────────────────────────

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

        var offset = 0;
        if (!string.IsNullOrEmpty(page_id) && int.TryParse(page_id, out var parsedOffset))
            offset = parsedOffset;

        var conversations = await query.Skip(offset).Take(DefaultPageSize + 1).ToListAsync(ct);
        var hasMore = conversations.Count > DefaultPageSize;
        if (hasMore) conversations = conversations.Take(DefaultPageSize).ToList();

        var nextPageId = hasMore ? (offset + DefaultPageSize).ToString() : null;

        // Load all participants for this property once
        var allParticipants = await db.Participants
            .Where(p => p.PropertyId == property.Id)
            .ToListAsync(ct);

        var list = new List<object>();
        foreach (var c in conversations)
        {
            var messages = await db.Messages
                .Where(m => m.ConversationId == c.Id)
                .OrderBy(m => m.TimestampUtc)
                .Include(m => m.Sender)
                .ToListAsync(ct);

            list.Add(new
            {
                conversation_id = c.ConversationId,
                conversation_reference = c.ConversationReference,
                conversation_type = c.ConversationType,
                access = "read_write",
                tags = new { no_reply_needed = new { set = c.NoReplyNeeded } },
                messages = messages.Select(MapMessageForList).ToList(),
                participants = allParticipants.Select(p => MapParticipant(p, property.PropertyId)).ToList()
            });
        }

        return Envelope(new
        {
            conversations = list,
            ok = true,
            next_page_id = nextPageId
        });
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
            .Where(p => p.PropertyId == property.Id)
            .ToListAsync(ct);

        return Envelope(new
        {
            conversation = new
            {
                conversation_id = conv.ConversationId,
                conversation_reference = conv.ConversationReference,
                conversation_type = conv.ConversationType,
                access = "read_write",
                tags = new { no_reply_needed = new { set = conv.NoReplyNeeded } },
                messages = conv.Messages.Select(MapMessageForDetail).ToList(),
                participants = participants.Select(p => MapParticipant(p, property.PropertyId)).ToList()
            },
            ok = true
        });
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

        return Envelope(new
        {
            message_id = message.MessageId,
            ok = true,
            guest_has_account = true
        });
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

        return Envelope(new
        {
            job_id = jobId,
            expires_at = now.Add(SearchJobTtl).ToString("O"),
            ok = true
        });
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
            timestamp = m.TimestampUtc.ToString("O"),
            sender_id = m.Sender.ParticipantId,
            content = m.Content,
            attachment_ids = Array.Empty<string>(),
            tags = new { read = new { set = false } },
            conversation = new
            {
                property_id = m.Conversation.Property.PropertyId,
                conversation_id = m.Conversation.ConversationId,
                conversation_reference = m.Conversation.ConversationReference,
                conversation_type = m.Conversation.ConversationType
            }
        }).ToList();

        return Envelope(new
        {
            messages = list,
            next_page_id = nextPageId
        });
    }

    // ── Tag endpoints ─────────────────────────────────────────────────────────

    // PUT .../tags/no_reply_needed — no body, sets the tag
    private static async Task<IResult> SetNoReplyNeeded(
        string propertyId, string conversationId,
        HttpContext context, SimulatorDbContext db, IConfiguration config, CancellationToken ct)
    {
        if (!Authorize(config, context)) return Results.Unauthorized();
        var (conv, _) = await LoadConversation(propertyId, conversationId, db, ct);
        if (conv == null) return Results.NotFound();
        conv.NoReplyNeeded = true;
        await db.SaveChangesAsync(ct);
        return Envelope(new { ok = true, tag = "no_reply_needed", is_set = true });
    }

    // DELETE .../tags/no_reply_needed — no body, removes the tag
    private static async Task<IResult> RemoveNoReplyNeeded(
        string propertyId, string conversationId,
        HttpContext context, SimulatorDbContext db, IConfiguration config, CancellationToken ct)
    {
        if (!Authorize(config, context)) return Results.Unauthorized();
        var (conv, _) = await LoadConversation(propertyId, conversationId, db, ct);
        if (conv == null) return Results.NotFound();
        conv.NoReplyNeeded = false;
        await db.SaveChangesAsync(ct);
        return Envelope(new { ok = true, tag = "no_reply_needed", is_set = false });
    }

    // PUT .../tags/message_read — body: { message_ids: [], participant_id: "" }
    private static async Task<IResult> SetMessageRead(
        string propertyId, string conversationId,
        HttpContext context, SimulatorDbContext db, IConfiguration config, CancellationToken ct)
    {
        if (!Authorize(config, context)) return Results.Unauthorized();
        var (conv, _) = await LoadConversation(propertyId, conversationId, db, ct);
        if (conv == null) return Results.NotFound();
        var body = await context.Request.ReadFromJsonAsync<MessageReadBody>(ct);
        if (body?.MessageIds is { Length: > 0 } ids)
        {
            var messages = await db.Messages
                .Where(m => m.ConversationId == conv.Id && ids.Contains(m.MessageId))
                .ToListAsync(ct);
            foreach (var msg in messages) msg.IsRead = true;
            await db.SaveChangesAsync(ct);
        }
        return Envelope(new { ok = true, tag = "read", is_set = true });
    }

    // DELETE .../tags/message_read — body: { message_ids: [], participant_id: "" }
    private static async Task<IResult> RemoveMessageRead(
        string propertyId, string conversationId,
        HttpContext context, SimulatorDbContext db, IConfiguration config, CancellationToken ct)
    {
        if (!Authorize(config, context)) return Results.Unauthorized();
        var (conv, _) = await LoadConversation(propertyId, conversationId, db, ct);
        if (conv == null) return Results.NotFound();
        var body = await context.Request.ReadFromJsonAsync<MessageReadBody>(ct);
        if (body?.MessageIds is { Length: > 0 } ids)
        {
            var messages = await db.Messages
                .Where(m => m.ConversationId == conv.Id && ids.Contains(m.MessageId))
                .ToListAsync(ct);
            foreach (var msg in messages) msg.IsRead = false;
            await db.SaveChangesAsync(ct);
        }
        // is_set: true — means "operation succeeded" for message_read (matches real API behaviour)
        return Envelope(new { ok = true, tag = "read", is_set = true });
    }

    /// Shared helper: resolves property + conversation, returns (null, null) if either not found.
    private static async Task<(Conversation? conv, Property? property)> LoadConversation(
        string propertyId, string conversationId, SimulatorDbContext db, CancellationToken ct)
    {
        var property = await db.Properties.FirstOrDefaultAsync(p => p.PropertyId == propertyId, ct);
        if (property == null) return (null, null);
        var conv = await db.Conversations
            .FirstOrDefaultAsync(c => c.PropertyId == property.Id && c.ConversationId == conversationId, ct);
        return (conv, property);
    }

    // ── Auth ──────────────────────────────────────────────────────────────────

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

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class MessageReadBody
{
    [JsonPropertyName("message_ids")]
    public string[]? MessageIds { get; set; }

    [JsonPropertyName("participant_id")]
    public string? ParticipantId { get; set; }
}

