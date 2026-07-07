using Microsoft.EntityFrameworkCore;
using Tickets.Application.Abstractions;
using Tickets.Domain;

namespace Tickets.Infrastructure;

public sealed class TicketRepository : ITicketRepository, ITicketsUnitOfWork
{
    private readonly TicketsDbContext _context;

    public TicketRepository(TicketsDbContext context)
    {
        _context = context;
    }

    public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _context.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);

    public void Add(Ticket ticket) => _context.Tickets.Add(ticket);

    public async Task<IReadOnlyList<Ticket>> ListAsync(TicketStatus? status, CancellationToken ct)
    {
        var query = _context.Tickets.AsQueryable();
        if (status is not null)
            query = query.Where(t => t.Status == status);

        return await query.OrderByDescending(t => t.CreatedAtUtc).ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
