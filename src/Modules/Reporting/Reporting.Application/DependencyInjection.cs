using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Tickets.Contracts.Events;
using Triage.Contracts.Events;

namespace Reporting.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddScoped<IIntegrationEventHandler<TicketCreated>, TicketCreatedReportHandler>();
        services.AddScoped<IIntegrationEventHandler<TicketTriaged>, TicketTriagedReportHandler>();
        services.AddScoped<IIntegrationEventHandler<TicketTriageFailed>, TicketTriageFailedReportHandler>();
        services.AddScoped<IIntegrationEventHandler<TicketResolved>, TicketResolvedReportHandler>();

        return services;
    }
}
