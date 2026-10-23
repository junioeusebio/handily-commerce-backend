using HandilyCommerce.Application.Leads;
using HandilyCommerce.Domain.Changelog;
using HandilyCommerce.Domain.Courses;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Notifications;
using HandilyCommerce.Domain.Products;
using HandilyCommerce.Infrastructure.Changelog;
using HandilyCommerce.Infrastructure.Courses;
using HandilyCommerce.Infrastructure.Health;
using HandilyCommerce.Infrastructure.Leads;
using HandilyCommerce.Infrastructure.Notifications;
using HandilyCommerce.Infrastructure.Persistence;
using HandilyCommerce.Infrastructure.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HandilyCommerce.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Transaction pooler (6543) needs No Reset On Close=true, or reused pooled connections hang.
        var connectionString = PostgresConnectionString.ForPooler(configuration.GetConnectionString("Default"));

        services.AddDbContext<HandilyCommerceDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IChangelogRepository, EfChangelogRepository>();
        services.AddScoped<IProductRepository, EfProductRepository>();
        // Scoped like the DbContext it depends on (never Singleton → captive DbContext).
        services.AddScoped<ICourseRepository, EfCourseRepository>();
        services.AddScoped<ILeadRepository, EfLeadRepository>();
        AddLeadNotifications(services, configuration);
        services.AddHealthChecks()
            .AddCheck<ApplicationHealthCheck>("application");

        return services;
    }

    private static void AddLeadNotifications(IServiceCollection services, IConfiguration configuration)
    {
        var resend = new ResendOptions();
        configuration.GetSection(ResendOptions.SectionName).Bind(resend);
        var leadNotification = new LeadNotificationOptions();
        configuration.GetSection(LeadNotificationOptions.SectionName).Bind(leadNotification);

        services.AddSingleton(resend);
        services.AddSingleton(leadNotification);
        services.AddHttpClient<IEmailSender, ResendEmailSender>(client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddScoped<IEmailSendLogRepository, EfEmailSendLogRepository>();
        services.AddSingleton<LeadNotificationQueue>();
        services.AddSingleton<ILeadNotificationQueue>(sp => sp.GetRequiredService<LeadNotificationQueue>());
        services.AddHostedService<LeadNotificationWorker>();
    }
}
