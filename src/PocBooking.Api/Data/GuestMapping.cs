namespace PocBooking.Api.Data;

/// <summary>Maps Booking.com participant/guest ID to internal guest ID.</summary>
public class GuestMapping
{
    public int Id { get; set; }
    /// <summary>Booking-side participant identifier.</summary>
    public string BookingGuestId { get; set; } = string.Empty;
    /// <summary>Internal (POC) guest GUID.</summary>
    public Guid InternalGuestId { get; set; }
    /// <summary>Randomly generated display name for the POC (e.g. "Emma Johnson").</summary>
    public string GuestName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
