using HandilyCommerce.Domain.Ping;

namespace HandilyCommerce.Domain.Tests.Ping;

public class ServicePingTests
{
    [Fact]
    public void CreateOk_ReturnsOkStatusServiceNameAndApiVersion()
    {
        var ping = ServicePing.CreateOk("v1");

        Assert.Equal(ServicePing.Ok, ping.Status);
        Assert.Equal(ServicePing.ServiceName, ping.Service);
        Assert.Equal("ok", ping.Status);
        Assert.Equal("handily-commerce-backend", ping.Service);
        Assert.Equal("v1", ping.ApiVersion);
    }

    [Fact]
    public void CreateOk_UsesProvidedApiVersion_NotHardcoded()
    {
        var ping = ServicePing.CreateOk("v2");

        Assert.Equal("v2", ping.ApiVersion);
        Assert.Equal(ServicePing.Ok, ping.Status);
    }

    [Fact]
    public void Record_SupportsEqualityByValue()
    {
        var a = new ServicePing(ServicePing.ServiceName, "v1", ServicePing.Ok);
        var b = ServicePing.CreateOk("v1");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Record_WithDifferentApiVersion_IsNotEqual()
    {
        var v1 = ServicePing.CreateOk("v1");
        var v2 = ServicePing.CreateOk("v2");

        Assert.NotEqual(v1, v2);
    }
}
