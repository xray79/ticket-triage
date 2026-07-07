namespace Shared.Infrastructure.Outbox;

/// <summary>
/// Written in the same DB transaction as the business change that raised the event.
/// A background dispatcher later publishes it to SQS and marks it processed —
/// this is what prevents "saved ticket but notification lost."
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset OccurredOnUtc { get; init; }
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
}
