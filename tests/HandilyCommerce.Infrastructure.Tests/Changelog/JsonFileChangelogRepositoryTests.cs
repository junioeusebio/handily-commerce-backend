using HandilyCommerce.Domain.Changelog;
using HandilyCommerce.Infrastructure.Changelog;

namespace HandilyCommerce.Infrastructure.Tests.Changelog;

public class JsonFileChangelogRepositoryTests
{
    private static readonly Guid SeedPr10Id = Guid.Parse("a10c0000-0010-4000-8000-00000000000a");
    private static readonly Guid SeedPr7Id = Guid.Parse("a10c0000-0007-4000-8000-000000000007");

    [Fact]
    public void ListAll_LoadsSeededEntriesFromEmbeddedJson()
    {
        var repository = new JsonFileChangelogRepository();

        var entries = repository.ListAll();

        Assert.NotEmpty(entries);
        Assert.Contains(entries, e => e.PrNumber == 10 && e.Title.Contains("Scalar", StringComparison.Ordinal));
        Assert.Contains(entries, e => e.PrNumber == 7 && e.Title.Contains("Versioning", StringComparison.Ordinal));
    }

    [Fact]
    public void ListAll_SeedEntriesHaveDeterministicIds()
    {
        var repository = new JsonFileChangelogRepository();

        var entries = repository.ListAll();

        Assert.Contains(entries, e => e.PrNumber == 10 && e.Id == SeedPr10Id);
        Assert.Contains(entries, e => e.PrNumber == 7 && e.Id == SeedPr7Id);
        Assert.All(entries, e => Assert.NotEqual(Guid.Empty, e.Id));
    }

    [Fact]
    public void ListAll_EntriesHaveRequiredTitleAndSummary()
    {
        var repository = new JsonFileChangelogRepository();

        foreach (var entry in repository.ListAll())
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Title));
            Assert.False(string.IsNullOrWhiteSpace(entry.Summary));
        }
    }

    [Fact]
    public void JsonFileChangelogRepository_ImplementsIChangelogRepository()
    {
        IChangelogRepository repository = new JsonFileChangelogRepository();

        Assert.NotNull(repository.ListAll());
    }
}
