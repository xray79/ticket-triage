namespace Shared.Kernel;

/// <summary>
/// Raised by an aggregate on a business-significant change. The outbox dispatcher
/// (Shared.Infrastructure) publishes every raised event to the async transport (SQS),
/// so this same type doubles as the module's published integration event contract.
/// </summary>
public abstract record DomainEvent(Guid Id, DateTimeOffset OccurredOnUtc)
{
    protected DomainEvent() : this(Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}
