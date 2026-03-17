namespace PocBooking.Api.Data;

/// <summary>Maps Booking.com participant/guest ID to internal guest ID.</summary>
public class GuestMapping
{
    public int Id { get; set; }
    /// <summary>Booking-side participant identifier.</summary>
    public string BookingGuestId { get; set; } = string.Empty;
    /// <summary>Internal (POC) guest GUID.</summary>
    public Guid InternalGuestId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
