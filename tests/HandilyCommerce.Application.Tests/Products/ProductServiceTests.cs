using HandilyCommerce.Application.Products;
using HandilyCommerce.Domain.Products;

namespace HandilyCommerce.Application.Tests.Products;

public class ProductServiceTests
{
    [Fact]
    public void GetProducts_ReturnsNameAscending()
    {
        var beta = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Beta",
            CreatedAt = DateTimeOffset.Parse("2026-09-01T00:00:00Z")
        };
        var alpha = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Alpha",
            CreatedAt = DateTimeOffset.Parse("2026-09-02T00:00:00Z")
        };
        var sut = new ProductService(new FakeRepository(beta, alpha));

        var result = sut.GetProducts();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
    }

    [Fact]
    public void GetProducts_WhenEmpty_ReturnsEmptyList()
    {
        var sut = new ProductService(new FakeRepository());

        Assert.Empty(sut.GetProducts());
    }

    [Fact]
    public void ProductService_ImplementsIProductPort()
    {
        ProductService service = new(new FakeRepository());

        Assert.IsAssignableFrom<IProductPort>(service);
    }

    private sealed class FakeRepository : IProductRepository
    {
        private readonly IReadOnlyList<Product> _products;

        public FakeRepository(params Product[] products) => _products = products;

        public IReadOnlyList<Product> ListAll() => _products;
    }
}
