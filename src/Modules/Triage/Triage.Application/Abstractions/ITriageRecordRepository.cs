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

    /// <summary>
    /// Saves and returns <c>false</c> instead of throwing specifically when the failure was
    /// another worker's concurrent insert winning the "at most one succeeded triage record per
    /// ticket" race — see docs/concurrency/001-redelivered-ticket-created-race.md. Any other
    /// failure still throws normally; this is not a general try/catch around SaveChanges.
    /// </summary>
    Task<bool> TrySaveChangesAsync(CancellationToken ct);
}
