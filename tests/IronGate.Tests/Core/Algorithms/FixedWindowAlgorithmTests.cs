using IronGate.Core.Algorithms;
using IronGate.Core.Models;

namespace IronGate.Tests.Core.Algorithms;

public class FixedWindowAlgorithmTests
{
    private readonly FixedWindowAlgorithm _algorithm = new();
    private readonly RateLimitRule _rule = new(maxRequests: 5, window: TimeSpan.FromMinutes(1));

    [Fact]
    public void Evaluate_WhenUnderLimit_AllowsRequest()
    {
        var result = _algorithm.Evaluate(currentCount: 3, _rule);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_WhenAtLimit_DeniesRequest()
    {
        var result = _algorithm.Evaluate(currentCount: 5, _rule);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public void Evaluate_WhenUnderLimit_ReturnsCorrectRemaining()
    {
        // count=3, limit=5 → after this request count becomes 4 → 1 remaining
        var result = _algorithm.Evaluate(currentCount: 3, _rule);

        Assert.Equal(1, result.Remaining);
    }

    [Fact]
    public void Evaluate_WhenLastRequestAllowed_ReturnsZeroRemaining()
    {
        // count=4, limit=5 → this is the last allowed request → 0 remaining
        var result = _algorithm.Evaluate(currentCount: 4, _rule);

        Assert.True(result.IsAllowed);
        Assert.Equal(0, result.Remaining);
    }

    [Fact]
    public void Evaluate_WhenDenied_ReturnsZeroRemaining()
    {
        var result = _algorithm.Evaluate(currentCount: 5, _rule);

        Assert.Equal(0, result.Remaining);
    }

    [Fact]
    public void Evaluate_WhenDenied_ReturnsRetryAfterEqualToWindow()
    {
        var result = _algorithm.Evaluate(currentCount: 5, _rule);

        Assert.Equal(_rule.Window, result.RetryAfter);
    }

    [Fact]
    public void Evaluate_AlwaysReturnsLimitFromRule()
    {
        var allowed = _algorithm.Evaluate(currentCount: 1, _rule);
        var denied  = _algorithm.Evaluate(currentCount: 5, _rule);

        Assert.Equal(_rule.MaxRequests, allowed.Limit);
        Assert.Equal(_rule.MaxRequests, denied.Limit);
    }
}
