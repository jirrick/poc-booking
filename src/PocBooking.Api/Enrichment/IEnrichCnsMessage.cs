using PocBooking.Api.Models;

namespace PocBooking.Api.Enrichment;

/// <summary>
/// Enriches a MESSAGING_API_NEW_MESSAGE payload with internal reservation/guest IDs via get-or-create mapping tables.
/// </summary>
public interface IEnrichCnsMessage
{
    /// <summary>
    /// Resolves or creates mapping for reservation (conversation_reference) and guest (sender.participant_id);
    /// persists new mappings; returns internal GUIDs.
    /// </summary>
    Task<EnrichedCnsResult?> EnrichAsync(MessagingApiNewMessagePayload payload, CancellationToken cancellationToken = default);
}
