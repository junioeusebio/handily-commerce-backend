using HandilyCommerce.Api.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace HandilyCommerce.Api.Tests.Observability;

public class LoggingSetupTests
{
    private const string HealthPath = "/api/v1/health";

    [Fact]
    public void Configure_WithoutLokiSettings_ReturnsFalse()
    {
        var configuration = new ConfigurationBuilder().Build();
        var logger = new LoggerConfiguration();

        var enabled = LoggingSetup.Configure(logger, configuration, "Development", "0.0.0");

        Assert.False(enabled);
        using var created = logger.CreateLogger();
        Assert.True(created.IsEnabled(LogEventLevel.Information));
        Assert.False(created.IsEnabled(LogEventLevel.Debug));
    }

    [Fact]
    public void Configure_WithLokiSettings_ReturnsTrue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GrafanaLoki:Url"] = "https://logs-prod-024.grafana.net",
                ["GrafanaLoki:Username"] = "123456",
                ["GrafanaLoki:ApiToken"] = "glc_test"
            })
            .Build();
        var logger = new LoggerConfiguration();

        var enabled = LoggingSetup.Configure(logger, configuration, "Production", "0.7.0");

        Assert.True(enabled);
    }

    [Fact]
    public void RequestLevel_ServerErrorOrException_IsError()
    {
        var context = new DefaultHttpContext();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        Assert.Equal(LogEventLevel.Error, LoggingSetup.RequestLevel(context, null, HealthPath));
        Assert.Equal(
            LogEventLevel.Error,
            LoggingSetup.RequestLevel(new DefaultHttpContext(), new InvalidOperationException(), HealthPath));
    }

    [Fact]
    public void RequestLevel_HealthProbe_IsDebug()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = HealthPath;

        Assert.Equal(LogEventLevel.Debug, LoggingSetup.RequestLevel(context, null, HealthPath));
    }

    [Fact]
    public void RequestLevel_RegularRequest_IsInformation()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/courses";

        Assert.Equal(LogEventLevel.Information, LoggingSetup.RequestLevel(context, null, HealthPath));
    }
}
