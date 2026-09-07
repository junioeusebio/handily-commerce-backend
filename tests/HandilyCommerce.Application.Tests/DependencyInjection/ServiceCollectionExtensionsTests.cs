using HandilyCommerce.Application.DependencyInjection;
using HandilyCommerce.Application.Health;
using HandilyCommerce.Domain.Health;
using Microsoft.Extensions.DependencyInjection;

namespace HandilyCommerce.Application.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApplication_RegistersHealthServiceAsIHealthPort()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        using var provider = services.BuildServiceProvider();
        var port = provider.GetRequiredService<IHealthPort>();

        Assert.IsType<HealthService>(port);
        Assert.Equal(ServiceHealth.Healthy, port.GetHealth().Status);
    }

    [Fact]
    public void AddApplication_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var returned = services.AddApplication();

        Assert.Same(services, returned);
    }
}
