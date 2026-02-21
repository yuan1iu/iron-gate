using IronGate.Core.Models;

namespace IronGate.Tests.Core.Models;

public class RateLimitResultTests
{
    [Fact]
    public void Allow_SetsIsAllowedTrue_AndZeroRetryAfter()
    {
        var result = RateLimitResult.Allow(limit: 10, remaining: 4);

        Assert.True(result.IsAllowed);
        Assert.Equal(10, result.Limit);
        Assert.Equal(4, result.Remaining);
        Assert.Equal(TimeSpan.Zero, result.RetryAfter);
    }

    [Fact]
    public void Deny_SetsIsAllowedFalse_AndZeroRemaining()
    {
        var result = RateLimitResult.Deny(limit: 10, retryAfter: TimeSpan.FromSeconds(30));

        Assert.False(result.IsAllowed);
        Assert.Equal(10, result.Limit);
        Assert.Equal(0, result.Remaining);
        Assert.Equal(TimeSpan.FromSeconds(30), result.RetryAfter);
    }
}
