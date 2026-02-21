using IronGate.AspNetCore.Options;
using IronGate.Core;

namespace IronGate.AspNetCore.Middleware;

public class RateLimiterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IClientIdentifier _clientIdentifier;
    private readonly RateLimiterOptions _options;
    private readonly RateLimiterService _rateLimiterService;
    private readonly ILogger<RateLimiterMiddleware> _logger;

    public RateLimiterMiddleware(
        RequestDelegate next,
        IClientIdentifier clientIdentifier,
        RateLimiterOptions options,
        RateLimiterService rateLimiterService,
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
        var rule = _options.GetRule(endpoint);

        // No rule configured for this endpoint — let it through
        if (rule is null)
        {
            await _next(context);
            return;
        }

        var result = await _rateLimiterService.EvaluateAsync(clientKey, endpoint, rule);

        context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();

        if (result.IsAllowed)
        {
            await _next(context);
            return;
        }

        context.Response.Headers["Retry-After"] = ((int)result.RetryAfter.TotalSeconds).ToString();
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests.",
            retryAfterSeconds = (int)result.RetryAfter.TotalSeconds
        });
    }
}
