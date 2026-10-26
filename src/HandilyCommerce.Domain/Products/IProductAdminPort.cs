using HandilyCommerce.Domain.Admin;

namespace HandilyCommerce.Domain.Products;

/// <summary>Driving port: admin product catalog management (implemented in Application).</summary>
public interface IProductAdminPort
{
    Product? Find(Guid id);

    AdminResult<Product> Create(ProductUpsertCommand command);

    AdminResult<Product> Update(Guid id, ProductUpsertCommand command);

    bool Delete(Guid id);
}
