using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HandilyCommerce.Infrastructure.Leads;

/// <summary>
/// EF Core adapter for <see cref="ILeadRepository"/> against Supabase Postgres (<c>Leads</c> table).
/// </summary>
public sealed class EfLeadRepository(HandilyCommerceDbContext dbContext) : ILeadRepository
{
    public void Add(Lead lead)
    {
        dbContext.Leads.Add(lead);
        dbContext.SaveChanges();
    }

    public (IReadOnlyList<Lead> Items, int Total) List(LeadStatus? status, int skip, int take)
    {
        var query = dbContext.Leads.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(l => l.Status == status);
        }

        var total = query.Count();
        var items = query
            .OrderByDescending(l => l.CreatedAt)
            .ThenBy(l => l.Id)
            .Skip(skip)
            .Take(take)
            .ToList();
        return (items, total);
    }

    public Lead? Find(Guid id) =>
        dbContext.Leads.AsNoTracking().FirstOrDefault(l => l.Id == id);

    public bool Update(Lead lead)
    {
        var stored = dbContext.Leads.FirstOrDefault(l => l.Id == lead.Id);
        if (stored is null)
        {
            return false;
        }

        dbContext.Entry(stored).CurrentValues.SetValues(lead);
        dbContext.SaveChanges();
        return true;
    }

    public bool Delete(Guid id)
    {
        var stored = dbContext.Leads.FirstOrDefault(l => l.Id == id);
        if (stored is null)
        {
            return false;
        }

        dbContext.Leads.Remove(stored);
        dbContext.SaveChanges();
        return true;
    }

    public IReadOnlyDictionary<LeadStatus, int> CountByStatus() =>
        dbContext.Leads
            .AsNoTracking()
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionary(x => x.Status, x => x.Count);
}
