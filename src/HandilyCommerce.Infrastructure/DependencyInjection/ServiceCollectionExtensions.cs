using HandilyCommerce.Domain.Changelog;
using HandilyCommerce.Infrastructure.Changelog;
using HandilyCommerce.Infrastructure.Health;
using HandilyCommerce.Infrastructure.Persistence;
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
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<HandilyCommerceDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IChangelogRepository, EfChangelogRepository>();
        services.AddHealthChecks()
            .AddCheck<ApplicationHealthCheck>("application");

        return services;
    }
}
