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

                    /// <summary>
                    /// Flag changing if-based matching into one based on a cached dictionary.
                    /// </summary>
                    public bool UseDictionaryForIndexMatching { get; set; } = false;
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
                    public static IReadOnlyCollection<{{props.DeclarationName}}> AllValues => _allValues ?? [];
            
                    {{(!isNumbered ? """
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
                            return (_valuesBySerializedName?.TryGetValue(serializedValue, out {{props.DeclarationName}} value) ?? false)
                                ? value
                                : null;
                        }
                        """)}}


                    public static {{props.DeclarationName}}? GetByEnumIndex(int index)
                    {
                        {{(!isNumbered
                            ? $$"""
                            return index switch
                            {
                                {{String.Join(_nl, enumValues.Select((enumValue, index) => $"{index} => {enumValue.Name},"))}}
                                _ => null
                            };
                            """
                            : props.Config.UseDictionaryForIndexMatching
                                ? $$"""
                                return (_valuesByIndex?.TryGetValue(index, out {{props.DeclarationName}} value) ?? false)
                                    ? value
                                    : null;
                                """
                                : $$"""
                                {{String.Join(_nl, enumValues.Select(enumValue => $"if (index == {enumValue.Name}.EnumIndex) return {enumValue.Name};"))}}
                                return null;
                                """)}}
                    }


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
                        using (var lockScope = _InitializationLock.EnterScope())
                        {
                            _allValues ??= new({{valuesCount}});

                            {{(!isNumbered
                                ? "EnumIndex = _enumIndexCounter++;"
                                : "")}}
            
                            _allValues.Add(this);

                            {{(props.Config.UseDictionaryForDeserialization
                                ? $$"""
                                _valuesBySerializedName ??= new({{valuesCount}});
                                _valuesBySerializedName.Value[GetSerializedName({{props.DeclarationName}})] = this;
                                """
                                : "")}}
                            {{(props.Config.UseDictionaryForIndexMatching
                                ? $$"""
                                _valuesByIndex ??= new({{valuesCount}});
                                """
                                : "")}}
                        }
                    }
            
            
                    private static HashSet<{{props.DeclarationName}}>? _allValues;
                    {{(props.Config.UseDictionaryForDeserialization
                        ? $"private static Dictionary<string, {props.DeclarationName}>? _valuesBySerializedName;"
                        : "")}}
                    {{(props.Config.UseDictionaryForIndexMatching
                        ? $"private static Dictionary<int, {props.DeclarationName}>? _valuesByIndex;"
                        : "")}}
                    {{(!isNumbered
                        ? "private static int _enumIndexCounter = 0;"
                        : "")}}
                    private static System.Threading.Lock _InitializationLock => LazyInitializer.EnsureInitialized(ref _initializationLockBackingField);
                    private static System.Threading.Lock? _initializationLockBackingField;

                    private string _SerializedName => _serializedNameBackingField ??= GetSerializedName(this);
                    private string? _serializedNameBackingField = null;
            
                    private string GetSerializedName({{props.DeclarationName}} value)
                    {
                        {{String.Join(_nl, enumValues.Select(enumValue => $"if (value == {enumValue.Name}) return nameof({enumValue.Name});"))}}
                        throw new Exception("Attempted to serialize invalid value of {{props.DeclarationName}}");
                    }



                            
                    private static void EnsureEnumIndexIsFree({{props.DeclarationName}} value, int index)
                    {
                        using var lockScope = _InitializationLock.EnterScope();

                        var conflictingValue = _allValues!.FirstOrDefault(x => x.EnumIndex == index);

                        if (conflictingValue is not null
                            && !AreEqual(value, conflictingValue))
                        {
                            throw new System.ArgumentException($"EnumIndex of {index} can not be used more than once.");
                        }
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



                    public static IdMapper GetIdMapper()
                        {{(props.Config.UseDictionaryForIndexMatching
                            ? "=> new IdMapper(_valuesByIndex);"
                            : "=> new IdMapper(AllValues.ToDictionary(x => x.EnumIndex));")}}

                    public class IdMapper(Dictionary<int, {{props.DeclarationName}}> mapping)
                    {
                        private readonly Dictionary<int, {{props.DeclarationName}}> _mapping = mapping;

                        public {{props.DeclarationName}}? Get(int index)
                            => _mapping.TryGetValue(index, out var res)
                                ? res : null;

                        public bool TryGet(int index, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.DeclarationName}}? result)
                            => _mapping.TryGetValue(index, out result);

                        public bool Contains(int index)
                            => _mapping.ContainsKey(index);
                    }


                    public static SerializedMapper GetSerializedMapper()
                        {{(props.Config.UseDictionaryForDeserialization
                            ? "=> new SerializedMapper(_valuesBySerializedName);"
                            : "=> new SerializedMapper(AllValues.ToDictionary(x => x._SerializedName));")}}

                    public class SerializedMapper(Dictionary<string, {{props.DeclarationName}}> mapping)
                    {
                        private readonly Dictionary<string, {{props.DeclarationName}}> _mapping = mapping;

                        public {{props.DeclarationName}}? Get(string serialized)
                            => _mapping.TryGetValue(serialized, out var res)
                                ? res : null;
            
                        public bool TryGet(string serialized, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.DeclarationName}}? result)
                            => _mapping.TryGetValue(serialized, out result);
            
                        public bool Contains(string serialized)
                            => _mapping.ContainsKey(serialized);
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
