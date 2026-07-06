using MediatR;
using Shared.Kernel;

namespace Tickets.Application.Tickets.CreateTicket;

public sealed record CreateTicketCommand(
    string Subject,
    string Body,
    string CustomerEmail,
    string RequestedProvider,
    Guid CreatedByUserId) : IRequest<Result<Guid>>;
