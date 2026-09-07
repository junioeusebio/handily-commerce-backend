using HandilyCommerce.Domain.Health;
using HandilyCommerce.Infrastructure.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HandilyCommerce.Infrastructure.Tests.Health;

public class ApplicationHealthCheckTests
{
    private sealed class StubHealthPort(ServiceHealth health) : IHealthPort
    {
        public ServiceHealth GetHealth() => health;
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthy_ReturnsHealthyResultWithServiceData()
    {
        var port = new StubHealthPort(ServiceHealth.CreateHealthy());
        var sut = new ApplicationHealthCheck(port);
        var context = new HealthCheckContext();

        var result = await sut.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal(ServiceHealth.Healthy, result.Description);
        Assert.Equal(ServiceHealth.Service, result.Data["service"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUnhealthy_ReturnsUnhealthyResultWithServiceData()
    {
        var port = new StubHealthPort(new ServiceHealth("Unhealthy", "custom-service"));
        var sut = new ApplicationHealthCheck(port);
        var context = new HealthCheckContext();

        var result = await sut.CheckHealthAsync(context, CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Unhealthy", result.Description);
        Assert.Equal("custom-service", result.Data["service"]);
    }
}
