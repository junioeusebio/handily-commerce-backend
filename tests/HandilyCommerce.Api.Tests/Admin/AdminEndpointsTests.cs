using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Infrastructure.Auth;
using HandilyCommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HandilyCommerce.Api.Tests.Admin;

/// <summary>Admin routes end to end on EF InMemory: 401 without token, CRUD with a real login token.</summary>
public class AdminEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Email = "admin@ajksys.test";
    private const string Password = "senha-de-teste";
    private const string Origin = "https://junioeusebio.github.io";
    private static readonly string Hash = Pbkdf2PasswordVerifier.Hash(Password, iterations: 1000);

    private (HttpClient Client, IServiceProvider Services) CreateApp()
    {
        var database = Guid.NewGuid().ToString();
        var app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Default", "");
            builder.UseSetting("Auth:AdminEmail", Email);
            builder.UseSetting("Auth:AdminPasswordHash", Hash);
            builder.UseSetting("Auth:JwtKey", "chave-de-teste-com-pelo-menos-32-bytes!!");
            builder.ConfigureTestServices(services =>
            {
                var npgsql = services
                    .Where(d => d.ServiceType.IsGenericType
                        && d.ServiceType.GenericTypeArguments.Contains(typeof(HandilyCommerceDbContext)))
                    .ToList();
                foreach (var descriptor in npgsql)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<HandilyCommerceDbContext>(o => o.UseInMemoryDatabase(database));
            });
        });
        return (app.CreateClient(), app.Services);
    }

    private static async Task<HttpClient> LoginAsync(HttpClient client)
    {
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email, password = Password });
        using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.RootElement.GetProperty("accessToken").GetString());
        return client;
    }

    [Theory]
    [InlineData("GET", "/api/v1/admin/summary")]
    [InlineData("GET", "/api/v1/admin/leads")]
    [InlineData("GET", "/api/v1/admin/leads/00000000-0000-0000-0000-000000000001")]
    [InlineData("PATCH", "/api/v1/admin/leads/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/v1/admin/leads/00000000-0000-0000-0000-000000000001")]
    [InlineData("GET", "/api/v1/admin/courses/x")]
    [InlineData("POST", "/api/v1/admin/courses")]
    [InlineData("PUT", "/api/v1/admin/courses/x")]
    [InlineData("DELETE", "/api/v1/admin/courses/x")]
    [InlineData("GET", "/api/v1/admin/products/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/v1/admin/products")]
    [InlineData("PUT", "/api/v1/admin/products/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/v1/admin/products/00000000-0000-0000-0000-000000000001")]
    public async Task AdminRoutes_WithoutToken_Return401(string method, string url)
    {
        var (client, _) = CreateApp();
        using var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = JsonContent.Create(new { }),
        };

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateLead_WithInvalidPhone_ReturnsPhoneValidationError()
    {
        var (anonymous, _) = CreateApp();
        using var response = await anonymous.PostAsJsonAsync("/api/v1/leads",
            new { name = "Ana", email = "ana@x.com", municipality = "Recife", phone = "123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("errors");
        Assert.Equal(JsonValueKind.Array, errors.GetProperty("phone").ValueKind);
    }

    [Fact]
    public async Task Leads_ListPatchGetDelete_WithToken()
    {
        var (anonymous, _) = CreateApp();
        using var created = await anonymous.PostAsJsonAsync("/api/v1/leads",
            new { name = "Ana", email = "ana@x.com", municipality = "Recife", phone = "(11) 91234-5678" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        var client = await LoginAsync(anonymous);

        var page = JsonDocument.Parse(await client.GetStringAsync("/api/v1/admin/leads?page=1&pageSize=10&status=Novo")).RootElement;
        Assert.Equal(1, page.GetProperty("total").GetInt32());
        Assert.Equal("Novo", page.GetProperty("items")[0].GetProperty("status").GetString());

        using var badPage = await client.GetAsync("/api/v1/admin/leads?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, badPage.StatusCode);

        using var patch = await client.PatchAsJsonAsync($"/api/v1/admin/leads/{id}", new { status = "Contatado", internalNote = "Ligar" });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        var lead = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/admin/leads/{id}")).RootElement;
        Assert.Equal("Contatado", lead.GetProperty("status").GetString());
        Assert.Equal("Ligar", lead.GetProperty("internalNote").GetString());

        using var badPatch = await client.PatchAsJsonAsync($"/api/v1/admin/leads/{id}", new { status = "Outro" });
        Assert.Equal(HttpStatusCode.BadRequest, badPatch.StatusCode);
        using var missingPatch = await client.PatchAsJsonAsync($"/api/v1/admin/leads/{Guid.NewGuid()}", new { status = "Novo" });
        Assert.Equal(HttpStatusCode.NotFound, missingPatch.StatusCode);

        var summary = JsonDocument.Parse(await client.GetStringAsync("/api/v1/admin/summary")).RootElement;
        Assert.Equal(1, summary.GetProperty("leads").GetProperty("contatado").GetInt32());

        using var delete = await client.DeleteAsync($"/api/v1/admin/leads/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using var gone = await client.GetAsync($"/api/v1/admin/leads/{id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        using var deleteAgain = await client.DeleteAsync($"/api/v1/admin/leads/{id}");
        Assert.Equal(HttpStatusCode.NotFound, deleteAgain.StatusCode);
    }

    [Fact]
    public async Task Courses_Crud_WithToken_AndPublicListStaysPublic()
    {
        var (anonymous, _) = CreateApp();
        var client = await LoginAsync(anonymous);
        var body = new { id = "novo-curso", title = "Novo", axis = "mundo-digital", summary = "s", audience = "a", workloadHours = 20, sortOrder = (int?)9 };

        using var create = await client.PostAsJsonAsync("/api/v1/admin/courses", body);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Contains("\"axis\":\"mundo-digital\"", await create.Content.ReadAsStringAsync());
        using var duplicate = await client.PostAsJsonAsync("/api/v1/admin/courses", body);
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);

        using var put = await client.PutAsJsonAsync("/api/v1/admin/courses/novo-curso", body with { title = "Editado", sortOrder = (int?)null });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Contains("\"sortOrder\":9", await put.Content.ReadAsStringAsync());
        var detail = await client.GetStringAsync("/api/v1/admin/courses/novo-curso");
        Assert.Contains("Editado", detail);
        Assert.Contains("\"sortOrder\":9", detail);
        using var putMissing = await client.PutAsJsonAsync("/api/v1/admin/courses/nao-tem", body);
        Assert.Equal(HttpStatusCode.NotFound, putMissing.StatusCode);

        using var publicList = await anonymous.GetAsync("/api/v1/courses");
        Assert.Equal(HttpStatusCode.OK, publicList.StatusCode);
        Assert.Contains("\"sortOrder\":9", await publicList.Content.ReadAsStringAsync());

        using var delete = await client.DeleteAsync("/api/v1/admin/courses/novo-curso");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using var getMissing = await client.GetAsync("/api/v1/admin/courses/novo-curso");
        Assert.Equal(HttpStatusCode.NotFound, getMissing.StatusCode);
        using var deleteMissing = await client.DeleteAsync("/api/v1/admin/courses/novo-curso");
        Assert.Equal(HttpStatusCode.NotFound, deleteMissing.StatusCode);
    }

    [Fact]
    public async Task Products_Crud_WithToken()
    {
        var (anonymous, _) = CreateApp();
        var client = await LoginAsync(anonymous);
        var body = new { name = "Kit", sku = "K1", items = new[] { new { name = "Licença", unitPrice = 10.5m } } };

        using var invalid = await client.PostAsJsonAsync("/api/v1/admin/products", new { name = "" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using var create = await client.PostAsJsonAsync("/api/v1/admin/products", body);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = JsonDocument.Parse(await create.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        using var put = await client.PutAsJsonAsync($"/api/v1/admin/products/{id}", body with { name = "Kit 2" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var product = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/admin/products/{id}")).RootElement;
        Assert.Equal("Kit 2", product.GetProperty("name").GetString());
        Assert.Equal(1, product.GetProperty("items").GetArrayLength());
        Assert.Contains("Kit 2", await anonymous.GetStringAsync("/api/v1/products"));

        using var putMissing = await client.PutAsJsonAsync($"/api/v1/admin/products/{Guid.NewGuid()}", body with { sku = "K9" });
        Assert.Equal(HttpStatusCode.NotFound, putMissing.StatusCode);

        var summary = JsonDocument.Parse(await client.GetStringAsync("/api/v1/admin/summary")).RootElement;
        Assert.Equal(1, summary.GetProperty("products").GetInt32());

        using var delete = await client.DeleteAsync($"/api/v1/admin/products/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using var getMissing = await client.GetAsync($"/api/v1/admin/products/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getMissing.StatusCode);
        using var deleteMissing = await client.DeleteAsync($"/api/v1/admin/products/{id}");
        Assert.Equal(HttpStatusCode.NotFound, deleteMissing.StatusCode);
    }

    [Fact]
    public async Task Cors_Preflight_AllowsAdminMethodsAndAuthorization()
    {
        var (client, _) = CreateApp();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/admin/leads/00000000-0000-0000-0000-000000000001");
        request.Headers.Add("Origin", Origin);
        request.Headers.Add("Access-Control-Request-Method", "PATCH");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(Origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("PATCH", string.Join(",", response.Headers.GetValues("Access-Control-Allow-Methods")));
    }

    [Fact]
    public async Task Cors_Preflight_RejectsUnknownOrigin()
    {
        var (client, _) = CreateApp();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/admin/leads");
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "DELETE");

        using var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task OpenApi_DocumentsAdminRoutesWithBearer()
    {
        var (client, _) = CreateApp();

        var doc = await client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/v1/admin/leads/{id}", doc);
        Assert.Contains("/api/v1/admin/summary", doc);
        Assert.Contains("\"Bearer\"", doc);
    }

    [Fact]
    public void LeadResponse_From_MapsStatusAsText()
    {
        var lead = new Lead(Guid.NewGuid(), "a", "a@x.com", "m", "oi", DateTimeOffset.UtcNow, LeadStatus.Descartado, "n");

        var response = HandilyCommerce.Api.Admin.LeadResponse.From(lead);

        Assert.Equal("Descartado", response.Status);
        Assert.Equal("n", response.InternalNote);
    }
}
