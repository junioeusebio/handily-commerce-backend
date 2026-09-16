namespace HandilyCommerce.Domain.Products;

/// <summary>
/// Sellable line / SKU under a <see cref="Product"/>. Maps 1:1 to <c>Items</c>
/// with FK <c>ProductId</c> → <c>Products</c>.
/// </summary>
public sealed class Item
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal? UnitPrice { get; set; }
}
