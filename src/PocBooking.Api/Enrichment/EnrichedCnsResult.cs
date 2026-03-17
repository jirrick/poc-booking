namespace PocBooking.Api.Enrichment;

/// <summary>Result of enriching a CNS message: internal reservation and guest IDs (get-or-create from mapping tables).</summary>
public sealed class EnrichedCnsResult
{
    public Guid InternalReservationId { get; init; }
    public Guid InternalGuestId { get; init; }
}
