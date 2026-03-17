using Microsoft.EntityFrameworkCore;
using PocBooking.Api.Data;

namespace PocBooking.Api.Mapping;

public sealed class ConversationMappingService(AppDbContext db) : IConversationMappingService
{
    public async Task<ConversationMapping?> GetMappingAsync(
        string conversationReference,
        IEnumerable<string> guestParticipantIds,
        CancellationToken ct = default)
    {
        var convRef = conversationReference.Trim();
        if (string.IsNullOrEmpty(convRef)) return null;

        var reservation = await db.ReservationMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BookingReservationId == convRef, ct);

        if (reservation == null) return null;

        GuestMapping? guest = null;
        var participantIds = guestParticipantIds
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        if (participantIds.Count > 0)
            guest = await db.GuestMappings
                .AsNoTracking()
                .FirstOrDefaultAsync(g => participantIds.Contains(g.BookingGuestId), ct);

        return new ConversationMapping
        {
            BookingReservationId = reservation.BookingReservationId,
            ConfirmationNumber = reservation.ConfirmationNumber,
            InternalReservationId = reservation.InternalReservationId,
            GuestName = guest?.GuestName,
            InternalGuestId = guest?.InternalGuestId,
            BookingGuestId = guest?.BookingGuestId,
        };
    }

    public async Task<IReadOnlyDictionary<string, ConversationMapping>> GetMappingsByRefAsync(
        IEnumerable<string> conversationReferences,
        CancellationToken ct = default)
    {
        var refs = conversationReferences
            .Where(r => !string.IsNullOrEmpty(r))
            .Distinct()
            .ToList();

        if (refs.Count == 0) return new Dictionary<string, ConversationMapping>();

        var reservationsByRef = await db.ReservationMappings
            .AsNoTracking()
            .Where(r => refs.Contains(r.BookingReservationId))
            .ToDictionaryAsync(r => r.BookingReservationId, ct);

        var internalReservationIds = reservationsByRef.Values
            .Select(r => r.InternalReservationId)
            .Distinct()
            .ToList();

        var guestIdByReservation = await db.ProcessedMessages
            .Where(p => internalReservationIds.Contains(p.InternalReservationId))
            .GroupBy(p => p.InternalReservationId)
            .Select(g => new { ReservationId = g.Key, GuestId = g.First().InternalGuestId })
            .ToDictionaryAsync(x => x.ReservationId, x => x.GuestId, ct);

        var guestIds = guestIdByReservation.Values.Distinct().ToList();
        var guestsByInternalId = await db.GuestMappings
            .AsNoTracking()
            .Where(g => guestIds.Contains(g.InternalGuestId))
            .ToDictionaryAsync(g => g.InternalGuestId, ct);

        var result = new Dictionary<string, ConversationMapping>();
        foreach (var (bookingRef, reservation) in reservationsByRef)
        {
            GuestMapping? guest = null;
            if (guestIdByReservation.TryGetValue(reservation.InternalReservationId, out var guestId))
                guestsByInternalId.TryGetValue(guestId, out guest);

            result[bookingRef] = new ConversationMapping
            {
                BookingReservationId = reservation.BookingReservationId,
                ConfirmationNumber = reservation.ConfirmationNumber,
                InternalReservationId = reservation.InternalReservationId,
                GuestName = guest?.GuestName,
                InternalGuestId = guest?.InternalGuestId,
                BookingGuestId = guest?.BookingGuestId,
            };
        }

        return result;
    }
}
