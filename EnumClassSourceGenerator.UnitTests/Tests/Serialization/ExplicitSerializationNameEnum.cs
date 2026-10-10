using EnumClasses;

namespace EnumClassSourceGenerator.UnitTests.Tests;

[EnumClass]
public partial class ExplicitSerializationNameEnum
{
    [EnumClassValue(SerializedName = "property-name")]
    public static readonly ExplicitSerializationNameEnum FromProperty = new();

    [EnumClassValue(7, "constructor-name")]
    public static readonly ExplicitSerializationNameEnum FromConstructor = new();

    [EnumClassValue(serializedName: "named-constructor-name", enumIndex: 9)]
    public static readonly ExplicitSerializationNameEnum FromNamedConstructor = new();

    [EnumClassValue(SerializedName = "quote\" slash\\ line\n")]
    public static readonly ExplicitSerializationNameEnum WithEscapedCharacters = new();

    public static readonly ExplicitSerializationNameEnum DefaultName = new();
}

[EnumClass(LookupMode = LookupMode.ForceDictionary)]
public partial class DictionaryExplicitSerializationNameEnum
{
    [EnumClassValue(SerializedName = "dictionary-name")]
    public static readonly DictionaryExplicitSerializationNameEnum Value = new();
}
