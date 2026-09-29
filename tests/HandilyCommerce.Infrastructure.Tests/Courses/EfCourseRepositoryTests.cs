using HandilyCommerce.Domain.Courses;
using HandilyCommerce.Infrastructure.Courses;
using HandilyCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HandilyCommerce.Infrastructure.Tests.Courses;

/// <summary>
/// Uses EF InMemory — no live Supabase / Postgres required in CI.
/// Production rows are seeded by migration <c>AddCourses</c> (idempotent SQL).
/// </summary>
public class EfCourseRepositoryTests
{
    private const string AddCoursesMigration = "20260929194435_AddCourses";

    private static HandilyCommerceDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HandilyCommerceDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var context = new HandilyCommerceDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void AddCourse(HandilyCommerceDbContext context, Course course, int sortOrder)
    {
        context.Courses.Add(course);
        context.Entry(course).Property(CourseModel.SortOrderColumn).CurrentValue = sortOrder;
    }

    [Fact]
    public void ListAll_ReturnsCoursesBySortOrder_NotInsertionOrder()
    {
        using var context = CreateContext(nameof(ListAll_ReturnsCoursesBySortOrder_NotInsertionOrder));
        AddCourse(context, new Course("cd", "CD", BnccAxis.CulturaDigital, "s", "a", 24), 40);
        AddCourse(context, new Course("pc", "PC", BnccAxis.PensamentoComputacional, "s", "a", 40), 10);
        AddCourse(context, new Course("md", "MD", BnccAxis.MundoDigital, "s", "a"), 30);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var courses = new EfCourseRepository(context).ListAll();

        Assert.Equal(["pc", "md", "cd"], courses.Select(c => c.Id));
    }

    [Fact]
    public void ListAll_RoundTripsAxisAndOptionalWorkloadHours()
    {
        using var context = CreateContext(nameof(ListAll_RoundTripsAxisAndOptionalWorkloadHours));
        AddCourse(context, new Course("pc", "Título", BnccAxis.PensamentoComputacional, "Resumo", "Professores", 40), 10);
        AddCourse(context, new Course("md", "MD", BnccAxis.MundoDigital, "s", "a"), 20);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var byId = new EfCourseRepository(context).ListAll().ToDictionary(c => c.Id);

        Assert.Equal(
            new Course("pc", "Título", BnccAxis.PensamentoComputacional, "Resumo", "Professores", 40),
            byId["pc"]);
        Assert.Equal(BnccAxis.MundoDigital, byId["md"].Axis);
        Assert.Null(byId["md"].WorkloadHours);
    }

    [Fact]
    public void ListAll_WhenEmpty_ReturnsEmptyList()
    {
        using var context = CreateContext(nameof(ListAll_WhenEmpty_ReturnsEmptyList));

        Assert.Empty(new EfCourseRepository(context).ListAll());
    }

    [Fact]
    public void Model_MapsCoursesTable_WithAxisAsStringAndShadowSortOrder()
    {
        using var context = CreateContext(nameof(Model_MapsCoursesTable_WithAxisAsStringAndShadowSortOrder));

        var entity = context.Model.FindEntityType(typeof(Course))!;

        Assert.Equal(CourseModel.TableName, entity.GetTableName());
        Assert.Equal(typeof(string), entity.FindProperty(nameof(Course.Axis))!.GetProviderClrType());
        Assert.True(entity.FindProperty(nameof(Course.WorkloadHours))!.IsNullable);
        var sortOrder = entity.FindProperty(CourseModel.SortOrderColumn)!;
        Assert.True(sortOrder.IsShadowProperty());
        Assert.False(sortOrder.IsNullable);
    }

    [Fact]
    public void AddCoursesMigration_CreatesTableAndSeedsInitialCoursesIdempotently()
    {
        // Script generation only — Npgsql provider never opens a connection here.
        var options = new DbContextOptionsBuilder<HandilyCommerceDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options;
        using var context = new HandilyCommerceDbContext(options);
        var migrator = context.GetService<IMigrator>();

        var script = migrator.GenerateScript(
            fromMigration: "20260917002705_RemoveChangelogHasDataSeed",
            toMigration: AddCoursesMigration);

        Assert.Contains("CREATE TABLE \"Courses\"", script);
        Assert.Contains("ON CONFLICT (\"Id\") DO NOTHING", script);
        foreach (var id in new[]
                 {
                     "pensamento-computacional-na-pratica",
                     "programacao-criativa-com-blocos",
                     "mundo-digital-dados-e-redes",
                     "cultura-digital-e-cidadania"
                 })
        {
            Assert.Contains($"'{id}'", script);
        }

        foreach (var axis in Enum.GetNames<BnccAxis>())
        {
            Assert.Contains($"'{axis}'", script);
        }
    }
}
