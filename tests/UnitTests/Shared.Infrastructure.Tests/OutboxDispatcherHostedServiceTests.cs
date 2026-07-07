using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Outbox;
using Shared.Kernel;

namespace Shared.Infrastructure.Tests;

public sealed record TestEvent(Guid Id, DateTimeOffset OccurredOnUtc, string Payload) : DomainEvent(Id, OccurredOnUtc);

public sealed class TestDbContext : DbContext, IHasOutbox
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
}

/// <summary>
/// Exercises the exact behavior a real corrupted-message incident surfaced (see
/// docs/postmortems/001-poison-outbox-message.md): a message whose Type/Content can never
/// resolve must stop retrying instead of blocking the batch forever, while a genuinely
/// transient publish failure must still be retried on the next poll.
/// </summary>
public sealed class OutboxDispatcherHostedServiceTests
{
    private static (TestDbContext DbContext, IEventPublisher Publisher, OutboxDispatcherHostedService<TestDbContext> Dispatcher) CreateSut()
    {
        var dbContext = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var publisher = Substitute.For<IEventPublisher>();

        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddSingleton(publisher);
        var provider = services.BuildServiceProvider();

        var dispatcher = new OutboxDispatcherHostedService<TestDbContext>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OutboxDispatcherHostedService<TestDbContext>>.Instance);

        return (dbContext, publisher, dispatcher);
    }

    private static OutboxMessage MakeMessage(DomainEvent evt, DateTimeOffset occurredOnUtc, string type) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Content = JsonSerializer.Serialize(evt, evt.GetType()),
        OccurredOnUtc = occurredOnUtc,
    };

    [Fact]
    public async Task DispatchPendingAsync_publishes_a_valid_pending_message_and_marks_it_processed()
    {
        var (dbContext, publisher, dispatcher) = CreateSut();
        var evt = new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "hello");
        var message = MakeMessage(evt, DateTimeOffset.UtcNow, typeof(TestEvent).AssemblyQualifiedName!);
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        await publisher.Received(1).PublishAsync(Arg.Any<DomainEvent>(), Arg.Any<CancellationToken>());
        (await dbContext.OutboxMessages.FindAsync(message.Id))!.ProcessedOnUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task DispatchPendingAsync_marks_an_unresolvable_type_processed_without_publishing()
    {
        var (dbContext, publisher, dispatcher) = CreateSut();
        var message = MakeMessage(
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "x"),
            DateTimeOffset.UtcNow,
            "Totally.Bogus.Type, NoSuchAssembly");
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        await publisher.DidNotReceive().PublishAsync(Arg.Any<DomainEvent>(), Arg.Any<CancellationToken>());
        var reloaded = await dbContext.OutboxMessages.FindAsync(message.Id);
        reloaded!.ProcessedOnUtc.Should().NotBeNull();
        reloaded.Error.Should().Contain("Abandoned");
    }

    [Fact]
    public async Task DispatchPendingAsync_leaves_a_transient_publish_failure_unprocessed_but_still_dispatches_the_rest_of_the_batch()
    {
        var (dbContext, publisher, dispatcher) = CreateSut();
        var older = MakeMessage(
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "a"), DateTimeOffset.UtcNow.AddSeconds(-10), typeof(TestEvent).AssemblyQualifiedName!);
        var newer = MakeMessage(
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "b"), DateTimeOffset.UtcNow, typeof(TestEvent).AssemblyQualifiedName!);
        dbContext.OutboxMessages.AddRange(older, newer);
        await dbContext.SaveChangesAsync();

        publisher.PublishAsync(Arg.Is<DomainEvent>(e => ((TestEvent)e).Payload == "a"), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("broker unreachable"));

        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        (await dbContext.OutboxMessages.FindAsync(older.Id))!.ProcessedOnUtc
            .Should().BeNull("a transient publish failure should be retried on the next poll, not abandoned");
        (await dbContext.OutboxMessages.FindAsync(newer.Id))!.ProcessedOnUtc.Should().NotBeNull();
    }
}
