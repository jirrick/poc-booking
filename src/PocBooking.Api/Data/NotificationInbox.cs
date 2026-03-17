namespace PocBooking.Api.Data;

/// <summary>
/// Idempotency / inbox record for Booking.com CNS notifications (e.g. MESSAGING_API_NEW_MESSAGE).
/// </summary>
public class NotificationInbox
{
    public int Id { get; set; }
    public Guid NotificationUuid { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; }
    /// <summary>Raw JSON body for replay/debug.</summary>
    public string? PayloadJson { get; set; }
}
