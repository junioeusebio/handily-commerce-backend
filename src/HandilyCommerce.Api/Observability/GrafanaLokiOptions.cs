using Microsoft.Extensions.Configuration;

namespace HandilyCommerce.Api.Observability;

/// <summary>
/// Optional Grafana Cloud Loki sink (<c>GrafanaLoki</c> section / env <c>GrafanaLoki__Url</c>,
/// <c>GrafanaLoki__Username</c>, <c>GrafanaLoki__ApiToken</c>). Enabled only when all three are set,
/// so local runs and CI log to the console only.
/// </summary>
public sealed class GrafanaLokiOptions
{
    public const string SectionName = "GrafanaLoki";

    /// <summary>Loki base URL, e.g. <c>https://logs-prod-024.grafana.net</c> (push path is appended by the sink).</summary>
    public string? Url { get; set; }

    /// <summary>Grafana Cloud Loki user (numeric instance ID).</summary>
    public string? Username { get; set; }

    /// <summary>Grafana Cloud access policy token with <c>logs:write</c>. Never logged.</summary>
    public string? ApiToken { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(ApiToken)
        && Uri.TryCreate(Url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;

    public static GrafanaLokiOptions FromConfiguration(IConfiguration configuration) =>
        configuration.GetSection(SectionName).Get<GrafanaLokiOptions>() ?? new GrafanaLokiOptions();
}
