namespace HandilyCommerce.Domain.Notifications;

/// <summary>Outbound port for <see cref="EmailSendLog"/> (adapter: EF Core).</summary>
public interface IEmailSendLogRepository
{
    Task AddAsync(EmailSendLog log, CancellationToken cancellationToken);
}
