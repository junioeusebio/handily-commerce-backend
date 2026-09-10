namespace HandilyCommerce.Api.OpenApi;

/// <summary>
/// Resolves the public base URL for OpenAPI <c>servers</c> (Scalar Try-it / OpenAPI clients).
/// Render terminates TLS at the edge, so the app often sees <c>http</c> — never emit http for onrender.com.
/// </summary>
public static class OpenApiPublicUrl
{
    public const string RenderHost = "handily-commerce-backend.onrender.com";

    /// <summary>
    /// Prefer explicit <paramref name="configuredPublicBaseUrl"/>, else request host/scheme
    /// (honoring forwarded headers when configured), forcing https for *.onrender.com.
    /// </summary>
    public static string? Resolve(string? configuredPublicBaseUrl, string? requestScheme, HostString? requestHost)
    {
        if (!string.IsNullOrWhiteSpace(configuredPublicBaseUrl))
        {
            return EnsureHttpsForOnRender(configuredPublicBaseUrl.Trim().TrimEnd('/'));
        }

        if (requestHost is not { HasValue: true } host || string.IsNullOrWhiteSpace(host.Value))
        {
            return null;
        }

        var scheme = string.IsNullOrWhiteSpace(requestScheme) ? "https" : requestScheme;
        if (IsOnRenderHost(host.Value))
        {
            scheme = "https";
        }

        return $"{scheme}://{host.Value}";
    }

    /// <summary>
    /// Rewrites http → https for onrender.com hosts; leaves other URLs unchanged (trim trailing slash).
    /// </summary>
    public static string EnsureHttpsForOnRender(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url.TrimEnd('/');
        }

        if (IsOnRenderHost(uri.Host) && uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            var builder = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 };
            return builder.Uri.GetLeftPart(UriPartial.Authority);
        }

        return url.TrimEnd('/');
    }

    public static bool IsOnRenderHost(string host) =>
        host.Equals(RenderHost, StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".onrender.com", StringComparison.OrdinalIgnoreCase);
}
