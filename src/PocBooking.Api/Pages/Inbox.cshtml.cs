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
            .ToDictionaryAsync(p => p.NotificationInboxId, p => new { p.InternalReservationId, p.InternalGuestId }, ct);

        foreach (var x in inbox)
        {
            var p = processed.GetValueOrDefault(x.Id);
            Items.Add(new InboxRow
            {
                NotificationUuid = x.NotificationUuid,
                MessageId = x.MessageId,
                Type = x.Type,
                ReceivedAtUtc = x.ReceivedAtUtc,
                InternalReservationId = p?.InternalReservationId,
                InternalGuestId = p?.InternalGuestId,
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
    }
}
