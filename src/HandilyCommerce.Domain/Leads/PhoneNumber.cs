namespace HandilyCommerce.Domain.Leads;

/// <summary>Brazilian phone helpers: normalize to digits (DDD + number) and format for display.</summary>
public static class PhoneNumber
{
    public const int MinDigits = 10;
    public const int MaxDigits = 11;

    /// <summary>Keeps only ASCII digits; strips a leading +55 country code when the result would be 12/13 digits.</summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var digits = string.Concat(input.Where(char.IsAsciiDigit));
        if (digits.Length is 12 or 13 && digits.StartsWith("55", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        return digits;
    }

    public static bool IsValid(string digits) =>
        digits.Length is MinDigits or MaxDigits
        && digits.All(char.IsAsciiDigit)
        && digits[0] != '0';

    /// <summary>(11) 91234-5678 / (11) 3123-4567; other values are returned unchanged.</summary>
    public static string Format(string digits) => digits.Length switch
    {
        MaxDigits when digits.All(char.IsAsciiDigit) => $"({digits[..2]}) {digits[2..7]}-{digits[7..]}",
        MinDigits when digits.All(char.IsAsciiDigit) => $"({digits[..2]}) {digits[2..6]}-{digits[6..]}",
        _ => digits,
    };
}
