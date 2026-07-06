using Shared.Kernel;

namespace Tickets.Contracts.Events;

public sealed record TicketResolved(
    Guid Id,
    DateTimeOffset OccurredOnUtc,
    Guid TicketId,
    string CustomerEmail) : DomainEvent(Id, OccurredOnUtc);
