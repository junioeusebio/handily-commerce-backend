namespace HandilyCommerce.Domain.Admin;

/// <summary>Driving port: dashboard counters (implemented in Application).</summary>
public interface IAdminSummaryPort
{
    AdminSummary GetSummary();
}
