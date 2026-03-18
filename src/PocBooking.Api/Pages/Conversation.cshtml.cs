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

        ConversationReference = response.Data.ConversationReference;

        foreach (var m in response.Data.Messages ?? [])
        {
            Messages.Add(new MessageVm
            {
                MessageId = m.MessageId ?? "",
                Content = m.Content ?? "",
                TimestampUtc = ParseTimestamp(m.Timestamp),
                SenderName = m.Sender?.Metadata?.Name ?? "",
                SenderType = m.Sender?.Metadata?.ParticipantType ?? "",
                SenderParticipantId = m.Sender?.ParticipantId ?? "",
            });
        }

        if (!string.IsNullOrEmpty(response.Data.ConversationReference))
        {
            var guestParticipantIds = response.Data.Participants
                .Where(p => p.Metadata?.ParticipantType == "guest")
                .Select(p => p.ParticipantId ?? "")
                .Concat(Messages
                    .Where(m => m.SenderType == "guest")
                    .Select(m => m.SenderParticipantId));

            LocalMapping = await mappingService.GetMappingAsync(
                response.Data.ConversationReference,
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
        public string SenderName { get; set; } = "";
        public string SenderType { get; set; } = "";
        public string SenderParticipantId { get; set; } = "";
    }
}
