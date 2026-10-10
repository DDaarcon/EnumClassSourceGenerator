using EnumClasses.SourceGenerators.ExternalSourcesHelpers;
using EnumClasses.SourceGenerators.FeatureSupport;
using EnumClasses.SourceGenerators.GenerationStrategies;
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

                {{BuildRemarksWhenAppliable(enumValues, props)}}
                {{(props.Configuration.GenerateJsonConverter ? $"[System.Text.Json.Serialization.JsonConverter(typeof({definition.DeclarationName}.JsonConverter))]" : "")}}
                {{definition.Modifier}} partial class {{definition.DeclarationName}} : System.IEquatable<{{definition.DeclarationName}}>
                {
                    {{BuildConstructor(props)}}

                    {{BuildStaticConstructor(enumValues, props)}}

            
                    private static System.Collections.Immutable.ImmutableArray<{{definition.DeclarationName}}> _allValues;

                    /// <summary>
                    /// Gets all declared values in source declaration order.
                    /// </summary>
                    public static System.Collections.Immutable.ImmutableArray<{{definition.DeclarationName}}> AllValues => _allValues;

                    private int _internalIndex;
            

                    {{BuildEnumIndexing(enumValues, props)}}
            
                    {{BuildSerialization(enumValues, props)}}

                    {{BuildRawEnumWhenApplicable(enumValues, props)}}

                    {{BuildTypeMatchingWhenApplicable(enumValues, definition)}}

                    {{BuildValueMatching(enumValues, definition)}}
                            
                    {{BuildRuntimeIndexingHelpersWhenApplicable(enumValues, props)}}

                    {{BuildUtf8EncodedResourcesWhenApplicable(enumValues, props)}}
                            
                    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    private static bool AreEqual({{definition.DeclarationName}} one, {{definition.DeclarationName}} two)
                    {
                        return object.ReferenceEquals(one, two);
                    }

                    /// <summary>
                    /// Determines whether this instance and another value are the same object.
                    /// </summary>
                    /// <param name="other">The value to compare with this instance.</param>
                    /// <returns><see langword="true"/> when both references identify the same object; otherwise, <see langword="false"/>.</returns>
                    public bool Equals({{definition.DeclarationName}}? other)
                    {
                        if (other is null)
                            return false;

                        return AreEqual(this, other);
                    }
                            
                    /// <inheritdoc/>
                    public override bool Equals(object? obj)
                    {
                        return Equals(obj as {{definition.DeclarationName}});
                    }
                    /// <inheritdoc/>
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
                    {{(props.Configuration.IndexAssignmentMode is IndexAssignmentMode.CompileTime
                        ? string.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.Name}.EnumIndex = {enumValue.ResolvedIndex};"))
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

                    {{BuildRuntimeIndexAssignmentValidationWhenApplicable(enumValues, props)}}
                }
                """;

            static string BuildSerializationDictionaryAssignmentWhenApplicable(EnumValueDefinition[] enumValues, EnumClassProps props)
            {
                if (GetLookupImplementationForStringKeys(enumValues, props) is not LookupImplementation.Dictionary)
                    return "";

                var dictAssignment = $"""
                    _valuesBySerializedName = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(_allValues, x => x._serializedName, System.StringComparer.Ordinal);
                    """;

                if (props.Definition.FeatureSupport.AlternateLookup.IsSupported)
                    return dictAssignment + $$"""

                        _valuesBySerializedNameSpanLookup = _valuesBySerializedName.GetAlternateLookup<System.ReadOnlySpan<char>>();
                        """;

                return dictAssignment;
            }

            static string BuildUtf8EncodedResourcesDictionaryAssignmentWhenApplicable(EnumValueDefinition[] enumValues, EnumClassProps props)
            {
                if (!CheckIfShouldIncludeUtf8EncodedResources(enumValues, props))
                    return "";

                return $$"""
                    _valuesBySerializedUtf8Name = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(
                        _allValues,
                        x => (ReadOnlyMemory<byte>) System.MemoryExtensions.AsMemory(System.Text.Encoding.UTF8.GetBytes(x._serializedName)),
                        Utf8MemoryComparer.Instance);

                    {{ValuesBySerializedUtf8NameSpanLookupVariableName}} = _valuesBySerializedUtf8Name.GetAlternateLookup<System.ReadOnlySpan<byte>>();
                    """;
            }

            static string BuildRuntimeIndexAssignmentValidationWhenApplicable(EnumValueDefinition[] enumValues, EnumClassProps props)
            {
                if (!props.Configuration.IndexAssignmentMode.IsRuntime)
                    return "";

                return GetLookupImplementationForIntKeys(enumValues, props) is LookupImplementation.Dictionary
                    ? "_valuesByEnumIndex = CreateRuntimeIndexDictionary();"
                    : "ValidateRuntimeIndexes();";
            }
        }

        static string BuildEnumIndexing(EnumValueDefinition[] enumValues, EnumClassProps props)
        {
            return $$"""
                {{BuildProperty(props)}}

                {{BuildAccessorMethod(enumValues, props)}}
                
                /// <summary>
                /// Tries to get the declared value with the specified enum index.
                /// </summary>
                /// <param name="index">The enum index to find.</param>
                /// <param name="result">The matching declared value, or <see langword="null"/> when no value has the index.</param>
                /// <returns><see langword="true"/> when a declared value has the specified index; otherwise, <see langword="false"/>.</returns>
                public static bool TryGetByEnumIndex(int index, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.Definition.DeclarationName}}? result)
                {
                    result = GetByEnumIndex(index);
                    return result is not null;
                }
                
                /// <summary>
                /// Determines whether a declared value has the specified enum index.
                /// </summary>
                /// <param name="index">The enum index to find.</param>
                /// <returns><see langword="true"/> when a declared value has the specified index; otherwise, <see langword="false"/>.</returns>
                public static bool ContainsEnumIndex(int index)
                    => GetByEnumIndex(index) is not null;
                """;

            static string BuildProperty(EnumClassProps props)
            {
                if (props.Configuration.IndexAssignmentMode is IndexAssignmentMode.CompileTime)
                    // TODO think of a way for restricting value change after creation
                    return """
                        /// <summary>
                        /// Gets the zero-based index assigned from source declaration order.
                        /// </summary>
                        public int EnumIndex { get; private set; }
                        """;

                if (props.Configuration.IndexAssignmentMode is IndexAssignmentMode.RuntimeAtValueInit)
                    return """
                        private int _enumIndex;
                        private bool _enumIndexAssigned;

                        public required int EnumIndex
                        {
                            get => _enumIndex;
                            init
                            {
                                _enumIndex = value;
                                _enumIndexAssigned = true;
                            }
                        }
                        """;

                // IndexAssignmentMode.RuntimeAtValueCreation
                return """
                    private int _enumIndex;
                    private bool _enumIndexAssigned;

                    public int EnumIndex
                    {
                        get => _enumIndex;
                        init
                        {
                            _enumIndex = value;
                            _enumIndexAssigned = true;
                        }
                    }
                    """;
            }

            static string BuildAccessorMethod(EnumValueDefinition[] enumValues, EnumClassProps props)
            {
                return $$"""
                    /// <summary>
                    /// Gets the declared value with the specified enum index.
                    /// </summary>
                    /// <param name="index">The enum index to find.</param>
                    /// <returns>The matching declared value, or <see langword="null"/> when no value has the index.</returns>
                    public static {{props.Definition.DeclarationName}}? GetByEnumIndex(int index)
                    {
                        {{BuildBody(enumValues, props)}}
                    }
                    """;

                static string BuildBody(EnumValueDefinition[] enumValues, EnumClassProps props)
                {
                    if (props.Configuration.IndexAssignmentMode.IsRuntime)
                    {
                        if (GetLookupImplementationForIntKeys(enumValues, props) is LookupImplementation.Dictionary)
                        {
                            return $$"""
                                return (_valuesByEnumIndex?.TryGetValue(index, out {{props.Definition.DeclarationName}}? value) ?? false)
                                    ? value
                                    : null;
                                """;
                        }

                        return $$"""
                            {{String.Join(Consts.Nl, enumValues.Select(enumValue => $"if (index == {enumValue.Name}.EnumIndex) return {enumValue.Name};"))}}
                            return null;
                            """;
                    }

                    return $$"""
                        return index switch
                        {
                            {{String.Join(Consts.Nl, enumValues.Select(enumValue => $"{enumValue.ResolvedIndex} => {enumValue.Name},"))}}
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

                /// <summary>
                /// Represents the declared enum-class values as a conventional enum.
                /// </summary>
                /// <remarks>Members receive zero-based numeric values in source declaration order.</remarks>
                public enum Raw
                {
                    {{BuildRawEnumDefinition(enumValues, props)}}
                }

                /// <summary>
                /// Converts a raw enum value to its declared enum-class value.
                /// </summary>
                /// <param name="value">The raw value to convert.</param>
                /// <returns>The corresponding declared enum-class value.</returns>
                /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is not defined.</exception>
                public static {{props.Definition.DeclarationName}} FromRaw(Raw value)
                {
                    if (TryFromRaw(value, out var result))
                        return result;

                    throw new System.ArgumentOutOfRangeException(nameof(value), value, "The value is not a defined raw enum value.");
                }

                /// <summary>
                /// Tries to convert a raw enum value to its declared enum-class value.
                /// </summary>
                /// <param name="value">The raw value to convert.</param>
                /// <param name="result">The corresponding declared value, or <see langword="null"/> when the raw value is not defined.</param>
                /// <returns><see langword="true"/> when <paramref name="value"/> is defined; otherwise, <see langword="false"/>.</returns>
                public static bool TryFromRaw(Raw value, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.Definition.DeclarationName}}? result)
                {
                    result = value switch
                    {
                        {{String.Join(Consts.Nl, enumValues.Select(enumValue => $"Raw.{enumValue.Name} => {enumValue.Name},"))}}
                        _ => null
                    };

                    return result is not null;
                }

                /// <summary>
                /// Converts this enum-class value to its raw enum value.
                /// </summary>
                /// <returns>The corresponding raw enum value.</returns>
                /// <exception cref="System.ArgumentOutOfRangeException">This instance is not a declared enum-class value.</exception>
                public Raw ToRaw() => ToRaw(this);

                /// <summary>
                /// Converts an enum-class value to its raw enum value.
                /// </summary>
                /// <param name="value">The enum-class value to convert.</param>
                /// <returns>The corresponding raw enum value.</returns>
                /// <exception cref="System.ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
                /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is not a declared enum-class value.</exception>
                public static Raw ToRaw({{props.Definition.DeclarationName}} value)
                {
                    if (TryToRaw(value, out var result))
                        return result;

                    if (value is null)
                        throw new System.ArgumentNullException(nameof(value));

                    throw new System.ArgumentOutOfRangeException(nameof(value), value, "The value is not a declared enum class value.");
                }

                /// <summary>
                /// Tries to convert an enum-class value to its raw enum value.
                /// </summary>
                /// <param name="value">The enum-class value to convert.</param>
                /// <param name="result">The corresponding raw value when conversion succeeds; otherwise, the default raw value.</param>
                /// <returns>
                /// <see langword="true"/> when <paramref name="value"/> is a declared enum-class value;
                /// otherwise, <see langword="false"/>.
                /// </returns>
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

                /// <summary>Converts a raw enum value to its declared enum-class value.</summary>
                /// <param name="value">The raw value to convert.</param>
                /// <returns>The corresponding declared enum-class value.</returns>
                /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is not defined.</exception>
                public static implicit operator {{props.Definition.DeclarationName}}(Raw value) => FromRaw(value);

                /// <summary>Converts an enum-class value to its raw enum value.</summary>
                /// <param name="value">The enum-class value to convert.</param>
                /// <returns>The corresponding raw enum value.</returns>
                /// <exception cref="System.ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
                /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is not a declared enum-class value.</exception>
                public static explicit operator Raw({{props.Definition.DeclarationName}} value) => ToRaw(value);
                """;

            static string BuildRawEnumDefinition(EnumValueDefinition[] enumValues, EnumClassProps props)
            {
                if (props.Configuration.IndexAssignmentMode.IsRuntime)
                    return String.Join(Consts.CommaNl, enumValues.Select(enumValue => enumValue.Name));

                return String.Join(Consts.CommaNl, enumValues.Select(enumValue => $"{enumValue.Name} = {enumValue.ResolvedIndex}"));
            }
        }

        static string BuildValueMatching(EnumValueDefinition[] enumValues, EnumClassCollector.Definition definition)
        {
            return $$"""
                /// <summary>
                /// Invokes the callback associated with this value. A callback is required for every declared value.
                /// </summary>
                {{String.Join(Consts.Nl, enumValues.Select(x => $"/// <param name=\"on{x.NormalizedName}\">The callback for <c>{x.NormalizedName}</c>.</param>"))}}
                /// <exception cref="System.InvalidOperationException">This instance is not a declared enum-class value.</exception>
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
                /// Invokes the callback associated with this value, when one was supplied.
                /// </summary>
                /// <remarks>
                /// Does nothing when the matching callback is <see langword="null"/> or this instance is not a declared value.
                /// </remarks>
                {{String.Join(Consts.Nl, enumValues.Select(x => $"/// <param name=\"on{x.NormalizedName}\">The optional callback for <c>{x.NormalizedName}</c>.</param>"))}}
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
                /// Passes state to and invokes the callback associated with this value.
                /// A callback is required for every declared value.
                /// </summary>
                /// <typeparam name="TState">The type of state passed to the callback.</typeparam>
                /// <param name="state">State passed to the selected callback.</param>
                {{String.Join(Consts.Nl, enumValues.Select(x => $"/// <param name=\"on{x.NormalizedName}\">The callback for <c>{x.NormalizedName}</c>.</param>"))}}
                /// <exception cref="System.InvalidOperationException">This instance is not a declared enum-class value.</exception>
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
                /// Passes state to and invokes the callback associated with this value, when one was supplied.
                /// </summary>
                /// <typeparam name="TState">The type of state passed to the callback.</typeparam>
                /// <param name="state">State passed to the selected callback.</param>
                {{String.Join(Consts.Nl, enumValues.Select(x => $"/// <param name=\"on{x.NormalizedName}\">The optional callback for <c>{x.NormalizedName}</c>.</param>"))}}
                /// <remarks>
                /// Does nothing when the matching callback is <see langword="null"/> or this instance is not a declared value.
                /// </remarks>
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
                /// Invokes the callback associated with this value and returns its result.
                /// A callback is required for every declared value.
                /// </summary>
                /// <typeparam name="TResult">The callback result type.</typeparam>
                {{String.Join(Consts.Nl, enumValues.Select(x => $"/// <param name=\"on{x.NormalizedName}\">The callback for <c>{x.NormalizedName}</c>.</param>"))}}
                /// <returns>The result of the selected callback.</returns>
                /// <exception cref="System.InvalidOperationException">This instance is not a declared enum-class value.</exception>
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
                /// Invokes the callback associated with this value and returns its result, when a callback was supplied.
                /// </summary>
                /// <typeparam name="TResult">The callback result type.</typeparam>
                {{String.Join(Consts.Nl, enumValues.Select(x => $"/// <param name=\"on{x.NormalizedName}\">The optional callback for <c>{x.NormalizedName}</c>.</param>"))}}
                /// <returns>
                /// The result of the selected callback, or <see langword="default"/> when the matching callback is
                /// <see langword="null"/> or this instance is not a declared value.
                /// </returns>
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
                /// Passes state to and invokes the callback associated with this value, then returns its result.
                /// A callback is required for every declared value.
                /// </summary>
                /// <typeparam name="TResult">The callback result type.</typeparam>
                /// <typeparam name="TState">The type of state passed to the callback.</typeparam>
                /// <param name="state">State passed to the selected callback.</param>
                {{String.Join(Consts.Nl, enumValues.Select(x => $"/// <param name=\"on{x.NormalizedName}\">The callback for <c>{x.NormalizedName}</c>.</param>"))}}
                /// <returns>The result of the selected callback.</returns>
                /// <exception cref="System.InvalidOperationException">This instance is not a declared enum-class value.</exception>
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
                /// Passes state to and invokes the callback associated with this value, then returns its result,
                /// when a callback was supplied.
                /// </summary>
                /// <typeparam name="TResult">The callback result type.</typeparam>
                /// <typeparam name="TState">The type of state passed to the callback.</typeparam>
                /// <param name="state">State passed to the selected callback.</param>
                {{String.Join(Consts.Nl, enumValues.Select(x => $"/// <param name=\"on{x.NormalizedName}\">The optional callback for <c>{x.NormalizedName}</c>.</param>"))}}
                /// <returns>
                /// The result of the selected callback, or <see langword="default"/> when the matching callback is
                /// <see langword="null"/> or this instance is not a declared value.
                /// </returns>
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
                /// <summary>
                /// Tries to return a declared value as its more specific declared type.
                /// </summary>
                /// <typeparam name="TValue">The derived enum-class value type to match.</typeparam>
                /// <param name="value">The declared value to inspect.</param>
                /// <param name="typeMatchingValue">The value cast to <typeparamref name="TValue"/>, or <see langword="null"/> when it does not match.</param>
                /// <returns><see langword="true"/> when <paramref name="value"/> is declared as <typeparamref name="TValue"/>; otherwise, <see langword="false"/>.</returns>
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

        static string BuildRuntimeIndexingHelpersWhenApplicable(EnumValueDefinition[] enumValues, EnumClassProps props)
        {
            if (!props.Configuration.IndexAssignmentMode.IsRuntime)
                return "";

            if (GetLookupImplementationForIntKeys(enumValues, props) is LookupImplementation.Dictionary)
            {
                return $$"""
                    private static System.Collections.Frozen.FrozenDictionary<int, {{props.Definition.DeclarationName}}>? _valuesByEnumIndex;

                    private static System.Collections.Frozen.FrozenDictionary<int, {{props.Definition.DeclarationName}}> CreateRuntimeIndexDictionary()
                    {
                        var valuesByEnumIndex = new System.Collections.Generic.Dictionary<int, {{props.Definition.DeclarationName}}>(_allValues.Length);

                        foreach (var value in _allValues)
                        {
                            if (!value._enumIndexAssigned)
                            {
                                throw new System.InvalidOperationException(
                                    "Every declared {{props.Definition.DeclarationName}} value must assign EnumIndex.");
                            }

                            if (!valuesByEnumIndex.TryAdd(value._enumIndex, value))
                            {
                                throw new System.InvalidOperationException(
                                    $"EnumIndex {value._enumIndex} is assigned more than once.");
                            }
                        }

                        return System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(valuesByEnumIndex);
                    }
                    """;
            }

            return $$"""
                private static void ValidateRuntimeIndexes()
                {
                    var usedIndexes = new System.Collections.Generic.HashSet<int>();

                    foreach (var value in _allValues)
                    {
                        if (!value._enumIndexAssigned)
                        {
                            throw new System.InvalidOperationException(
                                "Every declared {{props.Definition.DeclarationName}} value must assign EnumIndex.");
                        }

                        if (!usedIndexes.Add(value._enumIndex))
                        {
                            throw new System.InvalidOperationException(
                                $"EnumIndex {value._enumIndex} is assigned more than once.");
                        }
                    }
                }
                """;
        }

        static string BuildSerialization(EnumValueDefinition[] enumValues, EnumClassProps props)
        {
            return $$"""
                private string _serializedName = null!;

                {{BuildDictionaryDeclaration(enumValues, props)}}

                /// <summary>
                /// Returns the source member name used to represent this declared value.
                /// </summary>
                /// <returns>The case-sensitive source member name.</returns>
                /// <remarks>Renaming the declared member changes its serialized and JSON representation.</remarks>
                public string Serialize() => _serializedName;

                /// <summary>
                /// Returns the source member name used to represent a declared value.
                /// </summary>
                /// <param name="value">The value to serialize.</param>
                /// <returns>The case-sensitive source member name.</returns>
                /// <exception cref="System.NullReferenceException"><paramref name="value"/> is <see langword="null"/>.</exception>
                /// <remarks>Renaming the declared member changes its serialized and JSON representation.</remarks>
                public static string Serialize({{props.Definition.DeclarationName}} value) => value.Serialize();

                /// <summary>Returns the source member name used to represent this declared value.</summary>
                /// <returns>The case-sensitive source member name.</returns>
                public override string ToString() => Serialize();
                
                {{BuildDeserializers(enumValues, props)}}
                
                /// <summary>
                /// Tries to find a declared value by its case-sensitive source member name.
                /// </summary>
                /// <param name="serializedValue">The source member name to find.</param>
                /// <param name="result">The matching declared value, or <see langword="null"/> when no value matches.</param>
                /// <returns><see langword="true"/> when a value matches; otherwise, <see langword="false"/>.</returns>
                public static bool TryDeserialize(string? serializedValue, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.Definition.DeclarationName}}? result)
                {
                    result = Deserialize(serializedValue);
                    return result is not null;
                }
                
                /// <summary>
                /// Tries to find a declared value by its case-sensitive source member name.
                /// </summary>
                /// <param name="serializedValue">The source member name to find.</param>
                /// <param name="result">The matching declared value, or <see langword="null"/> when no value matches.</param>
                /// <returns><see langword="true"/> when a value matches; otherwise, <see langword="false"/>.</returns>
                public static bool TryDeserialize(System.ReadOnlySpan<char> serializedValue, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out {{props.Definition.DeclarationName}}? result)
                {
                    result = Deserialize(serializedValue);
                    return result is not null;
                }
                
                /// <summary>
                /// Determines whether a declared value has the specified case-sensitive source member name.
                /// </summary>
                /// <param name="serializedValue">The source member name to find.</param>
                /// <returns><see langword="true"/> when a value matches; otherwise, <see langword="false"/>.</returns>
                public static bool ContainsSerializedValue(string? serializedValue)
                    => Deserialize(serializedValue) is not null;
                
                /// <summary>
                /// Determines whether a declared value has the specified case-sensitive source member name.
                /// </summary>
                /// <param name="serializedValue">The source member name to find.</param>
                /// <returns><see langword="true"/> when a value matches; otherwise, <see langword="false"/>.</returns>
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

                if (props.Definition.FeatureSupport.AlternateLookup.IsSupported)
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
                    // TODO throwing on missing dictionaries instead of defaulting to false
                    return $$"""
                        /// <summary>
                        /// Finds a declared value by its case-sensitive source member name.
                        /// </summary>
                        /// <param name="serializedValue">The source member name to find.</param>
                        /// <returns>The matching declared value, or <see langword="null"/> when no value matches.</returns>
                        public static {{props.Definition.DeclarationName}}? Deserialize(string? serializedValue)
                        {
                            if (serializedValue is null)
                                return null;

                            return (_valuesBySerializedName?.TryGetValue(serializedValue, out {{props.Definition.DeclarationName}}? value) ?? false)
                                ? value
                                : null;
                        }

                        /// <summary>
                        /// Finds a declared value by its case-sensitive source member name.
                        /// </summary>
                        /// <param name="serializedValue">The source member name to find.</param>
                        /// <returns>The matching declared value, or <see langword="null"/> when no value matches.</returns>
                        public static {{props.Definition.DeclarationName}}? Deserialize(System.ReadOnlySpan<char> serializedValue)
                        {
                            {{BuildSpanDeserializationBody(enumValues, props)}}
                        }
                        """;

                    static string BuildSpanDeserializationBody(EnumValueDefinition[] enumValues, EnumClassProps props)
                    {
                        if (props.Definition.FeatureSupport.AlternateLookup.IsSupported)
                        {
                            return $$"""
                                return _valuesBySerializedNameSpanLookup.TryGetValue(serializedValue, out {{props.Definition.DeclarationName}}? value)
                                    ? value
                                    : null;
                                """;
                        }

                        // fallback
                        return BuildIfChainSpanDeserializationBody(enumValues, props);
                    }
                }

                static string BuildIfChainBasedDeserialization(EnumValueDefinition[] enumValues, EnumClassProps props)
                {
                    return $$"""
                        /// <summary>
                        /// Finds a declared value by its case-sensitive source member name.
                        /// </summary>
                        /// <param name="serializedValue">The source member name to find.</param>
                        /// <returns>The matching declared value, or <see langword="null"/> when no value matches.</returns>
                        public static {{props.Definition.DeclarationName}}? Deserialize(string? serializedValue)
                        {
                            if (serializedValue is null)
                                return null;

                            return Deserialize(System.MemoryExtensions.AsSpan(serializedValue));
                        }

                        /// <summary>
                        /// Finds a declared value by its case-sensitive source member name.
                        /// </summary>
                        /// <param name="serializedValue">The source member name to find.</param>
                        /// <returns>The matching declared value, or <see langword="null"/> when no value matches.</returns>
                        public static {{props.Definition.DeclarationName}}? Deserialize(System.ReadOnlySpan<char> serializedValue)
                        {
                            {{BuildIfChainSpanDeserializationBody(enumValues, props)}}
                        }
                        """;
                }

                static string BuildIfChainSpanDeserializationBody(EnumValueDefinition[] enumValues, EnumClassProps props)
                {
                    return $$"""
                        {{String.Join(Consts.Nl, enumValues.Select(enumValue
                            => $"if (System.MemoryExtensions.Equals(serializedValue, System.MemoryExtensions.AsSpan({enumValue.Name}._serializedName), System.StringComparison.Ordinal)) return {enumValue.Name};"))}}
                        return null;
                        """;
                }
            }

        }


        static bool CheckIfShouldIncludeUtf8EncodedResources(EnumValueDefinition[] enumValues, EnumClassProps props)
            => GetLookupImplementationForUtf8Keys(enumValues, props) is LookupImplementation.Dictionary
                && props.Configuration.GenerateJsonConverter;

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



        static string BuildRemarksWhenAppliable(EnumValueDefinition[] enumValues, EnumClassProps props)
        {
            if (props.Configuration.DocumentationMode is DocumentationMode.Disabled)
                return "";

            if (props.Configuration.IndexAssignmentMode is not IndexAssignmentMode.CompileTime)
            {
                // nothing worth remarking
                return "";
            }

            return $$"""
                /// <remarks>
                /// Declared values and their compile-time indexes:
                /// <list type="table">
                ///   <listheader>
                ///     <term>Value</term>
                ///     <description>Enum index</description>
                ///   </listheader>
                {{string.Join(Consts.Nl, enumValues.Select(enumValue => BuildValueListItem(enumValue)))}}
                /// </list>
                /// </remarks>
                """;

            static string BuildValueListItem(EnumValueDefinition enumValue)
            {
                return $$"""
                    ///   <item>
                    ///     <term><see cref="{{enumValue.Name}}"/></term>
                    ///     <description>{{enumValue.ResolvedIndex}}</description>
                    ///   </item>
                    """;
            }
        }


        static LookupImplementation GetLookupImplementationForStringKeys(EnumValueDefinition[] enumValues, EnumClassProps props)
            => LookupImplementationSelector.Get(props, enumValues.Length, LookupImplementationSelector.Target.StringKeys);

        static LookupImplementation GetLookupImplementationForUtf8Keys(EnumValueDefinition[] enumValues, EnumClassProps props)
            => LookupImplementationSelector.Get(props, enumValues.Length, LookupImplementationSelector.Target.Utf8Keys);

        static LookupImplementation GetLookupImplementationForIntKeys(EnumValueDefinition[] enumValues, EnumClassProps props)
            => LookupImplementationSelector.Get(props, enumValues.Length, LookupImplementationSelector.Target.IntKeys);
    }


    public const string ValuesBySerializedUtf8NameSpanLookupVariableName = "_valuesBySerializedUtf8NameSpanLookup";
}
