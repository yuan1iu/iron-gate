using IronGate.Core;
using IronGate.Core.Abstractions;
using IronGate.Core.Algorithms;
using IronGate.Core.Models;
using Xunit.Abstractions;

namespace IronGate.Tests.Core;

public class RaceConditionTests(ITestOutputHelper output)
{
    private readonly RateLimitRule _rule = new(maxRequests: 5, window: TimeSpan.FromMinutes(1));

    // AsyncLocal flows with async continuations across thread switches
    // Check this in Watch/Locals panel while debugging to know which request you're in
    private static readonly AsyncLocal<int> _currentRequestId = new();

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
    // Scenario 2b: Same as above but with full instrumentation so you can SEE
    // every read and write happening in real time
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TOCTOU_Instrumented_ShowsAllReadsBeforeAnyWrite()
    {
        var start = DateTime.UtcNow;
        string Elapsed() => $"+{(DateTime.UtcNow - start).TotalMilliseconds:F0}ms".PadRight(8);

        var store = new InstrumentedStore(
            delayMs: 30,
            onGet: (key, count) =>
                output.WriteLine($"  [{Elapsed()}] Request-{_currentRequestId.Value} GET → read count={count}"),
            onSet: (key, count) =>
                output.WriteLine($"  [{Elapsed()}] Request-{_currentRequestId.Value} SET → wrote count={count}  ← overwrites previous!")
        );

        var service = new RateLimiterService(store, new FixedWindowAlgorithm());

        output.WriteLine("--- Firing 10 concurrent requests (limit=5, delay=30ms) ---");
        output.WriteLine("");

        var tasks = Enumerable.Range(0, 10)
            .Select(i => Task.Run(async () =>
            {
                // Name the thread so it shows as "Request-N" in the Threads panel
                Thread.CurrentThread.Name = $"Request-{i}";

                // AsyncLocal flows across awaits — check _currentRequestId.Value
                // in Watch/Locals to know which request you're inside after any await
                _currentRequestId.Value = i;

                return await service.EvaluateAsync("client", "/api/test", _rule);
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var allowed = results.Count(r => r.IsAllowed);

        output.WriteLine("");
        output.WriteLine($"  Final count in store : {store.CurrentCount}  (should be {Math.Min(10, _rule.MaxRequests)})");
        output.WriteLine($"  Requests allowed     : {allowed}/10  (limit was {_rule.MaxRequests})");
        output.WriteLine("");
        output.WriteLine("  Notice: all GETs complete before any SET — that is the race window.");

        Assert.True(allowed > 5);
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
/// Instrumented store that calls back on every read and write so you can observe the race in real time.
/// </summary>
file sealed class InstrumentedStore(int delayMs, Action<string, int> onGet, Action<string, int> onSet) : IRateLimitStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int Count, DateTime ExpiresAt)> _store = new();

    public int CurrentCount => _store.TryGetValue("/api/test:/api/test:client", out var e) ? e.Count
        : _store.FirstOrDefault().Value.Count;

    public Task<int> GetAsync(string key)
    {
        var count = _store.TryGetValue(key, out var e) && e.ExpiresAt > DateTime.UtcNow ? e.Count : 0;
        onGet(key, count);
        return Task.FromResult(count);
    }

    public async Task SetAsync(string key, int count, TimeSpan expiry)
    {
        await Task.Delay(delayMs);
        _store[key] = (count, DateTime.UtcNow.Add(expiry));
        onSet(key, count);
    }
}

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
