namespace PocBooking.Api.Data;

/// <summary>Enriched view: one row per processed MESSAGING_API_NEW_MESSAGE with internal reservation/guest/enterprise IDs.</summary>
public class ProcessedMessage
{
    public int Id { get; set; }
    public int NotificationInboxId { get; set; }
    public Guid InternalReservationId { get; set; }
    public Guid InternalGuestId { get; set; }
    public Guid InternalEnterpriseId { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
}
