using HandilyCommerce.Api.Options;
using HandilyCommerce.Application.DependencyInjection;
using HandilyCommerce.Domain.Ping;
using HandilyCommerce.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        var api = context.ApplicationServices
            .GetRequiredService<IOptions<ApiOptions>>()
            .Value;

        document.Info ??= new OpenApiInfo();
        document.Info.Title = api.Title;
        document.Info.Version = api.Version;

        return Task.CompletedTask;
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

var apiOptions = app.Services.GetRequiredService<IOptions<ApiOptions>>().Value;
var healthPath = apiOptions.Path("health");
var pingPath = apiOptions.Path("ping");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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

await app.RunAsync();
