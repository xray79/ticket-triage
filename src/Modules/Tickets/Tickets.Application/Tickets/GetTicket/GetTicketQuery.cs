using MediatR;
using Shared.Kernel;

namespace Tickets.Application.Tickets.GetTicket;

public sealed record GetTicketQuery(Guid TicketId) : IRequest<Result<TicketDto>>;
