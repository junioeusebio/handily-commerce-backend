using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Notifications;

namespace HandilyCommerce.Application.Leads;

/// <summary>
/// Durable "new lead" e-mail: a <see cref="EmailSendStatus.Pending"/> row is stored with the lead, then
/// <see cref="ProcessDueAsync"/> claims due rows, rebuilds the e-mail from the <c>Leads</c> row and sends it.
/// Transient failures are retried with exponential backoff; never throws because of the provider.
/// </summary>
public sealed class LeadNotificationService(
    IEmailSender sender,
    IEmailSendLogRepository logRepository,
    ILeadRepository leadRepository,
    LeadNotificationOptions options,
    TimeProvider timeProvider)
{
    public const int ErrorMaxLength = 1000;

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
            await logRepository.UpdateAsync(await AttemptAsync(log, cancellationToken), cancellationToken);
        }

        return claimed.Count;
    }

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
