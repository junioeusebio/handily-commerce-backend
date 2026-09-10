using HandilyCommerce.Domain.Changelog;
using HandilyCommerce.Infrastructure.Changelog;

namespace HandilyCommerce.Infrastructure.Tests.Changelog;

public class JsonFileChangelogStoreTests
{
    [Fact]
    public void ReadAll_LoadsSeededEntriesFromEmbeddedJson()
    {
        var store = new JsonFileChangelogStore();

        var entries = store.ReadAll();

        Assert.NotEmpty(entries);
        Assert.Contains(entries, e => e.PrNumber == 10 && e.Title.Contains("Scalar", StringComparison.Ordinal));
        Assert.Contains(entries, e => e.PrNumber == 7 && e.Title.Contains("Versioning", StringComparison.Ordinal));
    }

    [Fact]
    public void ReadAll_EntriesHaveRequiredTitleAndSummary()
    {
        var store = new JsonFileChangelogStore();

        foreach (var entry in store.ReadAll())
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Title));
            Assert.False(string.IsNullOrWhiteSpace(entry.Summary));
        }
    }

    [Fact]
    public void JsonFileChangelogStore_ImplementsIChangelogStore()
    {
        IChangelogStore store = new JsonFileChangelogStore();

        Assert.NotNull(store.ReadAll());
    }
}
