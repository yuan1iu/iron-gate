using IronGate.AspNetCore.Extensions;
using IronGate.AspNetCore.Middleware;
using IronGate.Core.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIronGate(options =>
{
    options.AddRule("/api/products", new RateLimitRule(maxRequests: 3, window: TimeSpan.FromSeconds(10)));
    options.AddRule("/api/login",    new RateLimitRule(maxRequests: 2, window: TimeSpan.FromSeconds(30)));
});

var app = builder.Build();

app.UseMiddleware<RateLimiterMiddleware>();

app.MapGet("/",             () => "Hello World!");
app.MapGet("/api/products", () => new[] { "Apple", "Banana", "Cherry" });
app.MapGet("/api/login",    () => "Logged in!");

app.Run();
