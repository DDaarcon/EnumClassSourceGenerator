using EnumClasses.SourceGenerators.Schema;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnumClasses.SourceGenerators.Templates;

internal class EnumClassSerializationConverterTemplate
{
    public static string Build(EnumClassCollector.Definition.WithConfig props)
    {
        var definition = props.Definition;

        return $$"""
            #nullable enable
            
            namespace {{definition.NamespaceName}}
            {
                internal class {{definition.DeclarationName}}JsonConverter : System.Text.Json.Serialization.JsonConverter<{{definition.DeclarationName}}>
                {
                    public override {{definition.DeclarationName}}? Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
                        => {{definition.DeclarationName}}.Deserialize(reader.GetString());
            
                    public override void Write(System.Text.Json.Utf8JsonWriter writer, {{definition.DeclarationName}} value, System.Text.Json.JsonSerializerOptions options)
                        => writer.WriteStringValue(value.Serialize());
                }
            }
            
            #nullable disable
            """;
    }
}
