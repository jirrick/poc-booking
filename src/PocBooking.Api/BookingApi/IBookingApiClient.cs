namespace PocBooking.Api.BookingApi;

/// <summary>
/// Client for Booking.com property API: list conversations, get thread, send reply.
/// Configurable via Booking:ApiBaseUrl and Booking:ApiKey (same code path for simulator or real Booking).
/// </summary>
public interface IBookingApiClient
{
    /// <summary>GET conversations for a property. Returns JSON with data.conversations and data.next_page_id.</summary>
    Task<BookingApiResponse<ConversationListResponse>?> GetConversationsAsync(string propertyId, string? pageId = null, CancellationToken cancellationToken = default);

    /// <summary>GET a single conversation (thread) with messages. Returns JSON with data.messages, data.participants.</summary>
    Task<BookingApiResponse<ConversationDetailResponse>?> GetConversationAsync(string propertyId, string conversationId, CancellationToken cancellationToken = default);

    /// <summary>POST a message as property (hotel). Body: { message: { content: "..." } }.</summary>
    Task<BookingApiResponse<PostMessageResponse>?> PostMessageAsync(string propertyId, string conversationId, string content, CancellationToken cancellationToken = default);
}

public sealed class BookingApiResponse<T>
{
    public T? Data { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; }
}

public sealed class ConversationListResponse
{
    public List<ConversationListItem> Conversations { get; set; } = new();
    public string? NextPageId { get; set; }
}

public sealed class ConversationListItem
{
    public string? ConversationId { get; set; }
    public string? ConversationReference { get; set; }
    public string? ConversationType { get; set; }
    public List<MessageSummary> Messages { get; set; } = new();
}

public sealed class ConversationDetailResponse
{
    public string? ConversationId { get; set; }
    public string? ConversationReference { get; set; }
    public string? ConversationType { get; set; }
    public List<MessageSummary> Messages { get; set; } = new();
    public List<ParticipantSummary> Participants { get; set; } = new();
}

public sealed class MessageSummary
{
    public string? MessageId { get; set; }
    public string? MessageType { get; set; }
    public string? Timestamp { get; set; }
    public string? Content { get; set; }
    public SenderSummary? Sender { get; set; }
}

public sealed class SenderSummary
{
    public string? ParticipantId { get; set; }
    public SenderMetadata? Metadata { get; set; }
}

public sealed class SenderMetadata
{
    public string? Name { get; set; }
    public string? ParticipantType { get; set; }
}

public sealed class ParticipantSummary
{
    public string? ParticipantId { get; set; }
    public SenderMetadata? Metadata { get; set; }
}

public sealed class PostMessageResponse
{
    public string? MessageId { get; set; }
    public bool Ok { get; set; }
}
