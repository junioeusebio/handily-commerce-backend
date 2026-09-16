namespace HandilyCommerce.Domain.Products;

/// <summary>
/// Outbound persistence port for products (implemented in Infrastructure).
/// Adapter: EF Core (<c>EfProductRepository</c>) against Supabase Postgres.
/// </summary>
public interface IProductRepository
{
    /// <summary>Returns all products with their items (unordered; Application may sort).</summary>
    IReadOnlyList<Product> ListAll();
}
