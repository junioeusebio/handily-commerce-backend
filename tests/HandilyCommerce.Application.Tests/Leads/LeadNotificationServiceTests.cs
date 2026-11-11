using HandilyCommerce.Application.Leads;
using HandilyCommerce.Application.Tests.Fakes;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace HandilyCommerce.Application.Tests.Leads;

public class LeadNotificationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 18, 0, 0, TimeSpan.Zero);
    private static readonly Lead Lead = new(Guid.NewGuid(), "Ana <b>", "ana@x.com", "Recife & Olinda", null, Now);

    private sealed class Ctx
    {
        public FakeSender Sender { get; init; } = null!;
        public FakeLogRepository Logs { get; } = new();
        public InMemoryLeadRepository Leads { get; } = new();
        public MutableTimeProvider Clock { get; } = new(Now);
        public LeadNotificationOptions Options { get; } = new() { MaxAttempts = 3, BaseDelaySeconds = 10 };
        public CapturingLogger Logger { get; } = new();

        public LeadNotificationService Sut => new(Sender, Logs, Leads, Options, Clock, Logger);

        public EmailSendLog Seed()
        {
            Leads.Add(Lead);
            var pending = LeadNotificationService.NewPending(Lead, Options, Sender.Provider, Clock.GetUtcNow());
            Logs.Logs.Add(pending);
            return pending;
        }
    }

    private static Ctx Create(Func<EmailMessage, EmailSendResult> send) => new() { Sender = new FakeSender(send) };

    [Fact]
    public void NewPending_HasZeroAttempts_AndIsDueNow()
    {
        var log = LeadNotificationService.NewPending(Lead, new LeadNotificationOptions(), "Fake", Now);

        Assert.Equal(EmailSendStatus.Pending, log.Status);
        Assert.Equal(0, log.AttemptCount);
        Assert.Equal(Now, log.NextAttemptAtUtc);
        Assert.Equal(Lead.Id, log.LeadId);
        Assert.Equal("ajksys@protonmail.com", log.ToAddress);
        Assert.Equal("[NOVO CONTATO] - Lead BCC", log.Subject);
        Assert.True(log.IsOpen);
        Assert.Throws<ArgumentNullException>(() => LeadNotificationService.NewPending(null!, new LeadNotificationOptions(), "F", Now));
        Assert.Throws<ArgumentNullException>(() => LeadNotificationService.NewPending(Lead, null!, "F", Now));
    }

    [Fact]
    public async Task ProcessDue_Success_MarksSentWithDefaults()
    {
        var ctx = Create(_ => EmailSendResult.Sent("msg-1"));
        ctx.Seed();

        Assert.Equal(1, await ctx.Sut.ProcessDueAsync(CancellationToken.None));

        var log = Assert.Single(ctx.Logs.Logs);
        Assert.Equal(EmailSendStatus.Sent, log.Status);
        Assert.Equal("msg-1", log.ProviderMessageId);
        Assert.Equal(1, log.AttemptCount);
        Assert.Null(log.NextAttemptAtUtc);
        Assert.Null(log.ErrorMessage);
        Assert.Equal("ajksys@protonmail.com", ctx.Sender.Last!.To);
        Assert.Contains("Ana &lt;b&gt;", ctx.Sender.Last.HtmlBody);
    }

    [Fact]
    public async Task ProcessDue_TransientFailures_RetryWithExponentialBackoff_ThenGiveUp()
    {
        var ctx = Create(_ => EmailSendResult.Failed("HTTP 503: down", retryable: true));
        ctx.Seed();

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);
        var first = Assert.Single(ctx.Logs.Logs);
        Assert.Equal(EmailSendStatus.Retrying, first.Status);
        Assert.Equal(1, first.AttemptCount);
        Assert.Equal(Now.AddSeconds(10), first.NextAttemptAtUtc);

        // Not due yet → nothing claimed, nothing sent.
        Assert.Equal(0, await ctx.Sut.ProcessDueAsync(CancellationToken.None));
        Assert.Equal(1, ctx.Sender.Calls);

        ctx.Clock.Now = Now.AddSeconds(10);
        await ctx.Sut.ProcessDueAsync(CancellationToken.None);
        var second = Assert.Single(ctx.Logs.Logs);
        Assert.Equal(EmailSendStatus.Retrying, second.Status);
        Assert.Equal(2, second.AttemptCount);
        Assert.Equal(ctx.Clock.Now.AddSeconds(20), second.NextAttemptAtUtc);

        ctx.Clock.Now = ctx.Clock.Now.AddSeconds(20);
        await ctx.Sut.ProcessDueAsync(CancellationToken.None);
        var last = Assert.Single(ctx.Logs.Logs);
        Assert.Equal(EmailSendStatus.GivenUp, last.Status);
        Assert.Equal(3, last.AttemptCount);
        Assert.Null(last.NextAttemptAtUtc);
        Assert.Equal("HTTP 503: down", last.ErrorMessage);

        ctx.Clock.Now = ctx.Clock.Now.AddDays(1);
        Assert.Equal(0, await ctx.Sut.ProcessDueAsync(CancellationToken.None));
        Assert.Equal(3, ctx.Sender.Calls);
    }

    [Fact]
    public async Task ProcessDue_RetryThenSuccess_SendsExactlyOnce()
    {
        var calls = 0;
        var ctx = Create(_ => ++calls == 1 ? EmailSendResult.Failed("HTTP 429: slow down", retryable: true) : EmailSendResult.Sent("ok"));
        ctx.Seed();

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);
        ctx.Clock.Now = Now.AddMinutes(5);
        await ctx.Sut.ProcessDueAsync(CancellationToken.None);
        ctx.Clock.Now = Now.AddHours(5);
        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        var log = Assert.Single(ctx.Logs.Logs);
        Assert.Equal(EmailSendStatus.Sent, log.Status);
        Assert.Equal(2, log.AttemptCount);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task ProcessDue_NonRetryable4xx_IsFinalFailed()
    {
        var ctx = Create(_ => EmailSendResult.Failed("HTTP 422: invalid"));
        ctx.Seed();

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);
        ctx.Clock.Now = Now.AddDays(1);
        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        var log = Assert.Single(ctx.Logs.Logs);
        Assert.Equal(EmailSendStatus.Failed, log.Status);
        Assert.Null(log.NextAttemptAtUtc);
        Assert.Equal(1, ctx.Sender.Calls);
    }

    [Fact]
    public async Task ProcessDue_SenderThrows_IsRetryable_AndTruncated()
    {
        var ctx = Create(_ => throw new HttpRequestException(new string('x', 2000)));
        ctx.Seed();

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        var log = Assert.Single(ctx.Logs.Logs);
        Assert.Equal(EmailSendStatus.Retrying, log.Status);
        Assert.StartsWith("HttpRequestException: ", log.ErrorMessage);
        Assert.Equal(LeadNotificationService.ErrorMaxLength, log.ErrorMessage!.Length);
    }

    [Fact]
    public async Task ProcessDue_Skipped_IsFinal()
    {
        var ctx = Create(_ => EmailSendResult.Skipped("no key"));
        ctx.Seed();

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        var log = Assert.Single(ctx.Logs.Logs);
        Assert.Equal(EmailSendStatus.Skipped, log.Status);
        Assert.Equal("no key", log.ErrorMessage);
        Assert.Null(log.NextAttemptAtUtc);
    }

    [Fact]
    public async Task ProcessDue_MissingLead_FailsWithoutSending()
    {
        var ctx = Create(_ => EmailSendResult.Sent("x"));
        ctx.Logs.Logs.Add(LeadNotificationService.NewPending(Lead, ctx.Options, "Fake", Now));
        ctx.Logs.Logs.Add(LeadNotificationService.NewPending(Lead, ctx.Options, "Fake", Now) with { LeadId = null });

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        Assert.All(ctx.Logs.Logs, l => Assert.Equal(EmailSendStatus.Failed, l.Status));
        Assert.Equal(0, ctx.Sender.Calls);
    }

    [Theory]
    [InlineData(EmailSendStatus.Sent)]
    [InlineData(EmailSendStatus.Failed)]
    [InlineData(EmailSendStatus.Skipped)]
    [InlineData(EmailSendStatus.GivenUp)]
    public async Task Attempt_FinalRow_IsNeverSentAgain(EmailSendStatus status)
    {
        var ctx = Create(_ => EmailSendResult.Sent("x"));
        var row = ctx.Seed() with { Status = status };

        var result = await ctx.Sut.AttemptAsync(row, CancellationToken.None);

        Assert.Same(row, result);
        Assert.Equal(0, ctx.Sender.Calls);
    }

    [Fact]
    public void BackoffDelay_IsExponential_AndCapped()
    {
        var ctx = Create(_ => EmailSendResult.Sent(null));
        ctx.Options.MaxDelaySeconds = 100;

        Assert.Equal(TimeSpan.FromSeconds(10), ctx.Sut.BackoffDelay(1));
        Assert.Equal(TimeSpan.FromSeconds(40), ctx.Sut.BackoffDelay(3));
        Assert.Equal(TimeSpan.FromSeconds(100), ctx.Sut.BackoffDelay(10));
    }

    [Fact]
    public void Template_EncodesValuesAndShowsFields()
    {
        var html = LeadEmailTemplate.Build(Lead with { Message = "<script>x</script>" });

        Assert.Contains("Ana &lt;b&gt;", html);
        Assert.Contains("Recife &amp; Olinda", html);
        Assert.Contains("ana@x.com", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("23/10/2026 15:00", html);
    }

    [Fact]
    public async Task ProcessDue_TerminalFailed_EmitsOnceAfterSave_AndNotOnLaterPoll()
    {
        var ctx = Create(_ => EmailSendResult.Failed("HTTP 422: invalid"));
        var pending = ctx.Seed();
        var savedBeforeLog = false;
        ctx.Logger.OnLog = () =>
        {
            Assert.Equal(EmailSendStatus.Failed, ctx.Logs.Logs.Single().Status);
            savedBeforeLog = true;
        };

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        var entry = Assert.Single(ctx.Logger.Entries);
        Assert.True(savedBeforeLog);
        AssertTerminalFailure(entry, "Failed", 1, pending.Id, Lead.Id);

        ctx.Clock.Now = Now.AddDays(1);
        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        Assert.Single(ctx.Logger.Entries);
        Assert.Equal(1, ctx.Sender.Calls);
    }

    [Fact]
    public async Task ProcessDue_GivenUp_EmitsOnlyOnFinalTransition()
    {
        var ctx = Create(_ => EmailSendResult.Failed("HTTP 503: down", retryable: true));
        var pending = ctx.Seed();

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);
        Assert.Empty(ctx.Logger.Entries);
        Assert.Equal(EmailSendStatus.Retrying, ctx.Logs.Logs.Single().Status);

        ctx.Clock.Now = Now.AddSeconds(10);
        await ctx.Sut.ProcessDueAsync(CancellationToken.None);
        Assert.Empty(ctx.Logger.Entries);

        ctx.Clock.Now = ctx.Clock.Now.AddSeconds(20);
        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        var entry = Assert.Single(ctx.Logger.Entries);
        AssertTerminalFailure(entry, "GivenUp", 3, pending.Id, Lead.Id);
        Assert.Equal(EmailSendStatus.GivenUp, ctx.Logs.Logs.Single().Status);

        ctx.Clock.Now = ctx.Clock.Now.AddDays(1);
        await ctx.Sut.ProcessDueAsync(CancellationToken.None);
        Assert.Single(ctx.Logger.Entries);
    }

    [Fact]
    public async Task ProcessDue_RetryThenNonRetryable_EmitsOnlyFinalFailed()
    {
        var calls = 0;
        var ctx = Create(_ => ++calls == 1
            ? EmailSendResult.Failed("HTTP 429: slow down", retryable: true)
            : EmailSendResult.Failed("HTTP 422: invalid"));
        var pending = ctx.Seed();

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);
        Assert.Empty(ctx.Logger.Entries);

        ctx.Clock.Now = Now.AddMinutes(5);
        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        var entry = Assert.Single(ctx.Logger.Entries);
        AssertTerminalFailure(entry, "Failed", 2, pending.Id, Lead.Id);
        Assert.Equal(EmailSendStatus.Failed, ctx.Logs.Logs.Single().Status);
    }

    [Fact]
    public async Task ProcessDue_Success_DoesNotEmitTerminalFailure()
    {
        var ctx = Create(_ => EmailSendResult.Sent("msg-1"));
        ctx.Seed();

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        Assert.Equal(EmailSendStatus.Sent, ctx.Logs.Logs.Single().Status);
        Assert.Empty(ctx.Logger.Entries);
    }

    [Fact]
    public async Task ProcessDue_Skipped_DoesNotEmitTerminalFailure()
    {
        var ctx = Create(_ => EmailSendResult.Skipped("no key"));
        ctx.Seed();

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        Assert.Equal(EmailSendStatus.Skipped, ctx.Logs.Logs.Single().Status);
        Assert.Empty(ctx.Logger.Entries);
    }

    [Fact]
    public async Task ProcessDue_Retrying_DoesNotEmitTerminalFailure()
    {
        var ctx = Create(_ => EmailSendResult.Failed("HTTP 503: down", retryable: true));
        ctx.Options.MaxAttempts = 5;
        ctx.Seed();

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        Assert.Equal(EmailSendStatus.Retrying, ctx.Logs.Logs.Single().Status);
        Assert.Empty(ctx.Logger.Entries);
    }

    [Fact]
    public async Task ProcessDue_SaveThrows_DoesNotEmit_AndLeavesRowOpen()
    {
        const string sentinel = "SENTINEL_SAVE_NOME_ALICE";
        var ctx = Create(_ => EmailSendResult.Failed($"HTTP 422: {sentinel}"));
        ctx.Seed();
        ctx.Logs.UpdateException = new InvalidOperationException($"db down {sentinel}");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => ctx.Sut.ProcessDueAsync(CancellationToken.None));

        Assert.Contains(sentinel, ex.Message, StringComparison.Ordinal);
        Assert.Empty(ctx.Logger.Entries);
        Assert.Equal(EmailSendStatus.Pending, ctx.Logs.Logs.Single().Status);
    }

    [Theory]
    [InlineData(EmailSendStatus.Failed)]
    [InlineData(EmailSendStatus.GivenUp)]
    public async Task ProcessDue_ClaimedFinalRow_DoesNotEmitAgain(EmailSendStatus status)
    {
        var ctx = Create(_ => EmailSendResult.Sent("x"));
        var row = ctx.Seed() with { Status = status, NextAttemptAtUtc = Now, AttemptCount = 4 };
        ctx.Logs.Logs[0] = row;
        ctx.Logs.IncludeTerminalRows = true;

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        Assert.Empty(ctx.Logger.Entries);
        Assert.Equal(0, ctx.Sender.Calls);
        Assert.Equal(status, ctx.Logs.Logs.Single().Status);
    }

    [Fact]
    public async Task ProcessDue_MissingLead_EmitsFailedWithoutErrorText()
    {
        const string sentinelAddress = "sentinel.destinatario@example.invalid";
        const string sentinelSubject = "SENTINEL_ASSUNTO";
        var ctx = Create(_ => EmailSendResult.Sent("should-not-send"));
        ctx.Options.To = sentinelAddress;
        ctx.Options.Subject = sentinelSubject;
        var pending = LeadNotificationService.NewPending(Lead, ctx.Options, "Fake", Now) with { LeadId = null };
        ctx.Logs.Logs.Add(pending);

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        var stored = ctx.Logs.Logs.Single();
        Assert.Equal(EmailSendStatus.Failed, stored.Status);
        Assert.Equal("Lead não encontrado.", stored.ErrorMessage);
        Assert.Equal(0, stored.AttemptCount);
        var entry = Assert.Single(ctx.Logger.Entries);
        AssertTerminalFailure(entry, "Failed", 0, pending.Id, null);
        AssertNoSentinels(entry, sentinelAddress, sentinelSubject, "Lead não encontrado.", Lead.Name, Lead.Email);
    }

    [Fact]
    public async Task ProcessDue_Privacy_OmitsSentinelPersonalData()
    {
        const string sentinelName = "SENTINEL_NOME_ALICE";
        const string sentinelEmail = "sentinel.alice@example.invalid";
        const string sentinelPhone = "11988887777";
        const string sentinelPhoneFormatted = "(11) 98888-7777";
        const string sentinelCity = "SENTINEL_CIDADE";
        const string sentinelBody = "SENTINEL_MENSAGEM_CORPO";
        const string sentinelTo = "sentinel.destinatario@example.invalid";
        const string sentinelSubject = "SENTINEL_ASSUNTO";
        const string sentinelSecret = "glc_SENTINEL_SECRET";
        const string sentinelProviderId = "SENTINEL_PROVIDER_ID";
        var lead = new Lead(Guid.NewGuid(), sentinelName, sentinelEmail, sentinelCity, sentinelBody, Now, Phone: sentinelPhone);
        var ctx = Create(_ => EmailSendResult.Failed(
            $"to={sentinelTo} name={sentinelName} mail={sentinelEmail} phone={sentinelPhone} {sentinelPhoneFormatted} city={sentinelCity} body={sentinelBody} subject={sentinelSubject} secret={sentinelSecret}",
            retryable: false) with { ProviderMessageId = sentinelProviderId });
        ctx.Options.To = sentinelTo;
        ctx.Options.Subject = sentinelSubject;
        ctx.Leads.Add(lead);
        var pending = LeadNotificationService.NewPending(lead, ctx.Options, "Fake", Now);
        ctx.Logs.Logs.Add(pending);

        await ctx.Sut.ProcessDueAsync(CancellationToken.None);

        var stored = ctx.Logs.Logs.Single();
        Assert.Equal(EmailSendStatus.Failed, stored.Status);
        Assert.Contains(sentinelSecret, stored.ErrorMessage, StringComparison.Ordinal);
        Assert.Equal(sentinelTo, stored.ToAddress);
        Assert.Contains(sentinelName, ctx.Sender.Last!.HtmlBody, StringComparison.Ordinal);
        var entry = Assert.Single(ctx.Logger.Entries);
        AssertTerminalFailure(entry, "Failed", 1, pending.Id, lead.Id);
        AssertNoSentinels(
            entry,
            sentinelName,
            sentinelEmail,
            sentinelPhone,
            sentinelPhoneFormatted,
            sentinelCity,
            sentinelBody,
            sentinelTo,
            sentinelSubject,
            sentinelSecret,
            sentinelProviderId,
            "HTTP 422");
    }

    [Fact]
    public void Template_NullMessage_ShowsDash()
    {
        Assert.Contains("—", LeadEmailTemplate.Build(Lead));
        Assert.Throws<ArgumentNullException>(() => LeadEmailTemplate.Build(null!));
    }

    [Fact]
    public void Template_WithPhone_ShowsFormattedPhone()
    {
        var html = LeadEmailTemplate.Build(Lead with { Phone = "11912345678" });

        Assert.Contains("Telefone", html);
        Assert.Contains("(11) 91234-5678", html);
        Assert.DoesNotContain("Não informado", html);
    }

    [Fact]
    public void Template_WithoutPhone_ShowsNotInformed()
    {
        var html = LeadEmailTemplate.Build(Lead);

        Assert.Contains("N&#227;o informado", html);
    }

    private sealed class FakeSender(Func<EmailMessage, EmailSendResult> send) : IEmailSender
    {
        public EmailMessage? Last { get; private set; }

        public int Calls { get; private set; }

        public string Provider => "Fake";

        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Last = message;
            Calls++;
            return Task.FromResult(send(message));
        }
    }

    private sealed class FakeLogRepository : IEmailSendLogRepository
    {
        public List<EmailSendLog> Logs { get; } = [];

        public Task AddAsync(EmailSendLog log, CancellationToken cancellationToken)
        {
            Logs.Add(log);
            return Task.CompletedTask;
        }

        public bool IncludeTerminalRows { get; set; }

        public Exception? UpdateException { get; set; }

        public Task<IReadOnlyList<EmailSendLog>> ClaimDueAsync(
            DateTimeOffset now, DateTimeOffset leaseUntil, int max, CancellationToken cancellationToken)
        {
            var due = Logs.Where(l => (l.IsOpen || IncludeTerminalRows) && l.NextAttemptAtUtc <= now).Take(max).ToList();
            var claimed = due.Select(l => l with { NextAttemptAtUtc = leaseUntil }).ToList();
            foreach (var c in claimed)
            {
                Logs[Logs.FindIndex(l => l.Id == c.Id)] = c;
            }

            return Task.FromResult<IReadOnlyList<EmailSendLog>>(claimed);
        }

        public Task UpdateAsync(EmailSendLog log, CancellationToken cancellationToken)
        {
            if (UpdateException is not null)
            {
                throw UpdateException;
            }

            Logs[Logs.FindIndex(l => l.Id == log.Id)] = log;
            return Task.CompletedTask;
        }
    }

    private static void AssertTerminalFailure(
        CapturedLog entry, string status, int attemptCount, Guid emailSendLogId, Guid? leadId)
    {
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(LeadNotificationService.TerminalFailureEventId, entry.EventId.Id);
        Assert.Equal(LeadNotificationService.TerminalFailureEventName, entry.EventId.Name);
        Assert.Null(entry.Exception);
        Assert.Equal(status, entry.Properties["Status"]);
        Assert.Equal(attemptCount.ToString(), entry.Properties["AttemptCount"]);
        Assert.Equal(emailSendLogId.ToString(), entry.Properties["EmailSendLogId"]);
        Assert.Equal(leadId?.ToString(), entry.Properties["LeadId"]);
        Assert.Equal(LeadNotificationService.TerminalFailureMessageTemplate, entry.Properties["{OriginalFormat}"]);
        Assert.Equal(
            ["AttemptCount", "EmailSendLogId", "LeadId", "Status", "{OriginalFormat}"],
            entry.Properties.Keys.Order(StringComparer.Ordinal).ToArray());
        Assert.Contains(LeadNotificationService.TerminalFailureEventName, entry.Message, StringComparison.Ordinal);
        Assert.Contains($"Status={status}", entry.Message, StringComparison.Ordinal);
        Assert.Contains($"AttemptCount={attemptCount}", entry.Message, StringComparison.Ordinal);
    }

    private static void AssertNoSentinels(CapturedLog entry, params string[] sentinels)
    {
        var blob = string.Join(
            "\n",
            new[] { entry.Message, entry.Exception?.ToString() }.Concat(entry.Properties.Select(p => p.Key + "=" + p.Value)));
        foreach (var sentinel in sentinels)
        {
            Assert.DoesNotContain(sentinel, blob, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed record CapturedLog(
        LogLevel Level,
        EventId EventId,
        string Message,
        Exception? Exception,
        Dictionary<string, string?> Properties);

    private sealed class CapturingLogger : ILogger<LeadNotificationService>
    {
        public List<CapturedLog> Entries { get; } = [];

        public Action? OnLog { get; set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            OnLog?.Invoke();
            var properties = new Dictionary<string, string?>(StringComparer.Ordinal);
            if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                foreach (var pair in pairs)
                {
                    properties[pair.Key] = pair.Value?.ToString();
                }
            }

            Entries.Add(new CapturedLog(logLevel, eventId, formatter(state, exception), exception, properties));
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
