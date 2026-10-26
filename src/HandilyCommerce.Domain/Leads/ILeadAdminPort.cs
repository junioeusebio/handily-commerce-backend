using HandilyCommerce.Domain.Admin;

namespace HandilyCommerce.Domain.Leads;

/// <summary>Driving port: admin lead management (implemented in Application).</summary>
public interface ILeadAdminPort
{
    AdminResult<LeadPage> List(int? page, int? pageSize, string? status);

    Lead? Find(Guid id);

    AdminResult<Lead> Update(Guid id, UpdateLeadCommand command);

    bool Delete(Guid id);
}
