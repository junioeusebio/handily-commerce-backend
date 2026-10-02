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

    [Theory]
    [InlineData("https://logs-prod-024.grafana.net", "https://logs-prod-024.grafana.net")]
    [InlineData("https://logs-prod-024.grafana.net/", "https://logs-prod-024.grafana.net")]
    [InlineData("  https://logs-prod-024.grafana.net/loki/api/v1/push  ", "https://logs-prod-024.grafana.net")]
    [InlineData("https://logs-prod-024.grafana.net/loki/api/v1/push/", "https://logs-prod-024.grafana.net")]
    [InlineData("https://gateway.example.com/loki", "https://gateway.example.com/loki")]
    public void NormalizeUrl_TrimsSlashAndPushPath(string url, string expected)
    {
        Assert.Equal(expected, GrafanaLokiOptions.NormalizeUrl(url));
    }

    [Fact]
    public void LoginAndPassword_AreTrimmed()
    {
        var options = new GrafanaLokiOptions { Url = "https://logs-prod-024.grafana.net", Username = " 123456\n", ApiToken = "glc_abc\n" };

        Assert.Equal("123456", options.Login);
        Assert.Equal("glc_abc", options.Password);
        Assert.True(options.IsConfigured);
    }

    [Fact]
    public void SafeSummary_MasksUsernameAndNeverIncludesToken()
    {
        var options = new GrafanaLokiOptions
        {
            Url = "https://logs-prod-024.grafana.net/loki/api/v1/push",
            Username = "1234567",
            ApiToken = "glc_super_secret_token"
        };

        var summary = options.SafeSummary();

        Assert.Equal("host=logs-prod-024.grafana.net user=12***** (7 chars) token=set", summary);
        Assert.DoesNotContain("secret", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("1234567", summary, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, "missing")]
    [InlineData("123", "*** (3 chars)")]
    [InlineData("1852949", "18***** (7 chars)")]
    public void MaskUsername_ShowsOnlyPrefixAndLength(string? username, string expected)
    {
        Assert.Equal(expected, GrafanaLokiOptions.MaskUsername(username));
    }

    [Fact]
    public void ConfigurationWarnings_ValidLokiConfig_IsEmpty()
    {
        var options = new GrafanaLokiOptions { Url = "https://logs-prod-024.grafana.net", Username = "123456", ApiToken = "glc_x" };

        Assert.Empty(options.ConfigurationWarnings());
    }

    [Theory]
    [InlineData("https://otlp-gateway-prod-sa-east-1.grafana.net/otlp", "123456", "glc_x", "OTLP")]
    [InlineData("https://fondbanana2441.grafana.net", "123456", "glc_x", "not a Loki endpoint")]
    [InlineData("https://logs-prod-024.grafana.net", "user@example.com", "glc_x", "numeric Loki user")]
    [InlineData("https://logs-prod-024.grafana.net", "123456", "eyJrIjoi", "glc_")]
    public void ConfigurationWarnings_FlagsCommonMistakes(string url, string user, string token, string expected)
    {
        var options = new GrafanaLokiOptions { Url = url, Username = user, ApiToken = token };

        var warning = Assert.Single(options.ConfigurationWarnings());
        Assert.Contains(expected, warning, StringComparison.Ordinal);
        Assert.DoesNotContain(token, warning, StringComparison.Ordinal);
    }
}
