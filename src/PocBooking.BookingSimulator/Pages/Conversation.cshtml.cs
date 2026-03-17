using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PocBooking.BookingSimulator.Data;
using PocBooking.BookingSimulator.Services;

namespace PocBooking.BookingSimulator.Pages;

public class ConversationModel : PageModel
{
    private readonly SimulatorDbContext _db;
    private readonly IPocWebhookSender _webhookSender;

    public ConversationModel(SimulatorDbContext db, IPocWebhookSender webhookSender)
    {
        _db = db;
        _webhookSender = webhookSender;
    }

    public string PropertyId { get; set; } = "";
    public string ConversationId { get; set; } = "";
    public string ConversationReference { get; set; } = "";
    public string? PropertyName { get; set; }
    public List<MessageVm> Messages { get; set; } = new();
    public string? SendAs { get; set; } = "guest";
    public string? MessageContent { get; set; }
    public GuestVm? LinkedGuest { get; set; }

    public async Task<IActionResult> OnGetAsync(string propertyId, string conversationId, CancellationToken ct = default)
    {
        var (conv, prop) = await LoadAsync(propertyId, conversationId, ct);
        if (conv == null || prop == null) return NotFound();
        PropertyId = propertyId;
        ConversationId = conversationId;
        ConversationReference = conv.ConversationReference;
        PropertyName = prop.Name;
        Messages = conv.Messages.OrderBy(m => m.TimestampUtc)
            .Select(m => new MessageVm
            {
                MessageId = m.MessageId,
                Content = m.Content,
                MessageType = m.MessageType,
                TimestampUtc = m.TimestampUtc,
                SenderName = m.Sender.Name,
                SenderType = m.Sender.ParticipantType,
            })
            .ToList();
        if (conv.GuestParticipant != null)
            LinkedGuest = new GuestVm { Name = conv.GuestParticipant.Name, ParticipantId = conv.GuestParticipant.ParticipantId };
        return Page();
    }

    public async Task<IActionResult> OnPostSendMessageAsync(
        string propertyId, string conversationId, string sendAs, string? content, CancellationToken ct = default)
    {
        var (conv, prop) = await LoadAsync(propertyId, conversationId, ct);
        if (conv == null || prop == null) return NotFound();
        content = content?.Trim();
        if (string.IsNullOrEmpty(content))
        {
            TempData["Error"] = "Content is required.";
            return RedirectToPage(new { propertyId, conversationId });
        }

        Participant? sender;
        if (sendAs == "guest" && conv.GuestParticipantId.HasValue)
        {
            // Use the conversation's specific linked guest
            sender = await _db.Participants.FindAsync([conv.GuestParticipantId.Value], ct);
        }
        else
        {
            var participants = await _db.Participants.Where(p => p.PropertyId == prop.Id).ToListAsync(ct);
            sender = participants.FirstOrDefault(p => p.ParticipantType == sendAs) ?? participants.FirstOrDefault();
        }

        if (sender == null)
        {
            TempData["Error"] = "No participant found for this role.";
            return RedirectToPage(new { propertyId, conversationId });
        }

        var message = new Message
        {
            MessageId = Guid.NewGuid().ToString(),
            Content = content,
            MessageType = "free_text",
            TimestampUtc = DateTime.UtcNow,
            ConversationId = conv.Id,
            SenderParticipantId = sender.Id,
        };
        _db.Messages.Add(message);
        await _db.SaveChangesAsync(ct);
        await _webhookSender.SendNewMessageNotificationAsync(message, ct);
        TempData["Success"] = $"Sent as {sender.Name} ({sender.ParticipantType}).";
        return RedirectToPage(new { propertyId, conversationId });
    }

    private async Task<(Conversation? conv, Property? prop)> LoadAsync(string propertyId, string conversationId, CancellationToken ct)
    {
        var prop = await _db.Properties.FirstOrDefaultAsync(p => p.PropertyId == propertyId, ct);
        if (prop == null) return (null, null);
        var conv = await _db.Conversations
            .Include(c => c.Messages.OrderBy(m => m.TimestampUtc)).ThenInclude(m => m.Sender)
            .Include(c => c.GuestParticipant)
            .FirstOrDefaultAsync(c => c.PropertyId == prop.Id && c.ConversationId == conversationId, ct);
        return (conv, prop);
    }

    public class MessageVm
    {
        public string MessageId { get; set; } = "";
        public string Content { get; set; } = "";
        public string MessageType { get; set; } = "";
        public DateTime TimestampUtc { get; set; }
        public string SenderName { get; set; } = "";
        public string SenderType { get; set; } = "";
    }

    public class GuestVm
    {
        public string Name { get; set; } = "";
        public string ParticipantId { get; set; } = "";
    }
}
