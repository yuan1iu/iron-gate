using IronGate.AspNetCore.Options;

namespace IronGate.AspNetCore.Middleware;

public class RateLimiterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IClientIdentifier _clientIdentifier;
    private readonly RateLimiterOptions _options;
    private readonly ILogger<RateLimiterMiddleware> _logger;

    public RateLimiterMiddleware(
        RequestDelegate next,
        IClientIdentifier clientIdentifier,
        RateLimiterOptions options,
        ILogger<RateLimiterMiddleware> logger)
    {
        _next = next;
        _clientIdentifier = clientIdentifier;
        _options = options;
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

        await _next(context);
    }
}
