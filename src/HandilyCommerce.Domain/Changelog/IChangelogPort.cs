namespace HandilyCommerce.Domain.Changelog;

/// <summary>
/// Driven port: query changelog entries for the FE "What's new" modal (implemented in Application).
/// </summary>
public interface IChangelogPort
{
    /// <summary>Returns changelog entries, newest first.</summary>
    IReadOnlyList<ChangelogEntry> GetEntries();
}
