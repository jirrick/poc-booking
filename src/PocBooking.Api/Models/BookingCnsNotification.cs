using System.Text.Json;
using System.Text.Json.Serialization;

namespace PocBooking.Api.Models;

/// <summary>
/// Booking.com CNS notification envelope (all notification types).
/// See: https://developers.booking.com/connectivity/docs/notification-service/managing-notifications
/// </summary>
public sealed class BookingCnsNotification
{
    [JsonPropertyName("metadata")]
    public required BookingCnsMetadata Metadata { get; init; }

    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; init; }
}

public sealed class BookingCnsMetadata
{
    [JsonPropertyName("uuid")]
    public required string Uuid { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("payloadVersion")]
    public string? PayloadVersion { get; init; }
}

/// <summary>
/// Payload for MESSAGING_API_NEW_MESSAGE notifications.
/// </summary>
public sealed class MessagingApiNewMessagePayload
{
    [JsonPropertyName("message_id")]
    public required string MessageId { get; init; }

    [JsonPropertyName("message_type")]
    public string? MessageType { get; init; }

    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    [JsonPropertyName("reply_to")]
    public string? ReplyTo { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("attachment_ids")]
    public string[]? AttachmentIds { get; init; }

    [JsonPropertyName("attributes")]
    public JsonElement? Attributes { get; init; }

    [JsonPropertyName("sender")]
    public required MessagingParticipant Sender { get; init; }

    [JsonPropertyName("conversation")]
    public required MessagingConversation Conversation { get; init; }
}

public sealed class MessagingParticipant
{
    [JsonPropertyName("participant_id")]
    public required string ParticipantId { get; init; }

    [JsonPropertyName("metadata")]
    public MessagingParticipantMetadata? Metadata { get; init; }
}

public sealed class MessagingParticipantMetadata
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("participant_type")]
    public string? ParticipantType { get; init; }
}

public sealed class MessagingConversation
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
