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
                    /// <summary>
                    /// Flag controlling presence of `require` keyword for `EnumIndex` property.
                    /// </summary>
                    public bool RequireIndexAssignmentInInitializer { get; set; } = true;
                }
            }
            """;



        public static string BuildEnumClassDeclaration(
            EnumClass.Definition props)
        {
            var enumValues = props.EnumValues!.Value.Definitions.Where(x => x.IsValid).Select((x, index) => new
            {
                DefaultIndex = index,
                Def = x
            }).ToArray();

            bool hasAnyCustomTypesForEnumValues = enumValues.Any(x => x.Def.FullyQualifiedCustomType is not null);
            var enumValuesPerCustomTypes = enumValues.GroupBy(x => x.Def.FullyQualifiedCustomType!).Where(x => x.Key is not null);

            bool isNumberedByUser = props.OurAttributeType is EnumClass.OurAttributeType.NumberedEnumClass;

            var valuesCount = enumValues.Length;

            return $$"""
            #nullable enable
            namespace {{props.NamespaceName}}
            {
                {{(props.Config.GenerateJsonConverter ? $"[System.Text.Json.Serialization.JsonConverter(typeof({props.DeclarationName}JsonConverter))]" : "")}}
                {{props.Modifier}} partial class {{props.DeclarationName}} : System.IEquatable<{{props.DeclarationName}}>
                {
                    static {{props.DeclarationName}}()
                    {
                        {{(!isNumberedByUser
                            ? string.Join(_newLine, enumValues.Select(enumValue => $"{enumValue.Def.Name}.EnumIndex = {enumValue.DefaultIndex};"))
                            : "")}}

                        {{string.Join(_newLine, enumValues.Select(enumValue => $"{enumValue.Def.Name}._serializedName = nameof({enumValue.Def.Name});"))}}
                        
                        _allValues = [
                            {{string.Join(_commaNewLine, enumValues.Select(enumValue => enumValue.Def.Name))}}
                        ];
            
            
                        {{(props.Config.UseDictionaryForDeserialization
                            ? $"_valuesBySerializedName = _allValues.ToFrozenDictionary(x => x._serializedName);"
                            : "")}}

                        {{(isNumberedByUser
                            ? "_usedIndexes = null;"
                            : "")}}
                    }


                    public static System.Collections.Generic.IReadOnlyList<{{props.DeclarationName}}> AllValues => _allValues;
            
                    {{(!isNumberedByUser ? """
                        public int EnumIndex { get; private set; }
                        """
                        : props.Config.RequireIndexAssignmentInInitializer ? """
                            private int _enumIndex;
                            public required int EnumIndex
                            {
                                get => _enumIndex;
                                init
                                {
                                    _enumIndex = value;
                                    EnsureEnumIndexIsFree(this, value);
                                }
                            }
                            """
                            : """
                            private int _enumIndex;
                            public int EnumIndex
                            {
                                get => _enumIndex;
                                init
                                {
                                    _enumIndex = value;
                                    EnsureEnumIndexIsFree(this, value);
                                }
                            }
                            """)}}
            
                    public string Serialize() => _serializedName;
                    public static string Serialize({{props.DeclarationName}} value) => value.Serialize();

                    {{(!props.Config.UseDictionaryForDeserialization
                        ? $$"""
                        public static {{props.DeclarationName}}? Deserialize(string? serializedValue)
                        {
                            {{String.Join(_newLine, enumValues.Select(enumValue => $"if (serializedValue == {enumValue.Def.Name}._serializedName) return {enumValue.Def.Name};"))}}
                            return null;
                        }
                        """
                        : $$"""
                        public static {{props.DeclarationName}}? Deserialize(string? serializedValue)
                        {
                            if (serializedValue is null)
                                return null;
                            return (_valuesBySerializedName?.TryGetValue(serializedValue, out {{props.DeclarationName}} value) ?? false)
                                ? value
                                : null;
                        }
                        """)}}

                    public static bool TryDeserialize(string? serializedValue, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.DeclarationName}}? result)
                    {
                        result = Deserialize(serializedValue);
                        return result is not null;
                    }

                    public static bool ContainsSerializedValue(string? serializedValue)
                        => Deserialize(serializedValue) is not null;


                    public static {{props.DeclarationName}}? GetByEnumIndex(int index)
                    {
                        {{(!isNumberedByUser
                            ? $$"""
                            return index switch
                            {
                                {{String.Join(_newLine, enumValues.Select(enumValue => $"{enumValue.DefaultIndex} => {enumValue.Def.Name},"))}}
                                _ => null
                            };
                            """
                            : $$"""
                                {{String.Join(_newLine, enumValues.Select(enumValue => $"if (index == {enumValue.Def.Name}.EnumIndex) return {enumValue.Def.Name};"))}}
                                return null;
                                """)}}
                    }
            
                    public static bool TryGetByEnumIndex(int index, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.DeclarationName}}? result)
                    {
                        result = GetByEnumIndex(index);
                        return result is not null;
                    }

                    public static bool ContainsEnumIndex(int index)
                        => GetByEnumIndex(index) is not null;


                    {{(hasAnyCustomTypesForEnumValues ? $$"""
                        public static bool TryGetOfType<TValue>({{props.DeclarationName}} value, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TValue? typeMatchingValue)
                            where TValue : {{props.DeclarationName}}
                        {
                            var checkedType = typeof(TValue);

                            {{String.Join(_newLine,
                                enumValuesPerCustomTypes.Select(enumValuesPerType => $$"""

                                if (checkedType == typeof({{enumValuesPerType.Key}})
                                    && ({{String.Join("\r\n|| ",
                                        enumValuesPerType.Select(enumValue => $$"""
                                            AreEqual(value, {{enumValue.Def.Name}})
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



                    public void Switch(
                        {{String.Join(_commaNewLine, enumValues.Select(x => $"System.Action? on{x.Def.Name} = null"))}})
                    {
                        {{String.Join(_newLine,
                            enumValues.Select(x => $$"""
                            if (AreEqual(this, {{x.Def.Name}}))
                            {
                                on{{x.Def.Name}}?.Invoke();
                                return;
                            }
                            """))}}
                    }

                    public TResult? Switch<TResult>(
                        {{String.Join(_commaNewLine, enumValues.Select(x => $"System.Func<TResult>? on{x.Def.Name} = null"))}})
                        where TResult : class
                    {
                        {{String.Join(_newLine,
                            enumValues.Select(x => $$"""
                            if (AreEqual(this, {{x.Def.Name}}))
                            {
                                return on{{x.Def.Name}}?.Invoke();
                            }
                            """))}}
                        return null;
                    }

            
            
                    private static System.Collections.Immutable.ImmutableArray<{{props.DeclarationName}}> _allValues;
                    {{(props.Config.UseDictionaryForDeserialization
                        ? $"private static System.Collections.Frozen.FrozenDictionary<string, {props.DeclarationName}>? _valuesBySerializedName;"
                        : "")}}
                    private string _serializedName = null!;


                            

                    {{(isNumberedByUser ? $$"""
                        private static System.Collections.Generic.List<int>? _usedIndexes;

                        private static void EnsureEnumIndexIsFree({{props.DeclarationName}} value, int index)
                        {
                            if (_usedIndexes is null)
                            {
                                _usedIndexes = [index];
                                return;
                            }

                            if (_usedIndexes.Contains(index))
                            {
                                throw new System.ArgumentException($"EnumIndex of {index} can not be used more than once.");
                            }

                            _usedIndexes.Add(index);
                        }
                        """ : "")}}

                            
                    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    private static bool AreEqual({{props.DeclarationName}} one, {{props.DeclarationName}} two)
                    {
                        return object.ReferenceEquals(one, two);
                    }
                            
                    public bool Equals({{props.DeclarationName}}? other)
                    {
                        if (other is null)
                            return false;

                        return AreEqual(this, other);
                    }
                            
                    public override bool Equals(object? obj)
                    {
                        return Equals(obj as {{props.DeclarationName}});
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


        private const string _newLine = "\r\n";
        private const string _commaNewLine = ",\r\n";
    }
}
