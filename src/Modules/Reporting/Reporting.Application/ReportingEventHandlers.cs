using Shared.Abstractions.Messaging;
using Tickets.Contracts.Events;
using Triage.Contracts.Events;
using Reporting.Domain;

namespace Reporting.Application;

public sealed class TicketCreatedReportHandler : IIntegrationEventHandler<TicketCreated>
{
    private readonly ITicketReportRepository _repository;
    private readonly IReportingUnitOfWork _unitOfWork;

    public TicketCreatedReportHandler(ITicketReportRepository repository, IReportingUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(TicketCreated integrationEvent, CancellationToken ct)
    {
        if (await _repository.GetAsync(integrationEvent.TicketId, ct) is not null)
            return;

        _repository.Add(TicketReportEntry.Create(integrationEvent.TicketId, integrationEvent.OccurredOnUtc));
        await _unitOfWork.SaveChangesAsync(ct);
    }
}

public sealed class TicketTriagedReportHandler : IIntegrationEventHandler<TicketTriaged>
{
    private readonly ITicketReportRepository _repository;
    private readonly IReportingUnitOfWork _unitOfWork;

    public TicketTriagedReportHandler(ITicketReportRepository repository, IReportingUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(TicketTriaged integrationEvent, CancellationToken ct)
    {
        var entry = await _repository.GetAsync(integrationEvent.TicketId, ct);
        if (entry is null || entry.Status != "New")
            return; // no report row yet, or already applied (idempotent against redelivery)

        entry.ApplyTriaged(integrationEvent.Category, integrationEvent.Priority, integrationEvent.Provider, integrationEvent.WasFallback, integrationEvent.OccurredOnUtc);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}

public sealed class TicketTriageFailedReportHandler : IIntegrationEventHandler<TicketTriageFailed>
{
    private readonly ITicketReportRepository _repository;
    private readonly IReportingUnitOfWork _unitOfWork;

    public TicketTriageFailedReportHandler(ITicketReportRepository repository, IReportingUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(TicketTriageFailed integrationEvent, CancellationToken ct)
    {
        var entry = await _repository.GetAsync(integrationEvent.TicketId, ct);
        if (entry is null || entry.Status != "New")
            return;

        entry.ApplyTriageFailed();
        await _unitOfWork.SaveChangesAsync(ct);
    }
}

public sealed class TicketResolvedReportHandler : IIntegrationEventHandler<TicketResolved>
{
    private readonly ITicketReportRepository _repository;
    private readonly IReportingUnitOfWork _unitOfWork;

    public TicketResolvedReportHandler(ITicketReportRepository repository, IReportingUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(TicketResolved integrationEvent, CancellationToken ct)
    {
        var entry = await _repository.GetAsync(integrationEvent.TicketId, ct);
        if (entry is null || entry.Status == "Resolved")
            return;

        entry.ApplyResolved(integrationEvent.OccurredOnUtc);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
