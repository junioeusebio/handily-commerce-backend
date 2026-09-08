using HandilyCommerce.Application.ApiVersion;
using HandilyCommerce.Application.DependencyInjection;
using HandilyCommerce.Application.Health;
using HandilyCommerce.Application.Ping;
using HandilyCommerce.Domain.ApiVersion;
using HandilyCommerce.Domain.Health;
using HandilyCommerce.Domain.Ping;
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
    public void AddApplication_RegistersPingServiceAsIPingPort()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        using var provider = services.BuildServiceProvider();
        var port = provider.GetRequiredService<IPingPort>();

        Assert.IsType<PingService>(port);
        var ping = port.GetPing("v1");
        Assert.Equal(ServicePing.Ok, ping.Status);
        Assert.Equal("v1", ping.ApiVersion);
        Assert.Equal(ServicePing.ServiceName, ping.Service);
    }

    [Fact]
    public void AddApplication_RegistersApiVersionServiceAsIApiVersionPort()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        using var provider = services.BuildServiceProvider();
        var port = provider.GetRequiredService<IApiVersionPort>();

        Assert.IsType<ApiVersionService>(port);
        var apiVersion = port.GetApiVersion("v1");
        Assert.Equal("v1", apiVersion.Version);
        Assert.Equal(ServiceApiVersion.ServiceName, apiVersion.Service);
    }

    [Fact]
    public void AddApplication_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var returned = services.AddApplication();

        Assert.Same(services, returned);
    }
}
