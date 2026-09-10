using System.Reflection;
using System.Text.Json;
using HandilyCommerce.Domain.Changelog;

namespace HandilyCommerce.Infrastructure.Changelog;

/// <summary>
/// Reads committed changelog entries from the embedded <c>changelog.json</c> resource.
/// </summary>
public sealed class JsonFileChangelogStore : IChangelogStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Lazy<IReadOnlyList<ChangelogEntry>> _entries = new(LoadFromEmbeddedResource);

    public IReadOnlyList<ChangelogEntry> ReadAll() => _entries.Value;

    private static IReadOnlyList<ChangelogEntry> LoadFromEmbeddedResource()
    {
        var assembly = typeof(JsonFileChangelogStore).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("Changelog.changelog.json", StringComparison.Ordinal)
                                 || n.EndsWith("changelog.json", StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                "Embedded resource changelog.json was not found in HandilyCommerce.Infrastructure.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Unable to open embedded resource '{resourceName}'.");

        var entries = JsonSerializer.Deserialize<List<ChangelogEntryDto>>(stream, SerializerOptions)
                      ?? [];

        return entries
            .Select(dto => new ChangelogEntry(
                dto.Title ?? string.Empty,
                dto.Summary ?? string.Empty,
                dto.ProductVersion,
                dto.MergedAt,
                dto.PrNumber,
                dto.Label))
            .ToList();
    }

    private sealed class ChangelogEntryDto
    {
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public string? ProductVersion { get; set; }
        public DateTimeOffset? MergedAt { get; set; }
        public int? PrNumber { get; set; }
        public string? Label { get; set; }
    }
}
