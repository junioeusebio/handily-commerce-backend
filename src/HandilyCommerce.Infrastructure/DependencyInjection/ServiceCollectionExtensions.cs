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
        // JSON repo is temporary; next PR swaps to EF Core implementing IChangelogRepository.
        services.AddSingleton<IChangelogRepository, JsonFileChangelogRepository>();
        services.AddHealthChecks()
            .AddCheck<ApplicationHealthCheck>("application");

        return services;
    }
}
