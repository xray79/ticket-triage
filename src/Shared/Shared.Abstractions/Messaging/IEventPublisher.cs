using Shared.Kernel;

namespace Shared.Abstractions.Messaging;

/// <summary>
/// Publishes integration events onto the async transport (SQS in production).
/// Implemented once in Shared.Infrastructure; the outbox dispatcher is the only caller.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(DomainEvent domainEvent, CancellationToken ct = default);
}

/// <summary>
/// Implemented by a module that wants to react to another module's published event.
/// Registered per (module, event type) pair against a named queue in Shared.Infrastructure's
/// generic SQS consumer host.
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : DomainEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken ct);
}
