using EnumClasses;

namespace EnumClassSourceGenerator.UnitTests.Tests;

[EnumClass(LookupMode = LookupMode.ForceDictionary)]
internal partial class AlternateLookupEnum
{
    public static readonly AlternateLookupEnum One = new();
    public static readonly AlternateLookupEnum Two = new();
    public static readonly AlternateLookupEnum ValueWithNameLongEnoughToExerciseThePooledUtf8BufferPath = new();
}
