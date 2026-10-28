namespace HandilyCommerce.Api.RateLimiting;

/// <summary>
/// Rate limit for POST leads (section <c>RateLimiting:Leads</c>). Sliding window per client IP.
/// </summary>
public sealed class LeadRateLimitOptions
{
    public const string SectionName = "RateLimiting:Leads";

    /// <summary>Master switch (default: true).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Max requests per client IP inside <see cref="WindowSeconds"/> (default: 5).</summary>
    public int PermitLimit { get; set; } = 5;

    /// <summary>Window length in seconds (default: 600 = 10 min).</summary>
    public int WindowSeconds { get; set; } = 600;

    /// <summary>Sliding window segments (default: 10, i.e. 1 min each).</summary>
    public int SegmentsPerWindow { get; set; } = 10;

    public TimeSpan Window => TimeSpan.FromSeconds(Math.Max(1, WindowSeconds));
}
