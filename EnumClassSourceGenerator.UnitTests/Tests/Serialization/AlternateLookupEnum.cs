using EnumClasses;

namespace EnumClassSourceGenerator.UnitTests.Tests;

[EnumClass(SearchMode = SearchMode.ForceDictionary)]
internal partial class AlternateLookupEnum
{
    public static readonly AlternateLookupEnum One = new();
    public static readonly AlternateLookupEnum Two = new();
}
