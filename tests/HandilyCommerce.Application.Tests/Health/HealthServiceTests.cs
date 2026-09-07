using HandilyCommerce.Application.Health;
using HandilyCommerce.Domain.Health;

namespace HandilyCommerce.Application.Tests.Health;

public class HealthServiceTests
{
    [Fact]
    public void GetHealth_ReturnsHealthyServiceHealth()
    {
        var sut = new HealthService();

        var result = sut.GetHealth();

        Assert.Equal(ServiceHealth.Healthy, result.Status);
        Assert.Equal(ServiceHealth.Service, result.ServiceName);
    }

    [Fact]
    public void HealthService_ImplementsIHealthPort()
    {
        IHealthPort port = new HealthService();

        Assert.NotNull(port.GetHealth());
    }
}
