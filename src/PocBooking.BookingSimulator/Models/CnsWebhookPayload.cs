using System.Text.Json.Serialization;

namespace PocBooking.BookingSimulator.Models;

public sealed class CnsWebhookPayload
{
    [JsonPropertyName("metadata")]
    public required CnsMetadata Metadata { get; init; }

    [JsonPropertyName("payload")]
    public required CnsMessagePayload Payload { get; init; }
}

public sealed class CnsMetadata
{
    [JsonPropertyName("uuid")]
    public required string Uuid { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("payloadVersion")]
    public required string PayloadVersion { get; init; }
}

public sealed class CnsMessagePayload
{
    [JsonPropertyName("message_id")]
    public required string MessageId { get; init; }

    [JsonPropertyName("message_type")]
    public required string MessageType { get; init; }

    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    [JsonPropertyName("reply_to")]
    public string? ReplyTo { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("attachment_ids")]
    public string[] AttachmentIds { get; init; } = [];

    [JsonPropertyName("sender")]
    public required CnsSender Sender { get; init; }

    [JsonPropertyName("conversation")]
    public required CnsConversation Conversation { get; init; }
}

public sealed class CnsSender
{
    [JsonPropertyName("participant_id")]
    public required string ParticipantId { get; init; }

    [JsonPropertyName("metadata")]
    public required CnsSenderMetadata Metadata { get; init; }
}

public sealed class CnsSenderMetadata
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("participant_type")]
    public required string ParticipantType { get; init; }
}

public sealed class CnsConversation
{
    [JsonPropertyName("property_id")]
    public required string PropertyId { get; init; }

    [JsonPropertyName("conversation_id")]
    public required string ConversationId { get; init; }

    [JsonPropertyName("conversation_reference")]
    public required string ConversationReference { get; init; }

    [JsonPropertyName("conversation_type")]
    public required string ConversationType { get; init; }
}
