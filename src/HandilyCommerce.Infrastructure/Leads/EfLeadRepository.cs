using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Infrastructure.Persistence;

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
}
