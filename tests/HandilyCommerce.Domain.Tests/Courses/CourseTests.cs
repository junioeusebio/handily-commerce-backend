using HandilyCommerce.Domain.Courses;

namespace HandilyCommerce.Domain.Tests.Courses;

public class CourseTests
{
    [Fact]
    public void Course_DefaultsWorkloadHoursToNull()
    {
        var course = new Course("id", "Título", BnccAxis.MundoDigital, "Resumo", "Professores");

        Assert.Null(course.WorkloadHours);
        Assert.Equal(BnccAxis.MundoDigital, course.Axis);
    }

    [Fact]
    public void BnccAxis_DeclaresThreeAxesInCatalogOrder()
    {
        Assert.Equal(
            [BnccAxis.PensamentoComputacional, BnccAxis.MundoDigital, BnccAxis.CulturaDigital],
            Enum.GetValues<BnccAxis>());
    }
}
