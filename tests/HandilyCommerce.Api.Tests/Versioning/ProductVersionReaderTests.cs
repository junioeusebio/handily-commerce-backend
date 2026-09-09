using System.Reflection;
using System.Reflection.Emit;
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

        Assert.Equal("0.2.0", version);
    }

    [Fact]
    public void FromAssembly_FallsBackToAssemblyNameVersion_WhenInformationalVersionMissing()
    {
        var assemblyName = new AssemblyName("HandilyCommerce.Dynamic.WithVersion")
        {
            Version = new Version(2, 3, 4, 5)
        };
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);

        var result = ProductVersionReader.FromAssembly(assembly);

        Assert.Equal("2.3.4", result);
    }

    [Fact]
    public void FromAssembly_FallsBackToAssemblyNameVersion_WhenInformationalVersionIsWhitespace()
    {
        var assemblyName = new AssemblyName("HandilyCommerce.Dynamic.WhitespaceInfo")
        {
            Version = new Version(9, 8, 7, 0)
        };
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);
        var ctor = typeof(AssemblyInformationalVersionAttribute)
            .GetConstructor([typeof(string)]);
        Assert.NotNull(ctor);
        assembly.SetCustomAttribute(new CustomAttributeBuilder(ctor, ["   "]));

        var result = ProductVersionReader.FromAssembly(assembly);

        Assert.Equal("9.8.7", result);
    }

    [Fact]
    public void FromAssembly_ReturnsZeroVersion_WhenInformationalAndAssemblyVersionMissing()
    {
        // Reflection.Emit stamps Version=0.0.0.0 when unset; use a stub so GetName().Version is null.
        var assembly = new AssemblyWithoutVersion();

        Assert.Null(assembly.GetName().Version);

        var result = ProductVersionReader.FromAssembly(assembly);

        Assert.Equal("0.0.0", result);
    }

    /// <summary>
    /// Minimal <see cref="Assembly"/> stub: no InformationalVersion and null <see cref="AssemblyName.Version"/>.
    /// </summary>
    private sealed class AssemblyWithoutVersion : Assembly
    {
        private readonly AssemblyName _name = new("HandilyCommerce.Stub.NoVersion");

        public override AssemblyName GetName() => _name;

        public override AssemblyName GetName(bool copiedName) => _name;

        public override object[] GetCustomAttributes(bool inherit) => Array.Empty<Attribute>();

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            Array.Empty<Attribute>();

        public override bool IsDefined(Type attributeType, bool inherit) => false;

        public override IEnumerable<CustomAttributeData> CustomAttributes => [];

        public override IList<CustomAttributeData> GetCustomAttributesData() => [];
    }
}