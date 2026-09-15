namespace HandilyCommerce.Domain.Changelog;

/// <summary>
/// Outbound persistence port for changelog entries (implemented in Infrastructure).
/// Current adapter: EF Core (<c>EfChangelogRepository</c>) against Supabase Postgres.
/// </summary>
public interface IChangelogRepository
{
    /// <summary>Returns all persisted changelog entries (unordered; Application sorts).</summary>
    IReadOnlyList<ChangelogEntry> ListAll();
}
