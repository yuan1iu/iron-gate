using IronGate.Core;
using IronGate.Core.Abstractions;
using IronGate.Core.Algorithms;
using IronGate.Core.Models;
using Xunit.Abstractions;

namespace IronGate.Tests.Core;

public class RaceConditionTests(ITestOutputHelper output)
{
    private readonly RateLimitRule _rule = new(maxRequests: 5, window: TimeSpan.FromMinutes(1));

    // -------------------------------------------------------------------------
    // Scenario 1: Plain Dictionary — can corrupt or throw under concurrent writes
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PlainDictionary_ThrowsOrCorruptsUnderConcurrentWrites()
    {
        var store = new UnsafeDictionaryStore();
        var service = new RateLimiterService(store, new FixedWindowAlgorithm());

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => service.EvaluateAsync("client", "/api/test", _rule))
            .ToArray();

        // Dictionary is not thread-safe — this may throw InvalidOperationException
        // or silently corrupt state (wrong count, lost writes, etc.)
        try
        {
            var results = await Task.WhenAll(tasks);
            var allowed = results.Count(r => r.IsAllowed);
            output.WriteLine($"[Plain Dictionary] Allowed: {allowed}/100 (limit: 5) — state may be corrupted");
        }
        catch (Exception ex)
        {
            output.WriteLine($"[Plain Dictionary] Threw: {ex.GetType().Name} — {ex.Message}");
        }
    }

    // -------------------------------------------------------------------------
    // Scenario 2: TOCTOU race — even ConcurrentDictionary can't save us here.
    // The Get → Evaluate → Set sequence is not atomic. Two threads can both
    // read count=4, both decide to allow, and both write count=5.
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TOCTOU_WithDelayedStore_AllowsMoreThanLimit()
    {
        // This store adds a small delay between Get and Set to widen the race window
        // and reliably reproduce the TOCTOU problem
        var store = new DelayedStore(delayMs: 20);
        var service = new RateLimiterService(store, new FixedWindowAlgorithm());

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => service.EvaluateAsync("client", "/api/test", _rule))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var allowed = results.Count(r => r.IsAllowed);

        output.WriteLine($"[TOCTOU] Allowed: {allowed}/20 (limit: 5)");
        output.WriteLine("Expected: more than 5 — all threads read count=0 before any write completes");

        // This will pass — demonstrating the race condition
        Assert.True(allowed > 5, $"Race condition not reproduced — only {allowed} allowed");
    }

    // -------------------------------------------------------------------------
    // Scenario 3: ConcurrentDictionary alone doesn't fix TOCTOU either
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TOCTOU_WithConcurrentDictionary_StillAllowsMoreThanLimit()
    {
        var store = new DelayedConcurrentStore(delayMs: 20);
        var service = new RateLimiterService(store, new FixedWindowAlgorithm());

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => service.EvaluateAsync("client", "/api/test", _rule))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var allowed = results.Count(r => r.IsAllowed);

        output.WriteLine($"[TOCTOU + ConcurrentDict] Allowed: {allowed}/20 (limit: 5)");
        output.WriteLine("ConcurrentDictionary prevents corruption but NOT the read-then-write race");

        Assert.True(allowed > 5, $"Race condition not reproduced — only {allowed} allowed");
    }
}

// ---------------------------------------------------------------------------
// Test helpers — unsafe store implementations used only in these tests
// ---------------------------------------------------------------------------

/// <summary>
/// Intentionally unsafe store using plain Dictionary — demonstrates corruption under concurrency.
/// </summary>
file sealed class UnsafeDictionaryStore : IRateLimitStore
{
    private readonly Dictionary<string, (int Count, DateTime ExpiresAt)> _store = new();

    public Task<int> GetAsync(string key)
    {
        if (_store.TryGetValue(key, out var e) && e.ExpiresAt > DateTime.UtcNow)
            return Task.FromResult(e.Count);
        return Task.FromResult(0);
    }

    public Task SetAsync(string key, int count, TimeSpan expiry)
    {
        _store[key] = (count, DateTime.UtcNow.Add(expiry));
        return Task.CompletedTask;
    }
}

/// <summary>
/// Store with an artificial delay between Get and Set to widen the TOCTOU race window.
/// Uses ConcurrentDictionary to isolate the TOCTOU problem from Dictionary corruption.
/// </summary>
file sealed class DelayedStore(int delayMs) : IRateLimitStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int Count, DateTime ExpiresAt)> _store = new();

    public Task<int> GetAsync(string key)
    {
        if (_store.TryGetValue(key, out var e) && e.ExpiresAt > DateTime.UtcNow)
            return Task.FromResult(e.Count);
        return Task.FromResult(0);
    }

    public async Task SetAsync(string key, int count, TimeSpan expiry)
    {
        // Simulates slow I/O (database, network) — widens the race window
        await Task.Delay(delayMs);
        _store[key] = (count, DateTime.UtcNow.Add(expiry));
    }
}

/// <summary>
/// Store with delay but using ConcurrentDictionary — shows that thread-safe writes
/// alone do not prevent the TOCTOU race between GetAsync and SetAsync.
/// </summary>
file sealed class DelayedConcurrentStore(int delayMs) : IRateLimitStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int Count, DateTime ExpiresAt)> _store = new();

    public Task<int> GetAsync(string key)
    {
        if (_store.TryGetValue(key, out var e) && e.ExpiresAt > DateTime.UtcNow)
            return Task.FromResult(e.Count);
        return Task.FromResult(0);
    }

    public async Task SetAsync(string key, int count, TimeSpan expiry)
    {
        await Task.Delay(delayMs);
        _store[key] = (count, DateTime.UtcNow.Add(expiry));
    }
}
