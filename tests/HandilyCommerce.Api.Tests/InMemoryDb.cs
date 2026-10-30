using HandilyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HandilyCommerce.Api.Tests;

/// <summary>Swaps the Npgsql DbContext registration for EF InMemory (one database per app instance).</summary>
internal static class InMemoryDb
{
    public static void Use(IServiceCollection services, string database)
    {
        var npgsql = services
            .Where(d => d.ServiceType.IsGenericType
                && d.ServiceType.GenericTypeArguments.Contains(typeof(HandilyCommerceDbContext)))
            .ToList();
        foreach (var descriptor in npgsql)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<HandilyCommerceDbContext>(o => o.UseInMemoryDatabase(database));
    }
}
