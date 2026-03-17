using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PocBooking.BookingSimulator.Data;

namespace PocBooking.BookingSimulator.Pages;

public class NewConversationModel : PageModel
{
    private readonly SimulatorDbContext _db;

    public NewConversationModel(SimulatorDbContext db) => _db = db;

    public string PropertyId { get; set; } = "";
    public string? PropertyName { get; set; }
    public string ConversationReference { get; set; } = "";
    public string ConversationType { get; set; } = "reservation";
    /// <summary>Existing guest participants the user can choose from.</summary>
    public List<ParticipantOption> ExistingGuests { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string propertyId, CancellationToken ct = default)
    {
        var prop = await _db.Properties.FirstOrDefaultAsync(p => p.PropertyId == propertyId, ct);
        if (prop == null) return NotFound();
        PropertyId = propertyId;
        PropertyName = prop.Name;
        ConversationReference = GenerateReservationReference();
        await LoadGuestsAsync(prop.Id, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string propertyId,
        string conversationReference,
        string conversationType,
        string guestMode,       // "new" | "existing"
        string? newGuestName,
        int? existingGuestId,
        CancellationToken ct = default)
    {
        var prop = await _db.Properties.FirstOrDefaultAsync(p => p.PropertyId == propertyId, ct);
        if (prop == null) return NotFound();

        var refTrimmed = conversationReference?.Trim() ?? "";
        if (string.IsNullOrEmpty(refTrimmed))
            refTrimmed = GenerateReservationReference();

        var type = conversationType == "request_to_book" ? "request_to_book" : "reservation";

        // Resolve or create guest participant
        Participant? guest = null;
        if (guestMode == "new" && !string.IsNullOrWhiteSpace(newGuestName))
        {
            guest = new Participant
            {
                ParticipantId = Guid.NewGuid().ToString(),
                Name = newGuestName.Trim(),
                ParticipantType = "guest",
                PropertyId = prop.Id,
            };
            _db.Participants.Add(guest);
            await _db.SaveChangesAsync(ct);
        }
        else if (guestMode == "existing" && existingGuestId.HasValue)
        {
            guest = await _db.Participants.FirstOrDefaultAsync(
                p => p.Id == existingGuestId && p.PropertyId == prop.Id && p.ParticipantType == "guest", ct);
        }

        var conv = new Conversation
        {
            ConversationId = Guid.NewGuid().ToString(),
            ConversationReference = refTrimmed,
            ConversationType = type,
            PropertyId = prop.Id,
            GuestParticipantId = guest?.Id,
        };
        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = guest != null
            ? $"Conversation created with guest \"{guest.Name}\" (participant ID: {guest.ParticipantId})."
            : "Conversation created (no guest linked yet).";

        return RedirectToPage("/Conversation", new { propertyId, conversationId = conv.ConversationId });
    }

    private async Task LoadGuestsAsync(int propId, CancellationToken ct)
    {
        ExistingGuests = await _db.Participants
            .Where(p => p.PropertyId == propId && p.ParticipantType == "guest")
            .OrderBy(p => p.Name)
            .Select(p => new ParticipantOption { Id = p.Id, Name = p.Name, ParticipantId = p.ParticipantId })
            .ToListAsync(ct);
    }

    private static string GenerateReservationReference() =>
        Random.Shared.Next(1_000_000_000, 2_147_483_647).ToString();

    public class ParticipantOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string ParticipantId { get; set; } = "";
    }
}
