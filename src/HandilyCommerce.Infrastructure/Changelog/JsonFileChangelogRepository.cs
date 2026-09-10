using System.Reflection;
using System.Text.Json;
using HandilyCommerce.Domain.Changelog;

namespace HandilyCommerce.Infrastructure.Changelog;

/// <summary>
/// Temporary JSON-file adapter for <see cref="IChangelogRepository"/>.
/// Reads committed entries from the embedded <c>changelog.json</c> resource.
/// Next PR replaces this with an EF Core repository against SQL; Domain/Application contracts stay unchanged.
/// </summary>
public sealed class JsonFileChangelogRepository : IChangelogRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Lazy<IReadOnlyList<ChangelogEntry>> _entries = new(LoadFromEmbeddedResource);

    public IReadOnlyList<ChangelogEntry> ListAll() => _entries.Value;

    private static IReadOnlyList<ChangelogEntry> LoadFromEmbeddedResource()
    {
        var assembly = typeof(JsonFileChangelogRepository).Assembly;
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
                dto.Id,
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
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public string? ProductVersion { get; set; }
        public DateTimeOffset? MergedAt { get; set; }
        public int? PrNumber { get; set; }
        public string? Label { get; set; }
    }
}
