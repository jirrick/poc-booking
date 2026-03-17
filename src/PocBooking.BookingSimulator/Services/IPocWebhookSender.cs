using PocBooking.BookingSimulator.Models;

namespace PocBooking.BookingSimulator.Services;

public interface IPocWebhookSender
{
    Task SendNewMessageNotificationAsync(Data.Message message, CancellationToken cancellationToken = default);
    Task SendPayloadAsync(CnsWebhookPayload payload, CancellationToken cancellationToken = default);
}
