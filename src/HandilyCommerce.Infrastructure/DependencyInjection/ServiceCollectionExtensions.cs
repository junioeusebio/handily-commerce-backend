using HandilyCommerce.Domain.Changelog;
using HandilyCommerce.Domain.Courses;
using HandilyCommerce.Domain.Products;
using HandilyCommerce.Infrastructure.Changelog;
using HandilyCommerce.Infrastructure.Courses;
using HandilyCommerce.Infrastructure.Health;
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
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<HandilyCommerceDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IChangelogRepository, EfChangelogRepository>();
        services.AddScoped<IProductRepository, EfProductRepository>();
        services.AddSingleton<ICourseRepository, InMemoryCourseRepository>();
        services.AddHealthChecks()
            .AddCheck<ApplicationHealthCheck>("application");

        return services;
    }
}
