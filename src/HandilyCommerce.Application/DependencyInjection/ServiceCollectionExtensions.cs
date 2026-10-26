using HandilyCommerce.Application.Admin;
using HandilyCommerce.Application.ApiVersion;
using HandilyCommerce.Application.Auth;
using HandilyCommerce.Application.Changelog;
using HandilyCommerce.Application.Courses;
using HandilyCommerce.Application.Health;
using HandilyCommerce.Application.Leads;
using HandilyCommerce.Application.Ping;
using HandilyCommerce.Application.Products;
using HandilyCommerce.Domain.Admin;
using HandilyCommerce.Domain.ApiVersion;
using HandilyCommerce.Domain.Auth;
using HandilyCommerce.Domain.Changelog;
using HandilyCommerce.Domain.Courses;
using HandilyCommerce.Domain.Health;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Ping;
using HandilyCommerce.Domain.Products;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HandilyCommerce.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IHealthPort, HealthService>();
        services.AddSingleton<IPingPort, PingService>();
        services.AddSingleton<IApiVersionPort, ApiVersionService>();
        services.AddScoped<IChangelogPort, ChangelogService>();
        services.AddScoped<IProductPort, ProductService>();
        services.AddScoped<ICoursePort, CourseService>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ILeadPort, LeadService>();
        services.TryAddSingleton(new LeadNotificationOptions());
        services.AddScoped<LeadNotificationService>();
        services.TryAddSingleton(new AuthOptions());
        services.AddScoped<IAuthPort, AuthService>();
        services.AddScoped<ILeadAdminPort, LeadAdminService>();
        services.AddScoped<ICourseAdminPort, CourseAdminService>();
        services.AddScoped<IProductAdminPort, ProductAdminService>();
        services.AddScoped<IAdminSummaryPort, AdminSummaryService>();
        return services;
    }
}
