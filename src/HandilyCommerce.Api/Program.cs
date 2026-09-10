using System.Reflection;
using HandilyCommerce.Api.OpenApi;
using HandilyCommerce.Api.Options;
using HandilyCommerce.Api.Versioning;
using HandilyCommerce.Application.DependencyInjection;
using HandilyCommerce.Domain.ApiVersion;
using HandilyCommerce.Domain.Ping;
using HandilyCommerce.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Render (and similar hosts) inject PORT; bind explicitly when present.
// ListenAnyIP avoids a literal http:// URL (Sonar S5332); TLS ends at the edge.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port) && int.TryParse(port, out var portNumber))
{
    builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(portNumber));
}

builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Render / reverse proxies: trust forwarded headers from the edge.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                "https://junioeusebio.github.io",
                "https://handily-commerce-backend.onrender.com",
                "http://localhost:4200",
                "https://localhost:4200")
            .WithMethods("GET", "OPTIONS")
            .AllowAnyHeader();
    });
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        var api = context.ApplicationServices
            .GetRequiredService<IOptions<ApiOptions>>()
            .Value;
        var httpContext = context.ApplicationServices
            .GetRequiredService<IHttpContextAccessor>()
            .HttpContext;

        document.Info ??= new OpenApiInfo();
        document.Info.Title = api.Title;
        document.Info.Version = api.Version;

        var publicUrl = OpenApiPublicUrl.Resolve(
            api.PublicBaseUrl,
            httpContext?.Request.Scheme,
            httpContext?.Request.Host);

        if (!string.IsNullOrWhiteSpace(publicUrl))
        {
            document.Servers = [new OpenApiServer { Url = publicUrl }];
        }
        else if (document.Servers is { Count: > 0 })
        {
            foreach (var server in document.Servers)
            {
                if (!string.IsNullOrWhiteSpace(server.Url))
                {
                    server.Url = OpenApiPublicUrl.EnsureHttpsForOnRender(server.Url);
                }
            }
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

var apiOptions = app.Services.GetRequiredService<IOptions<ApiOptions>>().Value;
var healthPath = apiOptions.Path("health");
var pingPath = apiOptions.Path("ping");
var apiVersionPath = apiOptions.Path("apiVersion");

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    // TLS terminates at the edge on Render; redirect only locally.
    app.UseHttpsRedirection();
}

// CORS before Map* so GitHub Pages (and local Angular) can call the API.
app.UseCors();

// OpenAPI + Swagger UI available in all environments (needed for Render demo).
app.MapOpenApi();

app.MapHealthChecks(healthPath, new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var service = report.Entries
            .SelectMany(e => e.Value.Data)
            .FirstOrDefault(kv => kv.Key == "service")
            .Value?
            .ToString()
            ?? "handily-commerce-backend";

        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            service
        });
    }
});

app.MapGet(pingPath, (IOptions<ApiOptions> options, IPingPort pingPort) =>
    Results.Json(pingPort.GetPing(options.Value.Version)));

app.MapGet(apiVersionPath, (IOptions<ApiOptions> options, IApiVersionPort apiVersionPort) =>
{
    var productVersion = ProductVersionReader.FromAssembly(Assembly.GetExecutingAssembly());
    return Results.Json(apiVersionPort.GetApiVersion(productVersion, options.Value.Version));
});

// Swagger UI consumes Microsoft.AspNetCore.OpenApi document at /openapi/v1.json.
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", apiOptions.Title);
    options.RoutePrefix = "swagger";
    options.DocumentTitle = apiOptions.Title;
});

await app.RunAsync();
