namespace HandilyCommerce.Domain.Courses;

/// <summary>Admin create/replace of a course. <see cref="Id"/> is only used on create (slug).</summary>
public sealed record CourseUpsertCommand(
    string? Id,
    string? Title,
    string? Axis,
    string? Summary,
    string? Audience,
    int? WorkloadHours,
    int? SortOrder);
