using HandilyCommerce.Application.Leads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HandilyCommerce.Infrastructure.Notifications;

/// <summary>
/// Background consumer: one DI scope (fresh DbContext) per lead → <see cref="LeadNotificationService"/>.
/// </summary>
public sealed partial class LeadNotificationWorker(
    LeadNotificationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<LeadNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var lead in queue.Reader.ReadAllAsync(stoppingToken))
            {
                await ProcessAsync(lead, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutting down.
        }
    }

    internal async Task ProcessAsync(Domain.Leads.Lead lead, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<LeadNotificationService>();
            var log = await service.NotifyAsync(lead, cancellationToken);
            LogResult(lead.Id, log.Status.ToString());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogError(ex, lead.Id);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Lead {LeadId} notification: {Status}")]
    private partial void LogResult(Guid leadId, string status);

    [LoggerMessage(Level = LogLevel.Error, Message = "Lead {LeadId} notification could not be processed")]
    private partial void LogError(Exception exception, Guid leadId);
}
