using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Tickets.Contracts.Events;
using Triage.Application.Abstractions;
using Triage.Application.Caching;
using Triage.Application.Providers;
using Triage.Application.Redaction;
using Triage.Domain;

namespace Triage.Application;

/// <summary>
/// The async triage pipeline: redact -> check cache -> resolve provider (with local fallback)
/// -> rehydrate -> persist + publish. Idempotent — a redelivered TicketCreated is a safe no-op
/// if this ticket already has a triage record.
/// </summary>
public sealed class TicketCreatedIntegrationEventHandler : IIntegrationEventHandler<TicketCreated>
{
    private readonly IRedactionEngine _redactionEngine;
    private readonly ITriageOrchestrator _orchestrator;
    private readonly ITriageResultCache _cache;
    private readonly ITriageRecordRepository _repository;
    private readonly ITriageUnitOfWork _unitOfWork;
    private readonly ILogger<TicketCreatedIntegrationEventHandler> _logger;

    public TicketCreatedIntegrationEventHandler(
        IRedactionEngine redactionEngine,
        ITriageOrchestrator orchestrator,
        ITriageResultCache cache,
        ITriageRecordRepository repository,
        ITriageUnitOfWork unitOfWork,
        ILogger<TicketCreatedIntegrationEventHandler> logger)
    {
        _redactionEngine = redactionEngine;
        _orchestrator = orchestrator;
        _cache = cache;
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

            var cached = await _cache.GetAsync(redacted.Subject.MaskedText, redacted.Body.MaskedText, ct);
            TriageAttempt attempt;
            if (cached is not null)
            {
                _logger.LogInformation("Triage cache hit for ticket {TicketId}; skipping the LLM call.", integrationEvent.TicketId);
                attempt = new TriageAttempt(cached.Result, cached.Provider, cached.WasFallback);
            }
            else
            {
                attempt = await _orchestrator.TriageAsync(integrationEvent.RequestedProvider, maskedTicket, ct);
                await _cache.SetAsync(
                    redacted.Subject.MaskedText, redacted.Body.MaskedText,
                    new CachedTriageAttempt(attempt.Result, attempt.Provider, attempt.WasFallback), ct);
            }

            var rehydratedDraft = redacted.RehydrateBody(attempt.Result.DraftReply);
            var rehydratedSummary = redacted.RehydrateBody(attempt.Result.Summary);

            var record = TriageRecord.CreateSucceeded(
                integrationEvent.TicketId,
                attempt.Result.Category,
                attempt.Result.Priority,
                rehydratedSummary,
                rehydratedDraft,
                attempt.Provider,
                attempt.WasFallback,
                integrationEvent.CustomerEmail);

            _repository.Add(record);

            // The ExistsForTicketAsync check above only narrows the window — it can't close a
            // race between two workers both processing a redelivered TicketCreated concurrently
            // (see docs/concurrency/001-redelivered-ticket-created-race.md). The unique index on
            // (TicketId, Succeeded) is what actually closes it: if another worker's insert won,
            // this one fails here, and that's expected, not an error — the ticket is already
            // correctly triaged by the worker that got there first.
            if (!await _unitOfWork.TrySaveChangesAsync(ct))
            {
                _logger.LogInformation(
                    "Another worker already recorded a successful triage for ticket {TicketId} first " +
                    "(redelivery race) — discarding this attempt's result as a safe no-op.",
                    integrationEvent.TicketId);
            }
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
