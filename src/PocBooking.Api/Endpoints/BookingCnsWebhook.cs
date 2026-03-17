using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PocBooking.Api.Data;
using PocBooking.Api.Models;

namespace PocBooking.Api.Endpoints;

/// <summary>Category for webhook logging.</summary>
internal sealed class BookingCnsWebhookEndpoint;

public static class BookingCnsWebhook
{
    public const string Route = "/api/webhooks/booking/cns";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static void MapBookingCnsWebhook(this IEndpointRouteBuilder routes)
    {
        routes.MapPost(Route, Handle);
    }

    private static async Task<string> ReadRawBodyAsync(HttpRequest request, CancellationToken ct)
    {
        request.EnableBuffering();
        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(ct);
        request.Body.Position = 0;
        return body;
    }

    private static async Task<IResult> Handle(
        HttpRequest request,
        AppDbContext db,
        IConfiguration config,
        ILogger<BookingCnsWebhookEndpoint> logger,
        CancellationToken ct)
    {
        // 1) Optional auth: require Bearer for webhook (POC: no JWT validation, just presence)
        if (config.GetValue<bool>("Booking:Cns:RequireBearer", true))
        {
            if (!request.Headers.Authorization.Any())
                return Results.Json(new { error = "Missing Authorization header" }, statusCode: 401);
            var auth = request.Headers.Authorization.ToString();
            if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Results.Json(new { error = "Invalid Authorization scheme" }, statusCode: 401);
        }

        // 2) Read and parse body
        BookingCnsNotification? notification;
        string rawBody;
        try
        {
            rawBody = await ReadRawBodyAsync(request, ct);
            if (string.IsNullOrWhiteSpace(rawBody))
                return Results.BadRequest(new { error = "Empty body" });
            notification = JsonSerializer.Deserialize<BookingCnsNotification>(rawBody, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid JSON in Booking CNS webhook");
            return Results.BadRequest(new { error = "Invalid JSON", detail = ex.Message });
        }

        if (notification?.Metadata == null)
            return Results.BadRequest(new { error = "Missing metadata" });

        var metadata = notification.Metadata;
        if (string.IsNullOrEmpty(metadata.Uuid))
            return Results.BadRequest(new { error = "Missing metadata.uuid" });

        if (!Guid.TryParse(metadata.Uuid, out var notificationUuid))
            return Results.BadRequest(new { error = "Invalid metadata.uuid" });

        var type = metadata.Type ?? string.Empty;

        // 3) Idempotency: already processed this notification?
        var existingByUuid = await db.NotificationInbox
            .AsNoTracking()
            .AnyAsync(e => e.NotificationUuid == notificationUuid, ct);
        if (existingByUuid)
        {
            logger.LogDebug("Duplicate notification (uuid): {Uuid}", metadata.Uuid);
            return Results.Ok(new { received = true, duplicate = true });
        }

        // 4) Resolve message_id for messaging type (and optional extra dedup by message_id)
        string messageId;
        var payloadElement = notification.Payload;
        if (type == "MESSAGING_API_NEW_MESSAGE" && payloadElement.HasValue && payloadElement.Value.ValueKind == JsonValueKind.Object)
        {
            try
            {
                var payload = payloadElement.Value.Deserialize<MessagingApiNewMessagePayload>(JsonOptions);
                messageId = payload?.MessageId ?? metadata.Uuid;
                // Optional: dedup by message_id (e.g. replay with different notification uuid)
                var existingByMessageId = await db.NotificationInbox
                    .AsNoTracking()
                    .AnyAsync(e => e.MessageId == messageId, ct);
                if (existingByMessageId)
                {
                    logger.LogDebug("Duplicate notification (message_id): {MessageId}", messageId);
                    return Results.Ok(new { received = true, duplicate = true });
                }
            }
            catch
            {
                messageId = metadata.Uuid;
            }
        }
        else
        {
            messageId = metadata.Uuid;
        }

        // 5) Persist
        var inbox = new NotificationInbox
        {
            NotificationUuid = notificationUuid,
            MessageId = messageId,
            Type = type,
            ReceivedAtUtc = DateTime.UtcNow,
            PayloadJson = rawBody,
        };
        db.NotificationInbox.Add(inbox);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Booking CNS notification received: Type={Type}, Uuid={Uuid}, MessageId={MessageId}",
            type, metadata.Uuid, messageId);

        return Results.Ok(new { received = true });
    }
}
