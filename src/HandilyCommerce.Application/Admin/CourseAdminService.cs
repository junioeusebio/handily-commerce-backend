using System.Text.RegularExpressions;
using HandilyCommerce.Domain.Admin;
using HandilyCommerce.Domain.Courses;

namespace HandilyCommerce.Application.Admin;

/// <summary>Use cases: create, replace and delete catalog courses (admin).</summary>
public sealed partial class CourseAdminService(ICourseRepository repository) : ICourseAdminPort
{
    public const int IdMaxLength = 100;
    public const int TitleMaxLength = 200;
    public const int SummaryMaxLength = 1000;
    public const int AudienceMaxLength = 200;
    public const int MaxWorkloadHours = 10000;

    public Course? Find(string id) => repository.Find(id);

    public AdminResult<Course> Create(CourseUpsertCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new Dictionary<string, string[]>();
        var id = AdminValidation.Required(errors, "id", command.Id, IdMaxLength, "Id");
        if (!errors.ContainsKey("id") && !SlugPattern().IsMatch(id))
        {
            errors["id"] = ["Id deve ser um slug: letras minúsculas, números e hífens (ex.: cultura-digital)."];
        }

        var course = Validate(id, command, errors);
        if (errors.Count == 0 && repository.Find(id) is not null)
        {
            errors["id"] = ["Já existe um curso com este id."];
        }

        if (errors.Count > 0)
        {
            return AdminResult<Course>.Invalid(errors);
        }

        repository.Add(course!, command.SortOrder);
        return AdminResult<Course>.Success(repository.Find(id) ?? course!);
    }

    public AdminResult<Course> Update(string id, CourseUpsertCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new Dictionary<string, string[]>();
        var course = Validate(id, command, errors);
        if (errors.Count > 0)
        {
            return AdminResult<Course>.Invalid(errors);
        }

        // Null SortOrder keeps the stored order (FE may omit it).
        return repository.Update(course!, command.SortOrder)
            ? AdminResult<Course>.Success(repository.Find(id) ?? course!)
            : AdminResult<Course>.NotFound();
    }

    public bool Delete(string id) => repository.Delete(id);

    private static Course? Validate(string id, CourseUpsertCommand command, Dictionary<string, string[]> errors)
    {
        var title = AdminValidation.Required(errors, "title", command.Title, TitleMaxLength, "Título");
        var summary = AdminValidation.Required(errors, "summary", command.Summary, SummaryMaxLength, "Resumo");
        var audience = AdminValidation.Required(errors, "audience", command.Audience, AudienceMaxLength, "Público-alvo");

        if (!AdminValidation.TryParseEnum<BnccAxis>(command.Axis, out var axis))
        {
            errors["axis"] = ["Eixo inválido. Use pensamento-computacional, mundo-digital ou cultura-digital."];
        }

        if (command.WorkloadHours is < 1 or > MaxWorkloadHours)
        {
            errors["workloadHours"] = [$"Carga horária deve estar entre 1 e {MaxWorkloadHours}."];
        }

        if (command.SortOrder is < 0)
        {
            errors["sortOrder"] = ["Ordem deve ser maior ou igual a 0."];
        }

        return errors.Count > 0 ? null : new Course(id, title, axis, summary, audience, command.WorkloadHours);
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
