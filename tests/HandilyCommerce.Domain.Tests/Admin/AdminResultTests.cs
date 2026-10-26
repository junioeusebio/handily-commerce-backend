using HandilyCommerce.Domain.Admin;
using HandilyCommerce.Domain.Leads;

namespace HandilyCommerce.Domain.Tests.Admin;

public class AdminResultTests
{
    [Fact]
    public void Success_HasValue()
    {
        var result = AdminResult<string>.Success("ok");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsNotFound);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Invalid_HasErrors()
    {
        var result = AdminResult<string>.Invalid(new Dictionary<string, string[]> { ["name"] = ["x"] });

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void NotFound_IsNotSuccess()
    {
        var result = AdminResult<string>.NotFound();

        Assert.True(result.IsNotFound);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Lead_DefaultsToNovo_WithoutNote()
    {
        var lead = new Lead(Guid.NewGuid(), "a", "a@x.com", "m", null, DateTimeOffset.UtcNow);

        Assert.Equal(LeadStatus.Novo, lead.Status);
        Assert.Null(lead.InternalNote);
    }

    [Fact]
    public void Records_KeepValues()
    {
        var page = new LeadPage([], 2, 10, 0);
        var summary = new AdminSummary(new LeadCounts(3, 1, 1, 1), 4, 5);
        var update = new UpdateLeadCommand("Novo", "n");
        var course = new HandilyCommerce.Domain.Courses.CourseUpsertCommand("id", "t", "mundo-digital", "s", "a", 1, 2);
        var product = new HandilyCommerce.Domain.Products.ProductUpsertCommand("n", "s", [new HandilyCommerce.Domain.Products.ItemInput("i", 1m)]);

        Assert.Equal(2, page.Page);
        Assert.Equal(3, summary.Leads.Total);
        Assert.Equal("n", update.InternalNote);
        Assert.Equal(2, course.SortOrder);
        Assert.Equal(1m, product.Items![0].UnitPrice);
    }
}
