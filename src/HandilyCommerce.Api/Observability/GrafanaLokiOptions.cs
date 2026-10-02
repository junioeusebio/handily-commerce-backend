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

    /// <summary>Push path appended by the sink; stripped from <see cref="Url"/> if pasted with it.</summary>
    public const string PushPath = "/loki/api/v1/push";

    /// <summary>
    /// Loki base URL from Grafana Cloud → Loki → Details, e.g. <c>https://logs-prod-024.grafana.net</c>.
    /// The sink appends <see cref="PushPath"/>; use <see cref="BaseUrl"/> (normalized) when configuring it.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>Grafana Cloud Loki user (numeric instance ID from Loki → Details, not the OTLP/Prometheus one).</summary>
    public string? Username { get; set; }

    /// <summary>Grafana Cloud access policy token with <c>logs:write</c>. Never logged.</summary>
    public string? ApiToken { get; set; }

    /// <summary>Normalized base URL: trimmed, no trailing slash, no <see cref="PushPath"/> suffix.</summary>
    public string? BaseUrl => NormalizeUrl(Url);

    /// <summary>Username without surrounding whitespace (env values pasted with a newline break basic auth).</summary>
    public string? Login => Username?.Trim();

    /// <summary>Token without surrounding whitespace. Never logged.</summary>
    public string? Password => ApiToken?.Trim();

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(Login)
        && !string.IsNullOrWhiteSpace(Password)
        && Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;

    public static GrafanaLokiOptions FromConfiguration(IConfiguration configuration) =>
        configuration.GetSection(SectionName).Get<GrafanaLokiOptions>() ?? new GrafanaLokiOptions();

    public static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        var value = url.Trim().TrimEnd('/');
        if (value.EndsWith(PushPath, StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^PushPath.Length].TrimEnd('/');
        }

        return value;
    }

    /// <summary>
    /// Safe one-line summary for startup logs: Loki host/path and a masked username. Never includes the token.
    /// </summary>
    public string SafeSummary()
    {
        var target = Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri)
            ? $"{uri.Authority}{(uri.AbsolutePath == "/" ? string.Empty : uri.AbsolutePath)}"
            : "(invalid url)";
        return $"host={target} user={MaskUsername(Login)} token={(string.IsNullOrEmpty(Password) ? "missing" : "set")}";
    }

    /// <summary>Common misconfigurations (no secret values), logged as warnings at startup.</summary>
    public IReadOnlyList<string> ConfigurationWarnings()
    {
        var warnings = new List<string>();
        if (Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri))
        {
            var host = uri.Host;
            if (host.Contains("otlp", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add("GrafanaLoki__Url looks like an OTLP endpoint; use the Loki URL (logs-prod-….grafana.net) from Grafana Cloud → Loki → Details");
            }
            else if (host.EndsWith(".grafana.net", StringComparison.OrdinalIgnoreCase)
                && !host.StartsWith("logs-", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add("GrafanaLoki__Url is not a Loki endpoint (expected https://logs-prod-….grafana.net, not the stack URL)");
            }
        }

        if (!string.IsNullOrEmpty(Login) && !Login.All(char.IsAsciiDigit))
        {
            warnings.Add("GrafanaLoki__Username should be the numeric Loki user id from Grafana Cloud → Loki → Details");
        }

        if (!string.IsNullOrEmpty(Password) && !Password.StartsWith("glc_", StringComparison.Ordinal))
        {
            warnings.Add("GrafanaLoki__ApiToken does not look like a Grafana Cloud access policy token (glc_…)");
        }

        return warnings;
    }

    /// <summary>First two characters + length only, e.g. <c>18***** (7 chars)</c>.</summary>
    public static string MaskUsername(string? username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return "missing";
        }

        var visible = username.Length > 4 ? username[..2] : string.Empty;
        return $"{visible}{new string('*', username.Length - visible.Length)} ({username.Length} chars)";
    }
}
