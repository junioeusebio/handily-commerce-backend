namespace HandilyCommerce.Domain.Products;

/// <summary>
/// Outbound persistence port for products (implemented in Infrastructure).
/// Adapter: EF Core (<c>EfProductRepository</c>) against Supabase Postgres.
/// </summary>
public interface IProductRepository
{
    /// <summary>Returns all products with their items (unordered; Application may sort).</summary>
    IReadOnlyList<Product> ListAll();

    /// <summary>Product with items, or null.</summary>
    Product? Find(Guid id);

    void Add(Product product);

    /// <summary>Replaces name, SKU and all items; false when missing.</summary>
    bool Update(Product product);

    bool Delete(Guid id);

    /// <summary>True when another product (not <paramref name="exceptId"/>) already uses the SKU.</summary>
    bool SkuExists(string sku, Guid? exceptId);

    int Count();
}
