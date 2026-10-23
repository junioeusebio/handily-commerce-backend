namespace HandilyCommerce.Domain.Notifications;

/// <summary>Outbound port: send an e-mail (adapter: <c>ResendEmailSender</c>).</summary>
public interface IEmailSender
{
    /// <summary>Provider name stored in the send log (e.g. "Resend").</summary>
    string Provider { get; }

    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
