namespace PocBooking.Api.BookingApi;

/// <summary>
/// Client for Booking.com property API: list conversations, get thread, send reply, manage tags.
/// Configurable via Booking:ApiBaseUrl and Booking:ApiKey (same code path for simulator or real Booking).
/// </summary>
public interface IBookingApiClient
{
    Task<BookingApiResponse<ConversationListResponse>?> GetConversationsAsync(string propertyId, string? pageId = null, CancellationToken cancellationToken = default);
    Task<BookingApiResponse<ConversationDetailResponse>?> GetConversationAsync(string propertyId, string conversationId, CancellationToken cancellationToken = default);
    Task<BookingApiResponse<PostMessageResponse>?> PostMessageAsync(string propertyId, string conversationId, string content, CancellationToken cancellationToken = default);
    Task<BookingApiResponse<TagResponse>?> SetNoReplyNeededAsync(string propertyId, string conversationId, CancellationToken cancellationToken = default);
    Task<BookingApiResponse<TagResponse>?> RemoveNoReplyNeededAsync(string propertyId, string conversationId, CancellationToken cancellationToken = default);
    Task<BookingApiResponse<TagResponse>?> SetMessageReadAsync(string propertyId, string conversationId, IEnumerable<string> messageIds, string participantId, CancellationToken cancellationToken = default);
    Task<BookingApiResponse<TagResponse>?> RemoveMessageReadAsync(string propertyId, string conversationId, IEnumerable<string> messageIds, string participantId, CancellationToken cancellationToken = default);
}

public sealed class BookingApiResponse<T>
{
    public T? Data { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; }
}

// ── Conversation list ─────────────────────────────────────────────────────────

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
    public string? Access { get; set; }
    public ConversationTags? Tags { get; set; }
    public List<MessageSummary> Messages { get; set; } = new();
    public List<ParticipantSummary> Participants { get; set; } = new();
}

// ── Conversation detail ───────────────────────────────────────────────────────

public sealed class ConversationDetailResponse
{
    public string? ConversationId { get; set; }
    public string? ConversationReference { get; set; }
    public string? ConversationType { get; set; }
    public string? Access { get; set; }
    public ConversationTags? Tags { get; set; }
    public List<MessageSummary> Messages { get; set; } = new();
    public List<ParticipantSummary> Participants { get; set; } = new();
}

// ── Messages ──────────────────────────────────────────────────────────────────

public sealed class MessageSummary
{
    public string? MessageId { get; set; }
    public string? Timestamp { get; set; }
    public string? Content { get; set; }
    /// <summary>Flat participant UUID — use Participants list to resolve name/type.</summary>
    public string? SenderId { get; set; }
    public MessageTags? Tags { get; set; }
}

// ── Participants ──────────────────────────────────────────────────────────────

public sealed class ParticipantSummary
{
    public string? ParticipantId { get; set; }
    public ParticipantMetadata? Metadata { get; set; }
}

public sealed class ParticipantMetadata
{
    /// <summary>"property" or "guest"</summary>
    public string? Type { get; set; }
    /// <summary>Property ID — only present for type=property.</summary>
    public string? Id { get; set; }
}

// ── Tags ──────────────────────────────────────────────────────────────────────

public sealed class ConversationTags
{
    public TagState? NoReplyNeeded { get; set; }
}

public sealed class MessageTags
{
    public TagState? Read { get; set; }
}

public sealed class TagState
{
    public bool Set { get; set; }
}

public sealed class TagResponse
{
    public string? Tag { get; set; }
    public bool IsSet { get; set; }
    public bool Ok { get; set; }
}

// ── Other responses ───────────────────────────────────────────────────────────

public sealed class PostMessageResponse
{
    public string? MessageId { get; set; }
    public bool Ok { get; set; }
}
