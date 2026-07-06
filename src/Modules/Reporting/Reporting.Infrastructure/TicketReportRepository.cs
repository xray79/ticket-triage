using Microsoft.EntityFrameworkCore;
using Reporting.Application;
using Reporting.Domain;

namespace Reporting.Infrastructure;

public sealed class TicketReportRepository : ITicketReportRepository, IReportingUnitOfWork
{
    private readonly ReportingDbContext _context;

    public TicketReportRepository(ReportingDbContext context)
    {
        _context = context;
    }

    public Task<TicketReportEntry?> GetAsync(Guid ticketId, CancellationToken ct) =>
        _context.TicketReportEntries.FirstOrDefaultAsync(x => x.TicketId == ticketId, ct);

    public void Add(TicketReportEntry entry) => _context.TicketReportEntries.Add(entry);

    public async Task<ReportingSummaryDto> GetSummaryAsync(CancellationToken ct)
    {
        var entries = await _context.TicketReportEntries.AsNoTracking().ToListAsync(ct);

        var triagedEntries = entries.Where(e => e.TriagedAtUtc is not null).ToList();
        double? avgLatency = triagedEntries.Count == 0
            ? null
            : triagedEntries.Average(e => (e.TriagedAtUtc!.Value - e.CreatedAtUtc).TotalSeconds);

        var byProvider = triagedEntries
            .Where(e => e.Provider is not null)
            .GroupBy(e => e.Provider!)
            .Select(g => new ProviderBreakdownDto(g.Key, g.Count(), g.Count(e => e.WasFallback)))
            .OrderByDescending(p => p.Count)
            .ToList();

        return new ReportingSummaryDto(
            TotalTickets: entries.Count,
            NewCount: entries.Count(e => e.Status == "New"),
            TriagedCount: entries.Count(e => e.Status == "Triaged"),
            ResolvedCount: entries.Count(e => e.Status == "Resolved"),
            TriageFailedCount: entries.Count(e => e.Status == "TriageFailed"),
            AverageTriageLatencySeconds: avgLatency,
            ByProvider: byProvider);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
