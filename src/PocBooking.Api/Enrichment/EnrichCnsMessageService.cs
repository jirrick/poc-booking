using Microsoft.EntityFrameworkCore;
using PocBooking.Api.Data;
using PocBooking.Api.Models;

namespace PocBooking.Api.Enrichment;

public sealed class EnrichCnsMessageService : IEnrichCnsMessage
{
    private static readonly string[] FirstNames =
        ["Emma", "Liam", "Sophia", "Noah", "Olivia", "James", "Ava", "William", "Mia", "Lucas",
         "Charlotte", "Henry", "Amelia", "Alexander", "Harper", "Sebastian", "Evelyn", "Jack", "Aria", "Owen"];

    private static readonly string[] LastNames =
        ["Johnson", "Chen", "Patel", "Williams", "Brown", "Garcia", "Miller", "Davis", "Wilson", "Moore",
         "Taylor", "Anderson", "Thomas", "Jackson", "White", "Harris", "Martin", "Thompson", "Lee", "Walker"];

    private readonly AppDbContext _db;

    public EnrichCnsMessageService(AppDbContext db) => _db = db;

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
                ConfirmationNumber = GenerateConfirmationNumber(),
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
                GuestName = GenerateGuestName(),
                CreatedAtUtc = now,
            };
            _db.GuestMappings.Add(guestMapping);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new EnrichedCnsResult
        {
            InternalReservationId = reservationMapping.InternalReservationId,
            InternalGuestId = guestMapping.InternalGuestId,
            GuestName = guestMapping.GuestName,
            ConfirmationNumber = reservationMapping.ConfirmationNumber,
        };
    }

    private static string GenerateGuestName()
    {
        var rng = Random.Shared;
        return $"{FirstNames[rng.Next(FirstNames.Length)]} {LastNames[rng.Next(LastNames.Length)]}";
    }

    private static string GenerateConfirmationNumber()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = Random.Shared;
        return "MWS-" + new string(Enumerable.Range(0, 6).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
    }
}
