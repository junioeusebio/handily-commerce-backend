using HandilyCommerce.Domain.Courses;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Products;

namespace HandilyCommerce.Application.Tests.Fakes;

/// <summary>List-backed lead repository for use case tests (no I/O).</summary>
public class InMemoryLeadRepository : ILeadRepository
{
    public List<Lead> Leads { get; } = [];

    public void Add(Lead lead) => Leads.Add(lead);

    public (IReadOnlyList<Lead> Items, int Total) List(LeadStatus? status, int skip, int take)
    {
        var query = Leads.Where(l => status is null || l.Status == status).OrderByDescending(l => l.CreatedAt).ToList();
        return (query.Skip(skip).Take(take).ToList(), query.Count);
    }

    public Lead? Find(Guid id) => Leads.FirstOrDefault(l => l.Id == id);

    public bool Update(Lead lead)
    {
        var index = Leads.FindIndex(l => l.Id == lead.Id);
        if (index < 0)
        {
            return false;
        }

        Leads[index] = lead;
        return true;
    }

    public bool Delete(Guid id) => Leads.RemoveAll(l => l.Id == id) > 0;

    public IReadOnlyDictionary<LeadStatus, int> CountByStatus() =>
        Leads.GroupBy(l => l.Status).ToDictionary(g => g.Key, g => g.Count());
}

/// <summary>List-backed course repository; sort order kept in a side dictionary.</summary>
public class InMemoryCourseRepository(params Course[] courses) : ICourseRepository
{
    public List<Course> Courses { get; } = [.. courses];

    public Dictionary<string, int> SortOrders { get; } = [];

    public IReadOnlyList<Course> ListAll() => Courses.Select(WithOrder).ToList();

    public Course? Find(string id) => Courses.FirstOrDefault(c => c.Id == id) is { } c ? WithOrder(c) : null;

    private Course WithOrder(Course c) => SortOrders.TryGetValue(c.Id, out var o) ? c with { DisplayOrder = o } : c;

    public void Add(Course course, int? sortOrder)
    {
        Courses.Add(course);
        SortOrders[course.Id] = sortOrder ?? Courses.Count;
    }

    public bool Update(Course course, int? sortOrder)
    {
        var index = Courses.FindIndex(c => c.Id == course.Id);
        if (index < 0)
        {
            return false;
        }

        Courses[index] = course;
        if (sortOrder is not null)
        {
            SortOrders[course.Id] = sortOrder.Value;
        }

        return true;
    }

    public bool Delete(string id) => Courses.RemoveAll(c => c.Id == id) > 0;

    public int Count() => Courses.Count;
}

/// <summary>List-backed product repository.</summary>
public class InMemoryProductRepository(params Product[] products) : IProductRepository
{
    public List<Product> Products { get; } = [.. products];

    public IReadOnlyList<Product> ListAll() => Products;

    public Product? Find(Guid id) => Products.FirstOrDefault(p => p.Id == id);

    public void Add(Product product) => Products.Add(product);

    public bool Update(Product product)
    {
        var index = Products.FindIndex(p => p.Id == product.Id);
        if (index < 0)
        {
            return false;
        }

        Products[index] = product;
        return true;
    }

    public bool Delete(Guid id) => Products.RemoveAll(p => p.Id == id) > 0;

    public bool SkuExists(string sku, Guid? exceptId) =>
        Products.Any(p => p.Sku == sku && p.Id != exceptId);

    public int Count() => Products.Count;
}
