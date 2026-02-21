namespace IronGate.AspNetCore.Identifiers;

/// <summary>
/// Identifies clients by their IP address.
/// Uses X-Forwarded-For header when present (e.g. behind a reverse proxy),
/// falling back to the direct connection IP.
/// </summary>
public sealed class IpClientIdentifier : IClientIdentifier
{
    /// <inheritdoc/>
    public string GetClientKey(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
