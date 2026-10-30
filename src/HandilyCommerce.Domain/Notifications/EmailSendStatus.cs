namespace HandilyCommerce.Domain.Notifications;

/// <summary>State of a lead e-mail (stored as text in <c>EmailSendLogs.Status</c>).</summary>
public enum EmailSendStatus
{
    /// <summary>Delivered to the provider (final).</summary>
    Sent,

    /// <summary>Non-retryable error, e.g. HTTP 4xx other than 429 (final).</summary>
    Failed,

    /// <summary>Provider not configured, e.g. no API key (final).</summary>
    Skipped,

    /// <summary>Persisted on lead creation, waiting for the first attempt.</summary>
    Pending,

    /// <summary>Transient failure; next attempt at <c>NextAttemptAtUtc</c>.</summary>
    Retrying,

    /// <summary>Transient failures reached <c>LeadNotification:MaxAttempts</c> (final).</summary>
    GivenUp,
}
