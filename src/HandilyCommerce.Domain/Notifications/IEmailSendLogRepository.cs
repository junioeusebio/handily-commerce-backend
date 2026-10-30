namespace HandilyCommerce.Domain.Notifications;

/// <summary>Outbound port for <see cref="EmailSendLog"/> (adapter: EF Core).</summary>
public interface IEmailSendLogRepository
{
    Task AddAsync(EmailSendLog log, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically claims up to <paramref name="max"/> open rows due at <paramref name="now"/>, pushing their
    /// <c>NextAttemptAtUtc</c> to <paramref name="leaseUntil"/> so no other instance picks them meanwhile.
    /// </summary>
    Task<IReadOnlyList<EmailSendLog>> ClaimDueAsync(
        DateTimeOffset now, DateTimeOffset leaseUntil, int max, CancellationToken cancellationToken);

    /// <summary>Persists the new state of an existing row.</summary>
    Task UpdateAsync(EmailSendLog log, CancellationToken cancellationToken);
}
