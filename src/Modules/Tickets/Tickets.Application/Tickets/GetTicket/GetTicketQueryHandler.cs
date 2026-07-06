using MediatR;
using Shared.Kernel;
using Tickets.Application.Abstractions;

namespace Tickets.Application.Tickets.GetTicket;

public sealed class GetTicketQueryHandler : IRequestHandler<GetTicketQuery, Result<TicketDto>>
{
    private readonly ITicketRepository _repository;

    public GetTicketQueryHandler(ITicketRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TicketDto>> Handle(GetTicketQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure<TicketDto>(Error.NotFound("Ticket.NotFound", $"Ticket {request.TicketId} was not found."));

        var triage = ticket.Triage is null
            ? null
            : new TriageResultDto(
                ticket.Triage.Category,
                ticket.Triage.Priority,
                ticket.Triage.Summary,
                ticket.Triage.DraftReply,
                ticket.Triage.Provider,
                ticket.Triage.WasFallback,
                ticket.Triage.TriagedAtUtc);

        return new TicketDto(
            ticket.Id,
            ticket.Subject,
            ticket.Body,
            ticket.CustomerEmail,
            ticket.Status.ToString(),
            ticket.RequestedProvider,
            ticket.CreatedByUserId,
            ticket.CreatedAtUtc,
            ticket.AssignedToUserId,
            triage);
    }
}
