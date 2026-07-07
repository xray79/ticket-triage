using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Triage.Application;
using Triage.Application.Caching;
using Triage.Infrastructure.Caching;
using Xunit;

namespace Triage.Tests;

public sealed class DistributedTriageResultCacheTests
{
    private static IDistributedCache CreateInMemoryCache()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        return services.BuildServiceProvider().GetRequiredService<IDistributedCache>();
    }

    [Fact]
    public async Task GetAsync_returns_null_for_content_never_cached()
    {
        var cache = new DistributedTriageResultCache(CreateInMemoryCache(), TimeSpan.FromHours(1));

        var result = await cache.GetAsync("subject", "body", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_then_GetAsync_round_trips_the_same_masked_content()
    {
        var cache = new DistributedTriageResultCache(CreateInMemoryCache(), TimeSpan.FromHours(1));
        var attempt = new CachedTriageAttempt(new TriageResult("billing", "high", "summary", "draft"), "openai", false);

        await cache.SetAsync("subject", "[EMAIL_1] needs help", attempt, CancellationToken.None);
        var result = await cache.GetAsync("subject", "[EMAIL_1] needs help", CancellationToken.None);

        result.Should().Be(attempt);
    }

    [Fact]
    public async Task GetAsync_is_a_miss_for_different_masked_content()
    {
        var cache = new DistributedTriageResultCache(CreateInMemoryCache(), TimeSpan.FromHours(1));
        var attempt = new CachedTriageAttempt(new TriageResult("billing", "high", "summary", "draft"), "openai", false);

        await cache.SetAsync("subject", "[EMAIL_1] needs help", attempt, CancellationToken.None);
        var result = await cache.GetAsync("subject", "a completely different ticket body", CancellationToken.None);

        result.Should().BeNull();
    }
}
