using HandilyCommerce.Domain.Changelog;
using HandilyCommerce.Infrastructure.Changelog;
using HandilyCommerce.Infrastructure.Health;
using Microsoft.Extensions.DependencyInjection;

namespace HandilyCommerce.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Future outbound adapters (DB, messaging, etc.) register here.
        services.AddSingleton<IChangelogStore, JsonFileChangelogStore>();
        services.AddHealthChecks()
            .AddCheck<ApplicationHealthCheck>("application");

        return services;
    }
}
