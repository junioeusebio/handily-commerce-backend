namespace HandilyCommerce.Domain.Products;

/// <summary>
/// Driven port: list catalog products for API / FE (implemented in Application).
/// </summary>
public interface IProductPort
{
    /// <summary>Returns products (with items), name ascending.</summary>
    IReadOnlyList<Product> GetProducts();
}
