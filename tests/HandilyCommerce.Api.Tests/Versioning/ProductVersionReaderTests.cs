using System.Reflection;
using HandilyCommerce.Api.Versioning;

namespace HandilyCommerce.Api.Tests.Versioning;

public class ProductVersionReaderTests
{
    [Fact]
    public void FromAssembly_UsesInformationalVersion_WithoutBuildMetadata()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        Assert.False(string.IsNullOrWhiteSpace(informational));

        var result = ProductVersionReader.FromAssembly(assembly);
        var expected = informational!.Contains('+', StringComparison.Ordinal)
            ? informational[..informational.IndexOf('+', StringComparison.Ordinal)]
            : informational;

        Assert.Equal(expected, result);
        Assert.DoesNotContain('+', result);
    }

    [Fact]
    public void FromAssembly_ThrowsWhenAssemblyIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ProductVersionReader.FromAssembly(null!));
    }

    [Fact]
    public void FromAssembly_ApiProject_ExposesProductVersioningFromDirectoryBuildProps()
    {
        var apiAssembly = typeof(ProductVersionReader).Assembly;
        var version = ProductVersionReader.FromAssembly(apiAssembly);

        Assert.Equal("0.1.0", version);
    }
}
