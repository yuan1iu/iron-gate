using IronGate.AspNetCore.Identifiers;
using IronGate.AspNetCore.Options;
using IronGate.Core;
using IronGate.Core.Abstractions;
using IronGate.Core.Algorithms;
using IronGate.Core.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace IronGate.AspNetCore.Extensions;

/// <summary>
/// Extension methods for registering IronGate rate limiting services.
/// </summary>
public static class IronGateServiceCollectionExtensions
{
    /// <summary>
    /// Registers IronGate rate limiting with default implementations:
    /// <see cref="FixedWindowAlgorithm"/>, <see cref="InMemoryRateLimitStore"/>,
    /// and <see cref="IpClientIdentifier"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">A delegate to configure per-endpoint rules.</param>
    public static IServiceCollection AddIronGate(
        this IServiceCollection services,
        Action<RateLimiterOptions> configure)
    {
        var options = new RateLimiterOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<IRateLimitAlgorithm, FixedWindowAlgorithm>();
        services.AddSingleton<IRateLimitStore, InMemoryRateLimitStore>();
        services.AddSingleton<IRateLimiterService, RateLimiterService>();
        services.AddSingleton<IClientIdentifier, IpClientIdentifier>();

        return services;
    }
}
