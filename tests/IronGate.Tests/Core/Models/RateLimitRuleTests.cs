using IronGate.Core.Models;

namespace IronGate.Tests.Core.Models;

public class RateLimitRuleTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var rule = new RateLimitRule(10, TimeSpan.FromMinutes(1));

        Assert.Equal(10, rule.MaxRequests);
        Assert.Equal(TimeSpan.FromMinutes(1), rule.Window);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidMaxRequests_Throws(int maxRequests)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RateLimitRule(maxRequests, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Constructor_WithZeroWindow_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RateLimitRule(10, TimeSpan.Zero));
    }

    [Fact]
    public void Constructor_WithNegativeWindow_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RateLimitRule(10, TimeSpan.FromSeconds(-1)));
    }
}
