using Triage.Domain;

namespace Triage.Application.Abstractions;

public interface ITriageRecordRepository
{
    void Add(TriageRecord record);
    Task<bool> ExistsForTicketAsync(Guid ticketId, CancellationToken ct);
}

public interface ITriageUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}
