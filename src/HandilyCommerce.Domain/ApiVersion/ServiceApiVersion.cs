namespace HandilyCommerce.Domain.ApiVersion;

/// <summary>
/// Domain model for the public API version probe (FE footer). Free of ASP.NET / infrastructure types.
/// </summary>
public sealed record ServiceApiVersion(string Version, string Service)
{
    public const string ServiceName = "handily-commerce-backend";

    public static ServiceApiVersion Create(string version) =>
        new(version, ServiceName);
}
