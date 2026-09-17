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

    private static HandilyCommerceDbContext CreateContext(string dbName, bool seedChangelog = false)
    {
        var options = new DbContextOptionsBuilder<HandilyCommerceDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var context = new HandilyCommerceDbContext(options);
        context.Database.EnsureCreated();
        if (seedChangelog)
        {
            SeedChangelogEntries(context);
        }

        return context;
    }

    /// <summary>
    /// Test-only seed mirroring former HasData rows (#11/#10/#7). Production data lives in Supabase.
    /// </summary>
    private static void SeedChangelogEntries(HandilyCommerceDbContext context)
    {
        context.ChangelogEntries.AddRange(
            new ChangelogEntry(
                SeedPr11Id,
                "feat: changelog API for Major/Mirror releases",
                "Changelog API for the FE \"What's new\" modal: hexagonal ports, GET /api/v1/changelog from embedded JSON seed, and merge workflow to append Major/Mirror entries.",
                "0.3.0",
                DateTimeOffset.Parse("2026-09-10T22:44:17Z"),
                11,
                "Release Mirror"),
            new ChangelogEntry(
                SeedPr10Id,
                "feat(A2): Scalar OpenAPI UI",
                "Adds Scalar UI over the existing OpenAPI document in all environments, including Production on Render.",
                "0.2.0",
                DateTimeOffset.Parse("2026-09-09T14:39:41Z"),
                10,
                "Release Mirror"),
            new ChangelogEntry(
                SeedPr7Id,
                "chore: Versioning 0.1.0 + Release labels process",
                "Introduces product Versioning separate from the API route version, with Directory.Build.props baseline 0.1.0 and the Release label process.",
                "0.1.0",
                DateTimeOffset.Parse("2026-09-09T00:41:37Z"),
                7,
                "Release Mirror"));
        context.SaveChanges();
    }

    [Fact]
    public void ListAll_ReturnsSeededEntriesFromTestSetup()
    {
        using var context = CreateContext(nameof(ListAll_ReturnsSeededEntriesFromTestSetup), seedChangelog: true);
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
        using var context = CreateContext(nameof(ListAll_EntriesHaveRequiredTitleAndSummary), seedChangelog: true);
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
