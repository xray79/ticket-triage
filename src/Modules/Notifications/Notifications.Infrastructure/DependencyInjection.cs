using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging;
using Tickets.Contracts.Events;
using Triage.Contracts.Events;

namespace Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Notifications"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", NotificationsDbContext.Schema)));

        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<INotificationsUnitOfWork>(sp => (INotificationsUnitOfWork)sp.GetRequiredService<INotificationLogRepository>());

        var smtpOptions = new SmtpOptions();
        configuration.GetSection(SmtpOptions.SectionName).Bind(smtpOptions);

        if (string.IsNullOrWhiteSpace(smtpOptions.Host))
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        else
            services.AddSingleton<IEmailSender>(new SmtpEmailSender(smtpOptions));

        var notificationsInboxQueueUrl = configuration["Sqs:Queues:NotificationsInbox"];
        if (!string.IsNullOrWhiteSpace(notificationsInboxQueueUrl))
        {
            services.AddSqsConsumer(notificationsInboxQueueUrl, new[]
            {
                new IntegrationEventRoute(
                    nameof(TicketTriaged),
                    typeof(TicketTriaged),
                    (sp, evt, ct) => sp.GetRequiredService<IIntegrationEventHandler<TicketTriaged>>()
                        .HandleAsync((TicketTriaged)evt, ct)),
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
