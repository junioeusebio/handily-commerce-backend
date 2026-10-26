using HandilyCommerce.Application.Admin;
using HandilyCommerce.Application.Tests.Fakes;
using HandilyCommerce.Domain.Products;

namespace HandilyCommerce.Application.Tests.Admin;

public class ProductAdminServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static ProductUpsertCommand Valid(string? sku = "SKU-1") =>
        new(" Kit ", sku, [new ItemInput("Licença", 10.5m), new ItemInput("Suporte", null)]);

    [Fact]
    public void Create_Valid_AddsProductWithItems()
    {
        var repo = new InMemoryProductRepository();
        var sut = new ProductAdminService(repo, new FixedTime());

        var result = sut.Create(Valid());

        Assert.True(result.IsSuccess);
        var product = Assert.Single(repo.Products);
        Assert.Equal("Kit", product.Name);
        Assert.Equal(Now, product.CreatedAt);
        Assert.Equal(2, product.Items.Count);
        Assert.All(product.Items, i => Assert.Equal(product.Id, i.ProductId));
        Assert.Same(product, sut.Find(product.Id));
    }

    [Fact]
    public void Create_Invalid_ReturnsFieldErrors()
    {
        var repo = new InMemoryProductRepository(new Product { Id = Guid.NewGuid(), Name = "A", Sku = "DUP" });
        var sut = new ProductAdminService(repo, new FixedTime());

        var result = sut.Create(new ProductUpsertCommand("", "DUP", [new ItemInput("", -1)]));

        Assert.Equal(["items[0].name", "items[0].unitPrice", "name", "sku"], result.Errors.Keys.Order());
    }

    [Fact]
    public void Create_TooManyItems_And_LongSku_ReturnErrors()
    {
        var sut = new ProductAdminService(new InMemoryProductRepository(), new FixedTime());
        var items = Enumerable.Range(0, 101).Select(i => new ItemInput($"i{i}", 1)).ToList();

        var result = sut.Create(new ProductUpsertCommand("Kit", new string('s', 65), items));

        Assert.Contains("items", result.Errors.Keys);
        Assert.Contains("sku", result.Errors.Keys);
    }

    [Fact]
    public void Create_WithoutItemsOrSku_IsValid()
    {
        var sut = new ProductAdminService(new InMemoryProductRepository(), new FixedTime());

        var result = sut.Create(new ProductUpsertCommand("Kit", " ", null));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Sku);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public void Update_KeepsCreatedAt_AllowsOwnSku_OrNotFound()
    {
        var created = Now.AddDays(-3);
        var existing = new Product { Id = Guid.NewGuid(), Name = "Velho", Sku = "SKU-1", CreatedAt = created };
        var repo = new InMemoryProductRepository(existing);
        var sut = new ProductAdminService(repo, new FixedTime());

        var ok = sut.Update(existing.Id, Valid());
        var missing = sut.Update(Guid.NewGuid(), Valid("OUTRO"));
        var invalid = sut.Update(existing.Id, Valid() with { Name = null });

        Assert.True(ok.IsSuccess);
        Assert.Equal(created, repo.Products[0].CreatedAt);
        Assert.Equal("Kit", repo.Products[0].Name);
        Assert.True(missing.IsNotFound);
        Assert.Contains("name", invalid.Errors.Keys);
    }

    [Fact]
    public void Delete_RemovesProduct()
    {
        var existing = new Product { Id = Guid.NewGuid(), Name = "A" };
        var sut = new ProductAdminService(new InMemoryProductRepository(existing), new FixedTime());

        Assert.True(sut.Delete(existing.Id));
        Assert.False(sut.Delete(existing.Id));
    }
}
