using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Triage.Application.Caching;

namespace Triage.Infrastructure.Caching;

/// <summary>
/// Works against whatever <see cref="IDistributedCache"/> is registered — Redis in a real
/// deployment, or the in-memory fallback Host wires up when no Redis connection string is
/// configured (see DependencyInjection.AddCacheServices). Cache key is a hash of the masked
/// (already-redacted) subject+body, so nothing PII-bearing is ever used as a cache key either.
/// </summary>
public sealed class DistributedTriageResultCache : ITriageResultCache
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _ttl;

    public DistributedTriageResultCache(IDistributedCache cache, TimeSpan ttl)
    {
        _cache = cache;
        _ttl = ttl;
    }

    public async Task<CachedTriageAttempt?> GetAsync(string maskedSubject, string maskedBody, CancellationToken ct)
    {
        var key = BuildKey(maskedSubject, maskedBody);
        var json = await _cache.GetStringAsync(key, ct);
        return json is null ? null : JsonSerializer.Deserialize<CachedTriageAttempt>(json);
    }

    public async Task SetAsync(string maskedSubject, string maskedBody, CachedTriageAttempt attempt, CancellationToken ct)
    {
        var key = BuildKey(maskedSubject, maskedBody);
        var json = JsonSerializer.Serialize(attempt);
        await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _ttl }, ct);
    }

    private static string BuildKey(string maskedSubject, string maskedBody)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{maskedSubject}{maskedBody}"));
        return $"triage:result:{Convert.ToHexString(hash)}";
    }
}
