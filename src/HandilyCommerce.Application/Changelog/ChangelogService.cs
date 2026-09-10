using HandilyCommerce.Domain.Changelog;

namespace HandilyCommerce.Application.Changelog;

public sealed class ChangelogService : IChangelogPort
{
    private readonly IChangelogRepository _repository;

    public ChangelogService(IChangelogRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<ChangelogEntry> GetEntries() =>
        _repository.ListAll()
            .OrderByDescending(e => e.MergedAt ?? DateTimeOffset.MinValue)
            .ThenByDescending(e => e.PrNumber ?? 0)
            .ToList();
}
