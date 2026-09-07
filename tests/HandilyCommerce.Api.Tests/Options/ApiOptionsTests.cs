using HandilyCommerce.Api.Options;

namespace HandilyCommerce.Api.Tests.Options;

public class ApiOptionsTests
{
    [Fact]
    public void Defaults_MatchExpectedValues()
    {
        var options = new ApiOptions();

        Assert.Equal("Api", ApiOptions.SectionName);
        Assert.Equal("api", options.RoutePrefix);
        Assert.Equal("v1", options.Version);
        Assert.Equal("Handily Commerce API", options.Title);
    }

    [Fact]
    public void Path_WithSingleSegment_BuildsVersionedRoute()
    {
        var options = new ApiOptions();

        Assert.Equal("/api/v1/health", options.Path("health"));
    }

    [Fact]
    public void Path_WithMultipleSegments_JoinsAllParts()
    {
        var options = new ApiOptions
        {
            RoutePrefix = "api",
            Version = "v2"
        };

        Assert.Equal("/api/v2/orders/123", options.Path("orders", "123"));
    }

    [Fact]
    public void Path_TrimsSlashesFromSegments()
    {
        var options = new ApiOptions
        {
            RoutePrefix = "/api/",
            Version = "/v1/"
        };

        Assert.Equal("/api/v1/health", options.Path("/health/"));
    }

    [Fact]
    public void Path_SkipsNullEmptyAndWhitespaceSegments()
    {
        var options = new ApiOptions();

        Assert.Equal("/api/v1/health", options.Path(" ", null!, "", "health"));
    }

    [Fact]
    public void Path_WithNoSegments_ReturnsPrefixAndVersionOnly()
    {
        var options = new ApiOptions();

        Assert.Equal("/api/v1", options.Path());
    }

    [Fact]
    public void Properties_CanBeOverridden()
    {
        var options = new ApiOptions
        {
            RoutePrefix = "gateway",
            Version = "v3",
            Title = "Custom Title"
        };

        Assert.Equal("gateway", options.RoutePrefix);
        Assert.Equal("v3", options.Version);
        Assert.Equal("Custom Title", options.Title);
        Assert.Equal("/gateway/v3/ping", options.Path("ping"));
    }
}
