using HandilyCommerce.Domain.Changelog;

namespace HandilyCommerce.Application.Changelog;

public sealed class ChangelogService : IChangelogPort
{
    private readonly IChangelogStore _store;

    public ChangelogService(IChangelogStore store)
    {
        _store = store;
    }

    public IReadOnlyList<ChangelogEntry> GetEntries() =>
        _store.ReadAll()
            .OrderByDescending(e => e.MergedAt ?? DateTimeOffset.MinValue)
            .ThenByDescending(e => e.PrNumber ?? 0)
            .ToList();
}
