using EnumClasses;

namespace EnumClassSourceGenerator.UnitTests.Tests;

[EnumClass(GenerateRawEnum = true, ConstructionRestrictionMode = ConstructionRestrictionMode.Off)]
internal partial class RawEnum
{
    public static readonly RawEnum One = new();
    public static readonly SpecializedRawEnum Two = new();
}

internal sealed class SpecializedRawEnum : RawEnum
{
}

[NumberedEnumClass(GenerateRawEnum = true, ConstructionRestrictionMode = ConstructionRestrictionMode.WithProtectedDefaultConstructor)]
internal partial class NumberedRawEnum
{
    public static readonly NumberedRawEnum Ten = new()
    {
        EnumIndex = 10
    };

    public static readonly NumberedRawEnum Twenty = new()
    {
        EnumIndex = 20
    };
}
