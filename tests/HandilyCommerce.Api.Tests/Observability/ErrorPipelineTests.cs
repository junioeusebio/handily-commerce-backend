using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HandilyCommerce.Api.Tests.Observability;

/// <summary>
/// Hosts the real API with an empty connection string: DB-backed endpoints fail and must answer
/// ProblemDetails 500 with a traceId (and keep CORS headers), while non-DB endpoints stay healthy.
/// </summary>
public class ErrorPipelineTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private HttpClient CreateClient() =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Default", "");
        }).CreateClient();

    [Fact]
    public async Task DbEndpoint_WhenDatabaseUnavailable_ReturnsProblemDetailsWithTraceIdAndCors()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/courses");
        request.Headers.Add("Origin", "https://junioeusebio.github.io");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"traceId\"", body);
        Assert.DoesNotContain("ConnectionString", body);
        Assert.Equal(
            "https://junioeusebio.github.io",
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Ping_StillReturnsOk()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/api/v1/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
