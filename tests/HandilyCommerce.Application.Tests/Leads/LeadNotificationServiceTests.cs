using HandilyCommerce.Application.Leads;
using HandilyCommerce.Application.Tests.Fakes;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Notifications;

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

        public LeadNotificationService Sut => new(Sender, Logs, Leads, Options, Clock);

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

        public Task<IReadOnlyList<EmailSendLog>> ClaimDueAsync(
            DateTimeOffset now, DateTimeOffset leaseUntil, int max, CancellationToken cancellationToken)
        {
            var due = Logs.Where(l => l.IsOpen && l.NextAttemptAtUtc <= now).Take(max).ToList();
            var claimed = due.Select(l => l with { NextAttemptAtUtc = leaseUntil }).ToList();
            foreach (var c in claimed)
            {
                Logs[Logs.FindIndex(l => l.Id == c.Id)] = c;
            }

            return Task.FromResult<IReadOnlyList<EmailSendLog>>(claimed);
        }

        public Task UpdateAsync(EmailSendLog log, CancellationToken cancellationToken)
        {
            Logs[Logs.FindIndex(l => l.Id == log.Id)] = log;
            return Task.CompletedTask;
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
