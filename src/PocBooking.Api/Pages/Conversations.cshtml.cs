using Microsoft.AspNetCore.Mvc.RazorPages;
using PocBooking.Api.BookingApi;
using PocBooking.Api.Mapping;

namespace PocBooking.Api.Pages;

public class ConversationsModel(IBookingApiClient bookingApi, IConversationMappingService mappingService) : PageModel
{
    public string PropertyId { get; set; } = "1383087";
    public string? NextPageId { get; set; }
    public string? Error { get; set; }
    public List<ConversationRow> Conversations { get; set; } = new();

    public async Task OnGetAsync(string? propertyId, string? pageId, CancellationToken ct = default)
    {
        PropertyId = propertyId ?? "1383087";
        var response = await bookingApi.GetConversationsAsync(PropertyId, pageId, ct);
        if (response?.Data == null)
        {
            Error = response?.Error ?? "Booking API not configured or unavailable. Set Booking:ApiBaseUrl (e.g. http://localhost:5160).";
            return;
        }

        NextPageId = response.Data.NextPageId;

        var convRefs = response.Data.Conversations
            .Select(c => c.ConversationReference)
            .Where(r => !string.IsNullOrEmpty(r))
            .Select(r => r!)
            .Distinct();

        var mappings = await mappingService.GetMappingsByRefAsync(convRefs, ct);

        foreach (var c in response.Data.Conversations)
        {
            var lastMsg = c.Messages?.FirstOrDefault();
            mappings.TryGetValue(c.ConversationReference ?? "", out var mapping);

            Conversations.Add(new ConversationRow
            {
                ConversationId = c.ConversationId ?? "",
                ConversationReference = c.ConversationReference,
                ConfirmationNumber = mapping?.ConfirmationNumber,
                InternalReservationId = mapping?.InternalReservationId,
                GuestName = mapping?.GuestName,
                InternalGuestId = mapping?.InternalGuestId,
                LastMessagePreview = lastMsg?.Content?.Length > 60 ? lastMsg.Content[..60] + "…" : lastMsg?.Content,
            });
        }
    }

    public sealed class ConversationRow
    {
        public string ConversationId { get; set; } = "";
        public string? ConversationReference { get; set; }
        public string? ConfirmationNumber { get; set; }
        public Guid? InternalReservationId { get; set; }
        public string? GuestName { get; set; }
        public Guid? InternalGuestId { get; set; }
        public string? LastMessagePreview { get; set; }
    }
}
