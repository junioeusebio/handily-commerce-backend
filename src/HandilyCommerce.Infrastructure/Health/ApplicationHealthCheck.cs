using HandilyCommerce.Domain.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HandilyCommerce.Infrastructure.Health;

/// <summary>
/// Outbound adapter: bridges domain health port to ASP.NET Core health checks.
/// </summary>
public sealed class ApplicationHealthCheck(IHealthPort healthPort) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var health = healthPort.GetHealth();
        var data = new Dictionary<string, object>
        {
            ["service"] = health.ServiceName
        };

        return Task.FromResult(
            health.Status == ServiceHealth.Healthy
                ? HealthCheckResult.Healthy(health.Status, data)
                : HealthCheckResult.Unhealthy(health.Status, data: data));
    }
}
