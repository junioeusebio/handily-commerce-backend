namespace HandilyCommerce.Domain.Ping;

/// <summary>
/// Domain model for a readable ping/version probe. Free of ASP.NET / infrastructure types.
/// </summary>
public sealed record ServicePing(string Service, string ApiVersion, string Status)
{
    public const string Ok = "ok";
    public const string ServiceName = "handily-commerce-backend";

    public static ServicePing CreateOk(string apiVersion) =>
        new(ServiceName, apiVersion, Ok);
}
