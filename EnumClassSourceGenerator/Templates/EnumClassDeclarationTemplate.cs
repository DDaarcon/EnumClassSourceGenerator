using EnumClassSourceGenerator.Schema;
using System;
using System.Linq;
using System.Net;

namespace EnumClassSourceGenerator.Templates;

internal class EnumClassDeclarationTemplate
{
    public static bool CheckIfNameIsReserved(string name)
    {
        if (_defaultKeywords.Contains(name))
            return true;
        return false;
    }

    private static readonly string[] _defaultKeywords = [
        "AllValues",
        "EnumIndex",
        "Serialize",
        "Deserialize",
        "TryDeserialize",
        "ContainsSerializedValue",
        "GetByEnumIndex",
        "TryGetByEnumIndex",
        "ContainsEnumIndex",
        "Switch",
        "AreEqual",
        "Equals",
        "GetHashCode",
        "TryGetOfType",
        "_enumIndex",
        "_allValues",
        "_valuesBySerializedName",
        "_serializedName",

        // Numbered
        "_usedIndexes",
        "EnsureEnumIndexIsFree",

        // Raw values
        "_rawValue",
        "Raw",
        "FromRaw",
        "TryFromRaw",
        "ToRaw",
        "TryToRaw",
        "value__" // compiler reserved enum value
    ];

