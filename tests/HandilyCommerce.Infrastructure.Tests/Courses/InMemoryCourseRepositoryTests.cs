using HandilyCommerce.Domain.Courses;
using HandilyCommerce.Infrastructure.Courses;

namespace HandilyCommerce.Infrastructure.Tests.Courses;

public class InMemoryCourseRepositoryTests
{
    private readonly InMemoryCourseRepository _sut = new();

    [Fact]
    public void ListAll_ReturnsCatalogCoursesInOrder()
    {
        var ids = _sut.ListAll().Select(c => c.Id);

        Assert.Equal(
            [
                "pensamento-computacional-na-pratica",
                "programacao-criativa-com-blocos",
                "mundo-digital-dados-e-redes",
                "cultura-digital-e-cidadania"
            ],
            ids);
    }

    [Fact]
    public void ListAll_FillsRequiredFields_AndCoversAllAxes()
    {
        var courses = _sut.ListAll();

        Assert.All(courses, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Title));
            Assert.False(string.IsNullOrWhiteSpace(c.Summary));
            Assert.False(string.IsNullOrWhiteSpace(c.Audience));
            Assert.True(c.WorkloadHours is null or > 0);
        });
        Assert.Equal(Enum.GetValues<BnccAxis>().Order(), courses.Select(c => c.Axis).Distinct().Order());
    }

    [Fact]
    public void ListAll_KeepsOptionalWorkloadHours()
    {
        var byId = _sut.ListAll().ToDictionary(c => c.Id);

        Assert.Equal(40, byId["pensamento-computacional-na-pratica"].WorkloadHours);
        Assert.Null(byId["programacao-criativa-com-blocos"].WorkloadHours);
    }
}
