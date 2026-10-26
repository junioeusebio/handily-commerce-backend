namespace HandilyCommerce.Domain.Leads;

/// <summary>
/// Outbound port for leads (implemented in Infrastructure).
/// Adapter: EF Core (<c>EfLeadRepository</c>) against Supabase Postgres table <c>Leads</c>.
/// </summary>
public interface ILeadRepository
{
    /// <summary>Persists a new lead.</summary>
    void Add(Lead lead);

    /// <summary>Returns a page (newest first) and the total matching <paramref name="status"/>.</summary>
    (IReadOnlyList<Lead> Items, int Total) List(LeadStatus? status, int skip, int take);

    Lead? Find(Guid id);

    /// <summary>Replaces a stored lead; false when it does not exist.</summary>
    bool Update(Lead lead);

    bool Delete(Guid id);

    IReadOnlyDictionary<LeadStatus, int> CountByStatus();
}
