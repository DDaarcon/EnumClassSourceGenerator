using EnumClassSourceGenerator.Schema;
using System;
using System.Linq;

namespace EnumClassSourceGenerator
{
    internal static class Templates
    {
        public readonly static string EnumClassAttribute = """
            namespace GenEnumClass
            {
                public abstract class BaseEnumClassAttribute : System.Attribute
                {
                    /// <summary>
                    /// Flag enabling generation of a custom <see cref="System.Text.Json.Serialization.JsonConverter{T}"/> for the enum class. Defaults to <c>true</c>.
                    /// </summary>
                    public bool GenerateJsonConverter { get; set; } = true;

                    /// <summary>
                    /// Flag changing if-based matching into one based on a cached dictionary. 
                    /// </summary>
                    public bool UseDictionaryForDeserialization { get; set; } = false;
                }

                            
                /// <summary>
                /// Defines a Enum Class. <br />
                /// Enumerable values should be defined as either fields with `public static readonly` modifiers or properties with `public static` modifiers and only a getter (no setter).
                /// Enumerable values can be of a containing type type or one inheriting from it.
                /// </summary>
                public sealed class EnumClassAttribute : BaseEnumClassAttribute
                {
                }
                            
                /// <summary>
                /// Defines a Numbered Enum Class. <br />
                /// Enumerable values should be defined as either fields with `public static readonly` modifiers or properties with `public static` modifiers and only a getter (no setter).
                /// Enumerable values can be of a containing type type or one inheriting from it.<br />
                /// Numbered Enum Values has to have a value provided for `EnumIndex` property.
                /// </summary>
                public sealed class NumberedEnumClassAttribute : BaseEnumClassAttribute
                {
                }
            }
            """;


