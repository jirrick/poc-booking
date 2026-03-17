using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PocBooking.Api.BookingApi;
using PocBooking.Api.Data;

namespace PocBooking.Api.Pages;

public class ConversationModel : PageModel
{
    private readonly IBookingApiClient _bookingApi;
    private readonly AppDbContext _db;

    public ConversationModel(IBookingApiClient bookingApi, AppDbContext db)
    {
        _bookingApi = bookingApi;
        _db = db;
    }

    public string PropertyId { get; set; } = "";
    public string ConversationId { get; set; } = "";
    public string? ConversationReference { get; set; }
    public string? Error { get; set; }
    public List<MessageVm> Messages { get; set; } = new();
    public LocalMappingVm? LocalMapping { get; set; }

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
                SenderParticipantId = m.Sender?.ParticipantId ?? "",
            });
        }

        await LoadLocalMappingAsync(response.Data, ct);

        return Page();
    }

    private async Task LoadLocalMappingAsync(ConversationDetailResponse detail, CancellationToken ct)
    {
        // Match reservation by conversation_reference
        var convRef = detail.ConversationReference?.Trim();
        if (string.IsNullOrEmpty(convRef)) return;

        var reservation = await _db.ReservationMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BookingReservationId == convRef, ct);

        if (reservation == null) return;

        // Find guest participant: prefer participants list, fall back to message senders
        var guestParticipantId = detail.Participants
            .FirstOrDefault(p => p.Metadata?.ParticipantType == "guest")?.ParticipantId
            ?? Messages.FirstOrDefault(m => m.SenderType == "guest")?.SenderParticipantId;

        GuestMapping? guest = null;
        if (!string.IsNullOrEmpty(guestParticipantId))
            guest = await _db.GuestMappings
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.BookingGuestId == guestParticipantId, ct);

        LocalMapping = new LocalMappingVm
        {
            ConfirmationNumber = reservation.ConfirmationNumber,
            InternalReservationId = reservation.InternalReservationId,
            BookingReservationId = reservation.BookingReservationId,
            GuestName = guest?.GuestName,
            InternalGuestId = guest?.InternalGuestId,
            BookingGuestId = guest?.BookingGuestId,
        };
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
        public string SenderParticipantId { get; set; } = "";
    }

    public class LocalMappingVm
    {
        public string ConfirmationNumber { get; set; } = "";
        public Guid InternalReservationId { get; set; }
        public string BookingReservationId { get; set; } = "";
        public string? GuestName { get; set; }
        public Guid? InternalGuestId { get; set; }
        public string? BookingGuestId { get; set; }
    }
}
