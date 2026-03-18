using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PocBooking.Api.Data;
using PocBooking.Api.Enrichment;
using PocBooking.Api.Models;

namespace PocBooking.Api.Processing;

public sealed class ProcessCnsNotificationHandler : IRequestHandler<ProcessCnsNotificationCommand, Unit>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly AppDbContext _db;
    private readonly IEnrichCnsMessage _enricher;

    public ProcessCnsNotificationHandler(AppDbContext db, IEnrichCnsMessage enricher)
    {
        _db = db;
        _enricher = enricher;
    }

    public async Task<Unit> Handle(ProcessCnsNotificationCommand request, CancellationToken cancellationToken)
    {
        var inbox = await _db.NotificationInbox
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.NotificationInboxId, cancellationToken);
        if (inbox == null) return Unit.Value;

        if (inbox.Type != "MESSAGING_API_NEW_MESSAGE" || string.IsNullOrEmpty(inbox.PayloadJson))
            return Unit.Value;

        MessagingApiNewMessagePayload? payload;
        try
        {
            var notification = JsonSerializer.Deserialize<BookingCnsNotification>(inbox.PayloadJson!, JsonOptions);
            var payloadElement = notification?.Payload;
            if (!payloadElement.HasValue || payloadElement.Value.ValueKind != JsonValueKind.Object)
                return Unit.Value;
            payload = payloadElement.Value.Deserialize<MessagingApiNewMessagePayload>(JsonOptions);
        }
        catch
        {
            return Unit.Value;
        }

        if (payload == null) return Unit.Value;

        var enriched = await _enricher.EnrichAsync(payload, cancellationToken);
        if (enriched == null) return Unit.Value;

        var alreadyProcessed = await _db.ProcessedMessages
            .AnyAsync(p => p.NotificationInboxId == request.NotificationInboxId, cancellationToken);
        if (alreadyProcessed) return Unit.Value;

        _db.ProcessedMessages.Add(new ProcessedMessage
        {
            NotificationInboxId = request.NotificationInboxId,
            InternalReservationId = enriched.InternalReservationId,
            InternalGuestId = enriched.InternalGuestId,
            InternalEnterpriseId = enriched.InternalEnterpriseId,
            ProcessedAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
