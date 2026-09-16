using HandilyCommerce.Domain.Products;

namespace HandilyCommerce.Domain.Tests.Products;

public class ProductTests
{
    [Fact]
    public void Product_DefaultsItemsToEmptyList()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Widget",
            CreatedAt = DateTimeOffset.UtcNow
        };

        Assert.Empty(product.Items);
        Assert.Null(product.Sku);
    }

    [Fact]
    public void Item_LinksToProductViaProductId()
    {
        var productId = Guid.NewGuid();
        var item = new Item
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Name = "Widget — unit",
            UnitPrice = 9.99m
        };

        Assert.Equal(productId, item.ProductId);
        Assert.Equal(9.99m, item.UnitPrice);
    }
}
