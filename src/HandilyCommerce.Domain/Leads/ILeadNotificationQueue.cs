namespace HandilyCommerce.Domain.Leads;

/// <summary>
/// Outbound port: schedule a "new lead" notification without blocking the request.
/// Implementations must not throw for normal operation.
/// </summary>
public interface ILeadNotificationQueue
{
    void Enqueue(Lead lead);
}
