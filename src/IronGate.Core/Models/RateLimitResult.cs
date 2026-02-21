namespace IronGate.Core.Models;

/// <summary>
/// Represents the outcome of a rate limit evaluation.
/// Use <see cref="Allow"/> or <see cref="Deny"/> to construct instances.
/// </summary>
public sealed class RateLimitResult
{
    /// <summary>
    /// Indicates whether the request is permitted to proceed.
    /// </summary>
    public bool IsAllowed { get; }

    /// <summary>
    /// The maximum number of requests permitted in the current window, as defined by the applied rule.
    /// </summary>
    public int Limit { get; }

    /// <summary>
    /// The number of requests still allowed in the current window.
    /// Always <c>0</c> when <see cref="IsAllowed"/> is <c>false</c>.
    /// </summary>
    public int Remaining { get; }

    /// <summary>
    /// How long the client should wait before retrying.
    /// Always <see cref="TimeSpan.Zero"/> when <see cref="IsAllowed"/> is <c>true</c>.
    /// </summary>
    public TimeSpan RetryAfter { get; }

    private RateLimitResult(bool isAllowed, int limit, int remaining, TimeSpan retryAfter)
    {
        IsAllowed = isAllowed;
        Limit = limit;
        Remaining = remaining;
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Creates a result indicating the request is allowed.
    /// </summary>
    /// <param name="limit">The maximum requests allowed in the window.</param>
    /// <param name="remaining">The number of requests still available.</param>
    public static RateLimitResult Allow(int limit, int remaining) =>
        new(true, limit, remaining, TimeSpan.Zero);

    /// <summary>
    /// Creates a result indicating the request is denied.
    /// </summary>
    /// <param name="limit">The maximum requests allowed in the window.</param>
    /// <param name="retryAfter">How long the client should wait before retrying.</param>
    public static RateLimitResult Deny(int limit, TimeSpan retryAfter) =>
        new(false, limit, 0, retryAfter);
}
