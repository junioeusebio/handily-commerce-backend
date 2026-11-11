using System.Net;
using System.Text.Json;
using HandilyCommerce.Api.Observability;
using HandilyCommerce.Application.Leads;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace HandilyCommerce.Api.Tests.Observability;

/// <summary>
/// Captures the Grafana Loki push body produced by <see cref="LoggingSetup"/> for the terminal-failure event.
/// The suggested LogQL in the PT-BR doc is grounded on this payload, not on a production query.
/// </summary>
public class LeadNotificationTerminalFailureLogTests
{
    private const string SentinelName = "SENTINEL_NOME_ALICE";
    private const string SentinelEmail = "sentinel.alice@example.invalid";
    private const string SentinelPhone = "11988887777";
    private const string SentinelPhoneFormatted = "(11) 98888-7777";
    private const string SentinelCity = "SENTINEL_CIDADE";
    private const string SentinelBody = "SENTINEL_MENSAGEM_CORPO";
    private const string SentinelTo = "sentinel.destinatario@example.invalid";
    private const string SentinelSubject = "SENTINEL_ASSUNTO";
    private const string SentinelSecret = "glc_SENTINEL_SECRET";
    private const string SentinelProviderId = "SENTINEL_PROVIDER_ID";
    private const string LokiToken = "glc_test_capture_only";

