using IronGate.AspNetCore.Middleware;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseMiddleware<RateLimiterMiddleware>();

app.MapGet("/", () => "Hello World!");
app.MapGet("/api/products", () => new[] { "Apple", "Banana", "Cherry" });

app.Run();
