using MediatR;
using Shared.Kernel;
using Tickets.Application.Abstractions;

namespace Tickets.Application.Tickets.ResolveTicket;

public sealed record ResolveTicketCommand(Guid TicketId) : IRequest<Result>;

public sealed class ResolveTicketCommandHandler : IRequestHandler<ResolveTicketCommand, Result>
{
    private readonly ITicketRepository _repository;
    private readonly ITicketsUnitOfWork _unitOfWork;

    public ResolveTicketCommandHandler(ITicketRepository repository, ITicketsUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ResolveTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure(Error.NotFound("Ticket.NotFound", $"Ticket {request.TicketId} was not found."));

        var result = ticket.Resolve();
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
