using Shared.Kernel;

namespace Tickets.Contracts.Events;

/// <summary>
/// Published when a new ticket is saved. Carries the raw (unredacted) content because
/// the Triage module has no direct access to the Tickets database — this event payload
/// is the only channel between the two modules.
/// </summary>
public sealed record TicketCreated(
    Guid Id,
    DateTimeOffset OccurredOnUtc,
    Guid TicketId,
    string Subject,
    string Body,
    string CustomerEmail,
    string RequestedProvider) : DomainEvent(Id, OccurredOnUtc);
