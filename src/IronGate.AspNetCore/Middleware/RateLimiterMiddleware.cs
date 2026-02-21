namespace IronGate.AspNetCore.Middleware;

public class RateLimiterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimiterMiddleware> _logger;

    public RateLimiterMiddleware(RequestDelegate next, ILogger<RateLimiterMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("IronGate: request hit middleware — {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        await _next(context);
    }
}
