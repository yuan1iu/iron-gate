using IronGate.Core.Models;

namespace IronGate.Core.Abstractions;

/// <summary>
/// Evaluates whether an incoming request should be allowed or denied
/// based on the configured rate limit rule.
/// </summary>
public interface IRateLimiterService
{
    /// <summary>
    /// Evaluates whether the request from <paramref name="clientKey"/> to <paramref name="endpoint"/>
    /// should be allowed under the given <paramref name="rule"/>.
    /// </summary>
    /// <param name="clientKey">A string identifying the client (e.g. IP address, user ID).</param>
    /// <param name="endpoint">The endpoint being accessed (e.g. /api/login).</param>
    /// <param name="rule">The rate limit rule to enforce.</param>
    Task<RateLimitResult> EvaluateAsync(string clientKey, string endpoint, RateLimitRule rule);
}
