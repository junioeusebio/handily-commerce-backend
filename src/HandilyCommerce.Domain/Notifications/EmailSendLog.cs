namespace HandilyCommerce.Domain.Notifications;

/// <summary>
/// Durable state of one lead e-mail (outbox row). Persisted in table <c>EmailSendLogs</c>. Never contains secrets.
/// The body is not stored: it is rebuilt from the <c>Leads</c> row at send time.
/// </summary>
/// <param name="AttemptedAtUtc">Last attempt instant (creation instant while <see cref="EmailSendStatus.Pending"/>).</param>
/// <param name="AttemptCount">Number of send attempts made so far.</param>
/// <param name="NextAttemptAtUtc">When the worker may (re)try; null for final states.</param>
public sealed record EmailSendLog(
    Guid Id,
    Guid? LeadId,
    string ToAddress,
    string Subject,
    string Provider,
    string? ProviderMessageId,
    EmailSendStatus Status,
    string? ErrorMessage,
    DateTimeOffset AttemptedAtUtc,
    int AttemptCount = 1,
    DateTimeOffset? NextAttemptAtUtc = null)
{
    /// <summary>True for Pending/Retrying (the worker may still send it).</summary>
    public bool IsOpen => Status is EmailSendStatus.Pending or EmailSendStatus.Retrying;
}
