using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Messaging;
using Shared.Kernel;
using System.Text.Json;

namespace Shared.Infrastructure.Outbox;

/// <summary>
/// Polls one module's outbox table and publishes pending rows via <see cref="IEventPublisher"/>.
/// Registered once per module DbContext (Host wires one instance per module).
/// </summary>
public sealed class OutboxDispatcherHostedService<TDbContext> : BackgroundService
    where TDbContext : DbContext, IHasOutbox
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcherHostedService<TDbContext>> _logger;

    public OutboxDispatcherHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxDispatcherHostedService<TDbContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Outbox dispatch loop for {DbContext} failed.", typeof(TDbContext).Name);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>Exposed so a test (or an operator) can force one dispatch pass without waiting for the timer.</summary>
    public async Task DispatchPendingAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var pending = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var message in pending)
        {
            // Resolving the CLR type and deserializing are deterministic: if a message's own
            // Type/Content is unresolvable or malformed, retrying will never succeed no matter how
            // many polls go by — unlike a publish failure below, which can be purely transient (the
            // broker is down right now, not that this message is bad). Treating both the same way
            // meant a single corrupted row sat at the head of the batch forever, retried every poll,
            // and could starve newer messages out of the fixed-size batch behind it.
            DomainEvent domainEvent;
            try
            {
                var clrType = Type.GetType(message.Type)
                    ?? throw new InvalidOperationException($"Cannot resolve outbox event type '{message.Type}'.");
                domainEvent = (DomainEvent)JsonSerializer.Deserialize(message.Content, clrType)!;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Outbox message {MessageId} has an unresolvable type or malformed content and will never " +
                    "publish successfully — marking it processed without publishing so it stops retrying and " +
                    "blocking the batch.",
                    message.Id);
                message.Error = $"Abandoned (unrecoverable): {ex.Message}";
                message.ProcessedOnUtc = DateTimeOffset.UtcNow;
                continue;
            }

            try
            {
                await publisher.PublishAsync(domainEvent, ct);
                message.ProcessedOnUtc = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}.", message.Id);
                message.Error = ex.Message;
            }
        }

        if (pending.Count > 0)
            await dbContext.SaveChangesAsync(ct);
    }
}
