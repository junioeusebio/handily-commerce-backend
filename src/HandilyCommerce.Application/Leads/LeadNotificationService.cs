using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Notifications;

namespace HandilyCommerce.Application.Leads;

/// <summary>
/// Sends the "new lead" e-mail and records every attempt in <c>EmailSendLogs</c>.
/// Never throws because of the provider: failures become a <see cref="EmailSendStatus.Failed"/> log.
/// </summary>
public sealed class LeadNotificationService(
    IEmailSender sender,
    IEmailSendLogRepository logRepository,
    LeadNotificationOptions options,
    TimeProvider timeProvider)
{
    public const int ErrorMaxLength = 1000;

    public async Task<EmailSendLog> NotifyAsync(Lead lead, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lead);

        var message = new EmailMessage(options.To, options.Subject, LeadEmailTemplate.Build(lead));

        EmailSendResult result;
        try
        {
            result = await sender.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            result = EmailSendResult.Failed($"{ex.GetType().Name}: {ex.Message}");
        }

        var log = new EmailSendLog(
            Guid.NewGuid(),
            lead.Id,
            message.To,
            message.Subject,
            sender.Provider,
            result.ProviderMessageId,
            result.Status,
            Truncate(result.ErrorMessage),
            timeProvider.GetUtcNow());

        await logRepository.AddAsync(log, cancellationToken);
        return log;
    }

    internal static string? Truncate(string? value) =>
        value is null || value.Length <= ErrorMaxLength ? value : value[..ErrorMaxLength];
}
