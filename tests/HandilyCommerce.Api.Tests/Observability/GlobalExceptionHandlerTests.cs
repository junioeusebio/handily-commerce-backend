using System.Text.Json;
using HandilyCommerce.Api.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HandilyCommerce.Api.Tests.Observability;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_LogsPathAndTraceId_AndWritesProblemDetailsWithoutExceptionMessage()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = c =>
                c.ProblemDetails.Extensions["traceId"] = GlobalExceptionHandler.GetTraceId(c.HttpContext));
        await using var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = provider, TraceIdentifier = "trace-123" };
        context.Request.Method = "GET";
        context.Request.Path = "/api/v1/courses";
        context.Request.QueryString = new QueryString("?token=secret");
        context.Response.Body = new MemoryStream();

        var logger = new CapturingLogger();
        var sut = new GlobalExceptionHandler(logger, provider.GetRequiredService<IProblemDetailsService>());

        var handled = await sut.TryHandleAsync(
            context, new InvalidOperationException("relation \"Courses\" does not exist"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.IsType<InvalidOperationException>(entry.Exception);
        Assert.Contains("GET /api/v1/courses", entry.Message);
        Assert.Contains("trace-123", entry.Message);
        Assert.DoesNotContain("secret", entry.Message);

        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(500, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("trace-123", body.RootElement.GetProperty("traceId").GetString());
        Assert.DoesNotContain("Courses", body.RootElement.GetRawText());
    }

    private sealed class CapturingLogger : ILogger<GlobalExceptionHandler>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception), exception));
    }
}
