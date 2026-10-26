namespace HandilyCommerce.Domain.Courses;

/// <summary>
/// Outbound port for the course catalog (implemented in Infrastructure).
/// Adapter: EF Core (<c>EfCourseRepository</c>) against Supabase Postgres table <c>Courses</c>.
/// </summary>
public interface ICourseRepository
{
    /// <summary>Returns all courses in catalog order (Application may sort).</summary>
    IReadOnlyList<Course> ListAll();

    Course? Find(string id);

    /// <summary>Adds a course; null <paramref name="sortOrder"/> appends it to the end of the catalog.</summary>
    void Add(Course course, int? sortOrder);

    /// <summary>Replaces a course; null <paramref name="sortOrder"/> keeps the current order. False when missing.</summary>
    bool Update(Course course, int? sortOrder);

    bool Delete(string id);

    int Count();
}
