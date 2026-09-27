using EnumClasses.SourceGenerators.GenerationStrategies;
using System.Linq;
using EnumClassProps = EnumClasses.SourceGenerators.Schema.EnumClassCollector.Definition.WithConfig;
using EnumValueDefinition = EnumClasses.SourceGenerators.Schema.EnumValueCollector.Definition;

namespace EnumClasses.SourceGenerators.Templates;

internal class EnumClassSerializationConverterTemplate
{
    public static string Build(EnumClassProps props)
    {
        var definition = props.Definition;

        var enumValues = definition.EnumValues.Definitions
            .Where(value => value.IsValid)
            .ToArray();

        return $$"""
            #nullable enable
            
            namespace {{definition.NamespaceName}}
            {
                {{definition.Modifier}} partial class {{definition.DeclarationName}}
                {

                    internal class JsonConverter : System.Text.Json.Serialization.JsonConverter<{{definition.DeclarationName}}>
                    {
                        public override {{definition.DeclarationName}}? Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
                        {
                            {{BuildReadingLogic(enumValues, props)}}
                        }
            
                        public override void Write(System.Text.Json.Utf8JsonWriter writer, {{definition.DeclarationName}} value, System.Text.Json.JsonSerializerOptions options)
                            => writer.WriteStringValue(value.Serialize());



                        {{BuildUtf8JsonReadingHelpWhenApplicable(enumValues, props)}}
                    }
                }
            }
            
            #nullable disable
            """;

        static string BuildReadingLogic(EnumValueDefinition[] enumValues, EnumClassProps props)
        {
            return GetLookupImplementationForUtf8Keys(enumValues, props) switch
            {
                LookupImplementation.Dictionary => BuildDictionaryLogic(enumValues, props),
                LookupImplementation.IfChain or _ => BuildIfChainLogic(enumValues, props),
            };


            static string BuildIfChainLogic(EnumValueDefinition[] enumValues, EnumClassProps props)
            {
                var readCases = string.Join(
                    Consts.Nl,
                    enumValues.Select(value =>
                    {
                        var serializedName =
                            Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(
                                value.NormalizedName,
                                quote: true);

                        return
                            $"if (reader.ValueTextEquals({serializedName}u8)) " +
                            $"return {props.Definition.DeclarationName}.{value.Name};";
                    }));

                return $$"""
                    if (reader.TokenType == System.Text.Json.JsonTokenType.Null)
                    return null;

                    if (reader.TokenType != System.Text.Json.JsonTokenType.String)
                    {
                        throw new System.Text.Json.JsonException(
                            "Expected a {{props.Definition.DeclarationName}} string.");
                    }

                    {{readCases}}

                    throw new System.Text.Json.JsonException(
                        "Unknown {{props.Definition.DeclarationName}} value.");
                    """;
            }


            static string BuildDictionaryLogic(EnumValueDefinition[] enumValues, EnumClassProps props)
            {
                if (!props.Definition.Meta.IsAlternateLookupSupported)
                    return BuildIfChainLogic(enumValues, props); // fallback to ifs


                return """
                    return ReadValueFromDictionary(ref reader);
                    """;
            }

        }


        static string BuildUtf8JsonReadingHelpWhenApplicable(EnumValueDefinition[] enumValues, EnumClassProps props)
        {
            if (GetLookupImplementationForUtf8Keys(enumValues, props) is not LookupImplementation.Dictionary
                || !props.Definition.Meta.IsAlternateLookupSupported)
            {
                return "";
            }

            return $$"""
                private static {{props.Definition.DeclarationName}} ReadValueFromDictionary(
                    ref global::System.Text.Json.Utf8JsonReader reader)
                {
                    if (reader.TokenType != global::System.Text.Json.JsonTokenType.String)
                    {
                        throw new global::System.Text.Json.JsonException(
                            "Expected a string.");
                    }

                    // Fast path: contiguous, already-unescaped UTF-8.
                    if (!reader.HasValueSequence && !reader.ValueIsEscaped)
                    {
                        global::System.ReadOnlySpan<byte> utf8 = reader.ValueSpan;

                        if ({{EnumClassDeclarationTemplate.ValuesBySerializedUtf8NameSpanLookupVariableName}}.TryGetValue(utf8, out {{props.Definition.DeclarationName}}? value))
                            return value;

                        throw new global::System.Text.Json.JsonException(
                            "Unknown enum value.");
                    }

                    // Slow path: escaped text or a multi-segment token.
                    int maximumLength = checked((int)(
                        reader.HasValueSequence
                            ? reader.ValueSequence.Length
                            : reader.ValueSpan.Length));


                    if (maximumLength <= 256)
                    {
                        global::System.Span<byte> stackBuffer = stackalloc byte[maximumLength];
                        
                        int bytesWritten = reader.CopyString(stackBuffer);
                        
                        return Lookup(stackBuffer[..bytesWritten]);
                    }

                                
                    byte[] rented =
                        global::System.Buffers.ArrayPool<byte>.Shared.Rent(
                            maximumLength);

                    try
                    {
                        global::System.Span<byte> rentedBuffer = rented;

                        int bytesWritten = reader.CopyString(rentedBuffer);

                        return Lookup(rentedBuffer[..bytesWritten]);
                    }
                    finally
                    {
                        global::System.Buffers.ArrayPool<byte>.Shared.Return(rented);
                    }

                    

                    static {{props.Definition.DeclarationName}} Lookup(
                        scoped global::System.ReadOnlySpan<byte> utf8)
                    {
                        if ({{EnumClassDeclarationTemplate.ValuesBySerializedUtf8NameSpanLookupVariableName}}.TryGetValue(
                            utf8,
                            out {{props.Definition.DeclarationName}}? value))
                        {
                            return value;
                        }

                        throw new global::System.Text.Json.JsonException(
                            "Unknown enum value.");
                    }
                }
                """;
        }



        static LookupImplementation GetLookupImplementationForUtf8Keys(EnumValueDefinition[] enumValues, EnumClassProps props)
            => LookupImplementationSelector.Get(props, enumValues.Length, LookupImplementationSelector.Target.Utf8Keys);
    }
}
