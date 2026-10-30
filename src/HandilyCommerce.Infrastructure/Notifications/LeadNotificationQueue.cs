using HandilyCommerce.Application.Leads;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Notifications;
using HandilyCommerce.Infrastructure.Persistence;

namespace HandilyCommerce.Infrastructure.Notifications;

/// <summary>
/// Durable queue (scoped, same DbContext as the request): stores a <see cref="EmailSendStatus.Pending"/> row in
/// <c>EmailSendLogs</c> and wakes <see cref="LeadNotificationWorker"/>. A restart never loses the e-mail.
/// </summary>
public sealed class LeadNotificationQueue(
    HandilyCommerceDbContext dbContext,
    LeadNotificationOptions options,
    IEmailSender sender,
    TimeProvider timeProvider,
    LeadNotificationSignal signal) : ILeadNotificationQueue
{
    public void Enqueue(Lead lead)
    {
        ArgumentNullException.ThrowIfNull(lead);
        dbContext.EmailSendLogs.Add(LeadNotificationService.NewPending(lead, options, sender.Provider, timeProvider.GetUtcNow()));
        dbContext.SaveChanges();
        signal.Notify();
    }
}
