namespace HandilyCommerce.Domain.Leads;

/// <summary>
/// Outcome of <see cref="ILeadPort.Create"/>: either the persisted <see cref="Lead"/> or validation errors
/// keyed by field (camelCase, ready for ValidationProblemDetails).
/// </summary>
public sealed record CreateLeadResult(Lead? Lead, IReadOnlyDictionary<string, string[]> Errors)
{
    public bool IsSuccess => Lead is not null && Errors.Count == 0;

    public static CreateLeadResult Success(Lead lead) =>
        new(lead, new Dictionary<string, string[]>());

    public static CreateLeadResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(null, errors);
}
