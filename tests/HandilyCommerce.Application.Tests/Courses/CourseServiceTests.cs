using HandilyCommerce.Application.Courses;
using HandilyCommerce.Domain.Courses;

namespace HandilyCommerce.Application.Tests.Courses;

public class CourseServiceTests
{
    [Fact]
    public void GetCourses_OrdersByAxis_KeepingCatalogOrderWithinAxis()
    {
        var cultura = new Course("cd", "CD", BnccAxis.CulturaDigital, "s", "a");
        var pc1 = new Course("pc-1", "PC 1", BnccAxis.PensamentoComputacional, "s", "a", 40);
        var mundo = new Course("md", "MD", BnccAxis.MundoDigital, "s", "a");
        var pc2 = new Course("pc-2", "PC 2", BnccAxis.PensamentoComputacional, "s", "a");
        var sut = new CourseService(new FakeRepository(cultura, pc1, mundo, pc2));

        var result = sut.GetCourses();

        Assert.Equal(["pc-1", "pc-2", "md", "cd"], result.Select(c => c.Id));
    }

    [Fact]
    public void GetCourses_WhenEmpty_ReturnsEmptyList()
    {
        var sut = new CourseService(new FakeRepository());

        Assert.Empty(sut.GetCourses());
    }

    [Fact]
    public void CourseService_ImplementsICoursePort()
    {
        CourseService service = new(new FakeRepository());

        Assert.IsType<ICoursePort>(service, exactMatch: false);
    }

    private sealed class FakeRepository : ICourseRepository
    {
        private readonly IReadOnlyList<Course> _courses;

        public FakeRepository(params Course[] courses) => _courses = courses;

        public IReadOnlyList<Course> ListAll() => _courses;
    }
}
