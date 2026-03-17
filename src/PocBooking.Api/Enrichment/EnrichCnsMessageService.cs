using Microsoft.EntityFrameworkCore;
using PocBooking.Api.Data;
using PocBooking.Api.Models;

namespace PocBooking.Api.Enrichment;

public sealed class EnrichCnsMessageService : IEnrichCnsMessage
{
    private readonly AppDbContext _db;

    public EnrichCnsMessageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<EnrichedCnsResult?> EnrichAsync(MessagingApiNewMessagePayload payload, CancellationToken cancellationToken = default)
    {
        var bookingReservationId = payload.Conversation?.ConversationReference?.Trim() ?? string.Empty;
        var bookingGuestId = payload.Sender?.ParticipantId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(bookingReservationId) || string.IsNullOrEmpty(bookingGuestId))
            return null;

        var now = DateTime.UtcNow;

        // Get-or-create reservation mapping
        var reservationMapping = await _db.ReservationMappings
            .FirstOrDefaultAsync(m => m.BookingReservationId == bookingReservationId, cancellationToken);
        if (reservationMapping == null)
        {
            reservationMapping = new ReservationMapping
            {
                BookingReservationId = bookingReservationId,
                InternalReservationId = Guid.NewGuid(),
                CreatedAtUtc = now,
            };
            _db.ReservationMappings.Add(reservationMapping);
        }

        // Get-or-create guest mapping
        var guestMapping = await _db.GuestMappings
            .FirstOrDefaultAsync(m => m.BookingGuestId == bookingGuestId, cancellationToken);
        if (guestMapping == null)
        {
            guestMapping = new GuestMapping
            {
                BookingGuestId = bookingGuestId,
                InternalGuestId = Guid.NewGuid(),
                CreatedAtUtc = now,
            };
            _db.GuestMappings.Add(guestMapping);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new EnrichedCnsResult
        {
            InternalReservationId = reservationMapping.InternalReservationId,
            InternalGuestId = guestMapping.InternalGuestId,
        };
    }
}
