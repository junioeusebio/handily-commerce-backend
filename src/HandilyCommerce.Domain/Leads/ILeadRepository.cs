namespace HandilyCommerce.Domain.Leads;

/// <summary>
/// Outbound port for leads (implemented in Infrastructure).
/// Adapter: EF Core (<c>EfLeadRepository</c>) against Supabase Postgres table <c>Leads</c>.
/// </summary>
public interface ILeadRepository
{
    /// <summary>Persists a new lead.</summary>
    void Add(Lead lead);
}
