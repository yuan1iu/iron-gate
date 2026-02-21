using IronGate.AspNetCore;
using IronGate.AspNetCore.Identifiers;
using IronGate.AspNetCore.Middleware;
using IronGate.AspNetCore.Options;
using IronGate.Core;
using IronGate.Core.Abstractions;
using IronGate.Core.Algorithms;
using IronGate.Core.Models;
using IronGate.Core.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IClientIdentifier, IpClientIdentifier>();
builder.Services.AddSingleton<IRateLimitStore, InMemoryRateLimitStore>();
builder.Services.AddSingleton<IRateLimitAlgorithm, FixedWindowAlgorithm>();
builder.Services.AddSingleton<RateLimiterService>();
builder.Services.AddSingleton(new RateLimiterOptions()
    .AddRule("/api/products", new RateLimitRule(maxRequests: 3, window: TimeSpan.FromMinutes(1))));

var app = builder.Build();

app.UseMiddleware<RateLimiterMiddleware>();

app.MapGet("/", () => "Hello World!");
app.MapGet("/api/products", () => new[] { "Apple", "Banana", "Cherry" });

app.Run();
