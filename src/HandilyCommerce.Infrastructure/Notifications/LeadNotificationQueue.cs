using System.Threading.Channels;
using HandilyCommerce.Domain.Leads;

namespace HandilyCommerce.Infrastructure.Notifications;

/// <summary>In-memory queue (singleton) between <c>POST /leads</c> and <see cref="LeadNotificationWorker"/>.</summary>
public sealed class LeadNotificationQueue : ILeadNotificationQueue
{
    private readonly Channel<Lead> channel = Channel.CreateUnbounded<Lead>(
        new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<Lead> Reader => channel.Reader;

    public void Enqueue(Lead lead)
    {
        ArgumentNullException.ThrowIfNull(lead);
        channel.Writer.TryWrite(lead);
    }
}
