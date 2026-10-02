using HandilyCommerce.Api.Observability;
using Microsoft.Extensions.Configuration;

namespace HandilyCommerce.Api.Tests.Observability;

public class GrafanaLokiOptionsTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void FromConfiguration_WhenSectionMissing_IsNotConfigured()
    {
        var options = GrafanaLokiOptions.FromConfiguration(Config());

        Assert.False(options.IsConfigured);
    }

    [Fact]
    public void FromConfiguration_WhenAllValuesSet_IsConfigured()
    {
        var options = GrafanaLokiOptions.FromConfiguration(Config(
            ("GrafanaLoki:Url", "https://logs-prod-024.grafana.net"),
            ("GrafanaLoki:Username", "123456"),
            ("GrafanaLoki:ApiToken", "glc_token")));

        Assert.True(options.IsConfigured);
        Assert.Equal("123456", options.Username);
    }

    [Theory]
    [InlineData(null, "123456", "glc_token")]
    [InlineData("https://logs.grafana.net", "", "glc_token")]
    [InlineData("https://logs.grafana.net", "123456", " ")]
    [InlineData("not-a-url", "123456", "glc_token")]
    [InlineData("http://logs.grafana.net", "123456", "glc_token")]
    public void IsConfigured_RequiresHttpsUrlUserAndToken(string? url, string? user, string? token)
    {
        var options = new GrafanaLokiOptions { Url = url, Username = user, ApiToken = token };

        Assert.False(options.IsConfigured);
    }
}
