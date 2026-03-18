namespace PocBooking.Api.Enrichment;

/// <summary>Result of enriching a CNS message: internal IDs plus generated display identity for the POC.</summary>
public sealed class EnrichedCnsResult
{
    public Guid InternalReservationId { get; init; }
    public Guid InternalGuestId { get; init; }
    public Guid InternalEnterpriseId { get; init; }
    public string GuestName { get; init; } = string.Empty;
    public string ConfirmationNumber { get; init; } = string.Empty;
}
