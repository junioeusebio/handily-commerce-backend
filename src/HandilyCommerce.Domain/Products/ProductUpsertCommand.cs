namespace HandilyCommerce.Domain.Products;

/// <summary>Admin create/replace of a product. <see cref="Items"/> replaces all child items.</summary>
public sealed record ProductUpsertCommand(string? Name, string? Sku, IReadOnlyList<ItemInput>? Items);

/// <summary>Child item (SKU line) of <see cref="ProductUpsertCommand"/>.</summary>
public sealed record ItemInput(string? Name, decimal? UnitPrice);
