namespace HandilyCommerce.Domain.Changelog;

/// <summary>
/// Outbound persistence port for changelog entries (implemented in Infrastructure).
/// JSON implementation is temporary; next PR replaces with EF Core against SQL.
/// </summary>
public interface IChangelogRepository
{
    /// <summary>Returns all persisted changelog entries (unordered; Application sorts).</summary>
    IReadOnlyList<ChangelogEntry> ListAll();
}
