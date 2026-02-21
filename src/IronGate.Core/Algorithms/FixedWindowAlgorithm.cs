using IronGate.Core.Abstractions;
using IronGate.Core.Models;

namespace IronGate.Core.Algorithms;

/// <summary>
/// Rate limiting algorithm that counts requests within fixed time windows.
/// </summary>
public class FixedWindowAlgorithm : IRateLimitAlgorithm
{
    /// <inheritdoc />
    public RateLimitResult Evaluate(int currentCount, RateLimitRule rule)
    {
        if (currentCount < rule.MaxRequests)
            return RateLimitResult.Allow(rule.MaxRequests, rule.MaxRequests - currentCount - 1);

        return RateLimitResult.Deny(rule.MaxRequests, rule.Window);
    }
}
