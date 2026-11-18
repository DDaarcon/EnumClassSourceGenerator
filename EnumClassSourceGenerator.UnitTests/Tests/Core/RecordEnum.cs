using EnumClasses;

namespace EnumClassSourceGenerator.UnitTests.Tests;

[EnumClass(ConstructionRestrictionMode = ConstructionRestrictionMode.WithPrivateDefaultConstructor)]
internal sealed partial record RecordEnum
{
    public static readonly RecordEnum One = new();
    public static readonly RecordEnum Two = new();
}
