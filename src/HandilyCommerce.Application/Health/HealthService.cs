using HandilyCommerce.Domain.Health;

namespace HandilyCommerce.Application.Health;

public sealed class HealthService : IHealthPort
{
    public ServiceHealth GetHealth() => ServiceHealth.CreateHealthy();
}
