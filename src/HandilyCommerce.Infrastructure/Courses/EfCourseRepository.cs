using HandilyCommerce.Domain.Courses;
using HandilyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HandilyCommerce.Infrastructure.Courses;

/// <summary>
/// EF Core adapter for <see cref="ICourseRepository"/> against Supabase Postgres (<c>Courses</c> table).
/// Returns rows in catalog order (shadow column <see cref="CourseModel.SortOrderColumn"/>).
/// </summary>
public sealed class EfCourseRepository(HandilyCommerceDbContext dbContext) : ICourseRepository
{
    public IReadOnlyList<Course> ListAll() =>
        dbContext.Courses
            .AsNoTracking()
            .OrderBy(c => EF.Property<int>(c, CourseModel.SortOrderColumn))
            .ThenBy(c => c.Id)
            .ToList();
}
