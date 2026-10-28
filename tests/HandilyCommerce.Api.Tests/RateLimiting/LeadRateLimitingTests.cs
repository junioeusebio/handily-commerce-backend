using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.RateLimiting;
using HandilyCommerce.Api.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HandilyCommerce.Api.Tests.RateLimiting;

public class LeadRateLimitingTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string LeadsPath = "/api/v1/leads";

    private HttpClient CreateClient(int permitLimit = 2, bool enabled = true) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Default", "");
            builder.UseSetting("RateLimiting:Leads:Enabled", enabled.ToString());
            builder.UseSetting("RateLimiting:Leads:PermitLimit", permitLimit.ToString());
            builder.UseSetting("RateLimiting:Leads:WindowSeconds", "600");
        }).CreateClient();

    private static async Task<HttpResponseMessage> PostLeadAsync(HttpClient client, string ip)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, LeadsPath)
        {
            // Invalid body → 400 without touching the DB; still counts toward the limit.
            Content = JsonContent.Create(new { name = "", email = "" })
        };
        request.Headers.Add("X-Forwarded-For", ip);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task PostLeads_OverLimit_Returns429ProblemDetailsWithRetryAfter()
    {
        using var client = CreateClient(permitLimit: 2);

        using var first = await PostLeadAsync(client, "203.0.113.10");
        using var second = await PostLeadAsync(client, "203.0.113.10");
        using var third = await PostLeadAsync(client, "203.0.113.10");

        Assert.Equal(HttpStatusCode.BadRequest, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Equal((HttpStatusCode)429, third.StatusCode);
        Assert.Equal("application/problem+json", third.Content.Headers.ContentType?.MediaType);
        Assert.True(third.Headers.TryGetValues("Retry-After", out var values));
        Assert.True(int.Parse(values.Single()) > 0);

        using var body = JsonDocument.Parse(await third.Content.ReadAsStringAsync());
        Assert.Equal(429, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(LeadRateLimiting.Title, body.RootElement.GetProperty("title").GetString());
        Assert.Contains("Tente novamente", body.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task PostLeads_PartitionsByForwardedClientIp()
    {
        using var client = CreateClient(permitLimit: 1);

        using var a1 = await PostLeadAsync(client, "198.51.100.1");
        using var a2 = await PostLeadAsync(client, "198.51.100.1");
        using var b1 = await PostLeadAsync(client, "198.51.100.2");

        Assert.Equal(HttpStatusCode.BadRequest, a1.StatusCode);
        Assert.Equal((HttpStatusCode)429, a2.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, b1.StatusCode);
    }

    [Fact]
    public async Task PostLeads_WhenDisabled_NeverReturns429()
    {
        using var client = CreateClient(permitLimit: 1, enabled: false);

        for (var i = 0; i < 3; i++)
        {
            using var response = await PostLeadAsync(client, "192.0.2.5");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact]
    public async Task OtherEndpoints_AreNotRateLimited()
    {
        using var client = CreateClient(permitLimit: 1);

        for (var i = 0; i < 4; i++)
        {
            using var ping = await client.GetAsync("/api/v1/ping");
            using var version = await client.GetAsync("/api/v1/apiVersion");
            Assert.Equal(HttpStatusCode.OK, ping.StatusCode);
            Assert.Equal(HttpStatusCode.OK, version.StatusCode);
        }
    }

    [Fact]
    public void PartitionKey_WithoutRemoteIp_IsUnknown()
    {
        Assert.Equal("unknown", LeadRateLimiting.PartitionKey(new DefaultHttpContext()));
    }

    [Fact]
    public void RetryAfterSeconds_WithoutMetadata_UsesFallback()
    {
        using var lease = new FakeLease();
        Assert.Equal(600, LeadRateLimiting.RetryAfterSeconds(lease, TimeSpan.FromMinutes(10)));
    }

    [Fact]
    public void Options_DefaultsAndWindowFloor()
    {
        var options = new LeadRateLimitOptions();
        Assert.True(options.Enabled);
        Assert.Equal(5, options.PermitLimit);
        Assert.Equal(TimeSpan.FromMinutes(10), options.Window);
        Assert.Equal(10, options.SegmentsPerWindow);
        options.WindowSeconds = 0;
        Assert.Equal(TimeSpan.FromSeconds(1), options.Window);
    }

    private sealed class FakeLease : RateLimitLease
    {
        public override bool IsAcquired => false;
        public override IEnumerable<string> MetadataNames => [];
        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
    }
}
