using HandilyCommerce.Application.ApiVersion;
using HandilyCommerce.Domain.ApiVersion;

namespace HandilyCommerce.Application.Tests.ApiVersion;

public class ApiVersionServiceTests
{
    [Fact]
    public void GetApiVersion_ReturnsServiceApiVersionWithGivenVersion()
    {
        var sut = new ApiVersionService();

        var result = sut.GetApiVersion("v1");

        Assert.Equal("v1", result.Version);
        Assert.Equal(ServiceApiVersion.ServiceName, result.Service);
    }

    [Fact]
    public void GetApiVersion_PropagatesConfiguredVersion()
    {
        var sut = new ApiVersionService();

        var result = sut.GetApiVersion("v3");

        Assert.Equal("v3", result.Version);
    }

    [Fact]
    public void ApiVersionService_ImplementsIApiVersionPort()
    {
        IApiVersionPort port = new ApiVersionService();

        Assert.NotNull(port.GetApiVersion("v1"));
    }
}
