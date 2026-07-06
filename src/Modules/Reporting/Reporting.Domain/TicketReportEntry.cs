namespace Reporting.Domain;

/// <summary>
/// A denormalized read-model row updated incrementally as ticket lifecycle events arrive —
/// Reporting's own copy of just the fields dashboards need, never a query against another
/// module's tables. One row per ticket.
/// </summary>
public sealed class TicketReportEntry
{
    public Guid TicketId { get; private set; }
    public string Status { get; private set; } = "New";
    public string? Category { get; private set; }
    public string? Priority { get; private set; }
    public string? Provider { get; private set; }
    public bool WasFallback { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? TriagedAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    private TicketReportEntry() { }

    public static TicketReportEntry Create(Guid ticketId, DateTimeOffset createdAtUtc) => new()
    {
        TicketId = ticketId,
        Status = "New",
        CreatedAtUtc = createdAtUtc
    };

    public void ApplyTriaged(string category, string priority, string provider, bool wasFallback, DateTimeOffset triagedAtUtc)
    {
        Status = "Triaged";
        Category = category;
        Priority = priority;
        Provider = provider;
        WasFallback = wasFallback;
        TriagedAtUtc = triagedAtUtc;
    }

    public void ApplyTriageFailed() => Status = "TriageFailed";

    public void ApplyResolved(DateTimeOffset resolvedAtUtc)
    {
        Status = "Resolved";
        ResolvedAtUtc = resolvedAtUtc;
    }
}
