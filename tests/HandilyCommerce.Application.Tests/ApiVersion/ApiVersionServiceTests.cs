using HandilyCommerce.Application.ApiVersion;
using HandilyCommerce.Domain.ApiVersion;

namespace HandilyCommerce.Application.Tests.ApiVersion;

public class ApiVersionServiceTests
{
    [Fact]
    public void GetApiVersion_ReturnsServiceApiVersionWithGivenVersions()
    {
        var sut = new ApiVersionService();

        var result = sut.GetApiVersion("0.1.0", "v1");

        Assert.Equal("0.1.0", result.Version);
        Assert.Equal("v1", result.ApiRouteVersion);
        Assert.Equal(ServiceApiVersion.ServiceName, result.Service);
    }

    [Fact]
    public void GetApiVersion_PropagatesConfiguredVersions()
    {
        var sut = new ApiVersionService();

        var result = sut.GetApiVersion("9.8.7", "v3");

        Assert.Equal("9.8.7", result.Version);
        Assert.Equal("v3", result.ApiRouteVersion);
    }

    [Fact]
    public void ApiVersionService_ImplementsIApiVersionPort()
    {
        ApiVersionService service = new();

        Assert.IsAssignableFrom<IApiVersionPort>(service);
        Assert.NotNull(service.GetApiVersion("0.1.0", "v1"));
    }
}
