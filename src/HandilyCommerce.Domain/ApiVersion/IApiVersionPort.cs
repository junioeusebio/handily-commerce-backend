namespace HandilyCommerce.Domain.ApiVersion;

/// <summary>
/// Driven port: query product Versioning and service identity (implemented in Application).
/// </summary>
public interface IApiVersionPort
{
    /// <param name="productVersion">Product Versioning (e.g. from assembly / Directory.Build.props).</param>
    /// <param name="apiRouteVersion">HTTP route segment from configuration (e.g. Api:Version).</param>
    ServiceApiVersion GetApiVersion(string productVersion, string apiRouteVersion);
}
