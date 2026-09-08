using HandilyCommerce.Application.Ping;
using HandilyCommerce.Domain.Ping;

namespace HandilyCommerce.Application.Tests.Ping;

public class PingServiceTests
{
    [Fact]
    public void GetPing_ReturnsOkServicePingWithGivenApiVersion()
    {
        var sut = new PingService();

        var result = sut.GetPing("v1");

        Assert.Equal(ServicePing.Ok, result.Status);
        Assert.Equal(ServicePing.ServiceName, result.Service);
        Assert.Equal("v1", result.ApiVersion);
    }

    [Fact]
    public void GetPing_PropagatesConfiguredApiVersion()
    {
        var sut = new PingService();

        var result = sut.GetPing("v3");

        Assert.Equal("v3", result.ApiVersion);
    }

    [Fact]
    public void PingService_ImplementsIPingPort()
    {
        IPingPort port = new PingService();

        Assert.NotNull(port.GetPing("v1"));
    }
}
