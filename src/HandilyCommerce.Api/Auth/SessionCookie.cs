using HandilyCommerce.Domain.Auth;

namespace HandilyCommerce.Api.Auth;

/// <summary>
/// Refresh cookie (HttpOnly; Secure; SameSite=None — FE and API are cross-site) and the CSRF header check
/// required on refresh/logout.
/// </summary>
public static class SessionCookie
{
    public const string Name = "hc_refresh";
    public const string CsrfHeader = "X-Requested-With";
    public const string CsrfValue = "handily";

    public static void Write(HttpResponse response, string path, IssuedRefreshToken token) =>
        response.Cookies.Append(Name, token.Token, Options(path, token.ExpiresAt));

    public static void Clear(HttpResponse response, string path) =>
        response.Cookies.Delete(Name, Options(path, null));

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static bool HasCsrfHeader(HttpRequest request) =>
        string.Equals(request.Headers[CsrfHeader], CsrfValue, StringComparison.Ordinal);

    private static CookieOptions Options(string path, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = path,
        Expires = expires,
        IsEssential = true,
    };
}
