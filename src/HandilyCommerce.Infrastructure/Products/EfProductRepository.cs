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

    public Product? Find(Guid id) =>
        dbContext.Products
            .AsNoTracking()
            .Include(p => p.Items)
            .FirstOrDefault(p => p.Id == id);

    public void Add(Product product)
    {
        dbContext.Products.Add(product);
        dbContext.SaveChanges();
    }

    public bool Update(Product product)
    {
        var stored = dbContext.Products.Include(p => p.Items).FirstOrDefault(p => p.Id == product.Id);
        if (stored is null)
        {
            return false;
        }

        stored.Name = product.Name;
        stored.Sku = product.Sku;
        dbContext.Items.RemoveRange(stored.Items);
        stored.Items.Clear();
        foreach (var item in product.Items)
        {
            item.ProductId = stored.Id;
            dbContext.Items.Add(item);
        }

        dbContext.SaveChanges();
        return true;
    }

    public bool Delete(Guid id)
    {
        var stored = dbContext.Products.Include(p => p.Items).FirstOrDefault(p => p.Id == id);
        if (stored is null)
        {
            return false;
        }

        dbContext.Products.Remove(stored);
        dbContext.SaveChanges();
        return true;
    }

    public bool SkuExists(string sku, Guid? exceptId) =>
        dbContext.Products.Any(p => p.Sku == sku && (exceptId == null || p.Id != exceptId));

    public int Count() => dbContext.Products.Count();
}
