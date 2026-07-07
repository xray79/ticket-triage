using Tickets.Domain;

namespace Tickets.Application.Abstractions;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct);
    void Add(Ticket ticket);
    Task<IReadOnlyList<Ticket>> ListAsync(TicketStatus? status, CancellationToken ct);
}

public interface ITicketsUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}
