namespace IronGate.Core.Abstractions;

/// <summary>
/// Abstracts the storage backend for rate limit counters.
/// Implementations can store state in memory, Redis, a database, or any other backend.
/// </summary>
public interface IRateLimitStore
{
    /// <summary>
    /// Retrieves the current request count for the given key.
    /// Returns <c>0</c> if no entry exists.
    /// </summary>
    /// <param name="key">The unique key identifying a client and endpoint combination.</param>
    Task<int> GetAsync(string key);

    /// <summary>
    /// Saves the request count for the given key with an expiry matching the rate limit window.
    /// </summary>
    /// <param name="key">The unique key identifying a client and endpoint combination.</param>
    /// <param name="count">The updated request count to store.</param>
    /// <param name="expiry">How long until this entry expires (should match the rule window).</param>
    Task SetAsync(string key, int count, TimeSpan expiry);
}
