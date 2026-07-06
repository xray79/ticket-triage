namespace Triage.Application;

public sealed record TicketContent(Guid TicketId, string Subject, string Body, string CustomerEmail);

public sealed record TriageResult(string Category, string Priority, string Summary, string DraftReply);
