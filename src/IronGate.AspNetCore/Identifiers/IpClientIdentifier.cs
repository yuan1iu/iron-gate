namespace IronGate.AspNetCore.Identifiers;

/// <summary>
/// Identifies clients by their IP address.
/// </summary>
public class IpClientIdentifier : IClientIdentifier
{
    /// <inheritdoc />
    public string GetClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
