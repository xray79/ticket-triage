using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reporting.Domain;
using Reporting.Infrastructure;
using Xunit;

namespace Reporting.Tests;

public sealed class TicketReportRepositoryTests
{
    private static ReportingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReportingDbContext(options);
    }

    [Fact]
    public async Task GetSummaryAsync_counts_tickets_by_status_and_computes_provider_breakdown()
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;

        var newTicket = TicketReportEntry.Create(Guid.NewGuid(), now);

        var triagedLocal = TicketReportEntry.Create(Guid.NewGuid(), now);
        triagedLocal.ApplyTriaged("billing", "high", "local", wasFallback: false, now.AddSeconds(10));

        var triagedFallback = TicketReportEntry.Create(Guid.NewGuid(), now);
        triagedFallback.ApplyTriaged("technical", "low", "local", wasFallback: true, now.AddSeconds(30));

        var resolved = TicketReportEntry.Create(Guid.NewGuid(), now);
        resolved.ApplyTriaged("billing", "medium", "openai", wasFallback: false, now.AddSeconds(20));
        resolved.ApplyResolved(now.AddMinutes(5));

        var failed = TicketReportEntry.Create(Guid.NewGuid(), now);
        failed.ApplyTriageFailed();

        context.TicketReportEntries.AddRange(newTicket, triagedLocal, triagedFallback, resolved, failed);
        await context.SaveChangesAsync();

        var repository = new TicketReportRepository(context);
        var summary = await repository.GetSummaryAsync(CancellationToken.None);

        summary.TotalTickets.Should().Be(5);
        summary.NewCount.Should().Be(1);
        summary.TriagedCount.Should().Be(2);
        summary.ResolvedCount.Should().Be(1);
        summary.TriageFailedCount.Should().Be(1);
        summary.AverageTriageLatencySeconds.Should().NotBeNull();

        var localBreakdown = summary.ByProvider.Single(p => p.Provider == "local");
        localBreakdown.Count.Should().Be(2);
        localBreakdown.FallbackCount.Should().Be(1);

        var openAiBreakdown = summary.ByProvider.Single(p => p.Provider == "openai");
        openAiBreakdown.Count.Should().Be(1);
        openAiBreakdown.FallbackCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSummaryAsync_returns_null_average_latency_when_nothing_has_been_triaged_yet()
    {
        await using var context = CreateContext();
        context.TicketReportEntries.Add(TicketReportEntry.Create(Guid.NewGuid(), DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();

        var repository = new TicketReportRepository(context);
        var summary = await repository.GetSummaryAsync(CancellationToken.None);

        summary.AverageTriageLatencySeconds.Should().BeNull();
        summary.ByProvider.Should().BeEmpty();
    }
}
