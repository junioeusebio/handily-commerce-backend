using HandilyCommerce.Application.Changelog;
using HandilyCommerce.Domain.Changelog;

namespace HandilyCommerce.Application.Tests.Changelog;

public class ChangelogServiceTests
{
    [Fact]
    public void GetEntries_ReturnsNewestFirst()
    {
        var older = new ChangelogEntry(
            Guid.Parse("a10c0000-0007-4000-8000-000000000007"),
            "old", "s1", "0.1.0", DateTimeOffset.Parse("2026-09-09T00:41:37Z"), 7, "Release Mirror");
        var newer = new ChangelogEntry(
            Guid.Parse("a10c0000-0010-4000-8000-00000000000a"),
            "new", "s2", "0.2.0", DateTimeOffset.Parse("2026-09-09T14:39:41Z"), 10, "Release Mirror");
        var sut = new ChangelogService(new FakeRepository(newer, older));

        var result = sut.GetEntries();

        Assert.Equal(2, result.Count);
        Assert.Equal("new", result[0].Title);
        Assert.Equal("old", result[1].Title);
    }

    [Fact]
    public void GetEntries_WhenMergedAtMissing_UsesPrNumberAsTieBreaker()
    {
        var low = new ChangelogEntry(Guid.NewGuid(), "low", "s", PrNumber: 3);
        var high = new ChangelogEntry(Guid.NewGuid(), "high", "s", PrNumber: 9);
        var sut = new ChangelogService(new FakeRepository(low, high));

        var result = sut.GetEntries();

        Assert.Equal("high", result[0].Title);
        Assert.Equal("low", result[1].Title);
    }

    [Fact]
    public void GetEntries_MapsRepositoryEntriesThroughPort()
    {
        var entry = new ChangelogEntry(Guid.NewGuid(), "t", "summary", "0.3.0", DateTimeOffset.UtcNow, 11, "Release Mirror");
        var sut = new ChangelogService(new FakeRepository(entry));

        var result = sut.GetEntries();

        Assert.Single(result);
        Assert.Equal(entry, result[0]);
    }

    [Fact]
    public void ChangelogService_ImplementsIChangelogPort()
    {
        ChangelogService service = new(new FakeRepository());

        Assert.IsAssignableFrom<IChangelogPort>(service);
        Assert.Empty(service.GetEntries());
    }

    private sealed class FakeRepository : IChangelogRepository
    {
        private readonly IReadOnlyList<ChangelogEntry> _entries;

        public FakeRepository(params ChangelogEntry[] entries) => _entries = entries;

        public IReadOnlyList<ChangelogEntry> ListAll() => _entries;
    }
}
