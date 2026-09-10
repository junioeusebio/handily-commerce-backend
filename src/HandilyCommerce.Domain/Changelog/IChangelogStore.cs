namespace HandilyCommerce.Domain.Changelog;

/// <summary>
/// Outbound port: load persisted changelog entries (implemented in Infrastructure).
/// </summary>
public interface IChangelogStore
{
    IReadOnlyList<ChangelogEntry> ReadAll();
}
