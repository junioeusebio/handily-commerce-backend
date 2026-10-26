using HandilyCommerce.Domain.Admin;

namespace HandilyCommerce.Domain.Courses;

/// <summary>Driving port: admin course catalog management (implemented in Application).</summary>
public interface ICourseAdminPort
{
    Course? Find(string id);

    AdminResult<Course> Create(CourseUpsertCommand command);

    AdminResult<Course> Update(string id, CourseUpsertCommand command);

    bool Delete(string id);
}
