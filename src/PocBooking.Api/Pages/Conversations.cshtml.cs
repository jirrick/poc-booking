using Microsoft.AspNetCore.Mvc.RazorPages;
using PocBooking.Api.BookingApi;

namespace PocBooking.Api.Pages;

public class ConversationsModel : PageModel
{
    private readonly IBookingApiClient _bookingApi;

    public ConversationsModel(IBookingApiClient bookingApi) => _bookingApi = bookingApi;

    public string PropertyId { get; set; } = "1383087";
    public string? NextPageId { get; set; }
    public string? Error { get; set; }
    public List<ConversationRow> Conversations { get; set; } = new();

    public async Task OnGetAsync(string? propertyId, string? pageId, CancellationToken ct = default)
    {
        PropertyId = propertyId ?? "1383087";
        var response = await _bookingApi.GetConversationsAsync(PropertyId, pageId, ct);
        if (response?.Data == null)
        {
            Error = response?.Error ?? "Booking API not configured or unavailable. Set Booking:ApiBaseUrl (e.g. http://localhost:5160).";
            return;
        }

        NextPageId = response.Data.NextPageId;
        foreach (var c in response.Data.Conversations)
        {
            var lastMsg = c.Messages?.FirstOrDefault();
            Conversations.Add(new ConversationRow
            {
                ConversationId = c.ConversationId ?? "",
                ConversationReference = c.ConversationReference,
                LastMessagePreview = lastMsg?.Content?.Length > 60 ? lastMsg.Content[..60] + "…" : lastMsg?.Content,
            });
        }
    }

    public class ConversationRow
    {
        public string ConversationId { get; set; } = "";
        public string? ConversationReference { get; set; }
        public string? LastMessagePreview { get; set; }
    }
}
