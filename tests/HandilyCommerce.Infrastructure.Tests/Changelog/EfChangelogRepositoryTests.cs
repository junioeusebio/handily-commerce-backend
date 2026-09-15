using HandilyCommerce.Domain.Changelog;
using HandilyCommerce.Infrastructure.Changelog;
using HandilyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HandilyCommerce.Infrastructure.Tests.Changelog;

/// <summary>
/// Uses EF InMemory — no live Supabase / Postgres required in CI.
/// </summary>
public class EfChangelogRepositoryTests
{
    private static readonly Guid SeedPr11Id = Guid.Parse("a10c0000-0011-4000-8000-00000000000b");
    private static readonly Guid SeedPr10Id = Guid.Parse("a10c0000-0010-4000-8000-00000000000a");
    private static readonly Guid SeedPr7Id = Guid.Parse("a10c0000-0007-4000-8000-000000000007");

    private static HandilyCommerceDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HandilyCommerceDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var context = new HandilyCommerceDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void ListAll_ReturnsSeededEntriesFromHasData()
    {
        using var context = CreateContext(nameof(ListAll_ReturnsSeededEntriesFromHasData));
        var repository = new EfChangelogRepository(context);

        var entries = repository.ListAll();

        Assert.Equal(3, entries.Count);
        Assert.Contains(entries, e => e.PrNumber == 11 && e.Id == SeedPr11Id);
        Assert.Contains(entries, e => e.PrNumber == 10 && e.Id == SeedPr10Id);
        Assert.Contains(entries, e => e.PrNumber == 7 && e.Id == SeedPr7Id);
    }

    [Fact]
    public void ListAll_EntriesHaveRequiredTitleAndSummary()
    {
        using var context = CreateContext(nameof(ListAll_EntriesHaveRequiredTitleAndSummary));
        var repository = new EfChangelogRepository(context);

        foreach (var entry in repository.ListAll())
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Title));
            Assert.False(string.IsNullOrWhiteSpace(entry.Summary));
        }
    }

    [Fact]
    public void EfChangelogRepository_ImplementsIChangelogRepository()
    {
        using var context = CreateContext(nameof(EfChangelogRepository_ImplementsIChangelogRepository));
        IChangelogRepository repository = new EfChangelogRepository(context);

        Assert.NotNull(repository.ListAll());
    }

    [Fact]
    public void ListAll_IncludesManuallyAddedEntry()
    {
        using var context = CreateContext(nameof(ListAll_IncludesManuallyAddedEntry));
        var extraId = Guid.Parse("b20c0000-0099-4000-8000-000000000099");
        context.ChangelogEntries.Add(new ChangelogEntry(
            extraId,
            "extra title",
            "extra summary",
            "9.9.9",
            DateTimeOffset.Parse("2026-09-14T12:00:00Z"),
            99,
            "Release Patch"));
        context.SaveChanges();

        var repository = new EfChangelogRepository(context);
        var entries = repository.ListAll();

        Assert.Contains(entries, e => e.Id == extraId && e.PrNumber == 99);
    }
}
