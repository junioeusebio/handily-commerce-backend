using HandilyCommerce.Domain.Products;
using HandilyCommerce.Infrastructure.Persistence;
using HandilyCommerce.Infrastructure.Products;
using Microsoft.EntityFrameworkCore;

namespace HandilyCommerce.Infrastructure.Tests.Products;

/// <summary>
/// Uses EF InMemory — no live Supabase / Postgres required in CI.
/// </summary>
public class EfProductRepositoryTests
{
    private static HandilyCommerceDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HandilyCommerceDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var context = new HandilyCommerceDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void ListAll_WhenEmpty_ReturnsEmpty()
    {
        using var context = CreateContext(nameof(ListAll_WhenEmpty_ReturnsEmpty));
        var repository = new EfProductRepository(context);

        Assert.Empty(repository.ListAll());
    }

    [Fact]
    public void ListAll_IncludesItemsForProduct()
    {
        using var context = CreateContext(nameof(ListAll_IncludesItemsForProduct));
        var productId = Guid.Parse("c30c0000-0001-4000-8000-000000000001");
        var itemId = Guid.Parse("c30c0000-0002-4000-8000-000000000002");
        context.Products.Add(new Product
        {
            Id = productId,
            Name = "Notebook",
            Sku = "NB-01",
            CreatedAt = DateTimeOffset.Parse("2026-09-15T12:00:00Z"),
            Items =
            [
                new Item
                {
                    Id = itemId,
                    ProductId = productId,
                    Name = "Notebook A5",
                    UnitPrice = 12.50m
                }
            ]
        });
        context.SaveChanges();

        var repository = new EfProductRepository(context);
        var products = repository.ListAll();

        var product = Assert.Single(products);
        Assert.Equal("Notebook", product.Name);
        Assert.Equal("NB-01", product.Sku);
        var item = Assert.Single(product.Items);
        Assert.Equal(itemId, item.Id);
        Assert.Equal(12.50m, item.UnitPrice);
    }

    [Fact]
    public void EfProductRepository_ImplementsIProductRepository()
    {
        using var context = CreateContext(nameof(EfProductRepository_ImplementsIProductRepository));
        IProductRepository repository = new EfProductRepository(context);

        Assert.NotNull(repository.ListAll());
    }
}
