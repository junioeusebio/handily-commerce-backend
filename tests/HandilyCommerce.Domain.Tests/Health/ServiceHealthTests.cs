using HandilyCommerce.Domain.Health;

namespace HandilyCommerce.Domain.Tests.Health;

public class ServiceHealthTests
{
    [Fact]
    public void CreateHealthy_ReturnsHealthyStatusAndServiceName()
    {
        var health = ServiceHealth.CreateHealthy();

        Assert.Equal(ServiceHealth.Healthy, health.Status);
        Assert.Equal(ServiceHealth.Service, health.ServiceName);
        Assert.Equal("Healthy", health.Status);
        Assert.Equal("handily-commerce-backend", health.ServiceName);
    }

    [Fact]
    public void Record_SupportsEqualityByValue()
    {
        var a = new ServiceHealth(ServiceHealth.Healthy, ServiceHealth.Service);
        var b = ServiceHealth.CreateHealthy();

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Record_WithDifferentStatus_IsNotEqual()
    {
        var healthy = ServiceHealth.CreateHealthy();
        var unhealthy = new ServiceHealth("Unhealthy", ServiceHealth.Service);

        Assert.NotEqual(healthy, unhealthy);
    }
}
