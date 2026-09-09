using System.Reflection;

namespace HandilyCommerce.Api.Versioning;

/// <summary>
/// Reads product Versioning stamped by MSBuild (<c>Directory.Build.props</c> → <c>Version</c> / InformationalVersion).
/// </summary>
public static class ProductVersionReader
{
    public static string FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+', StringComparison.Ordinal);
            return plus >= 0 ? informational[..plus] : informational;
        }

        var version = assembly.GetName().Version;
        return version is null
            ? "0.0.0"
            : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
