using System.Text.Json;
using System.Text.Json.Serialization;

namespace HandilyCommerce.Api.Courses;

/// <summary>
/// JSON contract for <c>GET /{RoutePrefix}/{Version}/courses</c>:
/// camelCase properties, <c>axis</c> as kebab-case string (e.g. <c>mundo-digital</c>),
/// and <c>workloadHours</c> omitted when not defined.
/// </summary>
public static class CourseJson
{
    public static JsonSerializerOptions SerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower) }
    };
}
