using HandilyCommerce.Application.Health;
using HandilyCommerce.Application.Ping;
using HandilyCommerce.Domain.Health;
using HandilyCommerce.Domain.Ping;
using Microsoft.Extensions.DependencyInjection;

namespace HandilyCommerce.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IHealthPort, HealthService>();
        services.AddSingleton<IPingPort, PingService>();
        return services;
    }
}
