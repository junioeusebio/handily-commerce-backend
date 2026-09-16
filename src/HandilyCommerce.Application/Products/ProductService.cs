using HandilyCommerce.Domain.Products;

namespace HandilyCommerce.Application.Products;

public sealed class ProductService : IProductPort
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<Product> GetProducts() =>
        _repository.ListAll()
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.CreatedAt)
            .ToList();
}
