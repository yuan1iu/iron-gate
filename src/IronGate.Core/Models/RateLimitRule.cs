namespace IronGate.Core.Models;

/// <summary>
/// Defines the configuration for a rate limit policy —
/// how many requests are allowed within a given time window.
/// </summary>
public sealed class RateLimitRule
{
    /// <summary>
    /// The maximum number of requests allowed within the <see cref="Window"/>.
    /// </summary>
    public int MaxRequests { get; }

    /// <summary>
    /// The duration of the time window over which <see cref="MaxRequests"/> is enforced.
    /// </summary>
    public TimeSpan Window { get; }

    /// <summary>
    /// Initializes a new <see cref="RateLimitRule"/>.
    /// </summary>
    /// <param name="maxRequests">Maximum number of requests allowed. Must be greater than zero.</param>
    /// <param name="window">Duration of the time window. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxRequests"/> or <paramref name="window"/> is not positive.
    /// </exception>
    public RateLimitRule(int maxRequests, TimeSpan window)
    {
        if (maxRequests <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRequests), "Must be greater than zero.");
        if (window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window), "Must be greater than zero.");

        MaxRequests = maxRequests;
        Window = window;
    }
}
