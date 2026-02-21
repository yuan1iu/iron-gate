using IronGate.Core.Models;

namespace IronGate.AspNetCore.Options;

/// <summary>
/// Holds per-endpoint rate limit rules. Endpoints with no configured rule are not rate limited.
/// </summary>
public sealed class RateLimiterOptions
{
    private readonly Dictionary<string, RateLimitRule> _rules = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a rate limit rule for a specific endpoint path.
    /// </summary>
    /// <param name="endpoint">The endpoint path (e.g. "/api/login").</param>
    /// <param name="rule">The rate limit rule to enforce on that endpoint.</param>
    public RateLimiterOptions AddRule(string endpoint, RateLimitRule rule)
    {
        _rules[endpoint] = rule;
        return this;
    }

    /// <summary>
    /// Returns the rule for the given endpoint, or <c>null</c> if none is registered.
    /// </summary>
    /// <param name="endpoint">The endpoint path of the incoming request.</param>
    public RateLimitRule? GetRule(string endpoint) =>
        _rules.GetValueOrDefault(endpoint);
}
