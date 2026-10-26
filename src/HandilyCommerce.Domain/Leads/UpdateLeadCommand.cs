namespace HandilyCommerce.Domain.Leads;

/// <summary>Admin update of internal lead fields. Null keeps the current value; empty note clears it.</summary>
public sealed record UpdateLeadCommand(string? Status, string? InternalNote);
