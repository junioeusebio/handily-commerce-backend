using HandilyCommerce.Domain.Products;
using HandilyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HandilyCommerce.Infrastructure.Products;

/// <summary>
/// EF Core adapter for <see cref="IProductRepository"/> against Supabase Postgres.
/// </summary>
public sealed class EfProductRepository(HandilyCommerceDbContext dbContext) : IProductRepository
{
    public IReadOnlyList<Product> ListAll() =>
        dbContext.Products
            .AsNoTracking()
            .Include(p => p.Items)
            .ToList();
}
