using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using SupportStatus = EnumClasses.SourceGenerators.FeatureSupport.FeatureSupportState.Status;


namespace EnumClasses.SourceGenerators.FeatureSupport;

internal static class FeaturesSupportResolver
{
    internal static FeatureSupportState GetSupportState(
        Compilation compilation,
        CSharpParseOptions parseOptions)
    {
        var spanBasedApis = GetSpanBasedApisSupport(compilation, parseOptions);
        var frozenDictionary = GetFrozenDictionarySupport(compilation);

        return new FeatureSupportState(
            AlternateLookup: GetAlternateLookupSupport(compilation, parseOptions, frozenDictionary),
            DictionaryTryAdd: GetDictionaryTryAddSupport(compilation),
            FrozenDictionary: frozenDictionary,
            Utf8StringLiteral: GetUtf8StringLiteralSupport(parseOptions),
            NullableReferenceTypes: GetNullableReferenceTypesSupport(parseOptions),
            UnconstrainedTypeParameterNullableAnnotation:
                GetUnconstrainedTypeParameterNullableAnnotationSupport(parseOptions),
            SwitchExpression: GetSwitchExpressionSupport(parseOptions),
            SpanBasedApis: spanBasedApis,
            JsonConverter: GetJsonConverterSupport(compilation, spanBasedApis),
            NotPattern: GetNotPatternSupport(parseOptions),
            RequiredKeyword: GetRequiredKeywordSupport(compilation, parseOptions),
            InitKeyword: GetInitKeywordSupport(compilation, parseOptions),
            NotNullWhenAttribute: GetNotNullWhenAttributeSupport(compilation));
    }

    /// <summary>
    /// .NET 9+
    /// </summary>
    private static SupportStatus GetAlternateLookupSupport(
        Compilation compilation,
        CSharpParseOptions parseOptions,
        SupportStatus frozenDictionarySupport)
    {
        if (parseOptions.LanguageVersion < LanguageVersion.CSharp13
            || !frozenDictionarySupport.IsSupported)
            return SupportStatus.Unsupported;

        var alternateComparer = compilation.GetTypeByMetadataName(
            "System.Collections.Generic.IAlternateEqualityComparer`2");

        if (alternateComparer is null)
            return SupportStatus.Unsupported;

        var frozenDictionary = compilation.GetTypeByMetadataName(
            "System.Collections.Frozen.FrozenDictionary`2");

        if (frozenDictionary is null)
            return SupportStatus.Unsupported;

        if (!frozenDictionary
            .GetMembers("GetAlternateLookup")
            .OfType<IMethodSymbol>()
            .Any(method =>
                method.Arity == 1 &&
                method.Parameters.Length == 0))
        {
            return SupportStatus.Unsupported;
        }

        return SupportStatus.Supported;
    }

    private static SupportStatus GetFrozenDictionarySupport(Compilation compilation)
    {
        var dict = compilation.GetTypeByMetadataName("System.Collections.Frozen.FrozenDictionary`2");
        if (dict is null)
            return SupportStatus.Unsupported;

        var dictTools = compilation.GetTypeByMetadataName("System.Collections.Frozen.FrozenDictionary");
        if (dictTools is null)
            return SupportStatus.Unsupported;

        var hasKeyValuePairOverload = dictTools.HasMethod(
            "ToFrozenDictionary",
            static method => method.TypeParameters.Length == 2
                && method.Parameters.Length == 2
                && method.Parameters[0].Type is INamedTypeSymbol enumerable
                && enumerable.MetadataName == "IEnumerable`1"
                && enumerable.TypeArguments[0].MetadataName == "KeyValuePair`2"
                && method.Parameters[1].Type.MetadataName == "IEqualityComparer`1"
                && method.Parameters[1].IsOptional);
        var hasKeySelectorOverload = dictTools.HasMethod(
            "ToFrozenDictionary",
            static method => method.TypeParameters.Length == 2
                && method.Parameters.Length == 3
                && method.Parameters[0].Type.MetadataName == "IEnumerable`1"
                && method.Parameters[1].Type.MetadataName == "Func`2"
                && method.Parameters[2].Type.MetadataName == "IEqualityComparer`1"
                && method.Parameters[2].IsOptional);

        if (!hasKeyValuePairOverload || !hasKeySelectorOverload)
        {
            return SupportStatus.Unsupported;
        }

        return SupportStatus.Supported;
    }

    private static SupportStatus GetDictionaryTryAddSupport(Compilation compilation)
    {
        var dictionary = compilation.GetTypeByMetadataName(
            "System.Collections.Generic.Dictionary`2");

        if (dictionary is null)
            return SupportStatus.Unsupported;

        return dictionary
            .HasMethod(
                "TryAdd",
                static method
                    => method.Parameters.Length == 2
                        && method.ReturnType.SpecialType == SpecialType.System_Boolean)
            ? SupportStatus.Supported
            : SupportStatus.Unsupported;
    }

