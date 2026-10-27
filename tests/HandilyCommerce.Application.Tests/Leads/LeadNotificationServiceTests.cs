using HandilyCommerce.Application.Leads;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Notifications;

namespace HandilyCommerce.Application.Tests.Leads;

public class LeadNotificationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 18, 0, 0, TimeSpan.Zero);
    private static readonly Lead Lead = new(Guid.NewGuid(), "Ana <b>", "ana@x.com", "Recife & Olinda", null, Now);

    private static (LeadNotificationService Sut, FakeLogRepository Logs) Create(IEmailSender sender)
    {
        var logs = new FakeLogRepository();
        return (new LeadNotificationService(sender, logs, new LeadNotificationOptions(), new FixedTimeProvider(Now)), logs);
    }

    [Fact]
    public async Task NotifyAsync_Sent_LogsSuccessWithDefaults()
    {
        var sender = new FakeSender(_ => EmailSendResult.Sent("msg-1"));
        var (sut, logs) = Create(sender);

        var log = await sut.NotifyAsync(Lead, CancellationToken.None);

        Assert.Same(log, Assert.Single(logs.Logs));
        Assert.Equal(EmailSendStatus.Sent, log.Status);
        Assert.Equal("msg-1", log.ProviderMessageId);
        Assert.Equal(Lead.Id, log.LeadId);
        Assert.Equal("ajksys@protonmail.com", log.ToAddress);
        Assert.Equal("[NOVO CONTATO] - Lead BCC", log.Subject);
        Assert.Equal("Fake", log.Provider);
        Assert.Equal(Now, log.AttemptedAtUtc);
        Assert.Null(log.ErrorMessage);
        Assert.Equal("ajksys@protonmail.com", sender.Last!.To);
        Assert.Contains("Ana &lt;b&gt;", sender.Last.HtmlBody);
    }

    [Fact]
    public async Task NotifyAsync_SenderThrows_LogsTruncatedFailure()
    {
        var (sut, logs) = Create(new FakeSender(_ => throw new HttpRequestException(new string('x', 2000))));

        var log = await sut.NotifyAsync(Lead, CancellationToken.None);

        Assert.Equal(EmailSendStatus.Failed, log.Status);
        Assert.StartsWith("HttpRequestException: ", log.ErrorMessage);
        Assert.Equal(LeadNotificationService.ErrorMaxLength, log.ErrorMessage!.Length);
        Assert.Single(logs.Logs);
    }

    [Fact]
    public async Task NotifyAsync_Skipped_LogsSkipped()
    {
        var (sut, _) = Create(new FakeSender(_ => EmailSendResult.Skipped("no key")));

        var log = await sut.NotifyAsync(Lead, CancellationToken.None);

        Assert.Equal(EmailSendStatus.Skipped, log.Status);
        Assert.Equal("no key", log.ErrorMessage);
    }

    [Fact]
    public async Task NotifyAsync_NullLead_Throws()
    {
        var (sut, _) = Create(new FakeSender(_ => EmailSendResult.Sent(null)));

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.NotifyAsync(null!, CancellationToken.None));
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

        public string Provider => "Fake";

        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Last = message;
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
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
