using Shared.Kernel;

namespace Triage.Contracts.Events;

/// <summary>Published once the Triage module has classified and drafted a reply for a ticket.</summary>
public sealed record TicketTriaged(
    Guid Id,
    DateTimeOffset OccurredOnUtc,
    Guid TicketId,
    string Category,
    string Priority,
    string Summary,
    string DraftReply,
    string Provider,
    bool WasFallback) : DomainEvent(Id, OccurredOnUtc);

/// <summary>Published when triage could not be completed even after the local fallback.</summary>
public sealed record TicketTriageFailed(
    Guid Id,
    DateTimeOffset OccurredOnUtc,
    Guid TicketId,
    string Reason) : DomainEvent(Id, OccurredOnUtc);
