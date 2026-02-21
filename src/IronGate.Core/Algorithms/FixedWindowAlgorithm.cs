using IronGate.Core.Abstractions;
using IronGate.Core.Models;

namespace IronGate.Core.Algorithms;

/// <summary>
/// Rate limiting algorithm that counts requests within fixed, non-overlapping time windows.
/// When the window expires, the counter resets and a new window begins.
/// </summary>
public sealed class FixedWindowAlgorithm : IRateLimitAlgorithm
{
    /// <inheritdoc/>
    public RateLimitResult Evaluate(int currentCount, RateLimitRule rule)
    {
        if (currentCount < rule.MaxRequests)
        {
            var remaining = rule.MaxRequests - currentCount - 1;
            return RateLimitResult.Allow(rule.MaxRequests, remaining);
        }

        return RateLimitResult.Deny(rule.MaxRequests, rule.Window);
    }
}
