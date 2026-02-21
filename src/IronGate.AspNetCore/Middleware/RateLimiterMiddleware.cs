using IronGate.AspNetCore.Options;
using IronGate.Core.Abstractions;

namespace IronGate.AspNetCore.Middleware;

/// <summary>
/// ASP.NET Core middleware that enforces rate limiting on incoming HTTP requests.
/// Requests are identified by <see cref="IClientIdentifier"/>, matched against
/// per-endpoint rules in <see cref="RateLimiterOptions"/>, and evaluated by
/// <see cref="IRateLimiterService"/>.
/// </summary>
public class RateLimiterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IClientIdentifier _clientIdentifier;
    private readonly RateLimiterOptions _options;
    private readonly IRateLimiterService _rateLimiterService;
    private readonly ILogger<RateLimiterMiddleware> _logger;

    public RateLimiterMiddleware(
        RequestDelegate next,
        IClientIdentifier clientIdentifier,
        RateLimiterOptions options,
        IRateLimiterService rateLimiterService,
        ILogger<RateLimiterMiddleware> logger)
    {
        _next = next;
        _clientIdentifier = clientIdentifier;
        _options = options;
        _rateLimiterService = rateLimiterService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.Request.Path.Value ?? "/";
        var clientKey = _clientIdentifier.GetClientKey(context);

        _logger.LogInformation("Incoming request: {ClientKey} → {Endpoint}", clientKey, endpoint);

        var rule = _options.GetRule(endpoint);

        if (rule is null)
        {
            _logger.LogDebug("No rule for {Endpoint} — passing through", endpoint);
            await _next(context);
            return;
        }

        var result = await _rateLimiterService.EvaluateAsync(clientKey, endpoint, rule);

        context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();

        if (result.IsAllowed)
        {
            _logger.LogInformation("Allowed: {ClientKey} → {Endpoint} ({Remaining}/{Limit} remaining)",
                clientKey, endpoint, result.Remaining, result.Limit);

            await _next(context);
            return;
        }

        _logger.LogWarning("Denied: {ClientKey} → {Endpoint} (retry after {RetryAfter}s)",
            clientKey, endpoint, (int)result.RetryAfter.TotalSeconds);

        context.Response.Headers["Retry-After"] = ((int)result.RetryAfter.TotalSeconds).ToString();
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests.",
            retryAfterSeconds = (int)result.RetryAfter.TotalSeconds
        });
    }
}
