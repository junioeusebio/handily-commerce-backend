using HandilyCommerce.Domain.Admin;
using HandilyCommerce.Domain.Products;

namespace HandilyCommerce.Application.Admin;

/// <summary>Use cases: create, replace (with items) and delete catalog products (admin).</summary>
public sealed class ProductAdminService(IProductRepository repository, TimeProvider timeProvider) : IProductAdminPort
{
    public const int NameMaxLength = 200;
    public const int SkuMaxLength = 64;
    public const int MaxItems = 100;

    public Product? Find(Guid id) => repository.Find(id);

    public AdminResult<Product> Create(ProductUpsertCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new Dictionary<string, string[]>();
        var product = Validate(Guid.NewGuid(), command, null, errors);
        if (errors.Count > 0)
        {
            return AdminResult<Product>.Invalid(errors);
        }

        product!.CreatedAt = timeProvider.GetUtcNow();
        repository.Add(product);
        return AdminResult<Product>.Success(product);
    }

    public AdminResult<Product> Update(Guid id, ProductUpsertCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new Dictionary<string, string[]>();
        var product = Validate(id, command, id, errors);
        if (errors.Count > 0)
        {
            return AdminResult<Product>.Invalid(errors);
        }

        var current = repository.Find(id);
        if (current is null)
        {
            return AdminResult<Product>.NotFound();
        }

        product!.CreatedAt = current.CreatedAt;
        return repository.Update(product) ? AdminResult<Product>.Success(product) : AdminResult<Product>.NotFound();
    }

    public bool Delete(Guid id) => repository.Delete(id);

    private Product? Validate(Guid id, ProductUpsertCommand command, Guid? exceptId, Dictionary<string, string[]> errors)
    {
        var name = AdminValidation.Required(errors, "name", command.Name, NameMaxLength, "Nome");
        var sku = AdminValidation.Optional(errors, "sku", command.Sku, SkuMaxLength, "SKU");
        if (sku is not null && !errors.ContainsKey("sku") && repository.SkuExists(sku, exceptId))
        {
            errors["sku"] = ["Já existe um produto com este SKU."];
        }

        var inputs = command.Items ?? [];
        if (inputs.Count > MaxItems)
        {
            errors["items"] = [$"No máximo {MaxItems} itens por produto."];
        }

        var items = new List<Item>();
        for (var i = 0; i < inputs.Count && inputs.Count <= MaxItems; i++)
        {
            var input = inputs[i];
            var itemName = AdminValidation.Required(errors, $"items[{i}].name", input?.Name, NameMaxLength, "Nome do item");
            if (input?.UnitPrice is < 0)
            {
                errors[$"items[{i}].unitPrice"] = ["Preço deve ser maior ou igual a 0."];
            }

            items.Add(new Item { Id = Guid.NewGuid(), ProductId = id, Name = itemName, UnitPrice = input?.UnitPrice });
        }

        return errors.Count > 0 ? null : new Product { Id = id, Name = name, Sku = sku, Items = items };
    }
}
