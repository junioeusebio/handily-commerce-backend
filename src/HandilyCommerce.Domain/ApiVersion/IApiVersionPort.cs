namespace HandilyCommerce.Domain.ApiVersion;

/// <summary>
/// Driven port: query configured public API version and service identity (implemented in Application).
/// </summary>
public interface IApiVersionPort
{
    /// <param name="version">Public API version segment from configuration (e.g. Api:Version).</param>
    ServiceApiVersion GetApiVersion(string version);
}
