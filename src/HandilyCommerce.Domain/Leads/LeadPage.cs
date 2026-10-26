namespace HandilyCommerce.Domain.Leads;

/// <summary>One page of leads, newest first.</summary>
public sealed record LeadPage(IReadOnlyList<Lead> Items, int Page, int PageSize, int Total);
