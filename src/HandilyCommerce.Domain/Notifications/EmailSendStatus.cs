namespace HandilyCommerce.Domain.Notifications;

/// <summary>Outcome of an e-mail send attempt (stored as text in <c>EmailSendLogs.Status</c>).</summary>
public enum EmailSendStatus
{
    Sent,
    Failed,
    Skipped,
}
