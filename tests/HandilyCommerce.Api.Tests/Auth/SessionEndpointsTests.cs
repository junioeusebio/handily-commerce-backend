using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HandilyCommerce.Api.Auth;
using HandilyCommerce.Infrastructure.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace HandilyCommerce.Api.Tests.Auth;

/// <summary>Refresh cookie session end to end: login cookie, rotation, reuse detection, logout, CSRF header, CORS.</summary>
public class SessionEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Email = "admin@ajksys.test";
    private const string Password = "senha-de-teste";
    private const string Origin = "https://junioeusebio.github.io";
    private static readonly string Hash = Pbkdf2PasswordVerifier.Hash(Password, iterations: 1000);

    private HttpClient CreateClient()
    {
        var database = Guid.NewGuid().ToString();
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Default", "");
            builder.UseSetting("Auth:AdminEmail", Email);
            builder.UseSetting("Auth:AdminPasswordHash", Hash);
            builder.UseSetting("Auth:JwtKey", "chave-de-teste-com-pelo-menos-32-bytes!!");
            builder.ConfigureTestServices(services => InMemoryDb.Use(services, database));
        }).CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    private static string? CookieFrom(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith(SessionCookie.Name + "=", StringComparison.Ordinal))
            : null;

    private static string? TokenOf(string? setCookie) =>
        setCookie?.Split(';')[0][(SessionCookie.Name.Length + 1)..];

    private static async Task<string> LoginAsync(HttpClient client)
    {
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return TokenOf(CookieFrom(login))!;
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string path, string? token, bool csrf = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (token is not null)
        {
            request.Headers.Add("Cookie", $"{SessionCookie.Name}={token}");
        }

        if (csrf)
        {
            request.Headers.Add(SessionCookie.CsrfHeader, SessionCookie.CsrfValue);
        }

        return client.SendAsync(request);
    }

    [Fact]
    public async Task Login_SetsHttpOnlySecureSameSiteNoneCookieOnAuthPath()
    {
        using var client = CreateClient();

        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email, password = Password });
        var cookie = CookieFrom(login)!.ToLowerInvariant();

        Assert.Contains("httponly", cookie);
        Assert.Contains("secure", cookie);
        Assert.Contains("samesite=none", cookie);
        Assert.Contains("path=/api/v1/auth", cookie);
        Assert.Contains("expires=", cookie);
    }

    [Fact]
    public async Task Refresh_RotatesCookieAndReturnsAccessToken()
    {
        using var client = CreateClient();
        var first = await LoginAsync(client);

        using var refresh = await PostAsync(client, "/api/v1/auth/refresh", first);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        using var body = JsonDocument.Parse(await refresh.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrEmpty(body.RootElement.GetProperty("accessToken").GetString()));
        Assert.True(body.RootElement.TryGetProperty("expiresAt", out _));
        var second = TokenOf(CookieFrom(refresh));
        Assert.False(string.IsNullOrEmpty(second));
        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task Refresh_ReusingRevokedToken_RevokesWholeChain()
    {
        using var client = CreateClient();
        var first = await LoginAsync(client);
        using var rotated = await PostAsync(client, "/api/v1/auth/refresh", first);
        var second = TokenOf(CookieFrom(rotated));

        using var reuse = await PostAsync(client, "/api/v1/auth/refresh", first);
        using var afterReuse = await PostAsync(client, "/api/v1/auth/refresh", second);

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("token-que-nao-existe")]
    public async Task Refresh_WithMissingOrUnknownCookie_Returns401(string? token)
    {
        using var client = CreateClient();

        using var response = await PostAsync(client, "/api/v1/auth/refresh", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/auth/refresh")]
    [InlineData("/api/v1/auth/logout")]
    public async Task WithoutCsrfHeader_Returns403(string path)
    {
        using var client = CreateClient();
        var token = await LoginAsync(client);

        using var response = await PostAsync(client, path, token, csrf: false);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTokenAndClearsCookie()
    {
        using var client = CreateClient();
        var token = await LoginAsync(client);

        using var logout = await PostAsync(client, "/api/v1/auth/logout", token);
        using var refresh = await PostAsync(client, "/api/v1/auth/refresh", token);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains("expires=thu, 01 jan 1970", CookieFrom(logout)!.ToLowerInvariant());
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Preflight_FromFrontend_AllowsCredentialsAndCsrfHeader()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/refresh");
        request.Headers.Add("Origin", Origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "x-requested-with");

        using var response = await client.SendAsync(request);

        Assert.Equal(Origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }
}
