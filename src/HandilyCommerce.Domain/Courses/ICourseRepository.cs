namespace HandilyCommerce.Domain.Courses;

/// <summary>
/// Outbound port for the course catalog (implemented in Infrastructure).
/// Adapter today: static in-memory list (<c>InMemoryCourseRepository</c>) until a persisted catalog exists.
/// </summary>
public interface ICourseRepository
{
    /// <summary>Returns all courses in catalog order (Application may sort).</summary>
    IReadOnlyList<Course> ListAll();
}
