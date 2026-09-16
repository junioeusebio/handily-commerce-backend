namespace HandilyCommerce.Domain.Products;

/// <summary>
/// Catalog product (commerce aggregate root). Maps 1:1 to <c>Products</c>.
/// Sellable lines live as child <see cref="Item"/> rows (SKU/line under a product).
/// </summary>
public sealed class Product
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Sku { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<Item> Items { get; set; } = [];
}
