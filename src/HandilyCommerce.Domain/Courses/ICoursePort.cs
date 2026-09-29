namespace HandilyCommerce.Domain.Courses;

/// <summary>
/// Driven port: list BNCC courses for API / FE (implemented in Application).
/// </summary>
public interface ICoursePort
{
    /// <summary>Returns courses grouped by <see cref="BnccAxis"/> order (stable within an axis).</summary>
    IReadOnlyList<Course> GetCourses();
}
