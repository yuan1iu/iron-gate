using IronGate.Core;
using IronGate.Core.Abstractions;
using IronGate.Core.Models;

namespace IronGate.Tests.Core;

public class RateLimiterServiceTests
{
    private readonly RateLimitRule _rule = new(maxRequests: 3, window: TimeSpan.FromMinutes(1));

    [Fact]
    public async Task EvaluateAsync_WhenUnderLimit_ReturnsAllowed()
    {
        var store = new StubStore(currentCount: 1);
        var service = new RateLimiterService(store, new StubAlgorithm(isAllowed: true));

        var result = await service.EvaluateAsync("client", "/api/test", _rule);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task EvaluateAsync_WhenAllowed_IncrementsCounter()
    {
        var store = new StubStore(currentCount: 2);
        var service = new RateLimiterService(store, new StubAlgorithm(isAllowed: true));

        await service.EvaluateAsync("client", "/api/test", _rule);

        Assert.Equal(3, store.SavedCount);
    }

    [Fact]
    public async Task EvaluateAsync_WhenDenied_DoesNotIncrementCounter()
    {
        var store = new StubStore(currentCount: 3);
        var service = new RateLimiterService(store, new StubAlgorithm(isAllowed: false));

        await service.EvaluateAsync("client", "/api/test", _rule);

        Assert.Null(store.SavedCount);
    }

    [Fact]
    public async Task EvaluateAsync_UsesCorrectStoreKey()
    {
        var store = new StubStore(currentCount: 0);
        var service = new RateLimiterService(store, new StubAlgorithm(isAllowed: true));

        await service.EvaluateAsync("192.168.1.1", "/api/login", _rule);

        Assert.Equal("/api/login:192.168.1.1", store.RequestedKey);
    }
}

// ---------------------------------------------------------------------------
// Stubs — simple hand-written fakes, no mocking library needed
// ---------------------------------------------------------------------------

file sealed class StubStore(int currentCount) : IRateLimitStore
{
    public string? RequestedKey { get; private set; }
    public int? SavedCount { get; private set; }

    public Task<int> GetAsync(string key)
    {
        RequestedKey = key;
        return Task.FromResult(currentCount);
    }

    public Task SetAsync(string key, int count, TimeSpan expiry)
    {
        SavedCount = count;
        return Task.CompletedTask;
    }
}

file sealed class StubAlgorithm(bool isAllowed) : IRateLimitAlgorithm
{
    public RateLimitResult Evaluate(int currentCount, RateLimitRule rule) =>
        isAllowed
            ? RateLimitResult.Allow(rule.MaxRequests, rule.MaxRequests - currentCount - 1)
            : RateLimitResult.Deny(rule.MaxRequests, rule.Window);
}
