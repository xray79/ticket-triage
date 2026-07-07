using Reporting.Domain;

namespace Reporting.Application;

public interface ITicketReportRepository
{
    Task<TicketReportEntry?> GetAsync(Guid ticketId, CancellationToken ct);
    void Add(TicketReportEntry entry);
    Task<ReportingSummaryDto> GetSummaryAsync(CancellationToken ct);
}

public interface IReportingUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}

public sealed record ProviderBreakdownDto(string Provider, int Count, int FallbackCount);

public sealed record ReportingSummaryDto(
    int TotalTickets,
    int NewCount,
    int TriagedCount,
    int ResolvedCount,
    int TriageFailedCount,
    double? AverageTriageLatencySeconds,
    IReadOnlyList<ProviderBreakdownDto> ByProvider);
