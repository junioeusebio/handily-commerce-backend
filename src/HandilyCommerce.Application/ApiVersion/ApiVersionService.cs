using HandilyCommerce.Domain.ApiVersion;

namespace HandilyCommerce.Application.ApiVersion;

public sealed class ApiVersionService : IApiVersionPort
{
    public ServiceApiVersion GetApiVersion(string version) => ServiceApiVersion.Create(version);
}
