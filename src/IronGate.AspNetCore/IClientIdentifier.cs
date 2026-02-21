namespace IronGate.AspNetCore;

/// <summary>
/// Resolves a unique string key that identifies the client making the request.
/// Implementations can identify by IP address, user ID, API key, or any custom strategy.
/// </summary>
public interface IClientIdentifier
{
    /// <summary>
    /// Extracts a unique key for the client from the current HTTP context.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A non-empty string identifying the client (e.g. "192.168.1.1", "user-42").</returns>
    string GetClientKey(HttpContext context);
}
