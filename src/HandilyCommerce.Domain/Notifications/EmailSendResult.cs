namespace HandilyCommerce.Domain.Notifications;

/// <summary>Result returned by an <see cref="IEmailSender"/>.</summary>
/// <param name="Retryable">For failures: transient (HTTP 5xx/429, timeout, network) and worth retrying.</param>
public sealed record EmailSendResult(EmailSendStatus Status, string? ProviderMessageId, string? ErrorMessage, bool Retryable = false)
{
    public static EmailSendResult Sent(string? providerMessageId) => new(EmailSendStatus.Sent, providerMessageId, null);

    public static EmailSendResult Failed(string error, bool retryable = false) => new(EmailSendStatus.Failed, null, error, retryable);

    public static EmailSendResult Skipped(string reason) => new(EmailSendStatus.Skipped, null, reason);
}
