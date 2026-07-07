using FluentAssertions;
using Reporting.Domain;
using Xunit;

namespace Reporting.Tests;

public sealed class TicketReportEntryTests
{
    [Fact]
    public void Create_starts_in_New_status()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var entry = TicketReportEntry.Create(Guid.NewGuid(), createdAt);

        entry.Status.Should().Be("New");
        entry.CreatedAtUtc.Should().Be(createdAt);
        entry.TriagedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ApplyTriaged_transitions_to_Triaged_and_records_provider_details()
    {
        var entry = TicketReportEntry.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var triagedAt = DateTimeOffset.UtcNow.AddMinutes(1);

        entry.ApplyTriaged("billing", "high", "openai", wasFallback: true, triagedAt);

        entry.Status.Should().Be("Triaged");
        entry.Category.Should().Be("billing");
        entry.Provider.Should().Be("openai");
        entry.WasFallback.Should().BeTrue();
        entry.TriagedAtUtc.Should().Be(triagedAt);
    }

    [Fact]
    public void ApplyTriageFailed_transitions_to_TriageFailed()
    {
        var entry = TicketReportEntry.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entry.ApplyTriageFailed();

        entry.Status.Should().Be("TriageFailed");
    }

    [Fact]
    public void ApplyResolved_transitions_to_Resolved_and_records_the_timestamp()
    {
        var entry = TicketReportEntry.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var resolvedAt = DateTimeOffset.UtcNow.AddHours(1);

        entry.ApplyResolved(resolvedAt);

        entry.Status.Should().Be("Resolved");
        entry.ResolvedAtUtc.Should().Be(resolvedAt);
    }
}
