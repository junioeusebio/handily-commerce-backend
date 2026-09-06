namespace HandilyCommerce.Domain.Health;

/// <summary>
/// Driven port: query current application health (implemented in Application).
/// </summary>
public interface IHealthPort
{
    ServiceHealth GetHealth();
}
