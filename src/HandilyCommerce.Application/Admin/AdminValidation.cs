namespace HandilyCommerce.Application.Admin;

/// <summary>Shared field checks for admin use cases (PT-BR messages, camelCase keys).</summary>
internal static class AdminValidation
{
    public static string Required(
        Dictionary<string, string[]> errors, string field, string? raw, int maxLength, string label)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            errors[field] = [$"{label} é obrigatório."];
        }
        else if (value.Length > maxLength)
        {
            errors[field] = [$"{label} deve ter no máximo {maxLength} caracteres."];
        }

        return value;
    }

    public static string? Optional(
        Dictionary<string, string[]> errors, string field, string? raw, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var value = raw.Trim();
        if (value.Length > maxLength)
        {
            errors[field] = [$"{label} deve ter no máximo {maxLength} caracteres."];
        }

        return value;
    }

    /// <summary>Accepts <c>MundoDigital</c>, <c>mundoDigital</c> or <c>mundo-digital</c>.</summary>
    public static bool TryParseEnum<TEnum>(string? raw, out TEnum value)
        where TEnum : struct, Enum
    {
        value = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = raw.Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal);
        return !int.TryParse(normalized, out _)
            && Enum.TryParse(normalized, ignoreCase: true, out value)
            && Enum.IsDefined(value);
    }
}
