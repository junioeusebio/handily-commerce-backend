namespace HandilyCommerce.Domain.Leads;

/// <summary>Input for creating a <see cref="Lead"/> (raw values, validated by the use case).</summary>
public sealed record CreateLeadCommand(
    string? Name,
    string? Email,
    string? Municipality,
    string? Message,
    string? Phone = null);
