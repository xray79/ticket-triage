using MediatR;
using Tickets.Domain;

namespace Tickets.Application.Tickets.ListTickets;

public sealed record TicketSummaryDto(
    Guid Id,
    string Subject,
    string CustomerEmail,
    string Status,
    string? Priority,
    Guid? AssignedToUserId,
    DateTimeOffset CreatedAtUtc);

public sealed record ListTicketsQuery(TicketStatus? Status) : IRequest<IReadOnlyList<TicketSummaryDto>>;
