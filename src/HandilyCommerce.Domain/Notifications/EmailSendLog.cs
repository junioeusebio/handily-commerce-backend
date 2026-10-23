namespace HandilyCommerce.Domain.Notifications;

/// <summary>One e-mail send attempt. Persisted in table <c>EmailSendLogs</c>. Never contains secrets.</summary>
public sealed record EmailSendLog(
    Guid Id,
    Guid? LeadId,
    string ToAddress,
    string Subject,
    string Provider,
    string? ProviderMessageId,
    EmailSendStatus Status,
    string? ErrorMessage,
    DateTimeOffset AttemptedAtUtc);
