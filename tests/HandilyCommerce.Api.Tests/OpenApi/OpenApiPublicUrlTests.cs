using HandilyCommerce.Api.OpenApi;
using Microsoft.AspNetCore.Http;

namespace HandilyCommerce.Api.Tests.OpenApi;

public class OpenApiPublicUrlTests
{
    [Fact]
    public void Resolve_UsesConfiguredPublicBaseUrl_AndForcesHttpsForOnRender()
    {
        var result = OpenApiPublicUrl.Resolve(
            "http://handily-commerce-backend.onrender.com/",
            "http",
            new HostString("localhost:5000"));

        Assert.Equal("https://handily-commerce-backend.onrender.com", result);
    }

    [Fact]
    public void Resolve_InfersHttpsFromRequest_WhenOnRender()
    {
        var result = OpenApiPublicUrl.Resolve(
            null,
            "http",
            new HostString("handily-commerce-backend.onrender.com"));

        Assert.Equal("https://handily-commerce-backend.onrender.com", result);
    }

    [Fact]
    public void Resolve_KeepsLocalHttpScheme()
    {
        var result = OpenApiPublicUrl.Resolve(
            null,
            "http",
            new HostString("localhost:5000"));

        Assert.Equal("http://localhost:5000", result);
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenNoConfigAndNoHost()
    {
        Assert.Null(OpenApiPublicUrl.Resolve(null, "https", null));
        Assert.Null(OpenApiPublicUrl.Resolve("  ", "https", new HostString()));
    }

    [Fact]
    public void EnsureHttpsForOnRender_UpgradesHttpOnRenderOnly()
    {
        Assert.Equal(
            "https://handily-commerce-backend.onrender.com",
            OpenApiPublicUrl.EnsureHttpsForOnRender("http://handily-commerce-backend.onrender.com"));
        Assert.Equal(
            "http://localhost:5000",
            OpenApiPublicUrl.EnsureHttpsForOnRender("http://localhost:5000"));
        Assert.Equal(
            "https://handily-commerce-backend.onrender.com",
            OpenApiPublicUrl.EnsureHttpsForOnRender("https://handily-commerce-backend.onrender.com/"));
    }

    [Theory]
    [InlineData("handily-commerce-backend.onrender.com", true)]
    [InlineData("svc.onrender.com", true)]
    [InlineData("localhost", false)]
    public void IsOnRenderHost_DetectsOnRender(string host, bool expected)
    {
        Assert.Equal(expected, OpenApiPublicUrl.IsOnRenderHost(host));
    }
}
