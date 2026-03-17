namespace PocBooking.Api.Mapping;

/// <summary>Resolved local identity for a Booking.com conversation.</summary>
public sealed class ConversationMapping
{
    public string BookingReservationId { get; init; } = "";
    public string ConfirmationNumber { get; init; } = "";
    public Guid InternalReservationId { get; init; }
    public string? GuestName { get; init; }
    public Guid? InternalGuestId { get; init; }
    public string? BookingGuestId { get; init; }
}
