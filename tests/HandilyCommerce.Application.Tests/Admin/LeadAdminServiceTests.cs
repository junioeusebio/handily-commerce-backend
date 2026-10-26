using HandilyCommerce.Application.Admin;
using HandilyCommerce.Application.Tests.Fakes;
using HandilyCommerce.Domain.Leads;

namespace HandilyCommerce.Application.Tests.Admin;

public class LeadAdminServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    private static (LeadAdminService Sut, InMemoryLeadRepository Repo) Create(params Lead[] leads)
    {
        var repo = new InMemoryLeadRepository();
        repo.Leads.AddRange(leads);
        return (new LeadAdminService(repo), repo);
    }

    private static Lead NewLead(int minutesAgo, LeadStatus status = LeadStatus.Novo) =>
        new(Guid.NewGuid(), "Ana", "ana@x.com", "Recife", null, Now.AddMinutes(-minutesAgo), status);

    [Fact]
    public void List_Defaults_NewestFirst()
    {
        var old = NewLead(10);
        var recent = NewLead(1);
        var (sut, _) = Create(old, recent);

        var result = sut.List(null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal([recent.Id, old.Id], result.Value!.Items.Select(l => l.Id));
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(LeadAdminService.DefaultPageSize, result.Value.PageSize);
        Assert.Equal(2, result.Value.Total);
    }

    [Fact]
    public void List_FiltersByStatus_AndPages()
    {
        var (sut, _) = Create(NewLead(1, LeadStatus.Contatado), NewLead(2, LeadStatus.Contatado), NewLead(3));

        var result = sut.List(2, 1, "contatado");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(2, result.Value.Total);
    }

    [Theory]
    [InlineData(0, 10, null, "page")]
    [InlineData(1, 0, null, "pageSize")]
    [InlineData(1, 101, null, "pageSize")]
    [InlineData(1, 10, "xyz", "status")]
    [InlineData(1, 10, "1", "status")]
    public void List_Invalid_ReturnsErrors(int page, int pageSize, string? status, string field)
    {
        var (sut, _) = Create();

        var result = sut.List(page, pageSize, status);

        Assert.False(result.IsSuccess);
        Assert.Contains(field, result.Errors.Keys);
    }

    [Fact]
    public void Find_ReturnsLeadOrNull()
    {
        var lead = NewLead(1);
        var (sut, _) = Create(lead);

        Assert.Equal(lead, sut.Find(lead.Id));
        Assert.Null(sut.Find(Guid.NewGuid()));
    }

    [Fact]
    public void Update_ChangesStatusAndNote()
    {
        var lead = NewLead(1);
        var (sut, repo) = Create(lead);

        var result = sut.Update(lead.Id, new UpdateLeadCommand("Contatado", "  ligar amanhã "));

        Assert.True(result.IsSuccess);
        Assert.Equal(LeadStatus.Contatado, repo.Leads[0].Status);
        Assert.Equal("ligar amanhã", repo.Leads[0].InternalNote);
    }

    [Fact]
    public void Update_OnlyNote_KeepsStatus_AndEmptyClearsNote()
    {
        var lead = NewLead(1, LeadStatus.Descartado) with { InternalNote = "x" };
        var (sut, repo) = Create(lead);

        var result = sut.Update(lead.Id, new UpdateLeadCommand(null, ""));

        Assert.True(result.IsSuccess);
        Assert.Equal(LeadStatus.Descartado, repo.Leads[0].Status);
        Assert.Null(repo.Leads[0].InternalNote);
    }

    [Fact]
    public void Update_Invalid_ReturnsErrors()
    {
        var lead = NewLead(1);
        var (sut, _) = Create(lead);

        Assert.Contains("status", sut.Update(lead.Id, new UpdateLeadCommand("x", null)).Errors.Keys);
        Assert.Contains("status", sut.Update(lead.Id, new UpdateLeadCommand(null, null)).Errors.Keys);
        Assert.Contains("internalNote", sut.Update(lead.Id, new UpdateLeadCommand(null, new string('a', 2001))).Errors.Keys);
    }

    [Fact]
    public void Update_Missing_ReturnsNotFound()
    {
        var (sut, _) = Create();

        Assert.True(sut.Update(Guid.NewGuid(), new UpdateLeadCommand("Novo", null)).IsNotFound);
    }

    [Fact]
    public void Delete_RemovesLead()
    {
        var lead = NewLead(1);
        var (sut, repo) = Create(lead);

        Assert.True(sut.Delete(lead.Id));
        Assert.False(sut.Delete(lead.Id));
        Assert.Empty(repo.Leads);
    }
}
