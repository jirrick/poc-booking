using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PocBooking.Api.BookingApi;

namespace PocBooking.Api.Pages;

public class ConversationModel : PageModel
{
    private readonly IBookingApiClient _bookingApi;

    public ConversationModel(IBookingApiClient bookingApi) => _bookingApi = bookingApi;

    public string PropertyId { get; set; } = "";
    public string ConversationId { get; set; } = "";
    public string? ConversationReference { get; set; }
    public string? Error { get; set; }
    public List<MessageVm> Messages { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string propertyId, string conversationId, CancellationToken ct = default)
    {
        PropertyId = propertyId;
        ConversationId = conversationId;
        var response = await _bookingApi.GetConversationAsync(propertyId, conversationId, ct);
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
            });
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

        var response = await _bookingApi.PostMessageAsync(propertyId, conversationId, content, ct);
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

    public class MessageVm
    {
        public string MessageId { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime TimestampUtc { get; set; }
        public string SenderName { get; set; } = "";
        public string SenderType { get; set; } = "";
    }
}
