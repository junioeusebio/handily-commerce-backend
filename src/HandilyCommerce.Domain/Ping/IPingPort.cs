namespace HandilyCommerce.Domain.Ping;

/// <summary>
/// Driven port: query service identity and configured API version (implemented in Application).
/// </summary>
public interface IPingPort
{
    /// <param name="apiVersion">Public API version segment from configuration (e.g. Api:Version).</param>
    ServicePing GetPing(string apiVersion);
}
