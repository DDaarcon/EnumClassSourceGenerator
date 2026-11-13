using EnumClasses;

namespace EnumClasses.CompatibilityTesting.NugetPackage;

[EnumClass]
internal partial class PackageStatus
{
    public static readonly PackageStatus Ready = new();
}

public class PackedPackageTests
{
    [Fact]
    public void PackageReference_LoadsAndRunsSourceGenerator()
    {
        var status = PackageStatus.Deserialize(nameof(PackageStatus.Ready));

        Assert.Same(PackageStatus.Ready, status);
        Assert.Equal([PackageStatus.Ready], PackageStatus.AllValues);
    }
}
