using HandilyCommerce.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HandilyCommerce.Infrastructure.Courses;

/// <summary>
/// EF mapping for <see cref="Course"/> → <c>Courses</c>. Catalog order lives in a shadow column so the
/// domain record (and the API JSON shape) stays free of persistence details.
/// </summary>
public static class CourseModel
{
    public const string TableName = "Courses";
    public const string SortOrderColumn = "SortOrder";

    public static void Configure(EntityTypeBuilder<Course> entity)
    {
        entity.ToTable(TableName);
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasMaxLength(100);
        entity.Property(c => c.Title).IsRequired().HasMaxLength(200);
        entity.Property(c => c.Axis).IsRequired().HasConversion<string>().HasMaxLength(64);
        entity.Property(c => c.Summary).IsRequired().HasMaxLength(1000);
        entity.Property(c => c.Audience).IsRequired().HasMaxLength(200);
        entity.Property<int>(SortOrderColumn).IsRequired();
        entity.HasIndex(SortOrderColumn);
    }
}
