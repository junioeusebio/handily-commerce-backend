using HandilyCommerce.Domain.Notifications;
using HandilyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HandilyCommerce.Infrastructure.Notifications;

/// <summary>EF Core adapter for <see cref="IEmailSendLogRepository"/> (<c>EmailSendLogs</c>).</summary>
public sealed class EfEmailSendLogRepository(HandilyCommerceDbContext dbContext) : IEmailSendLogRepository
{
    public async Task AddAsync(EmailSendLog log, CancellationToken cancellationToken)
    {
        dbContext.EmailSendLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmailSendLog>> ClaimDueAsync(
        DateTimeOffset now, DateTimeOffset leaseUntil, int max, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        var relational = dbContext.Database.IsRelational();
        await using var transaction = relational ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;

        // Postgres: row locks + SKIP LOCKED → concurrent instances never claim the same row.
        var due = relational
            ? await dbContext.EmailSendLogs.FromSqlInterpolated($"""
                SELECT * FROM "EmailSendLogs"
                WHERE "Status" IN ('Pending', 'Retrying') AND "NextAttemptAtUtc" <= {now}
                ORDER BY "NextAttemptAtUtc"
                LIMIT {max}
                FOR UPDATE SKIP LOCKED
                """).AsNoTracking().ToListAsync(cancellationToken)
            : await dbContext.EmailSendLogs.AsNoTracking()
                .Where(l => (l.Status == EmailSendStatus.Pending || l.Status == EmailSendStatus.Retrying)
                    && l.NextAttemptAtUtc <= now)
                .OrderBy(l => l.NextAttemptAtUtc)
                .Take(max)
                .ToListAsync(cancellationToken);

        var claimed = due.Select(l => l with { NextAttemptAtUtc = leaseUntil }).ToList();
        dbContext.EmailSendLogs.UpdateRange(claimed);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return claimed;
    }

    public async Task UpdateAsync(EmailSendLog log, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        dbContext.EmailSendLogs.Update(log);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }
}
