using System.Net;
using System.Text.Json;
using HandilyCommerce.Application.Leads;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Notifications;
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

    [Fact]
    public async Task Worker_ProcessesQueuedLead_AndLogsAttempt()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<HandilyCommerceDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<IEmailSendLogRepository, EfEmailSendLogRepository>();
        services.AddSingleton<IEmailSender>(Sender(new StubHandler(HttpStatusCode.OK, """{"id":"m1"}""")));
        services.AddSingleton(new LeadNotificationOptions());
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<LeadNotificationService>();
        await using var provider = services.BuildServiceProvider();

        var queue = new LeadNotificationQueue();
        var worker = new LeadNotificationWorker(queue, provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<LeadNotificationWorker>.Instance);
        var lead = new Lead(Guid.NewGuid(), "Ana", "ana@x.com", "Recife", null, DateTimeOffset.UtcNow);

        await worker.StartAsync(CancellationToken.None);
        queue.Enqueue(lead);
        EmailSendLog? stored = null;
        for (var i = 0; i < 50 && stored is null; i++)
        {
            await Task.Delay(50);
            using var scope = provider.CreateScope();
            stored = scope.ServiceProvider.GetRequiredService<HandilyCommerceDbContext>().EmailSendLogs.AsNoTracking().SingleOrDefault();
        }

        await worker.StopAsync(CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal(lead.Id, stored.LeadId);
        Assert.Equal(EmailSendStatus.Sent, stored.Status);
    }

    [Fact]
    public async Task Worker_ProcessFailure_IsSwallowed()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var worker = new LeadNotificationWorker(new LeadNotificationQueue(), provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<LeadNotificationWorker>.Instance);

        await worker.ProcessAsync(new Lead(Guid.NewGuid(), "A", "a@x.com", "R", null, DateTimeOffset.UtcNow), CancellationToken.None);
        Assert.Throws<ArgumentNullException>(() => new LeadNotificationQueue().Enqueue(null!));
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
