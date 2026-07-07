using MediatR;
using Tickets.Application.Abstractions;

namespace Tickets.Application.Tickets.ListTickets;

public sealed class ListTicketsQueryHandler : IRequestHandler<ListTicketsQuery, IReadOnlyList<TicketSummaryDto>>
{
    private readonly ITicketRepository _repository;

    public ListTicketsQueryHandler(ITicketRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<TicketSummaryDto>> Handle(ListTicketsQuery request, CancellationToken cancellationToken)
    {
        var tickets = await _repository.ListAsync(request.Status, cancellationToken);

        return tickets
            .Select(t => new TicketSummaryDto(
                t.Id,
                t.Subject,
                t.CustomerEmail,
                t.Status.ToString(),
                t.Triage?.Priority,
                t.AssignedToUserId,
                t.CreatedAtUtc))
            .ToList();
    }
}
