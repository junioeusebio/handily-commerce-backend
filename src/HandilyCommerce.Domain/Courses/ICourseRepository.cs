namespace HandilyCommerce.Domain.Courses;

/// <summary>
/// Outbound port for the course catalog (implemented in Infrastructure).
/// Adapter: EF Core (<c>EfCourseRepository</c>) against Supabase Postgres table <c>Courses</c>.
/// </summary>
public interface ICourseRepository
{
    /// <summary>Returns all courses in catalog order (Application may sort).</summary>
    IReadOnlyList<Course> ListAll();
}
