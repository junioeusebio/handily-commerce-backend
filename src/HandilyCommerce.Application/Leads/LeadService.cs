using System.Net.Mail;
using HandilyCommerce.Domain.Leads;

namespace HandilyCommerce.Application.Leads;

/// <summary>
/// Use case: validate and persist a contact lead ("Fale conosco").
/// </summary>
public sealed class LeadService(
    ILeadRepository repository,
    ILeadNotificationQueue notificationQueue,
    TimeProvider timeProvider) : ILeadPort
{
    public const int NameMaxLength = 120;
    public const int EmailMaxLength = 254;
    public const int MunicipalityMaxLength = 120;
    public const int MessageMaxLength = 2000;

    public CreateLeadResult Create(CreateLeadCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var name = command.Name?.Trim() ?? string.Empty;
        var email = command.Email?.Trim() ?? string.Empty;
        var municipality = command.Municipality?.Trim() ?? string.Empty;
        var phone = PhoneNumber.Normalize(command.Phone);
        var message = string.IsNullOrWhiteSpace(command.Message) ? null : command.Message.Trim();

        var errors = new Dictionary<string, string[]>();
        Required(errors, "name", name, NameMaxLength, "Nome");
        Required(errors, "email", email, EmailMaxLength, "E-mail");
        Required(errors, "municipality", municipality, MunicipalityMaxLength, "Município");

        if (!errors.ContainsKey("email") && !IsValidEmail(email))
        {
            errors["email"] = ["E-mail inválido."];
        }

        if (phone.Length == 0)
        {
            errors["phone"] = ["Telefone é obrigatório."];
        }
        else if (!PhoneNumber.IsValid(phone))
        {
            errors["phone"] = ["Telefone inválido. Informe DDD + número (10 ou 11 dígitos)."];
        }

        if (message is not null && message.Length > MessageMaxLength)
        {
            errors["message"] = [$"Mensagem deve ter no máximo {MessageMaxLength} caracteres."];
        }

        if (errors.Count > 0)
        {
            return CreateLeadResult.Invalid(errors);
        }

        var lead = new Lead(
            Guid.NewGuid(),
            name,
            email,
            municipality,
            message,
            timeProvider.GetUtcNow(),
            Phone: phone);

        repository.Add(lead);
        TryNotify(lead);
        return CreateLeadResult.Success(lead);
    }

    /// <summary>The lead is already saved: a notification problem must never fail the request.</summary>
    private void TryNotify(Lead lead)
    {
        try
        {
            notificationQueue.Enqueue(lead);
        }
        catch (Exception)
        {
            // Intentionally ignored: e-mail is best-effort; attempts are audited in EmailSendLogs.
        }
    }

    private static void Required(
        Dictionary<string, string[]> errors, string field, string value, int maxLength, string label)
    {
        if (value.Length == 0)
        {
            errors[field] = [$"{label} é obrigatório."];
        }
        else if (value.Length > maxLength)
        {
            errors[field] = [$"{label} deve ter no máximo {maxLength} caracteres."];
        }
    }

    private static bool IsValidEmail(string email) =>
        MailAddress.TryCreate(email, out var address)
        && string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase)
        && address.Host.Contains('.', StringComparison.Ordinal);
}
