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

    public async Task<IActionResult> OnGetAsync(string propertyId, CancellationToken ct = default)
    {
        var prop = await _db.Properties.FirstOrDefaultAsync(p => p.PropertyId == propertyId, ct);
        if (prop == null) return NotFound();
        PropertyId = propertyId;
        PropertyName = prop.Name;
        ConversationReference = GenerateReservationReference();
        return Page();
    }

    private static string GenerateReservationReference()
    {
        return (Random.Shared.Next(1_000_000_000, 2_147_483_647)).ToString();
    }

    public async Task<IActionResult> OnPostAsync(string propertyId, string conversationReference, string conversationType, CancellationToken ct = default)
    {
        var prop = await _db.Properties.FirstOrDefaultAsync(p => p.PropertyId == propertyId, ct);
        if (prop == null) return NotFound();

        var refTrimmed = conversationReference?.Trim() ?? "";
        if (string.IsNullOrEmpty(refTrimmed))
            refTrimmed = GenerateReservationReference();

        var type = conversationType == "request_to_book" ? "request_to_book" : "reservation";
        var conversationId = Guid.NewGuid().ToString();

        var conv = new Conversation
        {
            ConversationId = conversationId,
            ConversationReference = refTrimmed,
            ConversationType = type,
            PropertyId = prop.Id
        };
        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = "Conversation created.";
        return RedirectToPage("/Conversation", new { propertyId, conversationId });
    }
}
