namespace HandilyCommerce.Domain.Changelog;

/// <summary>
/// Domain model for a product changelog item (What's new). Free of ASP.NET / infrastructure types.
/// Created after merge of PRs labeled Release Major or Release Mirror (not Patch).
/// </summary>
public sealed record ChangelogEntry(
    string Title,
    string Summary,
    string? ProductVersion = null,
    DateTimeOffset? MergedAt = null,
    int? PrNumber = null,
    string? Label = null);
