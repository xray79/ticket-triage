using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Abstractions.Messaging;

namespace Shared.Infrastructure.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddSqsMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new SqsOptions();
        configuration.GetSection(SqsOptions.SectionName).Bind(options);
        services.Configure<SqsOptions>(configuration.GetSection(SqsOptions.SectionName));

        services.AddSingleton<IAmazonSQS>(_ =>
        {
            var config = new AmazonSQSConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region) };
            if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
                config.ServiceURL = options.ServiceUrl;

            // LocalStack accepts any non-empty static credentials; real AWS uses the default credential chain.
            return string.IsNullOrWhiteSpace(options.ServiceUrl)
                ? new AmazonSQSClient(config)
                : new AmazonSQSClient(new Amazon.Runtime.BasicAWSCredentials("local", "local"), config);
        });

        services.AddSingleton<IEventPublisher, SqsEventPublisher>();
        return services;
    }

    /// <summary>Registers a long-poll consumer for one queue. Call once per queue a module listens on.</summary>
    public static IServiceCollection AddSqsConsumer(
        this IServiceCollection services,
        string queueUrl,
        IEnumerable<IntegrationEventRoute> routes)
    {
        services.AddSingleton<IHostedService>(sp => new SqsIntegrationEventConsumer(
            sp.GetRequiredService<IAmazonSQS>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SqsIntegrationEventConsumer>>(),
            queueUrl,
            routes));
        return services;
    }
}
