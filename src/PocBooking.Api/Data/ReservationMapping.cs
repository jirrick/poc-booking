namespace PocBooking.Api.Data;

/// <summary>Maps Booking.com reservation/conversation reference to internal reservation ID.</summary>
public class ReservationMapping
{
    public int Id { get; set; }
    /// <summary>Booking-side identifier (e.g. conversation_reference).</summary>
    public string BookingReservationId { get; set; } = string.Empty;
    /// <summary>Internal (POC) reservation GUID.</summary>
    public Guid InternalReservationId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
