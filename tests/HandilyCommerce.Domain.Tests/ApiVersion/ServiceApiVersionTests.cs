using HandilyCommerce.Domain.ApiVersion;

namespace HandilyCommerce.Domain.Tests.ApiVersion;

public class ServiceApiVersionTests
{
    [Fact]
    public void Create_ReturnsServiceNameAndConfiguredVersion()
    {
        var result = ServiceApiVersion.Create("v1");

        Assert.Equal("v1", result.Version);
        Assert.Equal(ServiceApiVersion.ServiceName, result.Service);
        Assert.Equal("handily-commerce-backend", result.Service);
    }

    [Fact]
    public void Create_UsesProvidedVersion_NotHardcoded()
    {
        var result = ServiceApiVersion.Create("v2");

        Assert.Equal("v2", result.Version);
        Assert.Equal(ServiceApiVersion.ServiceName, result.Service);
    }

    [Fact]
    public void Record_SupportsEqualityByValue()
    {
        var a = new ServiceApiVersion("v1", ServiceApiVersion.ServiceName);
        var b = ServiceApiVersion.Create("v1");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Record_WithDifferentVersion_IsNotEqual()
    {
        var v1 = ServiceApiVersion.Create("v1");
        var v2 = ServiceApiVersion.Create("v2");

        Assert.NotEqual(v1, v2);
    }
}
