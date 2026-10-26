using HandilyCommerce.Application.Admin;
using HandilyCommerce.Application.Tests.Fakes;
using HandilyCommerce.Domain.Admin;
using HandilyCommerce.Domain.Courses;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Products;

namespace HandilyCommerce.Application.Tests.Admin;

public class AdminSummaryServiceTests
{
    [Fact]
    public void GetSummary_CountsLeadsByStatus_CoursesAndProducts()
    {
        var leads = new InMemoryLeadRepository();
        var now = DateTimeOffset.UtcNow;
        leads.Leads.Add(new Lead(Guid.NewGuid(), "a", "a@x.com", "m", null, now));
        leads.Leads.Add(new Lead(Guid.NewGuid(), "b", "b@x.com", "m", null, now, LeadStatus.Contatado));
        leads.Leads.Add(new Lead(Guid.NewGuid(), "c", "c@x.com", "m", null, now, LeadStatus.Contatado));
        var courses = new InMemoryCourseRepository(new Course("c", "T", BnccAxis.MundoDigital, "s", "a"));
        var products = new InMemoryProductRepository(new Product(), new Product());

        var summary = new AdminSummaryService(leads, courses, products).GetSummary();

        Assert.Equal(new AdminSummary(new LeadCounts(3, 1, 2, 0), 1, 2), summary);
    }
}
