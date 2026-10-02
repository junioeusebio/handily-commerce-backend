using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HandilyCommerce.Api.Observability;

/// <summary>
/// Logs unhandled exceptions with method, path and trace id, and answers RFC 7807 ProblemDetails (500)
/// without leaking exception details to clients. Query strings are never logged.
/// </summary>
public sealed partial class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = GetTraceId(httpContext);
        LogUnhandled(logger, exception, httpContext.Request.Method, httpContext.Request.Path.Value ?? "/", traceId);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred. Use traceId when reporting the problem."
            }
        });
    }

    /// <summary>W3C trace id of the current request (falls back to <see cref="HttpContext.TraceIdentifier"/>).</summary>
    public static string GetTraceId(HttpContext httpContext) =>
        Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

    [LoggerMessage(
        EventId = 5000,
        Level = LogLevel.Error,
        Message = "Unhandled exception on {Method} {Path} (traceId {TraceId})")]
    private static partial void LogUnhandled(
        ILogger logger, Exception exception, string method, string path, string traceId);
}
