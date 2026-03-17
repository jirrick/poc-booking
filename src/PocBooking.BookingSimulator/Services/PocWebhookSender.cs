using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PocBooking.BookingSimulator.Data;
using PocBooking.BookingSimulator.Models;

namespace PocBooking.BookingSimulator.Services;

public sealed class PocWebhookSender(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    IDbContextFactory<SimulatorDbContext> dbFactory,
    ILogger<PocWebhookSender> logger) : IPocWebhookSender
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public async Task SendNewMessageNotificationAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (!config.GetValue<bool>("BookingSimulator:SendWebhookOnNewMessage", true))
            return;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var msg = await db.Messages
            .AsNoTracking()
            .Include(m => m.Conversation).ThenInclude(c => c!.Property)
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == message.Id, cancellationToken);
        if (msg == null) return;

        var payload = BuildCnsPayload(msg);
        await SendPayloadAsync(payload, cancellationToken);
    }

    public async Task SendPayloadAsync(CnsWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        var baseUrl = config["BookingSimulator:PocWebhookBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            logger.LogDebug("PocWebhookBaseUrl not set; skipping webhook.");
            return;
        }

        var webhookUrl = $"{baseUrl}/api/webhooks/booking/cns";
        var bearerToken = config["BookingSimulator:PocBearerToken"];

        using var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };
        if (!string.IsNullOrEmpty(bearerToken))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

        try
        {
            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Webhook POST to {Url} returned {Code}", webhookUrl, response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to POST webhook to {Url}", webhookUrl);
        }
    }

    internal static CnsWebhookPayload BuildCnsPayload(Message msg)
    {
        return new CnsWebhookPayload
        {
            Metadata = new CnsMetadata
            {
                Uuid = Guid.NewGuid().ToString(),
                Type = "MESSAGING_API_NEW_MESSAGE",
                PayloadVersion = "1.0"
            },
            Payload = new CnsMessagePayload
            {
                MessageId = msg.MessageId,
                MessageType = msg.MessageType,
                Timestamp = msg.TimestampUtc.ToString("O"),
                ReplyTo = null,
                Content = msg.Content,
                AttachmentIds = [],
                Sender = new CnsSender
                {
                    ParticipantId = msg.Sender.ParticipantId,
                    Metadata = new CnsSenderMetadata
                    {
                        Name = msg.Sender.Name,
                        ParticipantType = msg.Sender.ParticipantType
                    }
                },
                Conversation = new CnsConversation
                {
                    PropertyId = msg.Conversation.Property.PropertyId,
                    ConversationId = msg.Conversation.ConversationId,
                    ConversationReference = msg.Conversation.ConversationReference,
                    ConversationType = msg.Conversation.ConversationType
                }
            }
        };
    }
}
