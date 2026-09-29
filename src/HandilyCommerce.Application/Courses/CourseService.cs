using HandilyCommerce.Domain.Courses;

namespace HandilyCommerce.Application.Courses;

public sealed class CourseService : ICoursePort
{
    private readonly ICourseRepository _repository;

    public CourseService(ICourseRepository repository)
    {
        _repository = repository;
    }

    /// <remarks><c>OrderBy</c> is stable, so catalog order is kept within each axis.</remarks>
    public IReadOnlyList<Course> GetCourses() =>
        _repository.ListAll()
            .OrderBy(c => c.Axis)
            .ToList();
}
