using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace HandilyCommerce.Application.Leads;

/// <summary>
/// Durable "new lead" e-mail: a <see cref="EmailSendStatus.Pending"/> row is stored with the lead, then
/// <see cref="ProcessDueAsync"/> claims due rows, rebuilds the e-mail from the <c>Leads</c> row and sends it.
/// Transient failures are retried with exponential backoff; never throws because of the provider.
/// After a durable save to <see cref="EmailSendStatus.Failed"/> or <see cref="EmailSendStatus.GivenUp"/>,
/// emits <see cref="TerminalFailureEventName"/> (Warning, no personal data). See
/// <c>docs/observabilidade-falha-terminal-email.md</c>.
/// </summary>
public sealed partial class LeadNotificationService(
    IEmailSender sender,
    IEmailSendLogRepository logRepository,
    ILeadRepository leadRepository,
    LeadNotificationOptions options,
    TimeProvider timeProvider,
    ILogger<LeadNotificationService> logger)
{
    public const int ErrorMaxLength = 1000;

    /// <summary>Stable MEL event id for a durable Failed/GivenUp transition. Not an HTTP status.</summary>
    public const int TerminalFailureEventId = 6102;

    /// <summary>Stable event name. Also the literal prefix of <see cref="TerminalFailureMessageTemplate"/>.</summary>
    public const string TerminalFailureEventName = "LeadNotificationTerminalFailure";

    /// <summary>
    /// Message template pushed to Loki. Placeholders are the only structured fields: status, attempt count,
    /// and internal ids. Never add addresses, names, phones, bodies, provider errors, or secrets.
    /// </summary>
    public const string TerminalFailureMessageTemplate =
        "LeadNotificationTerminalFailure Status={Status} AttemptCount={AttemptCount} EmailSendLogId={EmailSendLogId} LeadId={LeadId}";

    /// <summary>Row persisted on lead creation (first attempt due immediately).</summary>
    public static EmailSendLog NewPending(Lead lead, LeadNotificationOptions options, string provider, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(lead);
        ArgumentNullException.ThrowIfNull(options);
        return new EmailSendLog(
            Guid.NewGuid(), lead.Id, options.To, options.Subject, provider, null,
            EmailSendStatus.Pending, null, now, AttemptCount: 0, NextAttemptAtUtc: now);
    }

    /// <summary>Claims and processes one batch of due rows; returns how many were processed.</summary>
    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var claimed = await logRepository.ClaimDueAsync(
            now, now.AddSeconds(Math.Max(1, options.LeaseSeconds)), Math.Max(1, options.BatchSize), cancellationToken);

        foreach (var log in claimed)
        {
            var updated = await AttemptAsync(log, cancellationToken);
            await logRepository.UpdateAsync(updated, cancellationToken);
            if (log.IsOpen && updated.Status is EmailSendStatus.Failed or EmailSendStatus.GivenUp)
            {
                // Only after the terminal status is saved. Polling cannot claim a final row, and a failed save throws first.
                LogTerminalFailure(updated.Status.ToString(), updated.AttemptCount, updated.Id, updated.LeadId);
            }
        }

        return claimed.Count;
    }

    [LoggerMessage(
        EventId = TerminalFailureEventId,
        EventName = TerminalFailureEventName,
        Level = LogLevel.Warning,
        Message = TerminalFailureMessageTemplate)]
    private partial void LogTerminalFailure(string status, int attemptCount, Guid emailSendLogId, Guid? leadId);

    public async Task<EmailSendLog> AttemptAsync(EmailSendLog log, CancellationToken cancellationToken)
    {
        if (!log.IsOpen)
        {
            return log; // Sent/Failed/Skipped/GivenUp are final: never send twice.
        }

        var lead = log.LeadId is { } leadId ? leadRepository.Find(leadId) : null;
        var now = timeProvider.GetUtcNow();
        if (lead is null)
        {
            return log with { Status = EmailSendStatus.Failed, ErrorMessage = "Lead não encontrado.", NextAttemptAtUtc = null, AttemptedAtUtc = now };
        }

        var message = new EmailMessage(log.ToAddress, log.Subject, LeadEmailTemplate.Build(lead));
        EmailSendResult result;
        try
        {
            result = await sender.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Network error or HttpClient timeout: transient.
            result = EmailSendResult.Failed($"{ex.GetType().Name}: {ex.Message}", retryable: true);
        }

        var attempt = log.AttemptCount + 1;
        var next = log with
        {
            AttemptCount = attempt,
            AttemptedAtUtc = timeProvider.GetUtcNow(),
            ProviderMessageId = result.ProviderMessageId,
            ErrorMessage = Truncate(result.ErrorMessage),
            NextAttemptAtUtc = null,
        };

        if (result.Status != EmailSendStatus.Failed || !result.Retryable)
        {
            return next with { Status = result.Status };
        }

        if (attempt >= Math.Max(1, options.MaxAttempts))
        {
            return next with { Status = EmailSendStatus.GivenUp };
        }

        return next with { Status = EmailSendStatus.Retrying, NextAttemptAtUtc = next.AttemptedAtUtc + BackoffDelay(attempt) };
    }

    /// <summary>Delay after failed attempt <paramref name="attempt"/> (1-based): base·2^(attempt-1), capped.</summary>
    public TimeSpan BackoffDelay(int attempt)
    {
        var baseSeconds = Math.Max(1, options.BaseDelaySeconds);
        var seconds = baseSeconds * Math.Pow(2, Math.Clamp(attempt - 1, 0, 30));
        return TimeSpan.FromSeconds(Math.Min(seconds, Math.Max(baseSeconds, options.MaxDelaySeconds)));
    }

    internal static string? Truncate(string? value) =>
        value is null || value.Length <= ErrorMaxLength ? value : value[..ErrorMaxLength];
}
