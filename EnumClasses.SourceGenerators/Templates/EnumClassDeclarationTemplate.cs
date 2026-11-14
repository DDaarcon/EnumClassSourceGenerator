using EnumClasses.SourceGenerators.Configurations;
using EnumClasses.SourceGenerators.ExternalSourcesHelpers;
using EnumClasses.SourceGenerators.FeatureSupport;
using EnumClasses.SourceGenerators.GenerationStrategies;
using EnumClasses.SourceGenerators.Schema;
using System;
using System.Collections.Generic;
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
        var ctx = Context.Parse(props);

        return $$"""
            {{ctx.FeatureSupport.NullableReferenceTypes.IfSupportedOrNull("#nullable enable")}}
            namespace {{ctx.Definition.NamespaceName}}
            {
                {{BuildContainingTypesStart(ctx)}}

                {{BuildRemarksWhenAppliable(ctx)}}
                {{(ctx.Config.GenerateJsonConverter && ctx.FeatureSupport.JsonConverter.IsSupported ? $"[System.Text.Json.Serialization.JsonConverter(typeof({ctx.Definition.DeclarationName}.JsonConverter))]" : "")}}
                {{ctx.Definition.Modifier}} partial {{ctx.Definition.TypeKeyword}} {{ctx.Definition.DeclarationName}} : System.IEquatable<{{ctx.Definition.DeclarationName}}>
                {
                    {{BuildConstructor(ctx)}}

                    {{BuildStaticConstructor(ctx)}}

            
                    private static System.Collections.Immutable.ImmutableArray<{{ctx.Definition.DeclarationName}}> _allValues;

                    /// <summary>
                    /// Gets all declared values in source declaration order.
                    /// </summary>
                    public static System.Collections.Immutable.ImmutableArray<{{ctx.Definition.DeclarationName}}> AllValues => _allValues;

                    private int _internalIndex;
            

                    {{BuildEnumIndexing(ctx)}}
            
                    {{BuildSerialization(ctx)}}

                    {{BuildRawEnumWhenApplicable(ctx)}}

                    {{BuildTypeMatchingWhenApplicable(ctx)}}

                    {{BuildValueMatching(ctx)}}
                            
                    {{BuildRuntimeIndexingHelpersWhenApplicable(ctx)}}

                    {{BuildUtf8EncodedResourcesWhenApplicable(ctx)}}

                    {{BuildEqualityMethods(ctx)}}
                }
            }
            {{BuildContainingTypesEnd(ctx)}}

            {{ctx.FeatureSupport.NullableReferenceTypes.IfSupportedOrNull("#nullable disable")}}
            """;

        static string BuildConstructor(Context ctx)
        {
            if (ctx.Config.ConstructionRestrictionMode is ConstructionRestrictionMode.Off
                || ctx.Definition.HasExplicitInstanceConstructor)
            {
                return "";
            }

            var modifier = ctx.Config.ConstructionRestrictionMode switch
            {
                ConstructionRestrictionMode.WithPrivateDefaultConstructor => "private",
                ConstructionRestrictionMode.WithProtectedDefaultConstructor or _ => "protected"
            };

            return $$"""
                {{modifier}} {{ctx.Definition.DeclarationName}}() { }
                """;
        }


        static string BuildStaticConstructor(Context ctx)
        {
            return $$"""
                static {{ctx.Definition.DeclarationName}}()
                {
                    {{(ctx.Config.IndexAssignmentMode is IndexAssignmentMode.CompileTime
                        ? ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"{enumValue.Name}.EnumIndex = {enumValue.ResolvedIndex};")
                        : "")}}
                
                    {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"{enumValue.Name}._internalIndex = {enumValue.InternalIndex};")}}
                
                    {{BuildSerializedNamesAssignment(ctx)}}
                
                    {{(ctx.Config.GenerateRawEnum
                        ? ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"{enumValue.Name}._rawValue = Raw.{enumValue.Name};")
                        : "")}}
                            
                    _allValues = System.Collections.Immutable.ImmutableArray.Create<{{ctx.Definition.DeclarationName}}>(
                        {{ctx.GetJoinEnumValues(Consts.CommaNl, static enumValue => enumValue.Name)}}
                    );
                
                
                    {{BuildSerializationDictionaryAssignmentWhenApplicable(ctx)}}

                    {{BuildUtf8EncodedResourcesDictionaryAssignmentWhenApplicable(ctx)}}

                    {{BuildRuntimeIndexAssignmentValidationWhenApplicable(ctx)}}
                }
                """;

            static string BuildSerializedNamesAssignment(Context ctx)
            {
                return ctx.GetJoinEnumValues(Consts.Nl, static enumValue
                    => $"{enumValue.Name}._serializedName = {GetEnumValueSerializedName(enumValue)};");

                static string GetEnumValueSerializedName(EnumValueDefinition enumValue)
                    => enumValue.ExplicitSerializedName is not null
                        ? Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(
                            enumValue.ExplicitSerializedName,
                            quote: true)
                        : $"nameof({enumValue.Name})";
            }

            static string BuildSerializationDictionaryAssignmentWhenApplicable(Context ctx)
            {
                if (GetLookupImplementationForStringKeys(ctx) is not LookupImplementation.Dictionary)
                    return "";

                var dictAssignment = ctx.FeatureSupport.FrozenDictionary.IsSupported
                    ? """
                        _valuesBySerializedName = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(_allValues, x => x._serializedName, System.StringComparer.Ordinal);
                        """
                    : """
                        _valuesBySerializedName = System.Linq.Enumerable.ToDictionary(_allValues, x => x._serializedName, System.StringComparer.Ordinal);
                        """;

                if (ctx.FeatureSupport.AlternateLookup.IsSupported)
                    return dictAssignment + $$"""

                        _valuesBySerializedNameSpanLookup = _valuesBySerializedName.GetAlternateLookup<System.ReadOnlySpan<char>>();
                        """;

                return dictAssignment;
            }

            static string BuildUtf8EncodedResourcesDictionaryAssignmentWhenApplicable(Context ctx)
            {
                if (!CheckIfShouldIncludeUtf8EncodedResources(ctx))
                    return "";

                return $$"""
                    _valuesBySerializedUtf8Name = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(
                        _allValues,
                        x => (ReadOnlyMemory<byte>) System.MemoryExtensions.AsMemory(System.Text.Encoding.UTF8.GetBytes(x._serializedName)),
                        Utf8MemoryComparer.Instance);

                    {{ValuesBySerializedUtf8NameSpanLookupVariableName}} = _valuesBySerializedUtf8Name.GetAlternateLookup<System.ReadOnlySpan<byte>>();
                    """;
            }

            static string BuildRuntimeIndexAssignmentValidationWhenApplicable(Context ctx)
            {
                if (!ctx.Config.IndexAssignmentMode.IsRuntime)
                    return "";

                return GetLookupImplementationForIntKeys(ctx) is LookupImplementation.Dictionary
                    ? "_valuesByEnumIndex = CreateRuntimeIndexDictionary();"
                    : "ValidateRuntimeIndexes();";
            }
        }

        static string BuildEnumIndexing(Context ctx)
        {
            return $$"""
                {{BuildProperty(ctx)}}

                {{BuildAccessorMethod(ctx)}}
                
                /// <summary>
                /// Tries to get the declared value with the specified enum index.
                /// </summary>
                /// <param name="index">The enum index to find.</param>
                /// <param name="result">The matching declared value, or <see langword="null"/> when no value has the index.</param>
                /// <returns><see langword="true"/> when a declared value has the specified index; otherwise, <see langword="false"/>.</returns>
                public static bool TryGetByEnumIndex(int index,{{ctx.FeatureSupport.NotNullWhenAttribute.IfSupportedOrNull(" [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]")}} out {{ctx.GetNullableReferenceType(ctx.Definition.DeclarationName)}} result)
                {
                    result = GetByEnumIndex(index);
                    return {{ctx.GetIsNotNullExpression("result")}};
                }
                
                /// <summary>
                /// Determines whether a declared value has the specified enum index.
                /// </summary>
                /// <param name="index">The enum index to find.</param>
                /// <returns><see langword="true"/> when a declared value has the specified index; otherwise, <see langword="false"/>.</returns>
                public static bool ContainsEnumIndex(int index)
                    => {{ctx.GetIsNotNullExpression("GetByEnumIndex(index)")}};
                """;

            static string BuildProperty(Context ctx)
            {
                if (ctx.Config.IndexAssignmentMode is IndexAssignmentMode.CompileTime)
                    // TODO think of a way for restricting value change after creation
                    return """
                        /// <summary>
                        /// Gets the zero-based index assigned from source declaration order.
                        /// </summary>
                        public int EnumIndex { get; private set; }
                        """;

                if (ctx.Config.IndexAssignmentMode is IndexAssignmentMode.RuntimeAtValueInit)
                    return $$"""
                        private int _enumIndex;
                        private bool _enumIndexAssigned;

                        public {{ctx.FeatureSupport.RequiredKeyword.IfSupportedOrNull("required ")}}int EnumIndex
                        {
                            get => _enumIndex;
                            {{(ctx.FeatureSupport.InitKeyword.IsSupported ? "init" : "set")}}
                            {
                                _enumIndex = value;
                                _enumIndexAssigned = true;
                            }
                        }
                        """;

                // IndexAssignmentMode.RuntimeAtValueCreation
                return $$"""
                    private int _enumIndex;
                    private bool _enumIndexAssigned;

                    public int EnumIndex
                    {
                        get => _enumIndex;
                        {{(ctx.FeatureSupport.InitKeyword.IsSupported ? "init" : "set")}}
                        {
                            _enumIndex = value;
                            _enumIndexAssigned = true;
                        }
                    }
                    """;
            }

            static string BuildAccessorMethod(Context ctx)
            {
                return $$"""
                    /// <summary>
                    /// Gets the declared value with the specified enum index.
                    /// </summary>
                    /// <param name="index">The enum index to find.</param>
                    /// <returns>The matching declared value, or <see langword="null"/> when no value has the index.</returns>
                    public static {{ctx.GetNullableReferenceType(ctx.Definition.DeclarationName)}} GetByEnumIndex(int index)
                    {
                        {{BuildBody(ctx)}}
                    }
                    """;

                static string BuildBody(Context ctx)
                {
                    if (ctx.Config.IndexAssignmentMode.IsRuntime)
                    {
                        if (GetLookupImplementationForIntKeys(ctx) is LookupImplementation.Dictionary)
                        {
                            return $$"""
                                return (_valuesByEnumIndex?.TryGetValue(index, out {{ctx.GetNullableReferenceType(ctx.Definition.DeclarationName)}} value) ?? false)
                                    ? value
                                    : null;
                                """;
                        }

                        return $$"""
                            {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"if (index == {enumValue.Name}.EnumIndex) return {enumValue.Name};")}}
                            return null;
                            """;
                    }

                    if (ctx.FeatureSupport.SwitchExpression.IsSupported)
                    {
                        return $$"""
                            return index switch
                            {
                                {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"{enumValue.ResolvedIndex} => {enumValue.Name},")}}
                                _ => null
                            };
                            """;
                    }

                    return $$"""
                        switch (index)
                        {
                            {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"case {enumValue.ResolvedIndex}: return {enumValue.Name};")}}
                            default: return null;
                        }
                        """;
                }
            }
        }

        static string BuildRawEnumWhenApplicable(Context ctx)
        {
            if (!ctx.Config.GenerateRawEnum)
                return "";

            return $$"""
                private Raw? _rawValue;

                /// <summary>
                /// Represents the declared enum-class values as a conventional enum.
                /// </summary>
                /// <remarks>{{(ctx.Config.IndexAssignmentMode.IsRuntime
                    ? "Members receive zero-based numeric values in source declaration order; these values are independent of runtime EnumIndex assignments."
                    : "Each member receives the same resolved numeric value as its compile-time EnumIndex.")}}</remarks>
                public enum Raw
                {
                    {{BuildRawEnumDefinition(ctx)}}
                }

                /// <summary>
                /// Converts a raw enum value to its declared enum-class value.
                /// </summary>
                /// <param name="value">The raw value to convert.</param>
                /// <returns>The corresponding declared enum-class value.</returns>
                /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is not defined.</exception>
                public static {{ctx.Definition.DeclarationName}} FromRaw(Raw value)
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
                public static bool TryFromRaw(Raw value,{{ctx.FeatureSupport.NotNullWhenAttribute.IfSupportedOrNull(" [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]")}} out {{ctx.GetNullableReferenceType(ctx.Definition.DeclarationName)}} result)
                {
                    {{BuildRawValueAssignment(ctx)}}

                    return {{ctx.GetIsNotNullExpression("result")}};
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
                public static Raw ToRaw({{ctx.Definition.DeclarationName}} value)
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
                public static bool TryToRaw({{ctx.GetNullableReferenceType(ctx.Definition.DeclarationName)}} value, out Raw result)
                {
                    if (value?._rawValue is Raw rawValue)
                    {
                        result = rawValue;
                        return true;
                    }

                    result = default(Raw);
                    return false;
                }

                /// <summary>Converts a raw enum value to its declared enum-class value.</summary>
                /// <param name="value">The raw value to convert.</param>
                /// <returns>The corresponding declared enum-class value.</returns>
                /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is not defined.</exception>
                public static implicit operator {{ctx.Definition.DeclarationName}}(Raw value) => FromRaw(value);

                /// <summary>Converts an enum-class value to its raw enum value.</summary>
                /// <param name="value">The enum-class value to convert.</param>
                /// <returns>The corresponding raw enum value.</returns>
                /// <exception cref="System.ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
                /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is not a declared enum-class value.</exception>
                public static explicit operator Raw({{ctx.Definition.DeclarationName}} value) => ToRaw(value);
                """;

            static string BuildRawEnumDefinition(Context ctx)
            {
                if (ctx.Config.IndexAssignmentMode.IsRuntime)
                    return ctx.GetJoinEnumValues(Consts.CommaNl, static enumValue => enumValue.Name);

                return ctx.GetJoinEnumValues(
                    Consts.CommaNl,
                    static enumValue => $"{enumValue.Name} = {enumValue.ResolvedIndex}");
            }

            static string BuildRawValueAssignment(Context ctx)
            {
                if (ctx.FeatureSupport.SwitchExpression.IsSupported)
                {
                    return $$"""
                        result = value switch
                        {
                            {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"Raw.{enumValue.Name} => {enumValue.Name},")}}
                            _ => null
                        };
                        """;
                }

                return $$"""
                    switch (value)
                    {
                        {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $$"""
                            case Raw.{{enumValue.Name}}:
                                result = {{enumValue.Name}};
                                break;
                            """)}}
                        default:
                            result = null;
                            break;
                    }
                    """;
            }
        }

        static string BuildValueMatching(Context ctx)
        {
            return $$"""
                /// <summary>
                /// Invokes the callback associated with this value. A callback is required for every declared value.
                /// </summary>
                {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"/// <param name=\"on{enumValue.NormalizedName}\">The callback for <c>{enumValue.NormalizedName}</c>.</param>")}}
                /// <exception cref="System.InvalidOperationException">This instance is not a declared enum-class value.</exception>
                public void SwitchEx(
                    {{ctx.GetJoinEnumValues(Consts.CommaNl, static enumValue => $"System.Action on{enumValue.NormalizedName}")}})
                {
                    switch (_internalIndex)
                    {
                        {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $$"""
                            case {{enumValue.InternalIndex}} when AreEqual(this, {{enumValue.Name}}):
                                on{{enumValue.NormalizedName}}();
                                return;
                            """)}}
                        default:
                            throw new System.InvalidOperationException("Invalid value of {{ctx.Definition.DeclarationName}}.");
                    }
                }

                /// <summary>
                /// Invokes the callback associated with this value, when one was supplied.
                /// </summary>
                /// <remarks>
                /// Does nothing when the matching callback is <see langword="null"/> or this instance is not a declared value.
                /// </remarks>
                {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"/// <param name=\"on{enumValue.NormalizedName}\">The optional callback for <c>{enumValue.NormalizedName}</c>.</param>")}}
                public void Switch(
                    {{ctx.GetJoinEnumValues(
                        Consts.CommaNl,
                        ctx.GetNullableReferenceType("System.Action"),
                        static (enumValue, actionType) => $"{actionType} on{enumValue.NormalizedName} = null")}})
                {
                    switch (_internalIndex)
                    {
                        {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $$"""
                            case {{enumValue.InternalIndex}} when AreEqual(this, {{enumValue.Name}}) && on{{enumValue.NormalizedName}} != null:
                                on{{enumValue.NormalizedName}}();
                                return;
                            """)}}
                    }
                }

                /// <summary>
                /// Passes state to and invokes the callback associated with this value.
                /// A callback is required for every declared value.
                /// </summary>
                /// <typeparam name="TState">The type of state passed to the callback.</typeparam>
                /// <param name="state">State passed to the selected callback.</param>
                {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"/// <param name=\"on{enumValue.NormalizedName}\">The callback for <c>{enumValue.NormalizedName}</c>.</param>")}}
                /// <exception cref="System.InvalidOperationException">This instance is not a declared enum-class value.</exception>
                public void SwitchEx<TState>(
                    TState state,
                    {{ctx.GetJoinEnumValues(Consts.CommaNl, static enumValue => $"System.Action<TState> on{enumValue.NormalizedName}")}})
                {
                    switch (_internalIndex)
                    {
                        {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $$"""
                            case {{enumValue.InternalIndex}} when AreEqual(this, {{enumValue.Name}}):
                                on{{enumValue.NormalizedName}}(state);
                                return;
                            """)}}
                        default:
                            throw new System.InvalidOperationException("Invalid value of {{ctx.Definition.DeclarationName}}.");
                    }
                }

                /// <summary>
                /// Passes state to and invokes the callback associated with this value, when one was supplied.
                /// </summary>
                /// <typeparam name="TState">The type of state passed to the callback.</typeparam>
                /// <param name="state">State passed to the selected callback.</param>
                {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"/// <param name=\"on{enumValue.NormalizedName}\">The optional callback for <c>{enumValue.NormalizedName}</c>.</param>")}}
                /// <remarks>
                /// Does nothing when the matching callback is <see langword="null"/> or this instance is not a declared value.
                /// </remarks>
                public void Switch<TState>(
                    TState state,
                    {{ctx.GetJoinEnumValues(
                        Consts.CommaNl,
                        ctx.GetNullableReferenceType("System.Action<TState>"),
                        static (enumValue, actionType) => $"{actionType} on{enumValue.NormalizedName} = null")}})
                {
                    switch (_internalIndex)
                    {
                        {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $$"""
                            case {{enumValue.InternalIndex}} when AreEqual(this, {{enumValue.Name}}) && on{{enumValue.NormalizedName}} != null:
                                on{{enumValue.NormalizedName}}(state);
                                return;
                            """)}}
                    }
                }




                /// <summary>
                /// Invokes the callback associated with this value and returns its result.
                /// A callback is required for every declared value.
                /// </summary>
                /// <typeparam name="TResult">The callback result type.</typeparam>
                {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"/// <param name=\"on{enumValue.NormalizedName}\">The callback for <c>{enumValue.NormalizedName}</c>.</param>")}}
                /// <returns>The result of the selected callback.</returns>
                /// <exception cref="System.InvalidOperationException">This instance is not a declared enum-class value.</exception>
                public TResult MatchEx<TResult>(
                    {{ctx.GetJoinEnumValues(Consts.CommaNl, static enumValue => $"System.Func<TResult> on{enumValue.NormalizedName}")}})
                {
                    switch (_internalIndex)
                    {
                        {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $$"""
                            case {{enumValue.InternalIndex}} when AreEqual(this, {{enumValue.Name}}):
                                return on{{enumValue.NormalizedName}}();
                            """)}}
                        default:
                            throw new System.InvalidOperationException("Invalid value of {{ctx.Definition.DeclarationName}}.");
                    }
                }

                /// <summary>
                /// Invokes the callback associated with this value and returns its result, when a callback was supplied.
                /// </summary>
                /// <typeparam name="TResult">The callback result type.</typeparam>
                {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"/// <param name=\"on{enumValue.NormalizedName}\">The optional callback for <c>{enumValue.NormalizedName}</c>.</param>")}}
                /// <returns>
                /// The result of the selected callback, or <see langword="default"/> when the matching callback is
                /// <see langword="null"/> or this instance is not a declared value.
                /// </returns>
                public {{ctx.GetNullableUnconstrainedTypeParameter("TResult")}} Match<TResult>(
                    {{ctx.GetJoinEnumValues(
                        Consts.CommaNl,
                        ctx.GetNullableReferenceType("System.Func<TResult>"),
                        static (enumValue, functionType) => $"{functionType} on{enumValue.NormalizedName} = null")}})
                {
                    switch (_internalIndex)
                    {
                        {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $$"""
                            case {{enumValue.InternalIndex}} when AreEqual(this, {{enumValue.Name}}) && on{{enumValue.NormalizedName}} != null:
                                return on{{enumValue.NormalizedName}}();
                            """)}}
                    }
                    return default(TResult);
                }


                /// <summary>
                /// Passes state to and invokes the callback associated with this value, then returns its result.
                /// A callback is required for every declared value.
                /// </summary>
                /// <typeparam name="TResult">The callback result type.</typeparam>
                /// <typeparam name="TState">The type of state passed to the callback.</typeparam>
                /// <param name="state">State passed to the selected callback.</param>
                {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"/// <param name=\"on{enumValue.NormalizedName}\">The callback for <c>{enumValue.NormalizedName}</c>.</param>")}}
                /// <returns>The result of the selected callback.</returns>
                /// <exception cref="System.InvalidOperationException">This instance is not a declared enum-class value.</exception>
                public TResult MatchEx<TResult, TState>(
                    TState state,
                    {{ctx.GetJoinEnumValues(Consts.CommaNl, static enumValue => $"System.Func<TState, TResult> on{enumValue.NormalizedName}")}})
                {
                    switch (_internalIndex)
                    {
                        {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $$"""
                            case {{enumValue.InternalIndex}} when AreEqual(this, {{enumValue.Name}}):
                                return on{{enumValue.NormalizedName}}(state);
                            """)}}
                        default:
                            throw new System.InvalidOperationException("Invalid value of {{ctx.Definition.DeclarationName}}.");
                    }
                }

                /// <summary>
                /// Passes state to and invokes the callback associated with this value, then returns its result,
                /// when a callback was supplied.
                /// </summary>
                /// <typeparam name="TResult">The callback result type.</typeparam>
                /// <typeparam name="TState">The type of state passed to the callback.</typeparam>
                /// <param name="state">State passed to the selected callback.</param>
                {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $"/// <param name=\"on{enumValue.NormalizedName}\">The optional callback for <c>{enumValue.NormalizedName}</c>.</param>")}}
                /// <returns>
                /// The result of the selected callback, or <see langword="default"/> when the matching callback is
                /// <see langword="null"/> or this instance is not a declared value.
                /// </returns>
                public {{ctx.GetNullableUnconstrainedTypeParameter("TResult")}} Match<TResult, TState>(
                    TState state,
                    {{ctx.GetJoinEnumValues(
                        Consts.CommaNl,
                        ctx.GetNullableReferenceType("System.Func<TState, TResult>"),
                        static (enumValue, functionType) => $"{functionType} on{enumValue.NormalizedName} = null")}})
                {
                    switch (_internalIndex)
                    {
                        {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => $$"""
                            case {{enumValue.InternalIndex}} when AreEqual(this, {{enumValue.Name}}) && on{{enumValue.NormalizedName}} != null:
                                return on{{enumValue.NormalizedName}}(state);
                            """)}}
                    }
                    return default(TResult);
                }
                """;
        }

        static string BuildTypeMatchingWhenApplicable(Context ctx)
        {
            if (!ctx.HasAnyCustomTypesForEnumValues)
                return "";

            return $$"""
                /// <summary>
                /// Tries to return a declared value as its more specific declared type.
                /// </summary>
                /// <typeparam name="TValue">The derived enum-class value type to match.</typeparam>
                /// <param name="value">The declared value to inspect.</param>
                /// <param name="typeMatchingValue">The value cast to <typeparamref name="TValue"/>, or <see langword="null"/> when it does not match.</param>
                /// <returns><see langword="true"/> when <paramref name="value"/> is declared as <typeparamref name="TValue"/>; otherwise, <see langword="false"/>.</returns>
                public static bool TryGetOfType<TValue>({{ctx.Definition.DeclarationName}} value,{{ctx.FeatureSupport.NotNullWhenAttribute.IfSupportedOrNull(" [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]")}} out {{ctx.GetNullableReferenceType("TValue")}} typeMatchingValue)
                    where TValue : {{ctx.Definition.DeclarationName}}
                {
                    var checkedType = typeof(TValue);

                    {{String.Join(Consts.Nl,
                        ctx.EnumValuesPerCustomTypes!.Select(static enumValuesPerType => $$"""
                            if (checkedType == typeof({{enumValuesPerType.Key}})
                                && ({{String.Join("\r\n|| ",
                                enumValuesPerType.Select(static enumValue => $$"""
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

        static string BuildRuntimeIndexingHelpersWhenApplicable(Context ctx)
        {
            if (!ctx.Config.IndexAssignmentMode.IsRuntime)
                return "";

            if (GetLookupImplementationForIntKeys(ctx) is LookupImplementation.Dictionary)
            {
                var dictionaryType = ctx.GetDictionaryType("int");
                var returnDictionary = ctx.FeatureSupport.FrozenDictionary.IsSupported
                    ? "return System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(valuesByEnumIndex);"
                    : "return valuesByEnumIndex;";
                var addValueToDictionary = ctx.FeatureSupport.DictionaryTryAdd.IsSupported
                    ? """
                        if (!valuesByEnumIndex.TryAdd(value._enumIndex, value))
                        {
                            throw new System.InvalidOperationException(
                                $"EnumIndex {value._enumIndex} is assigned more than once.");
                        }
                        """
                    : """
                        if (valuesByEnumIndex.ContainsKey(value._enumIndex))
                        {
                            throw new System.InvalidOperationException(
                                $"EnumIndex {value._enumIndex} is assigned more than once.");
                        }

                        valuesByEnumIndex.Add(value._enumIndex, value);
                        """;

                return $$"""
                    private static {{ctx.GetNullableReferenceType(dictionaryType)}} _valuesByEnumIndex;

                    private static {{dictionaryType}} CreateRuntimeIndexDictionary()
                    {
                        var valuesByEnumIndex = new System.Collections.Generic.Dictionary<int, {{ctx.Definition.DeclarationName}}>(_allValues.Length);

                        foreach (var value in _allValues)
                        {
                            if (!value._enumIndexAssigned)
                            {
                                throw new System.InvalidOperationException(
                                    "Every declared {{ctx.Definition.DeclarationName}} value must assign EnumIndex.");
                            }

                            {{addValueToDictionary}}
                        }

                        {{returnDictionary}}
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
                                "Every declared {{ctx.Definition.DeclarationName}} value must assign EnumIndex.");
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

        static string BuildSerialization(Context ctx)
        {
            return $$"""
                private string _serializedName = null{{ctx.FeatureSupport.NullableReferenceTypes.IfSupportedOrNull("!")}};

                {{BuildDictionaryDeclaration(ctx)}}

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
                public static string Serialize({{ctx.Definition.DeclarationName}} value) => value.Serialize();

                /// <summary>Returns the source member name used to represent this declared value.</summary>
                /// <returns>The case-sensitive source member name.</returns>
                public override string ToString() => Serialize();
                
                {{BuildDeserializers(ctx)}}
                
                /// <summary>
                /// Tries to find a declared value by its case-sensitive source member name.
                /// </summary>
                /// <param name="serializedValue">The source member name to find.</param>
                /// <param name="result">The matching declared value, or <see langword="null"/> when no value matches.</param>
                /// <returns><see langword="true"/> when a value matches; otherwise, <see langword="false"/>.</returns>
                public static bool TryDeserialize({{ctx.GetNullableReferenceType("string")}} serializedValue,{{ctx.FeatureSupport.NotNullWhenAttribute.IfSupportedOrNull(" [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]")}} out {{ctx.GetNullableReferenceType(ctx.Definition.DeclarationName)}} result)
                {
                    result = Deserialize(serializedValue);
                    return {{ctx.GetIsNotNullExpression("result")}};
                }
                
                {{BuildTryDeserializeSpanOverload(ctx)}}
                
                /// <summary>
                /// Determines whether a declared value has the specified case-sensitive source member name.
                /// </summary>
                /// <param name="serializedValue">The source member name to find.</param>
                /// <returns><see langword="true"/> when a value matches; otherwise, <see langword="false"/>.</returns>
                public static bool ContainsSerializedValue({{ctx.GetNullableReferenceType("string")}} serializedValue)
                    => {{ctx.GetIsNotNullExpression("Deserialize(serializedValue)")}};
                
                {{BuildContainsSerializedSpanOverload(ctx)}}
                """;

            static string BuildTryDeserializeSpanOverload(Context ctx)
            {
                if (!ctx.FeatureSupport.SpanBasedApis.IsSupported)
                    return "";

                return $$"""
                    /// <summary>
                    /// Tries to find a declared value by its case-sensitive source member name.
                    /// </summary>
                    /// <param name="serializedValue">The source member name to find.</param>
                    /// <param name="result">The matching declared value, or <see langword="null"/> when no value matches.</param>
                    /// <returns><see langword="true"/> when a value matches; otherwise, <see langword="false"/>.</returns>
                    public static bool TryDeserialize(System.ReadOnlySpan<char> serializedValue,{{ctx.FeatureSupport.NotNullWhenAttribute.IfSupportedOrNull(" [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]")}} out {{ctx.GetNullableReferenceType(ctx.Definition.DeclarationName)}} result)
                    {
                        result = Deserialize(serializedValue);
                        return {{ctx.GetIsNotNullExpression("result")}};
                    }
                    """;
            }

            static string BuildContainsSerializedSpanOverload(Context ctx)
            {
                if (!ctx.FeatureSupport.SpanBasedApis.IsSupported)
                    return "";

                return $$"""
                    /// <summary>
                    /// Determines whether a declared value has the specified case-sensitive source member name.
                    /// </summary>
                    /// <param name="serializedValue">The source member name to find.</param>
                    /// <returns><see langword="true"/> when a value matches; otherwise, <see langword="false"/>.</returns>
                    public static bool ContainsSerializedValue(System.ReadOnlySpan<char> serializedValue)
                        => {{ctx.GetIsNotNullExpression("Deserialize(serializedValue)")}};
                    """;
            }

            static string BuildDictionaryDeclaration(Context ctx)
            {
                if (GetLookupImplementationForStringKeys(ctx) is not LookupImplementation.Dictionary)
                    return "";

                var dictAssignment = $$"""
                    private static {{ctx.GetNullableReferenceType(ctx.GetDictionaryType("string"))}} _valuesBySerializedName;
                    """;

                if (ctx.FeatureSupport.AlternateLookup.IsSupported)
                    return dictAssignment + $$"""

                        private static System.Collections.Frozen.FrozenDictionary<string, {{ctx.Definition.DeclarationName}}>.AlternateLookup<System.ReadOnlySpan<char>> _valuesBySerializedNameSpanLookup;
                        """;

                return dictAssignment;
            }

            static string BuildDeserializers(Context ctx)
            {
                if (GetLookupImplementationForStringKeys(ctx) is LookupImplementation.Dictionary)
                    return BuildDictionaryBasedDeserialization(ctx);

                return BuildIfChainBasedDeserialization(ctx);


                static string BuildDictionaryBasedDeserialization(Context ctx)
                {
                    // TODO throwing on missing dictionaries instead of defaulting to false
                    return $$"""
                        /// <summary>
                        /// Finds a declared value by its case-sensitive source member name.
                        /// </summary>
                        /// <param name="serializedValue">The source member name to find.</param>
                        /// <returns>The matching declared value, or <see langword="null"/> when no value matches.</returns>
                        public static {{ctx.GetNullableReferenceType(ctx.Definition.DeclarationName)}} Deserialize({{ctx.GetNullableReferenceType("string")}} serializedValue)
                        {
                            if (serializedValue is null)
                                return null;

                            return (_valuesBySerializedName?.TryGetValue(serializedValue, out {{ctx.GetNullableReferenceType(ctx.Definition.DeclarationName)}} value) ?? false)
                                ? value
                                : null;
                        }

                        {{BuildSpanDeserializer(ctx)}}
                        """;

                    static string BuildSpanDeserializer(Context ctx)
                    {
                        if (!ctx.FeatureSupport.SpanBasedApis.IsSupported)
                            return "";

                        return $$"""
                            /// <summary>
                            /// Finds a declared value by its case-sensitive source member name.
                            /// </summary>
                            /// <param name="serializedValue">The source member name to find.</param>
                            /// <returns>The matching declared value, or <see langword="null"/> when no value matches.</returns>
                            public static {{ctx.GetNullableReferenceType(ctx.Definition.DeclarationName)}} Deserialize(System.ReadOnlySpan<char> serializedValue)
                            {
                                {{BuildSpanDeserializationBody(ctx)}}
                            }
                            """;
                    }

                    static string BuildSpanDeserializationBody(Context ctx)
                    {
                        if (ctx.FeatureSupport.AlternateLookup.IsSupported)
                        {
                            return $$"""
                                return _valuesBySerializedNameSpanLookup.TryGetValue(serializedValue, out {{ctx.GetNullableReferenceType(ctx.Definition.DeclarationName)}} value)
                                    ? value
                                    : null;
                                """;
                        }

                        // fallback
                        return BuildIfChainSpanDeserializationBody(ctx);
                    }
                }

                static string BuildIfChainBasedDeserialization(Context ctx)
                {
                    return $$"""
                        /// <summary>
                        /// Finds a declared value by its case-sensitive source member name.
                        /// </summary>
                        /// <param name="serializedValue">The source member name to find.</param>
                        /// <returns>The matching declared value, or <see langword="null"/> when no value matches.</returns>
                        public static {{ctx.GetNullableReferenceType(ctx.Definition.DeclarationName)}} Deserialize({{ctx.GetNullableReferenceType("string")}} serializedValue)
                        {
                            if (serializedValue is null)
                                return null;

                            {{BuildStringDeserializationBody(ctx)}}
                        }

                        {{BuildSpanDeserializer(ctx)}}
                        """;

                    static string BuildStringDeserializationBody(Context ctx)
                    {
                        if (ctx.FeatureSupport.SpanBasedApis.IsSupported)
                            return "return Deserialize(System.MemoryExtensions.AsSpan(serializedValue));";

                        return $$"""
                            {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue
                                => $"if (System.String.Equals(serializedValue, {enumValue.Name}._serializedName, System.StringComparison.Ordinal)) return {enumValue.Name};")}}
                            return null;
                            """;
                    }

                    static string BuildSpanDeserializer(Context ctx)
                    {
                        if (!ctx.FeatureSupport.SpanBasedApis.IsSupported)
                            return "";

                        return $$"""
                            /// <summary>
                            /// Finds a declared value by its case-sensitive source member name.
                            /// </summary>
                            /// <param name="serializedValue">The source member name to find.</param>
                            /// <returns>The matching declared value, or <see langword="null"/> when no value matches.</returns>
                            public static {{ctx.GetNullableReferenceType(ctx.Definition.DeclarationName)}} Deserialize(System.ReadOnlySpan<char> serializedValue)
                            {
                                {{BuildIfChainSpanDeserializationBody(ctx)}}
                            }
                            """;
                    }
                }

                static string BuildIfChainSpanDeserializationBody(Context ctx)
                {
                    return $$"""
                        {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue
                            => $"if (System.MemoryExtensions.Equals(serializedValue, System.MemoryExtensions.AsSpan({enumValue.Name}._serializedName), System.StringComparison.Ordinal)) return {enumValue.Name};")}}
                        return null;
                        """;
                }
            }

        }


        static bool CheckIfShouldIncludeUtf8EncodedResources(Context ctx)
            => GetLookupImplementationForUtf8Keys(ctx) is LookupImplementation.Dictionary
                && ctx.Config.GenerateJsonConverter;

        static string BuildUtf8EncodedResourcesWhenApplicable(Context ctx)
        {
            if (!CheckIfShouldIncludeUtf8EncodedResources(ctx))
                return "";

            return $$"""
                private static {{ctx.GetNullableReferenceType($"System.Collections.Frozen.FrozenDictionary<System.ReadOnlyMemory<byte>, {ctx.Definition.DeclarationName}>")}} _valuesBySerializedUtf8Name;
                private static System.Collections.Frozen.FrozenDictionary<System.ReadOnlyMemory<byte>, {{ctx.Definition.DeclarationName}}>.AlternateLookup<System.ReadOnlySpan<byte>> {{ValuesBySerializedUtf8NameSpanLookupVariableName}};


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



        static string BuildEqualityMethods(Context ctx)
        {
            var equalsInheritanceKeywords = ctx.Definition.IsRecord && ctx.Definition.IsNotSealed
                ? " virtual"
                : "";

            return $$"""
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                private static bool AreEqual({{ctx.Definition.DeclarationName}} one, {{ctx.Definition.DeclarationName}} two)
                {
                    return object.ReferenceEquals(one, two);
                }

                /// <summary>
                /// Determines whether this instance and another value are the same object.
                /// </summary>
                /// <param name="other">The value to compare with this instance.</param>
                /// <returns><see langword="true"/> when both references identify the same object; otherwise, <see langword="false"/>.</returns>
                public{{equalsInheritanceKeywords}} bool Equals({{ctx.GetNullableReferenceType(ctx.Definition.DeclarationName)}} other)
                {
                    if (other is null)
                        return false;

                    return AreEqual(this, other);
                }

                {{BuildObjectEqualityMethodWhenApplicable(ctx)}}

                /// <inheritdoc/>
                public override int GetHashCode()
                {
                    return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
                }
                """;

            static string BuildObjectEqualityMethodWhenApplicable(Context ctx)
            {
                if (ctx.Definition.IsRecord)
                    return "";

                return $$"""
                    /// <inheritdoc/>
                    public override bool Equals({{ctx.GetNullableReferenceType("object")}} obj)
                    {
                        return Equals(obj as {{ctx.Definition.DeclarationName}});
                    }
                    """;
            }
        }



        static string BuildContainingTypesStart(Context ctx)
        {
            return string.Join(Consts.Nl, ctx.Definition.ContainersOrderedFromOuter.Select(x => $$"""
                {{x.Modifier}} partial {{x.TypeKeyword}} {{x.DeclarationName}}
                {
                """));
        }


        static string BuildContainingTypesEnd(Context ctx)
        {
            return string.Join(Consts.Nl, ctx.Definition.ContainersOrderedFromOuter.Select(x => "}"));
        }



        static string BuildRemarksWhenAppliable(Context ctx)
        {
            if (ctx.Config.DocumentationMode is DocumentationMode.Disabled)
                return "";

            if (ctx.Config.IndexAssignmentMode is not IndexAssignmentMode.CompileTime)
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
                {{ctx.GetJoinEnumValues(Consts.Nl, static enumValue => BuildValueListItem(enumValue))}}
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


        static LookupImplementation GetLookupImplementationForStringKeys(Context ctx)
            => LookupImplementationSelector.Get(
                ctx.Props,
                ctx.EnumValues.Length,
                LookupImplementationSelector.Target.StringKeys);

        static LookupImplementation GetLookupImplementationForUtf8Keys(Context ctx)
            => LookupImplementationSelector.Get(
                ctx.Props,
                ctx.EnumValues.Length,
                LookupImplementationSelector.Target.Utf8Keys);

        static LookupImplementation GetLookupImplementationForIntKeys(Context ctx)
            => LookupImplementationSelector.Get(
                ctx.Props,
                ctx.EnumValues.Length,
                LookupImplementationSelector.Target.IntKeys);
    }



    private sealed class Context
    {
        public EnumClassProps Props { get; }
        public EnumClassCollector.Definition Definition => Props.Definition;
        public Configuration Config => Props.Configuration;
        public FeatureSupportState FeatureSupport => Definition.FeatureSupport;
        public EnumValueDefinition[] EnumValues { get; }
        public IEnumerable<IGrouping<string?, EnumValueDefinition>>? EnumValuesPerCustomTypes { get; }
        public bool HasAnyCustomTypesForEnumValues => EnumValuesPerCustomTypes is not null;

        public string GetDictionaryType(string keyType)
            => FeatureSupport.FrozenDictionary.IsSupported
                ? $"System.Collections.Frozen.FrozenDictionary<{keyType}, {Definition.DeclarationName}>"
                : $"System.Collections.Generic.Dictionary<{keyType}, {Definition.DeclarationName}>";

        public static Context Parse(EnumClassProps props)
        {
            var enumValues = props.Definition.EnumValues.Definitions
                .Where(static enumValue => enumValue.IsValid)
                .ToArray();

            var enumValuesPerCustomTypes = enumValues.Any(
                static enumValue => enumValue.FullyQualifiedCustomType is not null)
                    ? enumValues
                        .GroupBy(static enumValue => enumValue.FullyQualifiedCustomType)
                        .Where(static group => group.Key is not null)
                    : null;

            return new Context(props, enumValues, enumValuesPerCustomTypes);
        }

        public string GetJoinEnumValues(
            string separator,
            Func<EnumValueDefinition, string> selector)
            => string.Join(separator, EnumValues.Select(selector));

        public string GetJoinEnumValues<TState>(
            string separator,
            TState state,
            Func<EnumValueDefinition, TState, string> selector)
        {
            var transformedValues = new string[EnumValues.Length];
            for (var i = 0; i < EnumValues.Length; i++)
                transformedValues[i] = selector(EnumValues[i], state);

            return string.Join(separator, transformedValues);
        }

        public string GetIsNotNullExpression(string expression)
            => FeatureSupport.NotPattern.IsSupported
                ? $"{expression} is not null"
                : $"{expression} != null";

        public string GetNullableReferenceType(string? typeName)
            => FeatureSupport.NullableReferenceTypes.GetNullableReferenceType(typeName);

        public string GetNullableUnconstrainedTypeParameter(string? typeName)
            => FeatureSupport.UnconstrainedTypeParameterNullableAnnotation
                .GetNullableReferenceType(typeName);


        private Context(
            EnumClassProps props,
            EnumValueDefinition[] enumValues,
            IEnumerable<IGrouping<string?, EnumValueDefinition>>? enumValuesPerCustomTypes)
        {
            Props = props;
            EnumValues = enumValues;
            EnumValuesPerCustomTypes = enumValuesPerCustomTypes;
        }
    }

    public const string ValuesBySerializedUtf8NameSpanLookupVariableName = "_valuesBySerializedUtf8NameSpanLookup";
}