        public static string BuildEnumClassDeclaration(
            EnumClass.Definition props)
        {
            var enumValues = props.EnumValues!.Value.Definitions.Where(x => x.IsValid).ToArray();

            bool hasAnyCustomTypesForEnumValues = enumValues.Any(x => x.FullyQualifiedCustomType is not null);
            var enumValuesPerCustomTypes = enumValues.GroupBy(x => x.FullyQualifiedCustomType!).Where(x => x.Key is not null);

            bool isNumbered = props.OurAttributeType is EnumClass.OurAttributeType.NumberedEnumClass;

            var valuesCount = enumValues.Length;

            return $$"""
            #nullable enable
            namespace {{props.NamespaceName}}
            {
                {{(props.Config.GenerateJsonConverter ? $"[System.Text.Json.Serialization.JsonConverter(typeof({props.DeclarationName}JsonConverter))]" : "")}}
                {{props.Modifier}} partial class {{props.DeclarationName}}
                {
                    public static IReadOnlyCollection<{{props.DeclarationName}}> AllValues => _allValues!;
            
                    {{(!isNumbered ? """
                        public int EnumIndex { get; private set; }
                        """
                        : """
                        public required int EnumIndex { get; init; }
                        """)}}
            
                    public string Serialize() => _SerializedName;
                    public static string Serialize({{props.DeclarationName}} value) => value.Serialize();

                    {{(!props.Config.UseDictionaryForDeserialization
                        ? $$"""
                        public static {{props.DeclarationName}}? Deserialize(string? serializedValue)
                        {
                            {{String.Join(_nl, enumValues.Select(enumValue => $"if (serializedValue == {enumValue.Name}._SerializedName) return {enumValue.Name};"))}}
                            return null;
                        }
                        """
                        : $$"""
                        public static {{props.DeclarationName}}? Deserialize(string? serializedValue)
                        {
                            if (serializedValue is null)
                                return null;
                            return _valuesBySerializedName.TryGetValue(serializedValue, out {{props.DeclarationName}} value)
                                ? value
                                : null;
                        }
                        """)}}


                    {{(hasAnyCustomTypesForEnumValues ? $$"""
                        public static bool TryGetOfType<TValue>({{props.DeclarationName}} value, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TValue? typeMatchingValue)
                            where TValue : {{props.DeclarationName}}
                        {
                            var checkedType = typeof(TValue);

                            {{String.Join(_nl,
                                enumValuesPerCustomTypes.Select(enumValuesPerType => $$"""

                                if (checkedType == typeof({{enumValuesPerType.Key}})
                                    && ({{String.Join("\r\n|| ",
                                        enumValuesPerType.Select(enumValue => $$"""
                                            AreEqual(value, {{enumValue.Name}})
                                            """))}}))
                                {
                                    typeMatchingValue = (TValue)(object)value;
                                    return true;
                                }
                                """))}}
                    
                            typeMatchingValue = null;
                            return false;
                        }
                        """ : "")}}
            
                    public static {{props.DeclarationName}}? GetByEnumIndex(int index)
                    {
                        return index switch
                        {
                            {{String.Join(_nl, enumValues.Select((enumValue, index) => $"{index} => {enumValue.Name},"))}}
                            _ => null
                        };
                    }


                    public void Switch(
                        {{String.Join(",\r\n", enumValues.Select(x => $"System.Action? on{x.Name} = null"))}})
                    {
                        {{String.Join("\r\n",
                            enumValues.Select(x => $$"""
                            if (AreEqual(this, {{x.Name}}))
                            {
                                on{{x.Name}}?.Invoke();
                                return;
                            }
                            """))}}
                    }

                    public TResult? Switch<TResult>(
                        {{String.Join(",\r\n", enumValues.Select(x => $"System.Func<TResult>? on{x.Name} = null"))}})
                        where TResult : class
                    {
                        {{String.Join("\r\n",
                            enumValues.Select(x => $$"""
                            if (AreEqual(this, {{x.Name}}))
                            {
                                return on{{x.Name}}?.Invoke();
                            }
                            """))}}
                        return null;
                    }


            
                    private {{props.DeclarationName}}() 
                    {
                        using (var lockScope = _initializationLock.EnterScope())
                        {
                            {{(!isNumbered
                                ? "EnumIndex = _enumIndexCounter++;"
                                : $$"""
                                var conflictingValue = _allValues.FirstOrDefault(x => x.EnumIndex == EnumIndex);

                                if (conflictingValue is not null)
                                {
                                    var otherName = conflictingValue.Serialize();
                                    var thisName = Serialize();

                                    throw new System.ArgumentException($"EnumIndex of {EnumIndex} can not be shared between '{otherName}' and '{thisName}'.");
                                }
                                """)}}
            
                            _allValues.Add(this);

                            {{(props.Config.UseDictionaryForDeserialization
                                ? $$"""
                                _valuesBySerializedName[GetSerializedName({{props.DeclarationName}})] = this;
                                """
                                : "")}}
                        }
                    }
            
            
                    private static HashSet<{{props.DeclarationName}}> _allValues = new({{valuesCount}});
                    {{(props.Config.UseDictionaryForDeserialization
                        ? $"private static Dictionary<string, {props.DeclarationName}> _valuesBySerializedName = new({valuesCount});"
                        : "")}}
                    {{(!isNumbered
                        ? "private static int _enumIndexCounter = 0;"
                        : "")}}
                    private static System.Threading.Lock _initializationLock = new();

                    private string _SerializedName => _serializedNameBackingField ??= GetSerializedName(this);
                    private string? _serializedNameBackingField = null;
            
                    private string GetSerializedName({{props.DeclarationName}} value)
                    {
                        {{String.Join(_nl, enumValues.Select(enumValue => $"if (value == {enumValue.Name}) return nameof({enumValue.Name});"))}}
                        throw new Exception("Attempted to serialize invalid value of {{props.DeclarationName}}");
                    }



                    private static bool AreEqual({{props.DeclarationName}} one, {{props.DeclarationName}} two)
                    {
                        return object.ReferenceEquals(one, two);
                    }
                            
                    public override bool Equals(object? obj)
                    {
                        return ReferenceEquals(this, obj);
                    }
                    public override int GetHashCode()
                    {
                        return EnumIndex.GetHashCode();
                    }
                }
            }
            #nullable disable
            """;
        }

        public static string BuildEnumClassSerializationConvertedDefinition(EnumClass.Definition props)
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


        private const string _nl = "\r\n";
    }
}
