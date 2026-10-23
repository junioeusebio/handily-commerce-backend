using HandilyCommerce.Domain.Notifications;

namespace HandilyCommerce.Domain.Tests.Notifications;

public class NotificationsDomainTests
{
    [Fact]
    public void EmailSendResult_Factories_SetStatus()
    {
        Assert.Equal(new EmailSendResult(EmailSendStatus.Sent, "id", null), EmailSendResult.Sent("id"));
        Assert.Equal(new EmailSendResult(EmailSendStatus.Failed, null, "e"), EmailSendResult.Failed("e"));
        Assert.Equal(new EmailSendResult(EmailSendStatus.Skipped, null, "r"), EmailSendResult.Skipped("r"));
    }

    [Fact]
    public void Records_ExposeValues()
    {
        var at = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        var lead = Guid.NewGuid();
        var log = new EmailSendLog(id, lead, "to@x.com", "s", "Resend", "m", EmailSendStatus.Failed, "err", at);
        var msg = new EmailMessage("to@x.com", "s", "<p/>");

        Assert.Equal((id, lead, "to@x.com", "s", "Resend", "m", EmailSendStatus.Failed, "err", at),
            (log.Id, log.LeadId, log.ToAddress, log.Subject, log.Provider, log.ProviderMessageId, log.Status, log.ErrorMessage, log.AttemptedAtUtc));
        Assert.Equal(("to@x.com", "s", "<p/>"), (msg.To, msg.Subject, msg.HtmlBody));
    }
}
