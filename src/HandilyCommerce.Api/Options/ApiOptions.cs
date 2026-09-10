namespace HandilyCommerce.Api.Options;

/// <summary>
/// API route and OpenAPI metadata driven by configuration (<c>Api</c> section).
/// Change <see cref="Version"/> in appsettings to bump the public route prefix without code edits.
/// </summary>
public sealed class ApiOptions
{
    public const string SectionName = "Api";

    /// <summary>URL segment before the version (default: api).</summary>
    public string RoutePrefix { get; set; } = "api";

    /// <summary>HTTP route version segment (default: v1). Also used as OpenAPI Info.Version. Not product Versioning.</summary>
    public string Version { get; set; } = "v1";

    /// <summary>OpenAPI / Swagger document title.</summary>
    public string Title { get; set; } = "Handily Commerce API";

    /// <summary>
    /// Optional public base URL for OpenAPI <c>servers</c> (e.g. https://handily-commerce-backend.onrender.com).
    /// When unset, inferred from the current request (with https forced for onrender.com).
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// Builds an absolute route path under <c>/{RoutePrefix}/{Version}/...</c>.
    /// </summary>
    public string Path(params string[] segments)
    {
        var parts = new List<string>
        {
            RoutePrefix.Trim('/'),
            Version.Trim('/')
        };

        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                continue;
            }

            parts.Add(segment.Trim('/'));
        }

        return "/" + string.Join('/', parts);
    }
}
