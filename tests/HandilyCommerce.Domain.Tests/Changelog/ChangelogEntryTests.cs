using HandilyCommerce.Domain.Changelog;

namespace HandilyCommerce.Domain.Tests.Changelog;

public class ChangelogEntryTests
{
    private static readonly Guid SampleId = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");

    [Fact]
    public void Record_StoresIdTitleAndSummary()
    {
        var entry = new ChangelogEntry(SampleId, "Title", "Summary");

        Assert.Equal(SampleId, entry.Id);
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
        var id = Guid.Parse("a10c0000-0010-4000-8000-00000000000a");
        var mergedAt = DateTimeOffset.Parse("2026-09-09T14:39:41Z");
        var entry = new ChangelogEntry(
            id,
            "feat(A2): Scalar OpenAPI UI",
            "Adds Scalar UI.",
            "0.2.0",
            mergedAt,
            10,
            "Release Mirror");

        Assert.Equal(id, entry.Id);
        Assert.Equal("0.2.0", entry.ProductVersion);
        Assert.Equal(mergedAt, entry.MergedAt);
        Assert.Equal(10, entry.PrNumber);
        Assert.Equal("Release Mirror", entry.Label);
    }

    [Fact]
    public void Record_SupportsEqualityByValue()
    {
        var id = Guid.Parse("a10c0000-0007-4000-8000-000000000007");
        var mergedAt = DateTimeOffset.Parse("2026-09-09T00:41:37Z");
        var a = new ChangelogEntry(id, "t", "s", "0.1.0", mergedAt, 7, "Release Mirror");
        var b = new ChangelogEntry(id, "t", "s", "0.1.0", mergedAt, 7, "Release Mirror");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Record_WithDifferentId_IsNotEqual()
    {
        var a = new ChangelogEntry(Guid.Parse("11111111-1111-4111-8111-111111111111"), "t", "s");
        var b = new ChangelogEntry(Guid.Parse("22222222-2222-4222-8222-222222222222"), "t", "s");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Record_WithDifferentTitle_IsNotEqual()
    {
        var id = SampleId;
        var a = new ChangelogEntry(id, "a", "s");
        var b = new ChangelogEntry(id, "b", "s");

        Assert.NotEqual(a, b);
    }
}
