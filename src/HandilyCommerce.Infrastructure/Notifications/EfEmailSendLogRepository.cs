using HandilyCommerce.Domain.Notifications;
using HandilyCommerce.Infrastructure.Persistence;

namespace HandilyCommerce.Infrastructure.Notifications;

/// <summary>EF Core adapter for <see cref="IEmailSendLogRepository"/> (<c>EmailSendLogs</c>).</summary>
public sealed class EfEmailSendLogRepository(HandilyCommerceDbContext dbContext) : IEmailSendLogRepository
{
    public async Task AddAsync(EmailSendLog log, CancellationToken cancellationToken)
    {
        dbContext.EmailSendLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
