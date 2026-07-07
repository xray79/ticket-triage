namespace Triage.Application.Providers;

public interface ITriageOrchestrator
{
    Task<TriageAttempt> TriageAsync(string providerPreference, TicketContent maskedTicket, CancellationToken ct);
}
