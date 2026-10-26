using HandilyCommerce.Domain.Admin;
using HandilyCommerce.Domain.Courses;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Products;

namespace HandilyCommerce.Application.Admin;

/// <summary>Use case: dashboard counters (leads by status, courses, products).</summary>
public sealed class AdminSummaryService(
    ILeadRepository leads,
    ICourseRepository courses,
    IProductRepository products) : IAdminSummaryPort
{
    public AdminSummary GetSummary()
    {
        var byStatus = leads.CountByStatus();
        int Count(LeadStatus status) => byStatus.TryGetValue(status, out var value) ? value : 0;

        var counts = new LeadCounts(
            byStatus.Values.Sum(),
            Count(LeadStatus.Novo),
            Count(LeadStatus.Contatado),
            Count(LeadStatus.Descartado));
        return new AdminSummary(counts, courses.Count(), products.Count());
    }
}
