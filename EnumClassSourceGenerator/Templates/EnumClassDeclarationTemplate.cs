using EnumClasses.SourceGenerators.Deserialization;
using EnumClasses.SourceGenerators.Schema;
using System;
using System.Linq;
using EnumClassProps = EnumClasses.SourceGenerators.Schema.EnumClassCollector.Definition.WithConfig;
using EnumValueDefinition = EnumClasses.SourceGenerators.Schema.EnumValueCollector.Definition;

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
        "_valuesBySerializedName",
        "_valuesBySerializedNameSpanLookup",
        "_serializedName",
        "_internalIndex",

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
        EnumClassProps props)
    {
        var definition = props.Definition;

        var enumValues = definition.EnumValues.Definitions
            .Where(x => x.IsValid)
            .ToArray();

        bool hasAnyCustomTypesForEnumValues = enumValues.Any(x => x.FullyQualifiedCustomType is not null);
        var enumValuesPerCustomTypes = enumValues.GroupBy(x => x.FullyQualifiedCustomType!).Where(x => x.Key is not null);

        var valuesCount = enumValues.Length;

        return $$"""
            #nullable enable
            namespace {{definition.NamespaceName}}
            {
                {{(props.Configuration.GenerateJsonConverter ? $"[System.Text.Json.Serialization.JsonConverter(typeof({definition.DeclarationName}.JsonConverter))]" : "")}}
                {{definition.Modifier}} partial class {{definition.DeclarationName}} : System.IEquatable<{{definition.DeclarationName}}>
                {
                    {{BuildConstructor(props)}}

                    {{BuildStaticConstructor(enumValues, props)}}

            
                    private static System.Collections.Immutable.ImmutableArray<{{definition.DeclarationName}}> _allValues;
                    public static System.Collections.Immutable.ImmutableArray<{{definition.DeclarationName}}> AllValues => _allValues;

                    private int _internalIndex;
            

                    {{BuildEnumIndexing(enumValues, props)}}
            
                    {{BuildSerialization(enumValues, props)}}

                    {{BuildRawEnumWhenApplicable(enumValues, props)}}

                    {{BuildTypeMatchingWhenApplicable(enumValues, definition)}}

                    {{BuildValueMatching(enumValues, definition)}}
                            
                    {{BuildNumberingHelpersWhenApplicable(definition)}}

                    {{BuildUtf8EncodedResourcesWhenApplicable(enumValues, props)}}
                            
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

        static string BuildConstructor(EnumClassProps props)
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


        static string BuildStaticConstructor(EnumValueDefinition[] enumValues, EnumClassProps props)
        {
            return $$"""
                static {{props.Definition.DeclarationName}}()
                {
                    {{(!CheckIfNumberedByUser(props.Definition)
                        ? string.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.Name}.EnumIndex = {enumValue.InternalIndex};"))
                        : "")}}
                
                    {{string.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.Name}._internalIndex = {enumValue.InternalIndex};"))}}
                
                    {{string.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.Name}._serializedName = nameof({enumValue.Name});"))}}
                
                    {{(props.Configuration.GenerateRawEnum
                        ? string.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.Name}._rawValue = Raw.{enumValue.Name};"))
                        : "")}}
                            
                    _allValues = [
                        {{string.Join(Consts.CommaNl, enumValues.Select(enumValue => enumValue.Name))}}
                    ];
                
                
                    {{BuildSerializationDictionaryAssignmentWhenApplicable(enumValues, props)}}

                    {{BuildUtf8EncodedResourcesDictionaryAssignmentWhenApplicable(enumValues, props)}}
                
                    {{(CheckIfNumberedByUser(props.Definition)
                        ? "_usedIndexes = null;"
                        : "")}}
                }
                """;

            static string BuildSerializationDictionaryAssignmentWhenApplicable(EnumValueDefinition[] enumValues, EnumClassProps props)
            {
                if (GetLookupImplementationForStringKeys(enumValues, props) is not LookupImplementation.Dictionary)
                    return "";

                var dictAssignment = $"""
                    _valuesBySerializedName = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(_allValues, x => x._serializedName, System.StringComparer.Ordinal);
                    """;

                if (props.Definition.Meta.IsAlternateLookupSupported)
                    return dictAssignment + $$"""

                        _valuesBySerializedNameSpanLookup = _valuesBySerializedName.GetAlternateLookup<System.ReadOnlySpan<char>>();
                        """;

                return dictAssignment;
            }

            static string BuildUtf8EncodedResourcesDictionaryAssignmentWhenApplicable(EnumValueDefinition[] enumValues, EnumClassProps props)
            {
                if (!CheckIfShouldIncludeUtf8EncodedResources(enumValues, props))
                    return "";

                return """
                    _valuesBySerializedUtf8Name = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(
                        _allValues,
                        x => (ReadOnlyMemory<byte>) System.MemoryExtensions.AsMemory(System.Text.Encoding.UTF8.GetBytes(x._serializedName)),
                        Utf8MemoryComparer.Instance);

                    _valuesBySerializedUtf8NameSpanLookup = _valuesBySerializedUtf8Name.GetAlternateLookup<System.ReadOnlySpan<byte>>();
                    """;
            }
        }

        static string BuildEnumIndexing(EnumValueDefinition[] enumValues, EnumClassProps props)
        {
            return $$"""
                {{BuildProperty(props)}}

                {{BuildAccessorMethod(enumValues, props)}}
                
                public static bool TryGetByEnumIndex(int index, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.Definition.DeclarationName}}? result)
                {
                    result = GetByEnumIndex(index);
                    return result is not null;
                }
                
                public static bool ContainsEnumIndex(int index)
                    => GetByEnumIndex(index) is not null;
                """;

            static string BuildProperty(EnumClassProps props)
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

            static string BuildAccessorMethod(EnumValueDefinition[] enumValues, EnumClassProps props)
            {
                return $$"""
                    public static {{props.Definition.DeclarationName}}? GetByEnumIndex(int index)
                    {
                        {{BuildBody(enumValues, props)}}
                    }
                    """;

                static string BuildBody(EnumValueDefinition[] enumValues, EnumClassProps props)
                {
                    if (CheckIfNumberedByUser(props.Definition))
                    {
                        // TODO to be considered dict based accessing, but likely only when EnumIndex has a required keyword - to make sure values are present at launch
                        // TODO + consider specifing "index assignment restrictions", meaning - defining during class declaration how much compile-time ready can we expect the indexes to be and basing on that generating the most efficient lookup mechanizm
                        return $$"""
                            {{String.Join(Consts.Nl, enumValues.Select(enumValue => $"if (index == {enumValue.Name}.EnumIndex) return {enumValue.Name};"))}}
                            return null;
                            """;
                    }

                    return $$"""
                        return index switch
                        {
                            {{String.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.InternalIndex} => {enumValue.Name},"))}}
                            _ => null
                        };
                        """;
                }
            }
        }

        static string BuildRawEnumWhenApplicable(EnumValueDefinition[] enumValues, EnumClassProps props)
        {
            if (!props.Configuration.GenerateRawEnum)
                return "";

            return $$"""
                private Raw? _rawValue;

                public enum Raw
                {
                    {{String.Join(Consts.CommaNl, enumValues.Select(enumValue => enumValue.Name))}}
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
                        {{String.Join(Consts.Nl, enumValues.Select(enumValue => $"Raw.{enumValue.Name} => {enumValue.Name},"))}}
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

        static string BuildValueMatching(EnumValueDefinition[] enumValues, EnumClassCollector.Definition definition)
        {
            return $$"""
                /// <summary>
                /// Exhaustive switch. Executes a callback matching the value.
                /// </summary>
                public void SwitchEx(
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Action on{x.NormalizedName}"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.InternalIndex}} when AreEqual(this, {{x.Name}}):
                                on{{x.NormalizedName}}();
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
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Action? on{x.NormalizedName} = null"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.InternalIndex}} when AreEqual(this, {{x.Name}}) && on{{x.NormalizedName}} != null:
                                on{{x.NormalizedName}}();
                                return;
                            """))}}
                    }
                }

                /// <summary>
                /// Exhaustive switch. Executes a callback matching the value.
                /// </summary>
                public void SwitchEx<TState>(
                    TState state,
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Action<TState> on{x.NormalizedName}"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.InternalIndex}} when AreEqual(this, {{x.Name}}):
                                on{{x.NormalizedName}}(state);
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
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Action<TState>? on{x.NormalizedName} = null"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.InternalIndex}} when AreEqual(this, {{x.Name}}) && on{{x.NormalizedName}} != null:
                                on{{x.NormalizedName}}(state);
                                return;
                            """))}}
                    }
                }




                /// <summary>
                /// Exhaustive match. Executes a callback matching the value and returns the result.
                /// </summary>
                public TResult MatchEx<TResult>(
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Func<TResult> on{x.NormalizedName}"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.InternalIndex}} when AreEqual(this, {{x.Name}}):
                                return on{{x.NormalizedName}}();
                            """))}}
                        default:
                            throw new System.InvalidOperationException("Invalid value of {{definition.DeclarationName}}.");
                    }
                }

                /// <summary>
                /// Executes a callback matching the value and returns the result.
                /// </summary>
                public TResult? Match<TResult>(
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Func<TResult>? on{x.NormalizedName} = null"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.InternalIndex}} when AreEqual(this, {{x.Name}}) && on{{x.NormalizedName}} != null:
                                return on{{x.NormalizedName}}();
                            """))}}
                    }
                    return default;
                }


                /// <summary>
                /// Exhaustive match. Executes a callback matching the value and returns the result.
                /// </summary>
                public TResult MatchEx<TResult, TState>(
                    TState state,
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Func<TState, TResult> on{x.NormalizedName}"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.InternalIndex}} when AreEqual(this, {{x.Name}}):
                                return on{{x.NormalizedName}}(state);
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
                    {{String.Join(Consts.CommaNl, enumValues.Select(x => $"System.Func<TState, TResult>? on{x.NormalizedName} = null"))}})
                {
                    switch (_internalIndex)
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(x => $$"""
                            case {{x.InternalIndex}} when AreEqual(this, {{x.Name}}) && on{{x.NormalizedName}} != null:
                                return on{{x.NormalizedName}}(state);
                            """))}}
                    }
                    return default;
                }
                """;
        }

        static string BuildTypeMatchingWhenApplicable(EnumValueDefinition[] enumValues, EnumClassCollector.Definition definition)
        {
            bool hasAnyCustomTypesForEnumValues = enumValues.Any(x => x.FullyQualifiedCustomType is not null);
            var enumValuesPerCustomTypes = enumValues.GroupBy(x => x.FullyQualifiedCustomType!).Where(x => x.Key is not null);

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

        static string BuildSerialization(EnumValueDefinition[] enumValues, EnumClassProps props)
        {
            return $$"""
                private string _serializedName = null!;

                {{BuildDictionaryDeclaration(enumValues, props)}}

                public string Serialize() => _serializedName;
                public static string Serialize({{props.Definition.DeclarationName}} value) => value.Serialize();

                public override string ToString() => Serialize();
                
                {{BuildDeserializers(enumValues, props)}}
                
                public static bool TryDeserialize(string? serializedValue, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.Definition.DeclarationName}}? result)
                {
                    result = Deserialize(serializedValue);
                    return result is not null;
                }
                
                public static bool TryDeserialize(System.ReadOnlySpan<char> serializedValue, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.Definition.DeclarationName}}? result)
                {
                    result = Deserialize(serializedValue);
                    return result is not null;
                }
                
                public static bool ContainsSerializedValue(string? serializedValue)
                    => Deserialize(serializedValue) is not null;
                
                public static bool ContainsSerializedValue(System.ReadOnlySpan<char> serializedValue)
                    => Deserialize(serializedValue) is not null;
                """;

            static string BuildDictionaryDeclaration(EnumValueDefinition[] enumValues, EnumClassProps props)
            {
                if (GetLookupImplementationForStringKeys(enumValues, props) is not LookupImplementation.Dictionary)
                    return "";

                var dictAssignment = $"""
                    private static System.Collections.Frozen.FrozenDictionary<string, {props.Definition.DeclarationName}>? _valuesBySerializedName;
                    """;

                if (props.Definition.Meta.IsAlternateLookupSupported)
                    return dictAssignment + $$"""

                        private static System.Collections.Frozen.FrozenDictionary<string, {{props.Definition.DeclarationName}}>.AlternateLookup<System.ReadOnlySpan<char>> _valuesBySerializedNameSpanLookup;
                        """;

                return dictAssignment;
            }

            static string BuildDeserializers(EnumValueDefinition[] enumValues, EnumClassProps props)
            {
                if (GetLookupImplementationForStringKeys(enumValues, props) is LookupImplementation.Dictionary)
                    return BuildDictionaryBasedDeserialization(enumValues, props);

                return BuildIfChainBasedDeserialization(enumValues, props);


                static string BuildDictionaryBasedDeserialization(EnumValueDefinition[] enumValues, EnumClassProps props)
                {
                    return $$"""
                        public static {{props.Definition.DeclarationName}}? Deserialize(string? serializedValue)
                        {
                            if (serializedValue is null)
                                return null;

                            return (_valuesBySerializedName?.TryGetValue(serializedValue, out {{props.Definition.DeclarationName}}? value) ?? false)
                                ? value
                                : null;
                        }

                        public static {{props.Definition.DeclarationName}}? Deserialize(System.ReadOnlySpan<char> serializedValue)
                        {
                            {{BuildSpanDeserializationLogic(props)}}
                        }
                        """;

                    static string BuildSpanDeserializationLogic(EnumClassProps props)
                    {
                        if (props.Definition.Meta.IsAlternateLookupSupported)
                        {
                            return $$"""
                                return _valuesBySerializedNameSpanLookup.TryGetValue(serializedValue, out {{props.Definition.DeclarationName}}? value)
                                    ? value
                                    : null;
                                """;
                        }

                        return $$"""
                            return (_valuesBySerializedName?.TryGetValue(serializedValue.ToString(), out {{props.Definition.DeclarationName}}? value) ?? false)
                                ? value
                                : null;
                            """;
                    }
                }

                static string BuildIfChainBasedDeserialization(EnumValueDefinition[] enumValues, EnumClassProps props)
                {
                    return $$"""
                        public static {{props.Definition.DeclarationName}}? Deserialize(string? serializedValue)
                        {
                            if (serializedValue is null)
                                return null;

                            return Deserialize(System.MemoryExtensions.AsSpan(serializedValue));
                        }

                        public static {{props.Definition.DeclarationName}}? Deserialize(System.ReadOnlySpan<char> serializedValue)
                        {
                            {{String.Join(Consts.Nl, enumValues.Select(enumValue
                                => $"if (System.MemoryExtensions.Equals(serializedValue, System.MemoryExtensions.AsSpan({enumValue.Name}._serializedName), System.StringComparison.Ordinal)) return {enumValue.Name};"))}}
                            return null;
                        }
                        """;
                }
            }

        }


        static bool CheckIfShouldIncludeUtf8EncodedResources(EnumValueDefinition[] enumValues, EnumClassProps props)
            => GetLookupImplementationForUtf8Keys(enumValues, props) is LookupImplementation.Dictionary
                && props.Configuration.GenerateJsonConverter
                && props.Definition.Meta.IsAlternateLookupSupported;

        static string BuildUtf8EncodedResourcesWhenApplicable(EnumValueDefinition[] enumValues, EnumClassProps props)
        {
            if (!CheckIfShouldIncludeUtf8EncodedResources(enumValues, props))
                return "";

            return $$"""
                private static System.Collections.Frozen.FrozenDictionary<System.ReadOnlyMemory<byte>, {{props.Definition.DeclarationName}}>? _valuesBySerializedUtf8Name;
                private static System.Collections.Frozen.FrozenDictionary<System.ReadOnlyMemory<byte>, {{props.Definition.DeclarationName}}>.AlternateLookup<System.ReadOnlySpan<byte>> {{ValuesBySerializedUtf8NameSpanLookupVariableName}};


                {{BuildComparerDefinition()}}

                """;



            static string BuildComparerDefinition()
            {
                return """
                    private sealed class Utf8MemoryComparer :
                        System.Collections.Generic.IEqualityComparer<System.ReadOnlyMemory<byte>>,
                        System.Collections.Generic.IAlternateEqualityComparer<System.ReadOnlySpan<byte>, System.ReadOnlyMemory<byte>>
                    {
                        public static Utf8MemoryComparer Instance { get; } = new();

                        public bool Equals(
                            System.ReadOnlyMemory<byte> x,
                            System.ReadOnlyMemory<byte> y)
                            => x.Span.SequenceEqual(y.Span);

                        public int GetHashCode(System.ReadOnlyMemory<byte> value)
                            => GetHashCode(value.Span);

                        public bool Equals(
                            System.ReadOnlySpan<byte> alternate,
                            System.ReadOnlyMemory<byte> stored)
                            => alternate.SequenceEqual(stored.Span);

                        public int GetHashCode(System.ReadOnlySpan<byte> value)
                        {
                            var hash = new System.HashCode();
                            hash.AddBytes(value);
                            return hash.ToHashCode();
                        }

                        public System.ReadOnlyMemory<byte> Create(System.ReadOnlySpan<byte> value)
                            => value.ToArray();
                    }
                    """;
            }
        }


        static bool CheckIfNumberedByUser(EnumClassCollector.Definition definition)
            => definition.OurAttributeType is EnumClassCollector.OurAttributeType.NumberedEnumClass;

        static LookupImplementation GetLookupImplementationForStringKeys(EnumValueDefinition[] enumValues, EnumClassProps props)
            => LookupImplementationSelector.Get(props, enumValues.Length, LookupImplementationSelector.Target.StringKeys);

        static LookupImplementation GetLookupImplementationForUtf8Keys(EnumValueDefinition[] enumValues, EnumClassProps props)
            => LookupImplementationSelector.Get(props, enumValues.Length, LookupImplementationSelector.Target.Utf8Keys);
    }


    public const string ValuesBySerializedUtf8NameSpanLookupVariableName = "_valuesBySerializedUtf8NameSpanLookup";
}
