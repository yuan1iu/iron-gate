using System.Collections.Concurrent;
using IronGate.Core.Abstractions;

namespace IronGate.Core.Storage;

/// <summary>
/// In-memory store for rate limit counters. Resets on app restart.
/// </summary>
public class InMemoryRateLimitStore : IRateLimitStore
{
    private readonly ConcurrentDictionary<string, (int Count, DateTime ExpiresAt)> _store = new();

    /// <inheritdoc />
    public Task<int> GetAsync(string key)
    {
        if (_store.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
            return Task.FromResult(entry.Count);

        return Task.FromResult(0);
    }

    /// <inheritdoc />
    public Task SetAsync(string key, int count, TimeSpan expiry)
    {
        _store[key] = (count, DateTime.UtcNow.Add(expiry));
        return Task.CompletedTask;
    }
}
