# IronGate

A lightweight, extensible rate limiter for ASP.NET Core built as middleware. IronGate enforces per-endpoint request limits, returning `429 Too Many Requests` with standard headers and a JSON body when a client exceeds its quota.

Built as a learning exercise for **OOP**, **clean code**, **SOLID**, and **DRY** principles in .NET 8.

---

## Features

- Per-endpoint rate limit rules
- Pluggable algorithm (Fixed Window included; swap in your own)
- Pluggable storage (in-memory included; swap in Redis, etc.)
- Pluggable client identification (IP-based included; swap in API key, JWT, etc.)
- Standard `X-RateLimit-Limit` / `X-RateLimit-Remaining` response headers
- `Retry-After` header and JSON body on denial
- Zero external runtime dependencies

---

## Quick Start

### 1. Register services

```csharp
builder.Services.AddIronGate(options =>
{
    options.AddRule("/api/products", new RateLimitRule(maxRequests: 100, window: TimeSpan.FromMinutes(1)));
    options.AddRule("/api/login",    new RateLimitRule(maxRequests: 5,   window: TimeSpan.FromMinutes(1)));
});
```

### 2. Add the middleware

```csharp
app.UseMiddleware<RateLimiterMiddleware>();
```

Add it **before** your endpoint handlers so requests are evaluated on the way in.

### 3. Try it

```
GET /api/login  →  200 OK
                   X-RateLimit-Limit: 5
                   X-RateLimit-Remaining: 4

GET /api/login  (6th request within the window)
            →  429 Too Many Requests
               Retry-After: 60
               Content-Type: application/json

               { "error": "Too many requests.", "retryAfterSeconds": 60 }
```

Endpoints with **no matching rule** pass through unaffected.

---

## Project Structure

```
src/
  IronGate.Core/              # Pure logic — no HTTP dependency
    Abstractions/             #   IRateLimitAlgorithm, IRateLimitStore, IRateLimiterService
    Algorithms/               #   FixedWindowAlgorithm
    Models/                   #   RateLimitRule, RateLimitResult
    Storage/                  #   InMemoryRateLimitStore
    RateLimiterService.cs     #   Orchestrates store + algorithm

  IronGate.AspNetCore/        # Thin HTTP layer
    Middleware/               #   RateLimiterMiddleware
    Options/                  #   RateLimiterOptions (per-endpoint config)
    Identifiers/              #   IpClientIdentifier
    Extensions/               #   IServiceCollection.AddIronGate()
    IClientIdentifier.cs      #   Interface for client key resolution

samples/
  IronGate.Sample/            # Minimal ASP.NET Core app demonstrating usage

tests/
  IronGate.Tests/             # xUnit — 30 tests across all layers
```

---

## Customisation

### Custom algorithm

Implement `IRateLimitAlgorithm` and register it:

```csharp
public class SlidingWindowAlgorithm : IRateLimitAlgorithm
{
    public RateLimitResult Evaluate(int currentCount, RateLimitRule rule)
    {
        // your logic here
    }
}

// In DI setup:
services.AddSingleton<IRateLimitAlgorithm, SlidingWindowAlgorithm>();
```

### Custom storage (e.g. Redis)

Implement `IRateLimitStore`:

```csharp
public class RedisRateLimitStore : IRateLimitStore
{
    public Task<int> GetAsync(string key) { ... }
    public Task SetAsync(string key, int count, TimeSpan expiry) { ... }
}

// In DI setup:
services.AddSingleton<IRateLimitStore, RedisRateLimitStore>();
```

### Custom client identification (e.g. API key)

Implement `IClientIdentifier`:

```csharp
public class ApiKeyClientIdentifier : IClientIdentifier
{
    public string GetClientKey(HttpContext context) =>
        context.Request.Headers["X-Api-Key"].FirstOrDefault() ?? "anonymous";
}

// In DI setup:
services.AddSingleton<IClientIdentifier, ApiKeyClientIdentifier>();
```

When registering a custom implementation, call `AddIronGate()` first (which registers defaults), then override the specific service:

```csharp
builder.Services.AddIronGate(options => { ... });
builder.Services.AddSingleton<IClientIdentifier, ApiKeyClientIdentifier>(); // overrides default
```

---

## Running the Sample

```bash
cd samples/IronGate.Sample
dotnet run --urls http://localhost:5050
```

Then fire requests at:

```bash
curl http://localhost:5050/api/login
curl http://localhost:5050/api/products
```

---

## Running the Tests

```bash
dotnet test
```

All 30 tests should pass.

---

## Architecture Notes

- **Strategy pattern** — algorithms are interchangeable behind `IRateLimitAlgorithm`
- **Dependency Inversion** — middleware depends on `IRateLimiterService`, not the concrete class
- **Immutable models** — `RateLimitRule` and `RateLimitResult` have no public setters; `RateLimitResult` uses static factory methods (`Allow` / `Deny`) to prevent invalid states
- **Store key** — counters are namespaced as `"{endpoint}:{clientKey}"` so each endpoint tracks each client independently
- **Thread safety** — `InMemoryRateLimitStore` uses `ConcurrentDictionary`; note that the get+increment sequence is not atomic (TOCTOU), which is acceptable for the in-memory default but worth addressing in a distributed store
