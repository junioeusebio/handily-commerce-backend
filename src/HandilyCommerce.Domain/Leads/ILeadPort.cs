namespace HandilyCommerce.Domain.Leads;

/// <summary>
/// Driving port: register a contact lead from the public site (implemented in Application).
/// </summary>
public interface ILeadPort
{
    /// <summary>Validates and persists a lead.</summary>
    CreateLeadResult Create(CreateLeadCommand command);
}
