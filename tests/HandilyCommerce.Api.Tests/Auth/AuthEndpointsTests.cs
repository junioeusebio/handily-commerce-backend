using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HandilyCommerce.Infrastructure.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HandilyCommerce.Api.Tests.Auth;

public class AuthEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Email = "admin@ajksys.test";
    private const string Password = "senha-de-teste";
    private static readonly string Hash = Pbkdf2PasswordVerifier.Hash(Password, iterations: 1000);

    private HttpClient CreateClient(bool configured = true) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Default", "");
            if (configured)
            {
                builder.UseSetting("Auth:AdminEmail", Email);
                builder.UseSetting("Auth:AdminPasswordHash", Hash);
                builder.UseSetting("Auth:JwtKey", "chave-de-teste-com-pelo-menos-32-bytes!!");
            }
        }).CreateClient();

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ThenMe_ReturnsEmail()
    {
        using var client = CreateClient();

        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = body.RootElement.GetProperty("accessToken").GetString();
        Assert.True(body.RootElement.TryGetProperty("expiresAt", out _));

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var me = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Contains(Email, await me.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401ProblemDetails()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email, password = "errada" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_WithEmptyBody_Returns400()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenNotConfigured_Returns401AndPingStaysPublic()
    {
        using var client = CreateClient(configured: false);

        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email, password = Password });
        using var ping = await client.GetAsync("/api/v1/ping");

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ping.StatusCode);
    }

    [Fact]
    public async Task OpenApi_ShowsLoginAndBearerScheme()
    {
        using var client = CreateClient();

        var doc = await client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/v1/auth/login", doc);
        Assert.Contains("\"bearer\"", doc);
    }
}
