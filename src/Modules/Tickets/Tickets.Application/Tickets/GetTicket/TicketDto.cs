namespace Tickets.Application.Tickets.GetTicket;

public sealed record TicketDto(
    Guid Id,
    string Subject,
    string Body,
    string CustomerEmail,
    string Status,
    string RequestedProvider,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    Guid? AssignedToUserId,
    TriageResultDto? Triage);

public sealed record TriageResultDto(
    string Category,
    string Priority,
    string Summary,
    string DraftReply,
    string Provider,
    bool WasFallback,
    DateTimeOffset TriagedAtUtc);
