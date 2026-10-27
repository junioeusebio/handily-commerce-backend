namespace HandilyCommerce.Domain.Leads;

/// <summary>
/// Contact request sent from the public site ("Fale conosco"). Persisted in table <c>Leads</c>.
/// </summary>
/// <param name="Id">Generated identifier.</param>
/// <param name="Name">Contact name.</param>
/// <param name="Email">Contact e-mail.</param>
/// <param name="Municipality">Municipality (município) the contact represents.</param>
/// <param name="Message">Optional free-text message.</param>
/// <param name="CreatedAt">Creation instant (UTC).</param>
/// <param name="Status">Internal follow-up status (admin).</param>
/// <param name="InternalNote">Internal note (admin only, never public).</param>
/// <param name="Phone">Brazilian phone, digits only (10 or 11). Null for leads created before the field existed.</param>
public sealed record Lead(
    Guid Id,
    string Name,
    string Email,
    string Municipality,
    string? Message,
    DateTimeOffset CreatedAt,
    LeadStatus Status = LeadStatus.Novo,
    string? InternalNote = null,
    string? Phone = null);
