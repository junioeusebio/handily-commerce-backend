namespace HandilyCommerce.Domain.Health;

/// <summary>
/// Domain model for application health. Free of ASP.NET / infrastructure types.
/// </summary>
public sealed record ServiceHealth(string Status, string ServiceName)
{
    public const string Healthy = "Healthy";
    public const string Service = "handily-commerce-backend";

    public static ServiceHealth CreateHealthy() => new(Healthy, Service);
}
