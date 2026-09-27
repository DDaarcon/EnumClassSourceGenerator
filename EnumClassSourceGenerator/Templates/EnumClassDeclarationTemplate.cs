using EnumClasses.SourceGenerators.Deserialization;
using EnumClasses.SourceGenerators.Schema;
using System;
using System.Linq;

namespace EnumClasses.SourceGenerators.Templates;

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
        "SwitchEx",
        "Match",
        "MatchEx",
        "AreEqual",
        "Equals",
        "GetHashCode",
        "TryGetOfType",
        "_enumIndex",
        "_allValues",
        "_valuesBySerializedNameHash",
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
        EnumClassCollector.Definition.WithConfig props)
    {
        var definition = props.Definition;

        var enumValues = definition.EnumValues!.Value.Definitions
            .Where(x => x.IsValid)
            .Select((x, index) => new IndexedEnumValue(
                DefaultIndex: index,
                Def: x))
            .ToArray();

        bool hasAnyCustomTypesForEnumValues = enumValues.Any(x => x.Def.FullyQualifiedCustomType is not null);
        var enumValuesPerCustomTypes = enumValues.GroupBy(x => x.Def.FullyQualifiedCustomType!).Where(x => x.Key is not null);

        var valuesCount = enumValues.Length;

        return $$"""
            #nullable enable
            namespace {{definition.NamespaceName}}
            {
                {{(props.Configuration.GenerateJsonConverter ? $"[System.Text.Json.Serialization.JsonConverter(typeof({definition.DeclarationName}JsonConverter))]" : "")}}
                {{definition.Modifier}} partial class {{definition.DeclarationName}} : System.IEquatable<{{definition.DeclarationName}}>
                {
                    {{BuildConstructor(props)}}

                    static {{definition.DeclarationName}}()
                    {
                        {{(!CheckIfNumberedByUser(definition)
                        ? string.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.Def.Name}.EnumIndex = {enumValue.DefaultIndex};"))
                        : "")}}

                        {{string.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.Def.Name}._internalIndex = {enumValue.DefaultIndex};"))}}

                        {{string.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.Def.Name}._serializedName = nameof({enumValue.Def.NormalizedName});"))}}

                        {{(props.Configuration.GenerateRawEnum
                        ? string.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.Def.Name}._rawValue = Raw.{enumValue.Def.Name};"))
                        : "")}}
                        
                        _allValues = [
                            {{string.Join(Consts.CommaNl, enumValues.Select(enumValue => enumValue.Def.Name))}}
                        ];
            
            
                        {{(GetDeserializationMethod(enumValues, props) is SearchMethod.Dictionary
                        ? $"_valuesBySerializedNameHash = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(_allValues, x => string.GetHashCode(x._serializedName));"
                        : "")}}

                        {{(CheckIfNumberedByUser(definition)
                        ? "_usedIndexes = null;"
                        : "")}}
                    }

            
                    private static System.Collections.Immutable.ImmutableArray<{{definition.DeclarationName}}> _allValues;
                    public static System.Collections.Generic.IReadOnlyList<{{definition.DeclarationName}}> AllValues => _allValues;

                    private int _internalIndex;
            

                    {{BuildEnumIndexing(enumValues, props)}}
            
                    {{BuildSerialization(enumValues, props)}}

                    {{BuildRawEnumWhenApplicable(enumValues, props)}}

                    {{BuildTypeMatchingWhenApplicable(enumValues, definition)}}

                    {{BuildValueMatching(enumValues, definition)}}
                            
                    {{BuildNumberingHelpersWhenApplicable(definition)}}

                            
                    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    private static bool AreEqual({{definition.DeclarationName}} one, {{definition.DeclarationName}} two)
                    {
                        return object.ReferenceEquals(one, two);
                    }
                            
                    public bool Equals({{definition.DeclarationName}}? other)
                    {
                        if (other is null)
                            return false;

                        return AreEqual(this, other);
                    }
                            
                    public override bool Equals(object? obj)
                    {
                        return Equals(obj as {{definition.DeclarationName}});
                    }
                    public override int GetHashCode()
                    {
                        return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
                    }

                }
            }
            #nullable disable
            """;

        static string BuildConstructor(EnumClassCollector.Definition.WithConfig props)
        {
            if (props.Configuration.ConstructionRestrictionMode is ConstructionRestrictionMode.Off
                || props.Definition.HasExplicitInstanceConstructor)
            {
                return "";
            }

            var modifier = props.Configuration.ConstructionRestrictionMode switch
            {
                ConstructionRestrictionMode.WithPrivateDefaultConstructor => "private",
                ConstructionRestrictionMode.WithProtectedDefaultConstructor or _ => "protected"
            };

            return $$"""
                {{modifier}} {{props.Definition.DeclarationName}}() { }
                """;
        }


        static string BuildEnumIndexing(IndexedEnumValue[] enumValues, EnumClassCollector.Definition.WithConfig props)
        {
            return $$"""
                {{BuildProperty(props)}}

                {{BuildAccessorMethod(enumValues, props.Definition)}}
                
                public static bool TryGetByEnumIndex(int index, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.Definition.DeclarationName}}? result)
                {
                    result = GetByEnumIndex(index);
                    return result is not null;
                }
                
                public static bool ContainsEnumIndex(int index)
                    => GetByEnumIndex(index) is not null;
                """;

            static string BuildProperty(EnumClassCollector.Definition.WithConfig props)
            {
                if (!CheckIfNumberedByUser(props.Definition))
                    return """
                        public int EnumIndex { get; private set; }
                        """;

                if (props.Configuration.RequireIndexAssignmentInInitializer)
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

            static string BuildAccessorMethod(IndexedEnumValue[] enumValues, EnumClassCollector.Definition definition)
            {
                return $$"""
                    public static {{definition.DeclarationName}}? GetByEnumIndex(int index)
                    {
                        {{BuildBody(enumValues, definition)}}
                    }
                    """;

                static string BuildBody(IndexedEnumValue[] enumValues, EnumClassCollector.Definition definition)
                {
                    if (CheckIfNumberedByUser(definition))
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

        static string BuildRawEnumWhenApplicable(IndexedEnumValue[] enumValues, EnumClassCollector.Definition.WithConfig props)
        {
            if (!props.Configuration.GenerateRawEnum)
                return "";

            return $$"""
                private Raw? _rawValue;

                public enum Raw
                {
                    {{String.Join(Consts.CommaNl, enumValues.Select(enumValue => enumValue.Def.Name))}}
                }

                public static {{props.Definition.DeclarationName}} FromRaw(Raw value)
                {
                    if (TryFromRaw(value, out var result))
                        return result;

                    throw new System.ArgumentOutOfRangeException(nameof(value), value, "The value is not a defined raw enum value.");
                }

                public static bool TryFromRaw(Raw value, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.Definition.DeclarationName}}? result)
                {
                    result = value switch
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(enumValue => $"Raw.{enumValue.Def.Name} => {enumValue.Def.Name},"))}}
                        _ => null
                    };

                    return result is not null;
                }

                public Raw ToRaw() => ToRaw(this);

                public static Raw ToRaw({{props.Definition.DeclarationName}} value)
                {
                    if (TryToRaw(value, out var result))
                        return result;

                    if (value is null)
                        throw new System.ArgumentNullException(nameof(value));

                    throw new System.ArgumentOutOfRangeException(nameof(value), value, "The value is not a declared enum class value.");
                }

                public static bool TryToRaw({{props.Definition.DeclarationName}}? value, out Raw result)
                {
                    if (value?._rawValue is Raw rawValue)
                    {
                        result = rawValue;
                        return true;
                    }

                    result = default;
                    return false;
                }

                public static implicit operator {{props.Definition.DeclarationName}}(Raw value) => FromRaw(value);
                public static explicit operator Raw({{props.Definition.DeclarationName}} value) => ToRaw(value);
                """;
        }

        static string BuildValueMatching(IndexedEnumValue[] enumValues, EnumClassCollector.Definition definition)
        {
            return $$"""
                /// <summary>
                /// Exhaustive switch. Executes a callback matching the value.
                /// </summary>
                public void SwitchEx(
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Action on{x.Def.NormalizedName}"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.DefaultIndex}} when AreEqual(this, {{x.Def.Name}}):
                                on{{x.Def.NormalizedName}}();
                                return;
                            """))}}
                        default:
                            throw new System.InvalidOperationException("Invalid value of {{definition.DeclarationName}}.");
                    }
                }

                /// <summary>
                /// Executes a callback matching the value.
                /// </summary>
                public void Switch(
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Action? on{x.Def.NormalizedName} = null"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.DefaultIndex}} when AreEqual(this, {{x.Def.Name}}) && on{{x.Def.NormalizedName}} != null:
                                on{{x.Def.NormalizedName}}();
                                return;
                            """))}}
                    }
                }

                /// <summary>
                /// Exhaustive switch. Executes a callback matching the value.
                /// </summary>
                public void SwitchEx<TState>(
                    TState state,
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Action<TState> on{x.Def.NormalizedName}"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.DefaultIndex}} when AreEqual(this, {{x.Def.Name}}):
                                on{{x.Def.NormalizedName}}(state);
                                return;
                            """))}}
                        default:
                            throw new System.InvalidOperationException("Invalid value of {{definition.DeclarationName}}.");
                    }
                }

                /// <summary>
                /// Executes a callback matching the value.
                /// </summary>
                public void Switch<TState>(
                    TState state,
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Action<TState>? on{x.Def.NormalizedName} = null"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.DefaultIndex}} when AreEqual(this, {{x.Def.Name}}) && on{{x.Def.NormalizedName}} != null:
                                on{{x.Def.NormalizedName}}(state);
                                return;
                            """))}}
                    }
                }




                /// <summary>
                /// Exhaustive match. Executes a callback matching the value and returns the result.
                /// </summary>
                public TResult MatchEx<TResult>(
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Func<TResult> on{x.Def.NormalizedName}"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.DefaultIndex}} when AreEqual(this, {{x.Def.Name}}):
                                return on{{x.Def.NormalizedName}}();
                            """))}}
                        default:
                            throw new System.InvalidOperationException("Invalid value of {{definition.DeclarationName}}.");
                    }
                }

                /// <summary>
                /// Executes a callback matching the value and returns the result.
                /// </summary>
                public TResult? Match<TResult>(
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Func<TResult>? on{x.Def.NormalizedName} = null"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.DefaultIndex}} when AreEqual(this, {{x.Def.Name}}) && on{{x.Def.NormalizedName}} != null:
                                return on{{x.Def.NormalizedName}}();
                            """))}}
                    }
                    return default;
                }


                /// <summary>
                /// Exhaustive match. Executes a callback matching the value and returns the result.
                /// </summary>
                public TResult MatchEx<TResult, TState>(
                    TState state,
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Func<TState, TResult> on{x.Def.NormalizedName}"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.DefaultIndex}} when AreEqual(this, {{x.Def.Name}}):
                                return on{{x.Def.NormalizedName}}(state);
                            """))}}
                        default:
                            throw new System.InvalidOperationException("Invalid value of {{definition.DeclarationName}}.");
                    }
                }

                /// <summary>
                /// Executes a callback matching the value and returns the result.
                /// </summary>
                public TResult? Match<TResult, TState>(
                    TState state,
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Func<TState, TResult>? on{x.Def.NormalizedName} = null"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.DefaultIndex}} when AreEqual(this, {{x.Def.Name}}) && on{{x.Def.NormalizedName}} != null:
                                return on{{x.Def.NormalizedName}}(state);
                            """))}}
                    }
                    return default;
                }
                """;
        }

        static string BuildTypeMatchingWhenApplicable(IndexedEnumValue[] enumValues, EnumClassCollector.Definition definition)
        {
            bool hasAnyCustomTypesForEnumValues = enumValues.Any(x => x.Def.FullyQualifiedCustomType is not null);
            var enumValuesPerCustomTypes = enumValues.GroupBy(x => x.Def.FullyQualifiedCustomType!).Where(x => x.Key is not null);

            if (!hasAnyCustomTypesForEnumValues)
                return "";

            return $$"""
                public static bool TryGetOfType<TValue>({{definition.DeclarationName}} value, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TValue? typeMatchingValue)
                    where TValue : {{definition.DeclarationName}}
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

        static string BuildNumberingHelpersWhenApplicable(EnumClassCollector.Definition definition)
        {
            if (!CheckIfNumberedByUser(definition))
                return "";

            return $$"""
                private static System.Collections.Generic.List<int>? _usedIndexes;

                private static void EnsureEnumIndexIsFree({{definition.DeclarationName}} value, int index)
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

        static string BuildSerialization(IndexedEnumValue[] enumValues, EnumClassCollector.Definition.WithConfig definition)
        {
            return $$"""
                private string _serializedName = null!;

                {{(GetDeserializationMethod(enumValues, definition) is SearchMethod.Dictionary
                    ? $"private static System.Collections.Frozen.FrozenDictionary<int, {definition.Definition.DeclarationName}>? _valuesBySerializedNameHash;"
                    : "")}}

                public string Serialize() => _serializedName;
                public static string Serialize({{definition.Definition.DeclarationName}} value) => value.Serialize();
                
                {{BuildDeserializers(enumValues, definition)}}
                
                public static bool TryDeserialize(string? serializedValue, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{definition.Definition.DeclarationName}}? result)
                {
                    result = Deserialize(serializedValue);
                    return result is not null;
                }
                
                public static bool TryDeserialize(System.ReadOnlySpan<char> serializedValue, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{definition.Definition.DeclarationName}}? result)
                {
                    result = Deserialize(serializedValue);
                    return result is not null;
                }
                
                public static bool ContainsSerializedValue(string? serializedValue)
                    => Deserialize(serializedValue) is not null;
                
                public static bool ContainsSerializedValue(System.ReadOnlySpan<char> serializedValue)
                    => Deserialize(serializedValue) is not null;
                """;

            static string BuildDeserializers(IndexedEnumValue[] enumValues, EnumClassCollector.Definition.WithConfig definition)
            {
                if (GetDeserializationMethod(enumValues, definition) is SearchMethod.Dictionary)
                    return $$"""
                        public static {{definition.Definition.DeclarationName}}? Deserialize(string? serializedValue)
                        {
                            if (serializedValue is null)
                                return null;

                            return Deserialize(System.MemoryExtensions.AsSpan(serializedValue));
                        }

                        public static {{definition.Definition.DeclarationName}}? Deserialize(System.ReadOnlySpan<char> serializedValue)
                        {
                            return (_valuesBySerializedNameHash?.TryGetValue(string.GetHashCode(serializedValue), out {{definition.Definition.DeclarationName}} value) ?? false)
                                ? value
                                : null;
                        }
                        """;

                return $$"""
                    public static {{definition.Definition.DeclarationName}}? Deserialize(string? serializedValue)
                    {
                        if (serializedValue is null)
                            return null;

                        return Deserialize(System.MemoryExtensions.AsSpan(serializedValue));
                    }

                    public static {{definition.Definition.DeclarationName}}? Deserialize(System.ReadOnlySpan<char> serializedValue)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(enumValue => $"if (serializedValue.Equals(System.MemoryExtensions.AsSpan({enumValue.Def.Name}._serializedName), System.StringComparison.InvariantCulture)) return {enumValue.Def.Name};"))}}
                        return null;
                    }
                    """;
            }

        }

        static bool CheckIfNumberedByUser(EnumClassCollector.Definition props)
            => props.OurAttributeType is EnumClassCollector.OurAttributeType.NumberedEnumClass;

        static SearchMethod GetDeserializationMethod(IndexedEnumValue[] enumValues, EnumClassCollector.Definition.WithConfig props)
            => SearchMethodProvider.Get(props.Configuration, enumValues.Length);
    }

    private record struct IndexedEnumValue(
        int DefaultIndex,
        EnumValueCollector.Definition Def);
}
