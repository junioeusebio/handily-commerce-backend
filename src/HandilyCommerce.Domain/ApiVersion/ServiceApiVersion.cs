namespace HandilyCommerce.Domain.ApiVersion;

/// <summary>
/// Domain model for the public product-version probe (FE footer). Free of ASP.NET / infrastructure types.
/// <see cref="Version"/> is product Versioning; <see cref="ApiRouteVersion"/> is the HTTP route segment (e.g. v1).
/// </summary>
public sealed record ServiceApiVersion(string Version, string Service, string ApiRouteVersion)
{
    public const string ServiceName = "handily-commerce-backend";

    public static ServiceApiVersion Create(string productVersion, string apiRouteVersion) =>
        new(productVersion, ServiceName, apiRouteVersion);
}
