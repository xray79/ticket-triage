using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging;
using Shared.Infrastructure.Outbox;
using Tickets.Application.Abstractions;
using Tickets.Application.Tickets.ApplyTriageResult;
using Triage.Contracts.Events;

namespace Tickets.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTicketsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TicketsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Tickets"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", TicketsDbContext.Schema)));

        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ITicketsUnitOfWork>(sp => (ITicketsUnitOfWork)sp.GetRequiredService<ITicketRepository>());

        services.AddScoped<IIntegrationEventHandler<TicketTriaged>, TicketTriagedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<TicketTriageFailed>, TicketTriageFailedIntegrationEventHandler>();

        services.AddHostedService<OutboxDispatcherHostedService<TicketsDbContext>>();

        var ticketsInboxQueueUrl = configuration["Sqs:Queues:TicketsInbox"];
        if (!string.IsNullOrWhiteSpace(ticketsInboxQueueUrl))
        {
            services.AddSqsConsumer(ticketsInboxQueueUrl, new[]
            {
                new IntegrationEventRoute(
                    nameof(TicketTriaged),
                    typeof(TicketTriaged),
                    (sp, evt, ct) => sp.GetRequiredService<IIntegrationEventHandler<TicketTriaged>>()
                        .HandleAsync((TicketTriaged)evt, ct)),
                new IntegrationEventRoute(
                    nameof(TicketTriageFailed),
                    typeof(TicketTriageFailed),
                    (sp, evt, ct) => sp.GetRequiredService<IIntegrationEventHandler<TicketTriageFailed>>()
                        .HandleAsync((TicketTriageFailed)evt, ct)),
            });
        }

        return services;
    }
}
