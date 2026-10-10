using EnumClasses;

namespace EnumClassSourceGenerator.UnitTests.Tests;

[EnumClass(GenerateRawEnum = true, ConstructionRestrictionMode = ConstructionRestrictionMode.Off, LookupMode = LookupMode.ForceDictionary)]
internal partial class RawEnum
{
    public static readonly RawEnum One = new();
    public static readonly SpecializedRawEnum Two = new();
}

internal sealed class SpecializedRawEnum : RawEnum
{
}

[EnumClass(
    GenerateRawEnum = true,
    ConstructionRestrictionMode = ConstructionRestrictionMode.WithProtectedDefaultConstructor,
    IndexAssignmentMode = IndexAssignmentMode.Runtime)]
internal partial class RuntimeIndexedRawEnum
{
    public static readonly RuntimeIndexedRawEnum Ten = new()
    {
        EnumIndex = 10
    };

    public static readonly RuntimeIndexedRawEnum Twenty = new()
    {
        EnumIndex = 20
    };
}
