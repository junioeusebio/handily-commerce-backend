using HandilyCommerce.Application.DependencyInjection;
using HandilyCommerce.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/api/v1/health", new HealthCheckOptions
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

app.Run();
