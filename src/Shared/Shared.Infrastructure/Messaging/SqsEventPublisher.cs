using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Abstractions.Messaging;
using Shared.Kernel;

namespace Shared.Infrastructure.Messaging;

public sealed class SqsEventPublisher : IEventPublisher
{
    private readonly IAmazonSQS _sqs;
    private readonly SqsOptions _options;
    private readonly ILogger<SqsEventPublisher> _logger;

    public SqsEventPublisher(IAmazonSQS sqs, IOptions<SqsOptions> options, ILogger<SqsEventPublisher> logger)
    {
        _sqs = sqs;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(DomainEvent domainEvent, CancellationToken ct = default)
    {
        var eventTypeName = domainEvent.GetType().Name;

        if (!_options.Routes.TryGetValue(eventTypeName, out var queueUrls) || queueUrls.Count == 0)
        {
            _logger.LogDebug("No subscribers configured for event {EventType}; skipping publish.", eventTypeName);
            return;
        }

        var body = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

        foreach (var queueUrl in queueUrls)
        {
            await _sqs.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = body,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["EventType"] = new() { DataType = "String", StringValue = eventTypeName }
                }
            }, ct);
        }
    }
}
