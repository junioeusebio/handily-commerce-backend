using HandilyCommerce.Application.DependencyInjection;
using HandilyCommerce.Application.Leads;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Notifications;
using HandilyCommerce.Infrastructure.DependencyInjection;
using HandilyCommerce.Infrastructure.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HandilyCommerce.Infrastructure.Tests.Notifications;

public class NotificationsRegistrationTests
{
    [Fact]
    public void AddInfrastructure_RegistersLeadNotifications_AndBindsConfig()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=localhost;Database=x;Username=u;Password=p",
            ["Resend:ApiKey"] = "k",
            ["Resend:From"] = "Leads <leads@example.com>",
            ["LeadNotification:To"] = "dest@example.com",
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();

        Assert.IsType<ResendEmailSender>(scope.ServiceProvider.GetRequiredService<IEmailSender>());
        Assert.IsType<EfEmailSendLogRepository>(scope.ServiceProvider.GetRequiredService<IEmailSendLogRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<LeadNotificationService>());
        Assert.Same(provider.GetRequiredService<LeadNotificationQueue>(), provider.GetRequiredService<ILeadNotificationQueue>());
        Assert.Contains(provider.GetServices<IHostedService>(), s => s is LeadNotificationWorker);
        Assert.Equal("Leads <leads@example.com>", provider.GetRequiredService<ResendOptions>().From);
        var options = provider.GetRequiredService<LeadNotificationOptions>();
        Assert.Equal("dest@example.com", options.To);
        Assert.Equal(LeadNotificationOptions.DefaultSubject, options.Subject);
    }
}
