namespace PocBooking.Api.Data;

/// <summary>Maps Booking.com property ID to internal enterprise ID.</summary>
public class PropertyMapping
{
    public int Id { get; set; }
    /// <summary>Booking-side property identifier.</summary>
    public string BookingPropertyId { get; set; } = string.Empty;
    /// <summary>Internal (POC) enterprise GUID.</summary>
    public Guid InternalEnterpriseId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

