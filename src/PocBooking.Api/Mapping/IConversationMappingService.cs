namespace PocBooking.Api.Mapping;

public interface IConversationMappingService
{
    /// <summary>
    /// Resolves the local mapping for a single conversation.
    /// Looks up reservation by <paramref name="conversationReference"/>, then resolves
    /// the guest by matching <paramref name="guestParticipantIds"/> against GuestMappings.
    /// Returns null if no reservation mapping exists yet.
    /// </summary>
    Task<ConversationMapping?> GetMappingAsync(
        string conversationReference,
        IEnumerable<string> guestParticipantIds,
        CancellationToken ct = default);

    /// <summary>
    /// Batch-resolves local mappings for a list of conversations, keyed by Booking.com
    /// conversation reference. Conversations with no mapping are omitted from the result.
    /// </summary>
    Task<IReadOnlyDictionary<string, ConversationMapping>> GetMappingsByRefAsync(
        IEnumerable<string> conversationReferences,
        CancellationToken ct = default);
}
