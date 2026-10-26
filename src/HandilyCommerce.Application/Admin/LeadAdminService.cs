using HandilyCommerce.Domain.Admin;
using HandilyCommerce.Domain.Leads;

namespace HandilyCommerce.Application.Admin;

/// <summary>Use cases: list, read, update internal fields and delete leads (admin).</summary>
public sealed class LeadAdminService(ILeadRepository repository) : ILeadAdminPort
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int InternalNoteMaxLength = 2000;

    public AdminResult<LeadPage> List(int? page, int? pageSize, string? status)
    {
        var errors = new Dictionary<string, string[]>();
        var pageValue = page ?? 1;
        var sizeValue = pageSize ?? DefaultPageSize;
        if (pageValue < 1)
        {
            errors["page"] = ["Página deve ser maior ou igual a 1."];
        }

        if (sizeValue is < 1 or > MaxPageSize)
        {
            errors["pageSize"] = [$"Tamanho da página deve estar entre 1 e {MaxPageSize}."];
        }

        LeadStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (AdminValidation.TryParseEnum<LeadStatus>(status, out var parsed))
            {
                statusFilter = parsed;
            }
            else
            {
                errors["status"] = [StatusError];
            }
        }

        if (errors.Count > 0)
        {
            return AdminResult<LeadPage>.Invalid(errors);
        }

        var (items, total) = repository.List(statusFilter, (pageValue - 1) * sizeValue, sizeValue);
        return AdminResult<LeadPage>.Success(new LeadPage(items, pageValue, sizeValue, total));
    }

    public Lead? Find(Guid id) => repository.Find(id);

    public AdminResult<Lead> Update(Guid id, UpdateLeadCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new Dictionary<string, string[]>();
        LeadStatus? newStatus = null;
        if (command.Status is not null)
        {
            if (AdminValidation.TryParseEnum<LeadStatus>(command.Status, out var parsed))
            {
                newStatus = parsed;
            }
            else
            {
                errors["status"] = [StatusError];
            }
        }

        var note = AdminValidation.Optional(errors, "internalNote", command.InternalNote, InternalNoteMaxLength, "Nota interna");
        if (command.Status is null && command.InternalNote is null)
        {
            errors["status"] = ["Informe status e/ou internalNote."];
        }

        if (errors.Count > 0)
        {
            return AdminResult<Lead>.Invalid(errors);
        }

        var current = repository.Find(id);
        if (current is null)
        {
            return AdminResult<Lead>.NotFound();
        }

        var updated = current with
        {
            Status = newStatus ?? current.Status,
            InternalNote = command.InternalNote is null ? current.InternalNote : note,
        };
        return repository.Update(updated) ? AdminResult<Lead>.Success(updated) : AdminResult<Lead>.NotFound();
    }

    public bool Delete(Guid id) => repository.Delete(id);

    private const string StatusError = "Status inválido. Use Novo, Contatado ou Descartado.";
}
