namespace HandilyCommerce.Domain.Admin;

/// <summary>Dashboard counters for the admin area.</summary>
public sealed record AdminSummary(LeadCounts Leads, int Courses, int Products);

/// <summary>Lead totals by status.</summary>
public sealed record LeadCounts(int Total, int Novo, int Contatado, int Descartado);
