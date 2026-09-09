using HandilyCommerce.Domain.ApiVersion;

namespace HandilyCommerce.Domain.Tests.ApiVersion;

public class ServiceApiVersionTests
{
    [Fact]
    public void Create_ReturnsServiceNameProductVersionAndRouteVersion()
    {
        var result = ServiceApiVersion.Create("0.1.0", "v1");

        Assert.Equal("0.1.0", result.Version);
        Assert.Equal("v1", result.ApiRouteVersion);
        Assert.Equal(ServiceApiVersion.ServiceName, result.Service);
        Assert.Equal("handily-commerce-backend", result.Service);
    }

    [Fact]
    public void Create_UsesProvidedVersions_NotHardcoded()
    {
        var result = ServiceApiVersion.Create("2.3.4", "v2");

        Assert.Equal("2.3.4", result.Version);
        Assert.Equal("v2", result.ApiRouteVersion);
        Assert.Equal(ServiceApiVersion.ServiceName, result.Service);
    }

    [Fact]
    public void Record_SupportsEqualityByValue()
    {
        var a = new ServiceApiVersion("0.1.0", ServiceApiVersion.ServiceName, "v1");
        var b = ServiceApiVersion.Create("0.1.0", "v1");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Record_WithDifferentProductVersion_IsNotEqual()
    {
        var v1 = ServiceApiVersion.Create("0.1.0", "v1");
        var v2 = ServiceApiVersion.Create("0.2.0", "v1");

        Assert.NotEqual(v1, v2);
    }

    [Fact]
    public void Record_WithDifferentRouteVersion_IsNotEqual()
    {
        var a = ServiceApiVersion.Create("0.1.0", "v1");
        var b = ServiceApiVersion.Create("0.1.0", "v2");

        Assert.NotEqual(a, b);
    }
}
