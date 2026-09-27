using EnumClasses;
using System.Collections.Frozen;

namespace EnumClassSourceGenerator.UnitTests.Tests;

[EnumClass(GenerateRawEnum = true, ConstructionRestrictionMode = ConstructionRestrictionMode.Off, SearchMode = SearchMode.ForceDictionary)]
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

//namespace EnumClassSourceGenerator.UnitTests.Tests
//{
//    internal class RawEnumJsonConverter : System.Text.Json.Serialization.JsonConverter<RawEnum>
//    {
//        public override RawEnum? Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
//        //=> RawEnum.Deserialize(reader.GetString());
//        {
//            Enumerable.Empty<int>().ToFrozenDictionary();
//            ReadOnlySpan<char> x;
//            x.Equals("dsadad".AsSpan(), System.StringComparison.Ordinal)
//            System.Collections.Frozen.FrozenDictionary<int, RawEnum> x = new();
//            var xx = x.GetAlternateLookup<ReadOnlySpan<char>>();
//            xx.
//        }

//        public override void Write(System.Text.Json.Utf8JsonWriter writer, RawEnum value, System.Text.Json.JsonSerializerOptions options)
//            => writer.WriteStringValue(value.Serialize());
//    }
//}