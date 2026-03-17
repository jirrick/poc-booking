using MediatR;

namespace PocBooking.Api.Processing;

/// <summary>
/// Command to process a persisted CNS notification (e.g. enrich and persist mappings).
/// Simulates queue processing: webhook persists then sends this; handler runs in same process.
/// </summary>
public sealed record ProcessCnsNotificationCommand(int NotificationInboxId) : IRequest<Unit>;
