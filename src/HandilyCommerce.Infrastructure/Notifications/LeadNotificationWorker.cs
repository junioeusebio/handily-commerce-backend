using HandilyCommerce.Application.Leads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HandilyCommerce.Infrastructure.Notifications;

/// <summary>
/// Background sender: on each signal (new lead) or every <c>LeadNotification:PollIntervalSeconds</c>, claims due
/// Pending/Retrying rows from <c>EmailSendLogs</c> (one DI scope per batch) via <see cref="LeadNotificationService"/>.
/// Rows left behind by a restart are picked up on startup.
/// </summary>
public sealed partial class LeadNotificationWorker(
    LeadNotificationSignal signal,
    LeadNotificationOptions options,
    IServiceScopeFactory scopeFactory,
    ILogger<LeadNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.PollIntervalSeconds));
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (await ProcessBatchAsync(stoppingToken) > 0)
                {
                    // Drain everything that is due.
                }

                await signal.WaitAsync(interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutting down.
        }
    }

    internal async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<LeadNotificationService>();
            var count = await service.ProcessDueAsync(cancellationToken);
            if (count > 0)
            {
                LogBatch(count);
            }

            return count;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogError(ex);
            return 0;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Lead notifications processed: {Count}")]
    private partial void LogBatch(int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Lead notification batch could not be processed")]
    private partial void LogError(Exception exception);
}
