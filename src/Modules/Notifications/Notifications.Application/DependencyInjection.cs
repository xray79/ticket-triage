using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Tickets.Contracts.Events;
using Triage.Contracts.Events;

namespace Notifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationEventHandler<TicketTriaged>, TicketTriagedNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<TicketResolved>, TicketResolvedNotificationHandler>();
        return services;
    }
}
