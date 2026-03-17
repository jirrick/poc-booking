using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PocBooking.BookingSimulator.Data;

namespace PocBooking.BookingSimulator.Pages;

public class IndexModel : PageModel
{
    private readonly SimulatorDbContext _db;

    public IndexModel(SimulatorDbContext db) => _db = db;

    public IList<Property> Properties { get; set; } = new List<Property>();
    public IList<ConversationSummary> Conversations { get; set; } = new List<ConversationSummary>();
    public string? SelectedPropertyId { get; set; }

    public async Task OnGetAsync(string? propertyId, CancellationToken ct = default)
    {
        Properties = await _db.Properties.OrderBy(p => p.Name).ToListAsync(ct);
        var prop = Properties.FirstOrDefault(p => p.PropertyId == (propertyId ?? Properties.FirstOrDefault()?.PropertyId));
        SelectedPropertyId = prop?.PropertyId;
        if (prop == null) return;

        var convs = await _db.Conversations
            .Where(c => c.PropertyId == prop.Id)
            .OrderByDescending(c => c.Messages.Select(m => m.TimestampUtc).DefaultIfEmpty(DateTime.MinValue).Max())
            .ThenByDescending(c => c.Id)
            .Take(100)
            .ToListAsync(ct);

        foreach (var c in convs)
        {
            var latest = await _db.Messages
                .Where(m => m.ConversationId == c.Id)
                .OrderByDescending(m => m.TimestampUtc)
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(ct);
            Conversations.Add(new ConversationSummary
            {
                ConversationId = c.ConversationId,
                ConversationReference = c.ConversationReference,
                ConversationType = c.ConversationType,
                LastMessagePreview = latest?.Content?.Length > 80 ? latest.Content[..80] + "…" : latest?.Content,
                LastMessageAt = latest?.TimestampUtc
            });
        }
    }

    public class ConversationSummary
    {
        public string ConversationId { get; set; } = "";
        public string ConversationReference { get; set; } = "";
        public string ConversationType { get; set; } = "";
        public string? LastMessagePreview { get; set; }
        public DateTime? LastMessageAt { get; set; }
    }
}
