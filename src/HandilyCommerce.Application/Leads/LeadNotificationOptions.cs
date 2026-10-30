namespace HandilyCommerce.Application.Leads;

/// <summary>Config section <c>LeadNotification</c>.</summary>
public sealed class LeadNotificationOptions
{
    public const string SectionName = "LeadNotification";
    public const string DefaultTo = "ajksys@protonmail.com";
    public const string DefaultSubject = "[NOVO CONTATO] - Lead BCC";

    public string To { get; set; } = DefaultTo;

    public string Subject { get; set; } = DefaultSubject;

    /// <summary>Total send attempts before <c>GivenUp</c> (transient errors only).</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Backoff base: delay before retry n is <c>BaseDelaySeconds * 2^(n-1)</c>.</summary>
    public int BaseDelaySeconds { get; set; } = 30;

    /// <summary>Upper bound for a single backoff delay.</summary>
    public int MaxDelaySeconds { get; set; } = 6 * 60 * 60;

    /// <summary>How often the worker polls the database for due rows.</summary>
    public int PollIntervalSeconds { get; set; } = 30;

    /// <summary>Claim lease: a row claimed by a crashed instance becomes due again after this.</summary>
    public int LeaseSeconds { get; set; } = 300;

    public int BatchSize { get; set; } = 10;
}
