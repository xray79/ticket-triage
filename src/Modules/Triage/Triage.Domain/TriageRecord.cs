using Shared.Kernel;
using Triage.Contracts.Events;

namespace Triage.Domain;

/// <summary>An immutable record of one triage attempt, kept for audit/history in the Triage schema.</summary>
public sealed class TriageRecord : AggregateRoot<Guid>
{
    private TriageRecord() { }

    public Guid TicketId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Priority { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string DraftReply { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public bool WasFallback { get; private set; }
    public bool Succeeded { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static TriageRecord CreateSucceeded(
        Guid ticketId, string category, string priority, string summary, string draftReply,
        string provider, bool wasFallback)
    {
        var record = new TriageRecord
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Category = category,
            Priority = priority,
            Summary = summary,
            DraftReply = draftReply,
            Provider = provider,
            WasFallback = wasFallback,
            Succeeded = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        record.Raise(new TicketTriaged(
            Guid.NewGuid(), DateTimeOffset.UtcNow, ticketId, category, priority, summary, draftReply, provider, wasFallback));

        return record;
    }

    public static TriageRecord CreateFailed(Guid ticketId, string reason)
    {
        var record = new TriageRecord
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Succeeded = false,
            FailureReason = reason,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        record.Raise(new TicketTriageFailed(Guid.NewGuid(), DateTimeOffset.UtcNow, ticketId, reason));

        return record;
    }
}
