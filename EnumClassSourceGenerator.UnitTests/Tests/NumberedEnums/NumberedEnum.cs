using EnumClasses;

namespace EnumClassSourceGenerator.UnitTests.Tests;

[NumberedEnumClass]
internal partial class NumberedEnum
{
    [EnumClassValue]
    public static readonly NumberedEnum Ten = new()
    {
        EnumIndex = 10
    };

    public static readonly NumberedEnum Twenty = new()
    {
        EnumIndex = 20
    };
}
