using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Tickets.Contracts.Events;
using Triage.Application.Abstractions;
using Triage.Application.Providers;
using Triage.Application.Redaction;
using Triage.Domain;

namespace Triage.Application;

/// <summary>
/// The async triage pipeline: redact -> resolve provider (with local fallback) -> rehydrate ->
/// persist + publish. Idempotent — a redelivered TicketCreated is a safe no-op if this ticket
/// already has a triage record.
/// </summary>
public sealed class TicketCreatedIntegrationEventHandler : IIntegrationEventHandler<TicketCreated>
{
    private readonly IRedactionEngine _redactionEngine;
    private readonly ITriageOrchestrator _orchestrator;
    private readonly ITriageRecordRepository _repository;
    private readonly ITriageUnitOfWork _unitOfWork;
    private readonly ILogger<TicketCreatedIntegrationEventHandler> _logger;

    public TicketCreatedIntegrationEventHandler(
        IRedactionEngine redactionEngine,
        ITriageOrchestrator orchestrator,
        ITriageRecordRepository repository,
        ITriageUnitOfWork unitOfWork,
        ILogger<TicketCreatedIntegrationEventHandler> logger)
    {
        _redactionEngine = redactionEngine;
        _orchestrator = orchestrator;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(TicketCreated integrationEvent, CancellationToken ct)
    {
        if (await _repository.ExistsForTicketAsync(integrationEvent.TicketId, ct))
            return;

        try
        {
            var redacted = await _redactionEngine.RedactAsync(integrationEvent.Subject, integrationEvent.Body, ct);

            var maskedTicket = new TicketContent(
                integrationEvent.TicketId, redacted.Subject.MaskedText, redacted.Body.MaskedText, integrationEvent.CustomerEmail);

            var attempt = await _orchestrator.TriageAsync(integrationEvent.RequestedProvider, maskedTicket, ct);

            var rehydratedDraft = redacted.RehydrateBody(attempt.Result.DraftReply);
            var rehydratedSummary = redacted.RehydrateBody(attempt.Result.Summary);

            var record = TriageRecord.CreateSucceeded(
                integrationEvent.TicketId,
                attempt.Result.Category,
                attempt.Result.Priority,
                rehydratedSummary,
                rehydratedDraft,
                attempt.Provider,
                attempt.WasFallback);

            _repository.Add(record);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Triage failed for ticket {TicketId} even after local fallback.", integrationEvent.TicketId);

            var failed = TriageRecord.CreateFailed(integrationEvent.TicketId, ex.Message);
            _repository.Add(failed);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
