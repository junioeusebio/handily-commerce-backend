using HandilyCommerce.Domain.Ping;

namespace HandilyCommerce.Application.Ping;

public sealed class PingService : IPingPort
{
    public ServicePing GetPing(string apiVersion) => ServicePing.CreateOk(apiVersion);
}
