using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reporting.Application;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging;
using Tickets.Contracts.Events;
using Triage.Contracts.Events;

namespace Reporting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ReportingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Reporting"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ReportingDbContext.Schema)));

        services.AddScoped<ITicketReportRepository, TicketReportRepository>();
        services.AddScoped<IReportingUnitOfWork>(sp => (IReportingUnitOfWork)sp.GetRequiredService<ITicketReportRepository>());

        var reportingInboxQueueUrl = configuration["Sqs:Queues:ReportingInbox"];
        if (!string.IsNullOrWhiteSpace(reportingInboxQueueUrl))
        {
            services.AddSqsConsumer(reportingInboxQueueUrl, new[]
            {
                new IntegrationEventRoute(
                    nameof(TicketCreated),
                    typeof(TicketCreated),
                    (sp, evt, ct) => sp.GetRequiredService<IIntegrationEventHandler<TicketCreated>>()
                        .HandleAsync((TicketCreated)evt, ct)),
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
                new IntegrationEventRoute(
                    nameof(TicketResolved),
                    typeof(TicketResolved),
                    (sp, evt, ct) => sp.GetRequiredService<IIntegrationEventHandler<TicketResolved>>()
                        .HandleAsync((TicketResolved)evt, ct)),
            });
        }

        return services;
    }
}
