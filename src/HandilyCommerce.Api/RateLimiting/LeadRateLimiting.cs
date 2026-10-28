using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace HandilyCommerce.Api.RateLimiting;

/// <summary>
/// Built-in ASP.NET Core RateLimiter applied only to POST leads (policy <see cref="PolicyName"/>).
/// Client IP comes from <c>RemoteIpAddress</c>, already rewritten by UseForwardedHeaders (Render X-Forwarded-For).
/// </summary>
public static class LeadRateLimiting
{
    public const string PolicyName = "leads";
    public const string Title = "Muitas requisições";

    public static IServiceCollection AddLeadRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LeadRateLimitOptions>(configuration.GetSection(LeadRateLimitOptions.SectionName));

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = (context, cancellationToken) =>
                WriteRejectionAsync(context.HttpContext, context.Lease, cancellationToken);

            limiter.AddPolicy(PolicyName, httpContext =>
            {
                var options = httpContext.RequestServices
                    .GetRequiredService<IOptions<LeadRateLimitOptions>>().Value;

                if (!options.Enabled)
                {
                    return RateLimitPartition.GetNoLimiter("disabled");
                }

                return RateLimitPartition.GetSlidingWindowLimiter(
                    PartitionKey(httpContext),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, options.PermitLimit),
                        Window = options.Window,
                        SegmentsPerWindow = Math.Max(1, options.SegmentsPerWindow),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    /// <summary>Partition key = client IP (after forwarded headers); "unknown" when absent.</summary>
    public static string PartitionKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    /// <summary>Retry-After in whole seconds (lease metadata, else full window).</summary>
    public static int RetryAfterSeconds(RateLimitLease lease, TimeSpan fallback)
    {
        var retry = lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? retryAfter : fallback;
        return Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds));
    }

    public static async ValueTask WriteRejectionAsync(HttpContext httpContext, RateLimitLease lease, CancellationToken cancellationToken)
    {
        var options = httpContext.RequestServices.GetRequiredService<IOptions<LeadRateLimitOptions>>().Value;
        var seconds = RetryAfterSeconds(lease, options.Window);
        httpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);

        var minutes = (int)Math.Ceiling(seconds / 60d);
        var problem = TypedResults.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: Title,
            detail: $"Você enviou muitos contatos em pouco tempo. Tente novamente em {minutes} minuto(s).",
            extensions: new Dictionary<string, object?> { ["retryAfterSeconds"] = seconds });

        await problem.ExecuteAsync(httpContext);
    }
}
