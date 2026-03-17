using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PocBooking.Api.Data;

namespace PocBooking.Api.Pages;

public class InboxModel : PageModel
{
    private readonly AppDbContext _db;

    public InboxModel(AppDbContext db) => _db = db;

    public List<InboxRow> Items { get; set; } = new();

    public async Task OnGetAsync(CancellationToken ct = default)
    {
        const int take = 50;
        var inbox = await _db.NotificationInbox
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Take(take)
            .ToListAsync(ct);

        var inboxIds = inbox.Select(x => x.Id).ToList();
        var processed = await _db.ProcessedMessages
            .Where(p => inboxIds.Contains(p.NotificationInboxId))
            .ToListAsync(ct);

        var reservationIds = processed.Select(p => p.InternalReservationId).Distinct().ToList();
        var guestIds = processed.Select(p => p.InternalGuestId).Distinct().ToList();

        var reservations = await _db.ReservationMappings
            .Where(r => reservationIds.Contains(r.InternalReservationId))
            .ToDictionaryAsync(r => r.InternalReservationId, ct);

        var guests = await _db.GuestMappings
            .Where(g => guestIds.Contains(g.InternalGuestId))
            .ToDictionaryAsync(g => g.InternalGuestId, ct);

        var processedByInboxId = processed.ToDictionary(p => p.NotificationInboxId);

        foreach (var x in inbox)
        {
            var p = processedByInboxId.GetValueOrDefault(x.Id);
            var reservation = p != null ? reservations.GetValueOrDefault(p.InternalReservationId) : null;
            var guest = p != null ? guests.GetValueOrDefault(p.InternalGuestId) : null;

            Items.Add(new InboxRow
            {
                NotificationUuid = x.NotificationUuid,
                MessageId = x.MessageId,
                Type = x.Type,
                ReceivedAtUtc = x.ReceivedAtUtc,
                InternalReservationId = p?.InternalReservationId,
                InternalGuestId = p?.InternalGuestId,
                ConfirmationNumber = reservation?.ConfirmationNumber,
                GuestName = guest?.GuestName,
            });
        }
    }

    public class InboxRow
    {
        public Guid NotificationUuid { get; set; }
        public string MessageId { get; set; } = "";
        public string Type { get; set; } = "";
        public DateTime ReceivedAtUtc { get; set; }
        public Guid? InternalReservationId { get; set; }
        public Guid? InternalGuestId { get; set; }
        public string? ConfirmationNumber { get; set; }
        public string? GuestName { get; set; }
    }
}
