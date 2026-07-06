using Microsoft.EntityFrameworkCore;
using Npgsql;
using Triage.Application.Abstractions;
using Triage.Domain;

namespace Triage.Infrastructure;

public sealed class TriageRecordRepository : ITriageRecordRepository, ITriageUnitOfWork
{
    private readonly TriageDbContext _context;

    public TriageRecordRepository(TriageDbContext context)
    {
        _context = context;
    }

    public void Add(TriageRecord record) => _context.TriageRecords.Add(record);

    public Task<bool> ExistsForTicketAsync(Guid ticketId, CancellationToken ct) =>
        _context.TriageRecords.AnyAsync(r => r.TicketId == ticketId && r.Succeeded, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);

    public async Task<bool> TrySaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueTicketIdViolation(ex))
        {
            return false;
        }
    }

    private static bool IsUniqueTicketIdViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
