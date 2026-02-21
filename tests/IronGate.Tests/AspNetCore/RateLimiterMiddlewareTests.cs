using System.Net;
using IronGate.AspNetCore;
using Microsoft.AspNetCore.Http;
using IronGate.AspNetCore.Extensions;
using IronGate.AspNetCore.Middleware;
using IronGate.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IronGate.Tests.AspNetCore;

public class RateLimiterMiddlewareTests
{
    private static IHost BuildHost(Action<IServiceCollection> configureServices) =>
        new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(configureServices)
                .Configure(app =>
                {
                    app.UseMiddleware<RateLimiterMiddleware>();
                    app.Run(ctx => ctx.Response.WriteAsync("OK"));
                }))
            .Build();

    [Fact]
    public async Task Request_WithNoRuleConfigured_Returns200()
    {
        using var host = BuildHost(services =>
            services.AddIronGate(_ => { })); // no rules

        await host.StartAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Request_UnderLimit_Returns200WithRateLimitHeaders()
    {
        using var host = BuildHost(services =>
            services.AddIronGate(options =>
                options.AddRule("/api/products", new RateLimitRule(3, TimeSpan.FromMinutes(1)))));

        await host.StartAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-RateLimit-Limit"));
        Assert.True(response.Headers.Contains("X-RateLimit-Remaining"));
    }

    [Fact]
    public async Task Request_WhenLimitExceeded_Returns429()
    {
        using var host = BuildHost(services =>
            services.AddIronGate(options =>
                options.AddRule("/api/login", new RateLimitRule(2, TimeSpan.FromMinutes(1)))));

        await host.StartAsync();
        var client = host.GetTestClient();

        await client.GetAsync("/api/login"); // request 1
        await client.GetAsync("/api/login"); // request 2
        var response = await client.GetAsync("/api/login"); // request 3 — over limit

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Request_WhenDenied_Returns429WithRetryAfterHeader()
    {
        using var host = BuildHost(services =>
            services.AddIronGate(options =>
                options.AddRule("/api/login", new RateLimitRule(1, TimeSpan.FromMinutes(1)))));

        await host.StartAsync();
        var client = host.GetTestClient();

        await client.GetAsync("/api/login");          // allowed
        var response = await client.GetAsync("/api/login"); // denied

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(response.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task Request_WhenDenied_ReturnsJsonBody()
    {
        using var host = BuildHost(services =>
            services.AddIronGate(options =>
                options.AddRule("/api/login", new RateLimitRule(1, TimeSpan.FromMinutes(1)))));

        await host.StartAsync();
        var client = host.GetTestClient();

        await client.GetAsync("/api/login");
        var response = await client.GetAsync("/api/login");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("Too many requests", body);
        Assert.Contains("retryAfterSeconds", body);
    }

    [Fact]
    public async Task RateLimitHeaders_DecrementWithEachRequest()
    {
        using var host = BuildHost(services =>
            services.AddIronGate(options =>
                options.AddRule("/api/products", new RateLimitRule(3, TimeSpan.FromMinutes(1)))));

        await host.StartAsync();
        var client = host.GetTestClient();

        var r1 = await client.GetAsync("/api/products");
        var r2 = await client.GetAsync("/api/products");
        var r3 = await client.GetAsync("/api/products");

        Assert.Equal("2", r1.Headers.GetValues("X-RateLimit-Remaining").First());
        Assert.Equal("1", r2.Headers.GetValues("X-RateLimit-Remaining").First());
        Assert.Equal("0", r3.Headers.GetValues("X-RateLimit-Remaining").First());
    }
}
