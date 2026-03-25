using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PocBooking.Api.BookingApi;
using PocBooking.Api.Data;
using PocBooking.Api.Mapping;

namespace PocBooking.Api.Pages;

public class ConversationModel(IBookingApiClient bookingApi, IConversationMappingService mappingService, AppDbContext db) : PageModel
{
    public string PropertyId { get; set; } = "";
    public string ConversationId { get; set; } = "";
    public string? ConversationReference { get; set; }
    public string? Error { get; set; }
    public List<MessageVm> Messages { get; set; } = new();
    public ConversationMapping? LocalMapping { get; set; }
    public Guid? InternalEnterpriseId { get; set; }
    public bool NoReplyNeeded { get; set; }
    public string? Access { get; set; }
    /// <summary>Participant ID of the property participant — used as participantId in tag calls.</summary>
    public string? PropertyParticipantId { get; set; }

    public async Task<IActionResult> OnGetAsync(string propertyId, string conversationId, CancellationToken ct = default)
    {
        PropertyId = propertyId;
        ConversationId = conversationId;

        var propertyMapping = await db.PropertyMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.BookingPropertyId == propertyId, ct);
        InternalEnterpriseId = propertyMapping?.InternalEnterpriseId;

        var response = await bookingApi.GetConversationAsync(propertyId, conversationId, ct);
        if (response?.Data == null)
        {
            Error = response?.Error ?? "Failed to load conversation.";
            return Page();
        }

        var conv = response.Data;
        ConversationReference = conv.ConversationReference;
        NoReplyNeeded = conv.Tags?.NoReplyNeeded?.Set ?? false;
        Access = conv.Access;

        // Build a participant lookup: participant_id → (type, name-hint)
        var participantIndex = conv.Participants
            .Where(p => p.ParticipantId != null)
            .ToDictionary(p => p.ParticipantId!, p => p);

        PropertyParticipantId = conv.Participants
            .FirstOrDefault(p => p.Metadata?.Type == "property")?.ParticipantId;

        foreach (var m in conv.Messages)
        {
            participantIndex.TryGetValue(m.SenderId ?? "", out var sender);
            var senderType = sender?.Metadata?.Type ?? "guest";
            Messages.Add(new MessageVm
            {
                MessageId = m.MessageId ?? "",
                Content = m.Content ?? "",
                TimestampUtc = ParseTimestamp(m.Timestamp),
                SenderType = senderType,
                SenderParticipantId = m.SenderId ?? "",
                IsRead = m.Tags?.Read?.Set ?? false,
            });
        }

        if (!string.IsNullOrEmpty(conv.ConversationReference))
        {
            var guestParticipantIds = conv.Participants
                .Where(p => p.Metadata?.Type == "guest")
                .Select(p => p.ParticipantId ?? "")
                .Concat(Messages
                    .Where(m => m.SenderType == "guest")
                    .Select(m => m.SenderParticipantId));

            LocalMapping = await mappingService.GetMappingAsync(
                conv.ConversationReference,
                guestParticipantIds,
                ct);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSendMessageAsync(string propertyId, string conversationId, string? content, CancellationToken ct = default)
    {
        content = content?.Trim();
        if (string.IsNullOrEmpty(content))
        {
            TempData["Error"] = "Content is required.";
            return RedirectToPage(new { propertyId, conversationId });
        }

        var response = await bookingApi.PostMessageAsync(propertyId, conversationId, content, ct);
        if (response?.Data == null || !response.Data.Ok)
        {
            TempData["Error"] = response?.Error ?? "Failed to send message.";
            return RedirectToPage(new { propertyId, conversationId });
        }

        TempData["Success"] = "Message sent.";
        return RedirectToPage(new { propertyId, conversationId });
    }

    public async Task<IActionResult> OnPostSetNoReplyNeededAsync(string propertyId, string conversationId, bool value, CancellationToken ct = default)
    {
        var response = value
            ? await bookingApi.SetNoReplyNeededAsync(propertyId, conversationId, ct)
            : await bookingApi.RemoveNoReplyNeededAsync(propertyId, conversationId, ct);

        if (response?.Data == null || !response.Data.Ok)
            TempData["Error"] = response?.Error ?? "Failed to update tag.";
        else
            TempData["Success"] = value ? "Marked as no reply needed." : "No reply needed tag removed.";

        return RedirectToPage(new { propertyId, conversationId });
    }

    public async Task<IActionResult> OnPostMarkReadAsync(
        string propertyId, string conversationId, string participantId, string messageIds, CancellationToken ct = default)
    {
        var ids = messageIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (ids.Length == 0)
        {
            TempData["Error"] = "No message IDs provided.";
            return RedirectToPage(new { propertyId, conversationId });
        }

        var response = await bookingApi.SetMessageReadAsync(propertyId, conversationId, ids, participantId, ct);
        if (response?.Data == null || !response.Data.Ok)
            TempData["Error"] = response?.Error ?? "Failed to mark as read.";
        else
            TempData["Success"] = $"Marked {ids.Length} message(s) as read.";

        return RedirectToPage(new { propertyId, conversationId });
    }

    private static DateTime ParseTimestamp(string? timestamp)
    {
        if (string.IsNullOrEmpty(timestamp)) return DateTime.UtcNow;
        return DateTime.TryParse(timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime()
            : DateTime.UtcNow;
    }

    public sealed class MessageVm
    {
        public string MessageId { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime TimestampUtc { get; set; }
        public string SenderType { get; set; } = "";
        public string SenderParticipantId { get; set; } = "";
        public bool IsRead { get; set; }
    }
}
