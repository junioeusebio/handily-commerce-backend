using HandilyCommerce.Application.Admin;
using HandilyCommerce.Application.Tests.Fakes;
using HandilyCommerce.Domain.Courses;

namespace HandilyCommerce.Application.Tests.Admin;

public class CourseAdminServiceTests
{
    private static CourseUpsertCommand Valid(string? id = "novo-curso", string axis = "mundo-digital") =>
        new(id, " Título ", axis, "Resumo", "Professores", 40, null);

    [Fact]
    public void Create_Valid_AddsCourse()
    {
        var repo = new InMemoryCourseRepository();
        var sut = new CourseAdminService(repo);

        var result = sut.Create(Valid());

        Assert.True(result.IsSuccess);
        Assert.Equal(new Course("novo-curso", "Título", BnccAxis.MundoDigital, "Resumo", "Professores", 40), result.Value);
        Assert.Single(repo.Courses);
        Assert.Equal(result.Value, sut.Find("novo-curso"));
    }

    [Theory]
    [InlineData("PensamentoComputacional", BnccAxis.PensamentoComputacional)]
    [InlineData("cultura-digital", BnccAxis.CulturaDigital)]
    public void Create_AcceptsAxisFormats(string axis, BnccAxis expected)
    {
        var sut = new CourseAdminService(new InMemoryCourseRepository());

        Assert.Equal(expected, sut.Create(Valid(axis: axis)).Value!.Axis);
    }

    [Fact]
    public void Create_Invalid_ReturnsFieldErrors()
    {
        var sut = new CourseAdminService(new InMemoryCourseRepository());

        var result = sut.Create(new CourseUpsertCommand("Não Slug", "", "x", "", new string('a', 201), 0, -1));

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ["audience", "axis", "id", "sortOrder", "summary", "title", "workloadHours"],
            result.Errors.Keys.Order());
    }

    [Fact]
    public void Create_MissingId_And_Duplicate_ReturnErrors()
    {
        var repo = new InMemoryCourseRepository(new Course("ja-existe", "T", BnccAxis.MundoDigital, "s", "a"));
        var sut = new CourseAdminService(repo);

        Assert.Contains("id", sut.Create(Valid(id: null)).Errors.Keys);
        Assert.Contains("id", sut.Create(Valid(id: "ja-existe")).Errors.Keys);
    }

    [Fact]
    public void Update_ReplacesCourse_OrNotFound()
    {
        var repo = new InMemoryCourseRepository(new Course("c1", "Antigo", BnccAxis.MundoDigital, "s", "a"));
        var sut = new CourseAdminService(repo);

        var ok = sut.Update("c1", Valid(id: "ignorado") with { SortOrder = 5 });
        var missing = sut.Update("nao-tem", Valid());
        var invalid = sut.Update("c1", Valid() with { Title = "" });

        Assert.True(ok.IsSuccess);
        Assert.Equal("c1", ok.Value!.Id);
        Assert.Equal("Título", repo.Courses[0].Title);
        Assert.Equal(5, repo.SortOrders["c1"]);
        Assert.True(missing.IsNotFound);
        Assert.Contains("title", invalid.Errors.Keys);
    }

    [Fact]
    public void Delete_RemovesCourse()
    {
        var repo = new InMemoryCourseRepository(new Course("c1", "T", BnccAxis.MundoDigital, "s", "a"));
        var sut = new CourseAdminService(repo);

        Assert.True(sut.Delete("c1"));
        Assert.False(sut.Delete("c1"));
    }
}
