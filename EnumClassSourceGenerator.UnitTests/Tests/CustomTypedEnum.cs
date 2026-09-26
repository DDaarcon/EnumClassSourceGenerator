using EnumClasses;

namespace EnumClassSourceGenerator.UnitTests.Tests;

[EnumClass]
internal partial class CustomTypedEnum
{
    public static readonly CustomTypedEnum Base = new()
    {
        Value = "base"
    };

    public static readonly DerivedTypedEnum Derived = new()
    {
        Value = "derived",
        Extra = 5
    };

    public required string Value { get; init; }
}

internal partial class DerivedTypedEnum : CustomTypedEnum
{
    public int Extra { get; init; }
}
