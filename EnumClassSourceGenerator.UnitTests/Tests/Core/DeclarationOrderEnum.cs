using EnumClasses;

namespace EnumClassSourceGenerator.UnitTests.Tests;

[EnumClass]
internal partial class DeclarationOrderEnum
{
    public static DeclarationOrderEnum FirstProperty { get; } = new();

    public static readonly DeclarationOrderEnum SecondField = new(), ThirdField = new();

    public static DeclarationOrderEnum FourthProperty { get; } = new();
}