    private static SupportStatus GetNotNullWhenAttributeSupport(Compilation compilation)
        => HasAccessibleType(
            compilation,
            "System.Diagnostics.CodeAnalysis.NotNullWhenAttribute");

    private static SupportStatus GetUtf8StringLiteralSupport(
        CSharpParseOptions parseOptions)
        => parseOptions.SupportedIfAtLeast(LanguageVersion.CSharp11);

    private static SupportStatus GetNullableReferenceTypesSupport(
        CSharpParseOptions parseOptions)
        => parseOptions.SupportedIfAtLeast(LanguageVersion.CSharp8);

    private static SupportStatus GetUnconstrainedTypeParameterNullableAnnotationSupport(
        CSharpParseOptions parseOptions)
        => parseOptions.SupportedIfAtLeast(LanguageVersion.CSharp9);

    private static SupportStatus GetSwitchExpressionSupport(
        CSharpParseOptions parseOptions)
        => parseOptions.SupportedIfAtLeast(LanguageVersion.CSharp8);

    private static SupportStatus GetSpanBasedApisSupport(
        Compilation compilation,
        CSharpParseOptions parseOptions)
    {
        if (parseOptions.LanguageVersion < LanguageVersion.CSharp7_2)
            return SupportStatus.Unsupported;

        if (compilation.GetTypeByMetadataName("System.ReadOnlySpan`1") is null
            || compilation.GetTypeByMetadataName("System.MemoryExtensions") is null)
        {
            return SupportStatus.Unsupported;
        }

        return SupportStatus.Supported;
    }

    private static SupportStatus GetJsonConverterSupport(
        Compilation compilation,
        SupportStatus spanBasedApis)
    {
        if (!spanBasedApis.IsSupported)
            return SupportStatus.Unsupported;

        string[] requiredTypes =
        [
            "System.Text.Json.JsonException",
            "System.Text.Json.JsonSerializerOptions",
            "System.Text.Json.JsonTokenType",
            "System.Text.Json.Utf8JsonWriter",
            "System.Text.Json.Serialization.JsonConverter`1",
            "System.Text.Json.Serialization.JsonConverterAttribute"
        ];

        foreach (var typeName in requiredTypes)
        {
            if (compilation.GetTypeByMetadataName(typeName) is null)
                return SupportStatus.Unsupported;
        }

        var reader = compilation.GetTypeByMetadataName(
            "System.Text.Json.Utf8JsonReader");
        if (reader is null)
            return SupportStatus.Unsupported;

        if (!reader.HasMethod(
                "ValueTextEquals",
                static method => method.Parameters.Length == 1 && method.ReturnType.SpecialType == SpecialType.System_Boolean)
            || !reader.HasMethod(
                "CopyString",
                static method => method.Parameters.Length == 1 && method.ReturnType.SpecialType == SpecialType.System_Int32)
            || !reader.HasProperty("TokenType")
            || !reader.HasProperty("HasValueSequence")
            || !reader.HasProperty("ValueIsEscaped")
            || !reader.HasProperty("ValueSpan"))
        {
            return SupportStatus.Unsupported;
        }



        return SupportStatus.Supported;
    }

    private static SupportStatus GetNotPatternSupport(
        CSharpParseOptions parseOptions)
        => parseOptions.SupportedIfAtLeast(LanguageVersion.CSharp9);

    private static SupportStatus GetRequiredKeywordSupport(
        Compilation compilation,
        CSharpParseOptions parseOptions)
    {
        if (parseOptions.LanguageVersion < LanguageVersion.CSharp11)
            return SupportStatus.Unsupported;

        if (!HasAccessibleType(
                compilation,
                "System.Runtime.CompilerServices.RequiredMemberAttribute").IsSupported
            || !HasAccessibleType(
                compilation,
                "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute").IsSupported)
        {
            return SupportStatus.Unsupported;
        }

        return SupportStatus.Supported;
    }

    private static SupportStatus GetInitKeywordSupport(
        Compilation compilation,
        CSharpParseOptions parseOptions)
    {
        if (parseOptions.LanguageVersion >= LanguageVersion.CSharp9
            && HasAccessibleType(
                compilation,
                "System.Runtime.CompilerServices.IsExternalInit").IsSupported)
        {
            return SupportStatus.Supported;
        }
        return SupportStatus.Unsupported;
    }

    private static SupportStatus HasAccessibleType(
        Compilation compilation,
        string metadataName)
    {
        var type = compilation.GetTypeByMetadataName(metadataName);

        return type is not null
            && compilation.IsSymbolAccessibleWithin(type, compilation.Assembly)
                ? SupportStatus.Supported
                : SupportStatus.Unsupported;
    }
}
