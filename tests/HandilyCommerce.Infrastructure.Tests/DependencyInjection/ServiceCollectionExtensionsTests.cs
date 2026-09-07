using HandilyCommerce.Application.DependencyInjection;
using HandilyCommerce.Domain.Health;
using HandilyCommerce.Infrastructure.DependencyInjection;
using HandilyCommerce.Infrastructure.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace HandilyCommerce.Infrastructure.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddInfrastructure_RegistersApplicationHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        services.AddInfrastructure();

        using var provider = services.BuildServiceProvider();
        var healthCheck = ActivatorUtilities.CreateInstance<ApplicationHealthCheck>(provider);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal(ServiceHealth.Service, result.Data["service"]);

        var service = provider.GetRequiredService<HealthCheckService>();
        var report = await service.CheckHealthAsync();

        Assert.Contains(report.Entries, e => e.Key == "application");
        Assert.Equal(HealthStatus.Healthy, report.Entries["application"].Status);
    }

    [Fact]
    public void AddInfrastructure_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var returned = services.AddInfrastructure();

        Assert.Same(services, returned);
    }
}
