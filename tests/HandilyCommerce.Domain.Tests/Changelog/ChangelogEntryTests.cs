using HandilyCommerce.Domain.Changelog;

namespace HandilyCommerce.Domain.Tests.Changelog;

public class ChangelogEntryTests
{
    [Fact]
    public void Record_StoresTitleAndSummary()
    {
        var entry = new ChangelogEntry("Title", "Summary");

        Assert.Equal("Title", entry.Title);
        Assert.Equal("Summary", entry.Summary);
        Assert.Null(entry.ProductVersion);
        Assert.Null(entry.MergedAt);
        Assert.Null(entry.PrNumber);
        Assert.Null(entry.Label);
    }

    [Fact]
    public void Record_StoresOptionalMetadata()
    {
        var mergedAt = DateTimeOffset.Parse("2026-09-09T14:39:41Z");
        var entry = new ChangelogEntry(
            "feat(A2): Scalar OpenAPI UI",
            "Adds Scalar UI.",
            "0.2.0",
            mergedAt,
            10,
            "Release Mirror");

        Assert.Equal("0.2.0", entry.ProductVersion);
        Assert.Equal(mergedAt, entry.MergedAt);
        Assert.Equal(10, entry.PrNumber);
        Assert.Equal("Release Mirror", entry.Label);
    }

    [Fact]
    public void Record_SupportsEqualityByValue()
    {
        var mergedAt = DateTimeOffset.Parse("2026-09-09T00:41:37Z");
        var a = new ChangelogEntry("t", "s", "0.1.0", mergedAt, 7, "Release Mirror");
        var b = new ChangelogEntry("t", "s", "0.1.0", mergedAt, 7, "Release Mirror");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Record_WithDifferentTitle_IsNotEqual()
    {
        var a = new ChangelogEntry("a", "s");
        var b = new ChangelogEntry("b", "s");

        Assert.NotEqual(a, b);
    }
}
