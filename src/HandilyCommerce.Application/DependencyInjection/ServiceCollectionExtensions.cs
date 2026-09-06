using HandilyCommerce.Application.Health;
using HandilyCommerce.Domain.Health;
using Microsoft.Extensions.DependencyInjection;

namespace HandilyCommerce.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IHealthPort, HealthService>();
        return services;
    }
}
