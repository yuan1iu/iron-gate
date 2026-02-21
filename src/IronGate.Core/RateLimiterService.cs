using IronGate.Core.Abstractions;
using IronGate.Core.Models;

namespace IronGate.Core;

/// <summary>
/// Orchestrates rate limiting — delegates storage to <see cref="IRateLimitStore"/>
/// and evaluation logic to <see cref="IRateLimitAlgorithm"/>.
/// </summary>
public class RateLimiterService
{
    private readonly IRateLimitStore _store;
    private readonly IRateLimitAlgorithm _algorithm;

    /// <summary>
    /// Initializes a new <see cref="RateLimiterService"/> with the required dependencies.
    /// </summary>
    /// <param name="store">The storage backend for rate limit counters.</param>
    /// <param name="algorithm">The algorithm used to evaluate whether a request is allowed.</param>
    public RateLimiterService(IRateLimitStore store, IRateLimitAlgorithm algorithm)
    {
        _store = store;
        _algorithm = algorithm;
    }

    /// <summary>
    /// Evaluates whether the request from <paramref name="clientKey"/> to <paramref name="endpoint"/>
    /// should be allowed under the given <paramref name="rule"/>.
    /// </summary>
    /// <param name="clientKey">A string identifying the client (e.g. IP address, user ID).</param>
    /// <param name="endpoint">The endpoint being accessed (e.g. /api/login).</param>
    /// <param name="rule">The rate limit rule to enforce.</param>
    public async Task<RateLimitResult> EvaluateAsync(string clientKey, string endpoint, RateLimitRule rule)
    {
        var storeKey = $"{endpoint}:{clientKey}";

        var count = await _store.GetAsync(storeKey);
        var result = _algorithm.Evaluate(count, rule);

        if (result.IsAllowed)
            await _store.SetAsync(storeKey, count + 1, rule.Window);

        return result;
    }
}
