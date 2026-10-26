using HandilyCommerce.Api.Courses;
using HandilyCommerce.Domain.Admin;
using HandilyCommerce.Domain.Courses;
using HandilyCommerce.Domain.Leads;
using HandilyCommerce.Domain.Products;

namespace HandilyCommerce.Api.Admin;

/// <summary>
/// Admin area under <c>/{RoutePrefix}/{Version}/admin</c>. Every route requires the bearer JWT;
/// public catalog and lead endpoints stay untouched.
/// </summary>
public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this IEndpointRouteBuilder app, string adminPath)
    {
        var admin = app.MapGroup(adminPath)
            .RequireAuthorization()
            .WithTags("Admin")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        admin.MapGet("summary", (IAdminSummaryPort port) => Results.Ok(port.GetSummary()))
            .WithName("AdminSummary").WithSummary("Contadores do painel admin")
            .Produces<AdminSummary>();

        MapLeads(admin.MapGroup("leads"));
        MapCourses(admin.MapGroup("courses"));
        MapProducts(admin.MapGroup("products"));
        return admin;
    }

    private static void MapLeads(RouteGroupBuilder leads)
    {
        leads.MapGet("", (ILeadAdminPort port, int? page, int? pageSize, string? status) =>
            {
                var result = port.List(page, pageSize, status);
                return result.IsSuccess
                    ? Results.Ok(new LeadPageResponse(
                        result.Value!.Items.Select(LeadResponse.From).ToList(),
                        result.Value.Page,
                        result.Value.PageSize,
                        result.Value.Total))
                    : Results.ValidationProblem(result.Errors.ToDictionary());
            })
            .WithName("AdminListLeads").WithSummary("Lista leads (mais novos primeiro, paginado)")
            .Produces<LeadPageResponse>().ProducesValidationProblem();

        leads.MapGet("{id:guid}", (Guid id, ILeadAdminPort port) =>
                port.Find(id) is { } lead ? Results.Ok(LeadResponse.From(lead)) : Results.NotFound())
            .WithName("AdminGetLead").WithSummary("Detalhe de um lead")
            .Produces<LeadResponse>().Produces(StatusCodes.Status404NotFound);

        leads.MapPatch("{id:guid}", (Guid id, UpdateLeadCommand command, ILeadAdminPort port) =>
                ToResult(port.Update(id, command), lead => Results.Ok(LeadResponse.From(lead))))
            .WithName("AdminUpdateLead").WithSummary("Atualiza status e/ou nota interna do lead")
            .Accepts<UpdateLeadCommand>("application/json")
            .Produces<LeadResponse>().ProducesValidationProblem().Produces(StatusCodes.Status404NotFound);

        leads.MapDelete("{id:guid}", (Guid id, ILeadAdminPort port) =>
                port.Delete(id) ? Results.NoContent() : Results.NotFound())
            .WithName("AdminDeleteLead").WithSummary("Apaga um lead")
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);
    }

    private static void MapCourses(RouteGroupBuilder courses)
    {
        courses.MapGet("{id}", (string id, ICourseAdminPort port) =>
                port.Find(id) is { } course
                    ? Results.Json(course, CourseJson.SerializerOptions)
                    : Results.NotFound())
            .WithName("AdminGetCourse").WithSummary("Detalhe de um curso")
            .Produces<Course>().Produces(StatusCodes.Status404NotFound);

        courses.MapPost("", (CourseUpsertCommand command, ICourseAdminPort port) =>
                ToResult(port.Create(command), course =>
                    Results.Json(course, CourseJson.SerializerOptions, statusCode: StatusCodes.Status201Created)))
            .WithName("AdminCreateCourse").WithSummary("Cria um curso")
            .Accepts<CourseUpsertCommand>("application/json")
            .Produces<Course>(StatusCodes.Status201Created).ProducesValidationProblem();

        courses.MapPut("{id}", (string id, CourseUpsertCommand command, ICourseAdminPort port) =>
                ToResult(port.Update(id, command), course => Results.Json(course, CourseJson.SerializerOptions)))
            .WithName("AdminUpdateCourse").WithSummary("Substitui um curso (id da rota)")
            .Accepts<CourseUpsertCommand>("application/json")
            .Produces<Course>().ProducesValidationProblem().Produces(StatusCodes.Status404NotFound);

        courses.MapDelete("{id}", (string id, ICourseAdminPort port) =>
                port.Delete(id) ? Results.NoContent() : Results.NotFound())
            .WithName("AdminDeleteCourse").WithSummary("Apaga um curso")
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);
    }

    private static void MapProducts(RouteGroupBuilder products)
    {
        products.MapGet("{id:guid}", (Guid id, IProductAdminPort port) =>
                port.Find(id) is { } product ? Results.Ok(product) : Results.NotFound())
            .WithName("AdminGetProduct").WithSummary("Detalhe de um produto (com itens)")
            .Produces<Product>().Produces(StatusCodes.Status404NotFound);

        products.MapPost("", (ProductUpsertCommand command, IProductAdminPort port) =>
                ToResult(port.Create(command), product => Results.Json(product, statusCode: StatusCodes.Status201Created)))
            .WithName("AdminCreateProduct").WithSummary("Cria um produto com itens")
            .Accepts<ProductUpsertCommand>("application/json")
            .Produces<Product>(StatusCodes.Status201Created).ProducesValidationProblem();

        products.MapPut("{id:guid}", (Guid id, ProductUpsertCommand command, IProductAdminPort port) =>
                ToResult(port.Update(id, command), Results.Ok))
            .WithName("AdminUpdateProduct").WithSummary("Substitui um produto e todos os itens")
            .Accepts<ProductUpsertCommand>("application/json")
            .Produces<Product>().ProducesValidationProblem().Produces(StatusCodes.Status404NotFound);

        products.MapDelete("{id:guid}", (Guid id, IProductAdminPort port) =>
                port.Delete(id) ? Results.NoContent() : Results.NotFound())
            .WithName("AdminDeleteProduct").WithSummary("Apaga um produto (e os itens)")
            .Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);
    }

    public static IResult ToResult<T>(AdminResult<T> result, Func<T, IResult> onSuccess)
        where T : class
    {
        if (result.IsNotFound)
        {
            return Results.NotFound();
        }

        return result.IsSuccess
            ? onSuccess(result.Value!)
            : Results.ValidationProblem(result.Errors.ToDictionary());
    }
}

/// <summary>Admin lead view (status as text: Novo | Contatado | Descartado).</summary>
public sealed record LeadResponse(
    Guid Id,
    string Name,
    string Email,
    string Municipality,
    string? Message,
    DateTimeOffset CreatedAt,
    string Status,
    string? InternalNote)
{
    public static LeadResponse From(Lead lead) =>
        new(lead.Id, lead.Name, lead.Email, lead.Municipality, lead.Message, lead.CreatedAt,
            lead.Status.ToString(), lead.InternalNote);
}

public sealed record LeadPageResponse(IReadOnlyList<LeadResponse> Items, int Page, int PageSize, int Total);
