using IronGate.Core.Models;

namespace IronGate.Core.Abstractions;

/// <summary>
/// Abstracts the rate limiting algorithm (Strategy pattern).
/// Implementations can use Fixed Window, Sliding Window, Token Bucket, etc. —
/// all interchangeable without changing the service or middleware.
/// </summary>
public interface IRateLimitAlgorithm
{
    /// <summary>
    /// Evaluates whether a request should be allowed given the current count and the rule.
    /// </summary>
    /// <param name="currentCount">The number of requests already made in the current window.</param>
    /// <param name="rule">The rule that defines the limit and window size.</param>
    /// <returns>A <see cref="RateLimitResult"/> indicating whether the request is allowed.</returns>
    RateLimitResult Evaluate(int currentCount, RateLimitRule rule);
}
