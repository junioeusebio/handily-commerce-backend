using System.Threading.Channels;

namespace HandilyCommerce.Infrastructure.Notifications;

/// <summary>Singleton wake-up signal so a new Pending row is sent right away instead of waiting for the next poll.</summary>
public sealed class LeadNotificationSignal
{
    private readonly Channel<bool> channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    public void Notify() => channel.Writer.TryWrite(true);

    /// <summary>Waits for a signal or until <paramref name="timeout"/> elapses.</summary>
    public async Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            await channel.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Poll interval elapsed.
        }
    }
}
