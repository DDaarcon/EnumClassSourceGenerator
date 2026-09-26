using EnumClassSourceGenerator.Schema;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnumClassSourceGenerator.Templates;

internal class EnumClassSerializationConverterTemplate
{
    public static string Build(EnumClass.Definition props)
        => $$"""
            #nullable enable
            
            namespace {{props.NamespaceName}}
            {
                internal class {{props.DeclarationName}}JsonConverter : System.Text.Json.Serialization.JsonConverter<{{props.DeclarationName}}>
                {
                    public override {{props.DeclarationName}}? Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
                        => {{props.DeclarationName}}.Deserialize(reader.GetString());
            
                    public override void Write(System.Text.Json.Utf8JsonWriter writer, {{props.DeclarationName}} value, System.Text.Json.JsonSerializerOptions options)
                        => writer.WriteStringValue(value.Serialize());
                }
            }
            
            #nullable disable
            """;
}
