using System.Net;
using System.Text.Json;
using HandilyCommerce.Application.Leads;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Notifications;
using HandilyCommerce.Infrastructure.Leads;
using HandilyCommerce.Infrastructure.Notifications;
using HandilyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HandilyCommerce.Infrastructure.Tests.Notifications;

public class NotificationsInfrastructureTests
{
    private static readonly EmailMessage Message = new("to@x.com", "Assunto", "<p>oi</p>");

    private static ResendEmailSender Sender(StubHandler handler, string? apiKey = "test-key") =>
        new(new HttpClient(handler), new ResendOptions { ApiKey = apiKey });

    [Fact]
    public async Task Resend_Success_PostsBearerJsonAndReturnsId()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"id":"abc-123"}""");

        var result = await Sender(handler).SendAsync(Message, CancellationToken.None);

        Assert.Equal(EmailSendStatus.Sent, result.Status);
        Assert.Equal("abc-123", result.ProviderMessageId);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://api.resend.com/emails", handler.Request.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        using var json = JsonDocument.Parse(handler.Body!);
        Assert.Equal("to@x.com", json.RootElement.GetProperty("to")[0].GetString());
        Assert.Equal("Assunto", json.RootElement.GetProperty("subject").GetString());
        Assert.Equal("<p>oi</p>", json.RootElement.GetProperty("html").GetString());
        Assert.Equal(ResendOptions.DefaultFrom, json.RootElement.GetProperty("from").GetString());
        Assert.Equal("Resend", Sender(handler).Provider);
    }

    [Fact]
    public async Task Resend_SuccessWithInvalidJson_ReturnsSentWithoutId()
    {
        var result = await Sender(new StubHandler(HttpStatusCode.OK, "not json")).SendAsync(Message, CancellationToken.None);

        Assert.Equal(EmailSendStatus.Sent, result.Status);
        Assert.Null(result.ProviderMessageId);
    }

    [Fact]
    public async Task Resend_ErrorStatus_ReturnsFailedWithTruncatedBody()
    {
        var handler = new StubHandler(HttpStatusCode.UnprocessableEntity, new string('e', 900));

        var result = await Sender(handler).SendAsync(Message, CancellationToken.None);

        Assert.Equal(EmailSendStatus.Failed, result.Status);
        Assert.StartsWith("HTTP 422: ", result.ErrorMessage);
        Assert.True(result.ErrorMessage!.Length < 520);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task Resend_NoApiKey_SkipsWithoutHttpCall(string? key)
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{}");

        var result = await Sender(handler, key).SendAsync(Message, CancellationToken.None);

        Assert.Equal(EmailSendStatus.Skipped, result.Status);
        Assert.Null(handler.Request);
        await Assert.ThrowsAsync<ArgumentNullException>(() => Sender(handler).SendAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task EfRepository_PersistsLog_AndMapsTable()
    {
        var options = new DbContextOptionsBuilder<HandilyCommerceDbContext>()
            .UseInMemoryDatabase(nameof(EfRepository_PersistsLog_AndMapsTable)).Options;
        var log = new EmailSendLog(Guid.NewGuid(), Guid.NewGuid(), "to@x.com", "s", "Resend", "id", EmailSendStatus.Sent, null, DateTimeOffset.UtcNow);

        await using (var ctx = new HandilyCommerceDbContext(options))
        {
            await new EfEmailSendLogRepository(ctx).AddAsync(log, CancellationToken.None);
            Assert.Equal("EmailSendLogs", ctx.Model.FindEntityType(typeof(EmailSendLog))!.GetTableName());
        }

        await using var read = new HandilyCommerceDbContext(options);
        Assert.Equal(log, Assert.Single(read.EmailSendLogs.AsNoTracking()));
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    public async Task Resend_ErrorStatus_RetryableOnlyFor429And5xx(HttpStatusCode status, bool retryable)
    {
        var result = await Sender(new StubHandler(status, "err")).SendAsync(Message, CancellationToken.None);

        Assert.Equal(EmailSendStatus.Failed, result.Status);
        Assert.Equal(retryable, result.Retryable);
    }

    private static ServiceProvider BuildProvider(string dbName, HttpStatusCode status, string body = """{"id":"m1"}""")
    {
        var services = new ServiceCollection();
        services.AddDbContext<HandilyCommerceDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<IEmailSendLogRepository, EfEmailSendLogRepository>();
        services.AddScoped<ILeadRepository, EfLeadRepository>();
        services.AddScoped<ILeadNotificationQueue, LeadNotificationQueue>();
        services.AddSingleton<IEmailSender>(Sender(new StubHandler(status, body)));
        services.AddSingleton(new LeadNotificationOptions { PollIntervalSeconds = 1, BaseDelaySeconds = 60 });
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<LeadNotificationSignal>();
        services.AddScoped<LeadNotificationService>();
        return services.BuildServiceProvider();
    }

    private static LeadNotificationWorker Worker(ServiceProvider provider) => new(
        provider.GetRequiredService<LeadNotificationSignal>(),
        provider.GetRequiredService<LeadNotificationOptions>(),
        provider.GetRequiredService<IServiceScopeFactory>(),
        NullLogger<LeadNotificationWorker>.Instance);

    private static Lead NewLead() => new(Guid.NewGuid(), "Ana", "ana@x.com", "Recife", null, DateTimeOffset.UtcNow, Phone: "11912345678");

    private static async Task<EmailSendLog?> WaitForAsync(ServiceProvider provider, Func<EmailSendLog, bool> predicate)
    {
        for (var i = 0; i < 100; i++)
        {
            using var scope = provider.CreateScope();
            var row = scope.ServiceProvider.GetRequiredService<HandilyCommerceDbContext>().EmailSendLogs.AsNoTracking().SingleOrDefault();
            if (row is not null && predicate(row))
            {
                return row;
            }

            await Task.Delay(50);
        }

        return null;
    }

    [Fact]
    public async Task Queue_PersistsPendingRow_AndWorkerSendsIt()
    {
        await using var provider = BuildProvider(Guid.NewGuid().ToString(), HttpStatusCode.OK);
        var worker = Worker(provider);
        var lead = NewLead();
        await worker.StartAsync(CancellationToken.None);

        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ILeadRepository>().Add(lead);
            scope.ServiceProvider.GetRequiredService<ILeadNotificationQueue>().Enqueue(lead);
        }

        var stored = await WaitForAsync(provider, r => r.Status == EmailSendStatus.Sent);
        await worker.StopAsync(CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(lead.Id, stored.LeadId);
        Assert.Equal("m1", stored.ProviderMessageId);
        Assert.Equal(1, stored.AttemptCount);
    }

    [Fact]
    public async Task Worker_OnStartup_RecoversPendingRowLeftByRestart()
    {
        var dbName = Guid.NewGuid().ToString();
        var lead = NewLead();
        await using (var before = BuildProvider(dbName, HttpStatusCode.OK))
        {
            // "Previous instance": lead + Pending row saved, process died before sending.
            using var scope = before.CreateScope();
            scope.ServiceProvider.GetRequiredService<ILeadRepository>().Add(lead);
            scope.ServiceProvider.GetRequiredService<ILeadNotificationQueue>().Enqueue(lead);
        }

        await using var provider = BuildProvider(dbName, HttpStatusCode.OK);
        var worker = Worker(provider);
        await worker.StartAsync(CancellationToken.None);
        var stored = await WaitForAsync(provider, r => r.Status == EmailSendStatus.Sent);
        await worker.StopAsync(CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(lead.Id, stored.LeadId);
    }

    [Fact]
    public async Task Worker_TransientFailure_SchedulesRetry()
    {
        await using var provider = BuildProvider(Guid.NewGuid().ToString(), HttpStatusCode.ServiceUnavailable, "down");
        var lead = NewLead();
        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ILeadRepository>().Add(lead);
            scope.ServiceProvider.GetRequiredService<ILeadNotificationQueue>().Enqueue(lead);
        }

        Assert.Equal(1, await Worker(provider).ProcessBatchAsync(CancellationToken.None));
        Assert.Equal(0, await Worker(provider).ProcessBatchAsync(CancellationToken.None));

        var stored = await WaitForAsync(provider, _ => true);
        Assert.Equal(EmailSendStatus.Retrying, stored!.Status);
        Assert.True(stored.NextAttemptAtUtc > DateTimeOffset.UtcNow.AddSeconds(30));
    }

    [Fact]
    public async Task EfRepository_Claim_LeasesDueOpenRowsOnly_SoSecondClaimGetsNothing()
    {
        var options = new DbContextOptionsBuilder<HandilyCommerceDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var now = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
        var due = new EmailSendLog(Guid.NewGuid(), Guid.NewGuid(), "a", "s", "R", null, EmailSendStatus.Pending, null, now, 0, now);
        var later = due with { Id = Guid.NewGuid(), Status = EmailSendStatus.Retrying, NextAttemptAtUtc = now.AddMinutes(1) };
        var sent = due with { Id = Guid.NewGuid(), Status = EmailSendStatus.Sent, NextAttemptAtUtc = null };
        await using var ctx = new HandilyCommerceDbContext(options);
        var repo = new EfEmailSendLogRepository(ctx);
        foreach (var l in new[] { due, later, sent })
        {
            await repo.AddAsync(l, CancellationToken.None);
        }

        var claimed = Assert.Single(await repo.ClaimDueAsync(now, now.AddMinutes(5), 10, CancellationToken.None));
        Assert.Equal(due.Id, claimed.Id);
        Assert.Equal(now.AddMinutes(5), claimed.NextAttemptAtUtc);
        Assert.Empty(await repo.ClaimDueAsync(now, now.AddMinutes(5), 10, CancellationToken.None));

        await repo.UpdateAsync(claimed with { Status = EmailSendStatus.Sent, NextAttemptAtUtc = null }, CancellationToken.None);
        Assert.Equal(EmailSendStatus.Sent, ctx.EmailSendLogs.AsNoTracking().Single(l => l.Id == due.Id).Status);
        Assert.Empty(await repo.ClaimDueAsync(now.AddDays(1), now.AddDays(2), 10, CancellationToken.None).ContinueWith(t => t.Result.Where(l => l.Id != later.Id).ToList()));
    }

    [Fact]
    public async Task Signal_WaitReturnsOnNotify_OrTimeout()
    {
        var signal = new LeadNotificationSignal();
        signal.Notify();
        signal.Notify(); // Coalesced.
        await signal.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        await signal.WaitAsync(TimeSpan.FromMilliseconds(20), CancellationToken.None);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => signal.WaitAsync(TimeSpan.FromSeconds(10), cts.Token));
    }

    [Fact]
    public async Task Worker_BatchFailure_IsSwallowed()
    {
        await using var provider = new ServiceCollection().AddSingleton<LeadNotificationSignal>()
            .AddSingleton(new LeadNotificationOptions()).BuildServiceProvider();

        Assert.Equal(0, await Worker(provider).ProcessBatchAsync(CancellationToken.None));
        await using var db = BuildProvider(Guid.NewGuid().ToString(), HttpStatusCode.OK);
        using var scope = db.CreateScope();
        Assert.Throws<ArgumentNullException>(() => scope.ServiceProvider.GetRequiredService<ILeadNotificationQueue>().Enqueue(null!));
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body) };
        }
    }
}
