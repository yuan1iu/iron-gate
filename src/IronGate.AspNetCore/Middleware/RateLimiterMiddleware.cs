namespace IronGate.AspNetCore.Middleware;

public class RateLimiterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IClientIdentifier _clientIdentifier;
    private readonly ILogger<RateLimiterMiddleware> _logger;

    public RateLimiterMiddleware(RequestDelegate next, IClientIdentifier clientIdentifier, ILogger<RateLimiterMiddleware> logger)
    {
        _next = next;
        _clientIdentifier = clientIdentifier;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientKey = _clientIdentifier.GetClientKey(context);

        await _next(context);
    }
}