    [Fact]
    public async Task TerminalFailure_LokiPushBody_HasStableFields_AndNoPersonalData()
    {
        var capture = new LokiCaptureHandler();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GrafanaLoki:Url"] = "https://logs-prod-024.grafana.net",
                ["GrafanaLoki:Username"] = "123456",
                ["GrafanaLoki:ApiToken"] = LokiToken,
            })
            .Build();
        var loggerConfiguration = new LoggerConfiguration();
        Assert.True(LoggingSetup.Configure(loggerConfiguration, configuration, "Production", "0.15.1", capture));

        using var serilogLogger = loggerConfiguration.CreateLogger();
        using var provider = new ServiceCollection()
            .AddLogging(builder => builder.AddSerilog(serilogLogger, dispose: false))
            .BuildServiceProvider();

        var now = new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);
        var lead = new Lead(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), SentinelName, SentinelEmail, SentinelCity, SentinelBody, now, Phone: SentinelPhone);
        var leads = new MemoryLeads();
        leads.Add(lead);
        var logs = new MemoryLogs();
        var options = new LeadNotificationOptions { To = SentinelTo, Subject = SentinelSubject, MaxAttempts = 3 };
        var pending = LeadNotificationService.NewPending(lead, options, "Fake", now);
        logs.Logs.Add(pending);
        var sender = new FakeSender(_ => new EmailSendResult(
            EmailSendStatus.Failed,
            SentinelProviderId,
            $"raw provider {SentinelName} {SentinelEmail} {SentinelPhone} {SentinelPhoneFormatted} {SentinelBody} {SentinelSecret}",
            false));
        var sut = new LeadNotificationService(sender, logs, leads, options, new FixedTime(now), provider.GetRequiredService<ILogger<LeadNotificationService>>());

        Assert.Equal(1, await sut.ProcessDueAsync(CancellationToken.None));
        serilogLogger.Dispose();

        var payload = Assert.Single(capture.Bodies);
        AssertNoSentinels(payload);

        using var push = JsonDocument.Parse(payload);
        var stream = Assert.Single(push.RootElement.GetProperty("streams").EnumerateArray());
        var labels = stream.GetProperty("stream");
        Assert.Equal(
            ["app", "env", "level"],
            labels.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal(LoggingSetup.ApplicationName, labels.GetProperty("app").GetString());
        Assert.Equal("production", labels.GetProperty("env").GetString());
        Assert.Equal("warning", labels.GetProperty("level").GetString());

        var value = Assert.Single(stream.GetProperty("values").EnumerateArray());
        var line = value[1].GetString();
        Assert.NotNull(line);
        AssertNoSentinels(line);

        using var body = JsonDocument.Parse(line);
        var root = body.RootElement;
        Assert.Equal(LeadNotificationService.TerminalFailureMessageTemplate, root.GetProperty("MessageTemplate").GetString());
        Assert.Equal("Failed", root.GetProperty("Status").GetString());
        Assert.Equal(1, root.GetProperty("AttemptCount").GetInt32());
        Assert.Equal(pending.Id.ToString(), root.GetProperty("EmailSendLogId").GetString());
        Assert.Equal(lead.Id.ToString(), root.GetProperty("LeadId").GetString());
        Assert.Equal(LoggingSetup.ApplicationName, root.GetProperty("Application").GetString());
        Assert.Equal("Production", root.GetProperty("Environment").GetString());
        Assert.Equal("0.15.1", root.GetProperty("Version").GetString());
        Assert.False(root.TryGetProperty("Exception", out _));
        var message = root.GetProperty("Message").GetString();
        Assert.Contains("Status=\"Failed\"", message, StringComparison.Ordinal);
        Assert.Equal(LeadNotificationService.TerminalFailureEventId, root.GetProperty("EventId").GetProperty("Id").GetInt32());
        Assert.Equal(LeadNotificationService.TerminalFailureEventName, root.GetProperty("EventId").GetProperty("Name").GetString());

        var stored = Assert.Single(logs.Logs);
        Assert.Equal(EmailSendStatus.Failed, stored.Status);
        Assert.Contains(SentinelSecret, stored.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TerminalFailure_NullLeadId_SerializesAsJsonNull()
    {
        var capture = new LokiCaptureHandler();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GrafanaLoki:Url"] = "https://logs-prod-024.grafana.net",
                ["GrafanaLoki:Username"] = "123456",
                ["GrafanaLoki:ApiToken"] = LokiToken,
            })
            .Build();
        var loggerConfiguration = new LoggerConfiguration();
        Assert.True(LoggingSetup.Configure(loggerConfiguration, configuration, "Production", "0.15.1", capture));
        using var serilogLogger = loggerConfiguration.CreateLogger();
        using var provider = new ServiceCollection()
            .AddLogging(builder => builder.AddSerilog(serilogLogger, dispose: false))
            .BuildServiceProvider();

        var now = new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);
        var logs = new MemoryLogs();
        var options = new LeadNotificationOptions { To = SentinelTo, Subject = SentinelSubject };
        var pending = LeadNotificationService.NewPending(
            new Lead(Guid.NewGuid(), SentinelName, SentinelEmail, SentinelCity, SentinelBody, now),
            options,
            "Fake",
            now) with { LeadId = null };
        logs.Logs.Add(pending);
        var sut = new LeadNotificationService(
            new FakeSender(_ => EmailSendResult.Sent("unused")),
            logs,
            new MemoryLeads(),
            options,
            new FixedTime(now),
            provider.GetRequiredService<ILogger<LeadNotificationService>>());

        await sut.ProcessDueAsync(CancellationToken.None);
        serilogLogger.Dispose();

        var line = JsonDocument.Parse(Assert.Single(capture.Bodies))
            .RootElement.GetProperty("streams")[0]
            .GetProperty("values")[0][1]
            .GetString();
        Assert.NotNull(line);
        AssertNoSentinels(line);
        using var body = JsonDocument.Parse(line);
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("LeadId").ValueKind);
        Assert.Equal(0, body.RootElement.GetProperty("AttemptCount").GetInt32());
        Assert.Equal("Failed", body.RootElement.GetProperty("Status").GetString());
    }

    private static void AssertNoSentinels(string text)
    {
        foreach (var sentinel in new[]
        {
            SentinelName, SentinelEmail, SentinelPhone, SentinelPhoneFormatted, SentinelCity, SentinelBody,
            SentinelTo, SentinelSubject, SentinelSecret, SentinelProviderId, LokiToken,
        })
        {
            Assert.DoesNotContain(sentinel, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class LokiCaptureHandler : HttpMessageHandler
    {
        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Bodies.Add(body);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeSender(Func<EmailMessage, EmailSendResult> send) : IEmailSender
    {
        public string Provider => "Fake";

        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
            Task.FromResult(send(message));
    }

    private sealed class MemoryLeads : ILeadRepository
    {
        private readonly List<Lead> leads = [];

        public void Add(Lead lead) => leads.Add(lead);

        public Lead? Find(Guid id) => leads.FirstOrDefault(lead => lead.Id == id);

        public (IReadOnlyList<Lead> Items, int Total) List(LeadStatus? status, int skip, int take) => ([], 0);

        public bool Update(Lead lead) => false;

        public bool Delete(Guid id) => false;

        public IReadOnlyDictionary<LeadStatus, int> CountByStatus() => new Dictionary<LeadStatus, int>();
    }

    private sealed class MemoryLogs : IEmailSendLogRepository
    {
        public List<EmailSendLog> Logs { get; } = [];

        public Task AddAsync(EmailSendLog log, CancellationToken cancellationToken)
        {
            Logs.Add(log);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<EmailSendLog>> ClaimDueAsync(
            DateTimeOffset now, DateTimeOffset leaseUntil, int max, CancellationToken cancellationToken)
        {
            var due = Logs.Where(log => log.IsOpen && log.NextAttemptAtUtc <= now).Take(max).ToList();
            var claimed = due.Select(log => log with { NextAttemptAtUtc = leaseUntil }).ToList();
            foreach (var row in claimed)
            {
                Logs[Logs.FindIndex(log => log.Id == row.Id)] = row;
            }

            return Task.FromResult<IReadOnlyList<EmailSendLog>>(claimed);
        }

        public Task UpdateAsync(EmailSendLog log, CancellationToken cancellationToken)
        {
            Logs[Logs.FindIndex(row => row.Id == log.Id)] = log;
            return Task.CompletedTask;
        }
    }
}
