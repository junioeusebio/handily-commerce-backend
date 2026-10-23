namespace HandilyCommerce.Domain.Notifications;

/// <summary>Result returned by an <see cref="IEmailSender"/>.</summary>
public sealed record EmailSendResult(EmailSendStatus Status, string? ProviderMessageId, string? ErrorMessage)
{
    public static EmailSendResult Sent(string? providerMessageId) => new(EmailSendStatus.Sent, providerMessageId, null);

    public static EmailSendResult Failed(string error) => new(EmailSendStatus.Failed, null, error);

    public static EmailSendResult Skipped(string reason) => new(EmailSendStatus.Skipped, null, reason);
}
