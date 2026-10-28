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
            .Select(c => new { Course = c, Order = EF.Property<int>(c, CourseModel.SortOrderColumn) })
            .AsEnumerable()
            .Select(r => r.Course with { DisplayOrder = r.Order })
            .ToList();

    public Course? Find(string id) =>
        dbContext.Courses.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new { Course = c, Order = EF.Property<int>(c, CourseModel.SortOrderColumn) })
            .AsEnumerable()
            .Select(r => r.Course with { DisplayOrder = r.Order })
            .FirstOrDefault();

    public void Add(Course course, int? sortOrder)
    {
        var order = sortOrder ?? NextSortOrder();
        var entry = dbContext.Courses.Add(course);
        entry.Property(CourseModel.SortOrderColumn).CurrentValue = order;
        dbContext.SaveChanges();
    }

    public bool Update(Course course, int? sortOrder)
    {
        var stored = dbContext.Courses.FirstOrDefault(c => c.Id == course.Id);
        if (stored is null)
        {
            return false;
        }

        var entry = dbContext.Entry(stored);
        entry.CurrentValues.SetValues(course);
        if (sortOrder is not null)
        {
            entry.Property(CourseModel.SortOrderColumn).CurrentValue = sortOrder.Value;
        }

        dbContext.SaveChanges();
        return true;
    }

    public bool Delete(string id)
    {
        var stored = dbContext.Courses.FirstOrDefault(c => c.Id == id);
        if (stored is null)
        {
            return false;
        }

        dbContext.Courses.Remove(stored);
        dbContext.SaveChanges();
        return true;
    }

    public int Count() => dbContext.Courses.Count();

    private int NextSortOrder() =>
        (dbContext.Courses.Max(c => (int?)EF.Property<int>(c, CourseModel.SortOrderColumn)) ?? 0) + 1;
}
