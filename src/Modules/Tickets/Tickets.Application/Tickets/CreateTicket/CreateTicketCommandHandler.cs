using MediatR;
using Shared.Kernel;
using Tickets.Application.Abstractions;
using Tickets.Domain;

namespace Tickets.Application.Tickets.CreateTicket;

public sealed class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Result<Guid>>
{
    private readonly ITicketRepository _repository;
    private readonly ITicketsUnitOfWork _unitOfWork;

    public CreateTicketCommandHandler(ITicketRepository repository, ITicketsUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var result = Ticket.Create(
            request.Subject,
            request.Body,
            request.CustomerEmail,
            request.RequestedProvider,
            request.CreatedByUserId);

        if (result.IsFailure)
            return Result.Failure<Guid>(result.Error);

        _repository.Add(result.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }
}
