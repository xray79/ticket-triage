using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Tickets.Contracts.Events;
using Triage.Application.Providers;
using Triage.Application.Redaction;

namespace Triage.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTriageApplication(this IServiceCollection services)
    {
        services.AddScoped<IRedactionEngine, RedactionEngine>();
        services.AddScoped<ITriageOrchestrator, FallbackTriageClient>();
        services.AddScoped<IIntegrationEventHandler<TicketCreated>, TicketCreatedIntegrationEventHandler>();
        return services;
    }
}
