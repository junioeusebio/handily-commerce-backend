using HandilyCommerce.Application.DependencyInjection;
using HandilyCommerce.Domain.Changelog;
using HandilyCommerce.Domain.Health;
using HandilyCommerce.Infrastructure.Changelog;
using HandilyCommerce.Infrastructure.DependencyInjection;
using HandilyCommerce.Infrastructure.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HandilyCommerce.Infrastructure.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    private static IConfiguration DummyConnectionConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Placeholder only — CI never opens a real Supabase connection.
                ["ConnectionStrings:Default"] = "Host=127.0.0.1;Database=handily_ci;Username=postgres;Password=unused"
            })
            .Build();

    [Fact]
    public async Task AddInfrastructure_RegistersApplicationHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        services.AddInfrastructure(DummyConnectionConfiguration());

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
    public void AddInfrastructure_RegistersEfChangelogRepositoryAsIChangelogRepository()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        services.AddInfrastructure(DummyConnectionConfiguration());

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IChangelogRepository));
        Assert.Equal(typeof(EfChangelogRepository), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var returned = services.AddInfrastructure(DummyConnectionConfiguration());

        Assert.Same(services, returned);
    }
}
