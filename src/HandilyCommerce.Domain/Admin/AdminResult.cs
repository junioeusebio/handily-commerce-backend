namespace HandilyCommerce.Domain.Admin;

/// <summary>Outcome of an admin use case: a value, validation errors (keyed by camelCase field) or not found.</summary>
public sealed record AdminResult<T>(T? Value, IReadOnlyDictionary<string, string[]> Errors, bool IsNotFound)
    where T : class
{
    public bool IsSuccess => Value is not null && Errors.Count == 0 && !IsNotFound;

    public static AdminResult<T> Success(T value) => new(value, new Dictionary<string, string[]>(), false);

    public static AdminResult<T> Invalid(IReadOnlyDictionary<string, string[]> errors) => new(null, errors, false);

    public static AdminResult<T> NotFound() => new(null, new Dictionary<string, string[]>(), true);
}
