using EnumClasses;
using IgnoreEnumValue = EnumClasses.EnumClassIgnoreAttribute;

namespace EnumClassSourceGenerator.UnitTests.Tests;

[EnumClass]
internal partial class EnumClassIgnoreEnum
{
    public static readonly EnumClassIgnoreEnum FirstIncluded = new();

    [EnumClassIgnore]
    public static readonly EnumClassIgnoreEnum IgnoredField = new();

    [global::EnumClasses.EnumClassIgnore]
    public static readonly string IgnoredInvalidField = string.Empty;

    public static EnumClassIgnoreEnum SecondIncluded { get; } = new();

    [IgnoreEnumValue]
    public static EnumClassIgnoreEnum IgnoredProperty { get; } = new();

    [EnumClasses.EnumClassIgnoreAttribute]
    public static string IgnoredInvalidProperty { get; } = string.Empty;
}
