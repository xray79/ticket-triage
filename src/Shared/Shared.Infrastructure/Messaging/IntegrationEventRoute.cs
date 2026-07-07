namespace Shared.Infrastructure.Messaging;

/// <summary>One entry per event type a module's consumer host will dispatch from its queue.</summary>
public sealed record IntegrationEventRoute(
    string EventTypeName,
    Type ClrType,
    Func<IServiceProvider, object, CancellationToken, Task> Dispatch);
