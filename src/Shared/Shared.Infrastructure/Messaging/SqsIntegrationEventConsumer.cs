using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure.Messaging;

/// <summary>
/// Long-polls one SQS queue and dispatches each message to the matching registered handler.
/// A redelivered message (SQS's at-least-once guarantee) is expected — handlers are responsible
/// for idempotency; this class only guarantees at-least-once delivery, not exactly-once.
/// </summary>
public sealed class SqsIntegrationEventConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SqsIntegrationEventConsumer> _logger;
    private readonly string _queueUrl;
    private readonly IReadOnlyDictionary<string, IntegrationEventRoute> _routes;

    public SqsIntegrationEventConsumer(
        IAmazonSQS sqs,
        IServiceScopeFactory scopeFactory,
        ILogger<SqsIntegrationEventConsumer> logger,
        string queueUrl,
        IEnumerable<IntegrationEventRoute> routes)
    {
        _sqs = sqs;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _queueUrl = queueUrl;
        _routes = routes.ToDictionary(r => r.EventTypeName);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ReceiveMessageResponse response;
            try
            {
                response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 10,
                    MessageAttributeNames = new List<string> { "EventType" }
                }, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to poll queue {QueueUrl}.", _queueUrl);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            foreach (var message in response.Messages)
                await HandleMessageAsync(message, stoppingToken);
        }
    }

    private async Task HandleMessageAsync(Message message, CancellationToken ct)
    {
        if (!message.MessageAttributes.TryGetValue("EventType", out var attr) ||
            !_routes.TryGetValue(attr.StringValue, out var route))
        {
            _logger.LogWarning("No route for message {MessageId} on {QueueUrl}; deleting.", message.MessageId, _queueUrl);
            await DeleteAsync(message, ct);
            return;
        }

        try
        {
            var payload = JsonSerializer.Deserialize(message.Body, route.ClrType)!;
            using var scope = _scopeFactory.CreateScope();
            await route.Dispatch(scope.ServiceProvider, payload, ct);
            await DeleteAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handler failed for message {MessageId} ({EventType}); leaving for redelivery/DLQ.",
                message.MessageId, attr.StringValue);
        }
    }

    private Task DeleteAsync(Message message, CancellationToken ct) =>
        _sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, ct);
}
