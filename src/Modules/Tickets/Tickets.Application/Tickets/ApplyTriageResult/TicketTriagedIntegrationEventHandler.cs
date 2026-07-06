using Shared.Abstractions.Messaging;
using Tickets.Application.Abstractions;
using Tickets.Domain;
using Triage.Contracts.Events;

namespace Tickets.Application.Tickets.ApplyTriageResult;

public sealed class TicketTriagedIntegrationEventHandler : IIntegrationEventHandler<TicketTriaged>
{
    private readonly ITicketRepository _repository;
    private readonly ITicketsUnitOfWork _unitOfWork;

    public TicketTriagedIntegrationEventHandler(ITicketRepository repository, ITicketsUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(TicketTriaged integrationEvent, CancellationToken ct)
    {
        var ticket = await _repository.GetByIdAsync(integrationEvent.TicketId, ct);
        if (ticket is null)
            return;

        // Idempotent: a redelivered SQS message re-applying the same outcome is a safe no-op.
        if (ticket.Status == TicketStatus.Triaged &&
            ticket.Triage?.Provider == integrationEvent.Provider &&
            ticket.Triage?.TriagedAtUtc == integrationEvent.OccurredOnUtc)
        {
            return;
        }

        ticket.ApplyTriageResult(new TriageOutcome(
            integrationEvent.Category,
            integrationEvent.Priority,
            integrationEvent.Summary,
            integrationEvent.DraftReply,
            integrationEvent.Provider,
            integrationEvent.WasFallback,
            integrationEvent.OccurredOnUtc));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}

public sealed class TicketTriageFailedIntegrationEventHandler : IIntegrationEventHandler<TicketTriageFailed>
{
    private readonly ITicketRepository _repository;
    private readonly ITicketsUnitOfWork _unitOfWork;

    public TicketTriageFailedIntegrationEventHandler(ITicketRepository repository, ITicketsUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(TicketTriageFailed integrationEvent, CancellationToken ct)
    {
        var ticket = await _repository.GetByIdAsync(integrationEvent.TicketId, ct);
        if (ticket is null || ticket.Status == TicketStatus.TriageFailed)
            return;

        ticket.MarkTriageFailed();
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
