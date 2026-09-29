using System.Text.Json;
using HandilyCommerce.Api.Courses;
using HandilyCommerce.Domain.Courses;

namespace HandilyCommerce.Api.Tests.Courses;

public class CourseJsonTests
{
    [Fact]
    public void Serialize_UsesCamelCaseAndKebabCaseAxis()
    {
        var course = new Course("pc", "Título", BnccAxis.PensamentoComputacional, "Resumo", "Professores", 40);

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(course, CourseJson.SerializerOptions));
        var root = doc.RootElement;

        Assert.Equal("pc", root.GetProperty("id").GetString());
        Assert.Equal("Título", root.GetProperty("title").GetString());
        Assert.Equal("pensamento-computacional", root.GetProperty("axis").GetString());
        Assert.Equal("Resumo", root.GetProperty("summary").GetString());
        Assert.Equal("Professores", root.GetProperty("audience").GetString());
        Assert.Equal(40, root.GetProperty("workloadHours").GetInt32());
    }

    [Theory]
    [InlineData(BnccAxis.MundoDigital, "mundo-digital")]
    [InlineData(BnccAxis.CulturaDigital, "cultura-digital")]
    public void Serialize_MapsEveryAxisToKebabCase(BnccAxis axis, string expected)
    {
        var json = JsonSerializer.Serialize(axis, CourseJson.SerializerOptions);

        Assert.Equal($"\"{expected}\"", json);
    }

    [Fact]
    public void Serialize_OmitsWorkloadHoursWhenNull()
    {
        var course = new Course("cd", "CD", BnccAxis.CulturaDigital, "s", "a");

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(course, CourseJson.SerializerOptions));

        Assert.False(doc.RootElement.TryGetProperty("workloadHours", out _));
    }
}
