using System.Collections.Concurrent;
using IronGate.Core.Abstractions;

namespace IronGate.Core.Storage;

/// <summary>
/// In-memory implementation of <see cref="IRateLimitStore"/>.
/// Stores counters in a thread-safe dictionary with manual expiry tracking.
/// Suitable for single-instance applications. For distributed deployments, use a Redis-backed implementation.
/// </summary>
public sealed class InMemoryRateLimitStore : IRateLimitStore
{
    private record Entry(int Count, DateTime ExpiresAt);

    private readonly ConcurrentDictionary<string, Entry> _store = new();

    /// <inheritdoc/>
    public Task<int> GetAsync(string key)
    {
        if (_store.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
            return Task.FromResult(entry.Count);

        return Task.FromResult(0);
    }

    /// <inheritdoc/>
    public Task SetAsync(string key, int count, TimeSpan expiry)
    {
        var expiresAt = DateTime.UtcNow.Add(expiry);
        _store[key] = new Entry(count, expiresAt);
        return Task.CompletedTask;
    }
}