    public static string Build(
        EnumClass.Definition props)
    {
        var enumValues = props.EnumValues!.Value.Definitions
            .Where(x => x.IsValid)
            .Select((x, index) => new IndexedEnumValue(
                DefaultIndex: index,
                Def: x))
            .ToArray();

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
                        ? string.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.Def.Name}.EnumIndex = {enumValue.DefaultIndex};"))
                        : "")}}

                        {{string.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.Def.Name}._serializedName = nameof({enumValue.Def.Name});"))}}

                        {{(props.Config.GenerateRawEnum
                        ? string.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.Def.Name}._rawValue = Raw.{enumValue.Def.Name};"))
                        : "")}}
                        
                        _allValues = [
                            {{string.Join(Consts.CommaNl, enumValues.Select(enumValue => enumValue.Def.Name))}}
                        ];
            
            
                        {{(props.Config.UseDictionaryForDeserialization
                        ? $"_valuesBySerializedName = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(_allValues, x => x._serializedName);"
                        : "")}}

                        {{(isNumberedByUser
                        ? "_usedIndexes = null;"
                        : "")}}
                    }

            
                    private static System.Collections.Immutable.ImmutableArray<{{props.DeclarationName}}> _allValues;
                    public static System.Collections.Generic.IReadOnlyList<{{props.DeclarationName}}> AllValues => _allValues;
            

                    {{BuildEnumIndexing(enumValues, props)}}
            
                    {{BuildSerialization(enumValues, props)}}

                    {{BuildRawEnumWhenApplicable(enumValues, props)}}

                    {{BuildTypeMatchingWhenApplicable(enumValues, props)}}

                    {{BuildValueMatching(enumValues)}}
                            
                    {{BuildNumberingHelpersWhenApplicable(props)}}

                            
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

        static string BuildEnumIndexing(IndexedEnumValue[] enumValues, EnumClass.Definition props)
        {
            return $$"""
                {{BuildProperty(props)}}

                {{BuildAccessorMethod(enumValues, props)}}
                
                public static bool TryGetByEnumIndex(int index, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.DeclarationName}}? result)
                {
                    result = GetByEnumIndex(index);
                    return result is not null;
                }
                
                public static bool ContainsEnumIndex(int index)
                    => GetByEnumIndex(index) is not null;
                """;

            static string BuildProperty(EnumClass.Definition props)
            {
                if (!CheckIfNumberedByUser(props))
                    return """
                        public int EnumIndex { get; private set; }
                        """;

                if (props.Config.RequireIndexAssignmentInInitializer)
                    return """
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
                        """;

                return """
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
                    """;
            }

            static string BuildAccessorMethod(IndexedEnumValue[] enumValues, EnumClass.Definition props)
            {
                return $$"""
                    public static {{props.DeclarationName}}? GetByEnumIndex(int index)
                    {
                        {{BuildBody(enumValues, props)}}
                    }
                    """;

                static string BuildBody(IndexedEnumValue[] enumValues, EnumClass.Definition props)
                {
                    if (CheckIfNumberedByUser(props))
                        return $$"""
                            {{String.Join(Consts.Nl, enumValues.Select(enumValue => $"if (index == {enumValue.Def.Name}.EnumIndex) return {enumValue.Def.Name};"))}}
                            return null;
                            """;

                    return $$"""
                        return index switch
                        {
                            {{String.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.DefaultIndex} => {enumValue.Def.Name},"))}}
                            _ => null
                        };
                        """;
                }
            }
        }

        static string BuildRawEnumWhenApplicable(IndexedEnumValue[] enumValues, EnumClass.Definition props)
        {
            if (!props.Config.GenerateRawEnum)
                return "";

            return $$"""
                private Raw? _rawValue;

                public enum Raw
                {
                    {{String.Join(Consts.CommaNl, enumValues.Select(enumValue => enumValue.Def.Name))}}
                }

                public static {{props.DeclarationName}} FromRaw(Raw value)
                {
                    if (TryFromRaw(value, out var result))
                        return result;

                    throw new System.ArgumentOutOfRangeException(nameof(value), value, "The value is not a defined raw enum value.");
                }

                public static bool TryFromRaw(Raw value, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.DeclarationName}}? result)
                {
                    result = value switch
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(enumValue => $"Raw.{enumValue.Def.Name} => {enumValue.Def.Name},"))}}
                        _ => null
                    };

                    return result is not null;
                }

                public Raw ToRaw() => ToRaw(this);

                public static Raw ToRaw({{props.DeclarationName}} value)
                {
                    if (TryToRaw(value, out var result))
                        return result;

                    if (value is null)
                        throw new System.ArgumentNullException(nameof(value));

                    throw new System.ArgumentOutOfRangeException(nameof(value), value, "The value is not a declared enum class value.");
                }

                public static bool TryToRaw({{props.DeclarationName}}? value, out Raw result)
                {
                    if (value?._rawValue is Raw rawValue)
                    {
                        result = rawValue;
                        return true;
                    }

                    result = default;
                    return false;
                }

                public static implicit operator {{props.DeclarationName}}(Raw value) => FromRaw(value);
                public static explicit operator Raw({{props.DeclarationName}} value) => ToRaw(value);
                """;
        }

        static string BuildValueMatching(IndexedEnumValue[] enumValues)
        {
            return $$"""
                public void Switch(
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Action? on{x.Def.NormalizedName} = null"))}})
                {
                    {{String.Join(Consts.Nl,
                        enumValues.Select(x => $$"""
                            if (AreEqual(this, {{x.Def.Name}}))
                            {
                                on{{x.Def.NormalizedName}}?.Invoke();
                                return;
                            }
                            """))}}
                }
                
                public TResult? Switch<TResult>(
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Func<TResult>? on{x.Def.NormalizedName} = null"))}})
                    where TResult : class
                {
                    {{String.Join(Consts.Nl,
                        enumValues.Select(x => $$"""
                            if (AreEqual(this, {{x.Def.Name}}))
                            {
                                return on{{x.Def.NormalizedName}}?.Invoke();
                            }
                            """))}}
                    return null;
                }
                """;
        }

        static string BuildTypeMatchingWhenApplicable(IndexedEnumValue[] enumValues, EnumClass.Definition props)
        {
            bool hasAnyCustomTypesForEnumValues = enumValues.Any(x => x.Def.FullyQualifiedCustomType is not null);
            var enumValuesPerCustomTypes = enumValues.GroupBy(x => x.Def.FullyQualifiedCustomType!).Where(x => x.Key is not null);

            if (!hasAnyCustomTypesForEnumValues)
                return "";

            return $$"""
                public static bool TryGetOfType<TValue>({{props.DeclarationName}} value, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TValue? typeMatchingValue)
                    where TValue : {{props.DeclarationName}}
                {
                    var checkedType = typeof(TValue);

                    {{String.Join(Consts.Nl,
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
                """;
        }

        static string BuildNumberingHelpersWhenApplicable(EnumClass.Definition props)
        {
            if (!CheckIfNumberedByUser(props))
                return "";

            return $$"""
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
                """;
        }

        static string BuildSerialization(IndexedEnumValue[] enumValues, EnumClass.Definition props)
        {
            return $$"""
                private string _serializedName = null!;
                {{(props.Config.UseDictionaryForDeserialization
                    ? $"private static System.Collections.Frozen.FrozenDictionary<string, {props.DeclarationName}>? _valuesBySerializedName;"
                    : "")}}

                public string Serialize() => _serializedName;
                public static string Serialize({{props.DeclarationName}} value) => value.Serialize();
                
                {{BuildDeserializers(enumValues, props)}}
                
                public static bool TryDeserialize(string? serializedValue, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.DeclarationName}}? result)
                {
                    result = Deserialize(serializedValue);
                    return result is not null;
                }
                
                public static bool ContainsSerializedValue(string? serializedValue)
                    => Deserialize(serializedValue) is not null;
                """;

            static string BuildDeserializers(IndexedEnumValue[] enumValues, EnumClass.Definition props)
            {
                if (props.Config.UseDictionaryForDeserialization)
                    return $$"""
                        public static {{props.DeclarationName}}? Deserialize(string? serializedValue)
                        {
                            if (serializedValue is null)
                                return null;
                            return (_valuesBySerializedName?.TryGetValue(serializedValue, out {{props.DeclarationName}} value) ?? false)
                                ? value
                                : null;
                        }
                        """;

                return $$"""
                    public static {{props.DeclarationName}}? Deserialize(string? serializedValue)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(enumValue => $"if (serializedValue == {enumValue.Def.Name}._serializedName) return {enumValue.Def.Name};"))}}
                        return null;
                    }
                    """;
            }

        }

        static bool CheckIfNumberedByUser(EnumClass.Definition props)
            => props.OurAttributeType is EnumClass.OurAttributeType.NumberedEnumClass;
    }

    private record struct IndexedEnumValue(
        int DefaultIndex,
        EnumValue.Definition Def);
}
