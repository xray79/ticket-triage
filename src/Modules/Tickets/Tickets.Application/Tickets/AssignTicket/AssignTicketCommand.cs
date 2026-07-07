using MediatR;
using Shared.Kernel;
using Tickets.Application.Abstractions;

namespace Tickets.Application.Tickets.AssignTicket;

public sealed record AssignTicketCommand(Guid TicketId, Guid AssigneeUserId) : IRequest<Result>;

public sealed class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand, Result>
{
    private readonly ITicketRepository _repository;
    private readonly ITicketsUnitOfWork _unitOfWork;

    public AssignTicketCommandHandler(ITicketRepository repository, ITicketsUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AssignTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure(Error.NotFound("Ticket.NotFound", $"Ticket {request.TicketId} was not found."));

        var result = ticket.AssignTo(request.AssigneeUserId);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
