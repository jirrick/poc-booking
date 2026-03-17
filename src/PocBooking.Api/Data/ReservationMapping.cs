namespace PocBooking.Api.Data;

/// <summary>Maps Booking.com reservation/conversation reference to internal reservation ID.</summary>
public class ReservationMapping
{
    public int Id { get; set; }
    /// <summary>Booking-side identifier (e.g. conversation_reference).</summary>
    public string BookingReservationId { get; set; } = string.Empty;
    /// <summary>Internal (POC) reservation GUID.</summary>
    public Guid InternalReservationId { get; set; }
    /// <summary>Randomly generated confirmation number for the POC (e.g. "MWS-A3F8B2").</summary>
    public string ConfirmationNumber { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
