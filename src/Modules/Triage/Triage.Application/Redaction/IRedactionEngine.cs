namespace Triage.Application.Redaction;

public interface IRedactionEngine
{
    Task<RedactedTicket> RedactAsync(string subject, string body, CancellationToken ct);
}
