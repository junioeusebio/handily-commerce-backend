using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

namespace HandilyCommerce.Api.Observability;

/// <summary>
/// Serilog configuration: structured console logs everywhere, plus Grafana Cloud Loki when
/// <see cref="GrafanaLokiOptions"/> is configured. Never logs configuration values or connection strings.
/// </summary>
public static class LoggingSetup
{
    public const string ApplicationName = "handily-commerce-backend";

    private const string ConsoleTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {TraceId}{NewLine}{Exception}";

    /// <returns><c>true</c> when the Loki sink was added.</returns>
    public static bool Configure(
        LoggerConfiguration logger,
        IConfiguration configuration,
        string environmentName,
        string productVersion)
    {
        logger
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", ApplicationName)
            .Enrich.WithProperty("Environment", environmentName)
            .Enrich.WithProperty("Version", productVersion)
            .WriteTo.Console(outputTemplate: ConsoleTemplate);

        var loki = GrafanaLokiOptions.FromConfiguration(configuration);
        if (!loki.IsConfigured)
        {
            return false;
        }

        logger.WriteTo.GrafanaLoki(
            loki.BaseUrl!,
            labels:
            [
                new LokiLabel { Key = "app", Value = ApplicationName },
                new LokiLabel { Key = "env", Value = environmentName.ToLowerInvariant() }
            ],
            credentials: new LokiCredentials { Login = loki.Login!, Password = loki.Password! },
            traceIdMode: LokiFieldDestination.Body);
        return true;
    }

    /// <summary>
    /// Routes Serilog's internal diagnostics (e.g. Loki push failures: <c>received 401 from Loki</c>) to
    /// <paramref name="writer"/> (stderr in Program, so they show up in Render logs). Sinks never throw into the app,
    /// so without this a misconfigured sink fails silently.
    /// </summary>
    public static void EnableSelfLog(TextWriter writer)
    {
        var synchronized = TextWriter.Synchronized(writer);
        SelfLog.Enable(message => synchronized.WriteLine($"[Serilog SelfLog] {message}"));
    }

    /// <summary>
    /// Level for <c>UseSerilogRequestLogging</c>: errors for 5xx/exceptions, Debug (filtered out) for the
    /// Render health probe, Information otherwise.
    /// </summary>
    public static LogEventLevel RequestLevel(HttpContext context, Exception? exception, string healthPath)
    {
        if (exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        return context.Request.Path.Equals(healthPath, StringComparison.OrdinalIgnoreCase)
            ? LogEventLevel.Debug
            : LogEventLevel.Information;
    }
}
