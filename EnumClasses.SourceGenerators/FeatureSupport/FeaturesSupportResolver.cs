using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SupportStatus = EnumClasses.SourceGenerators.FeatureSupport.FeatureSupportState.Status;


namespace EnumClasses.SourceGenerators.FeatureSupport;

internal static class FeaturesSupportResolver
{
    internal static FeatureSupportState GetSupportState(
        Compilation compilation,
        CSharpParseOptions parseOptions)
    {
        return new FeatureSupportState(
            AlternateLookup: GetAlternateLookupSupport(compilation, parseOptions),
            RequiredKeyword: GetRequiredKeywordSupport(parseOptions),
            InitKeyword: GetInitKeywordSupport(compilation, parseOptions));
    }

    /// <summary>
    /// .NET 9+
    /// </summary>
    private static SupportStatus GetAlternateLookupSupport(
        Compilation compilation,
        CSharpParseOptions parseOptions)
    {
        if (parseOptions.LanguageVersion < LanguageVersion.CSharp13)
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

    private static SupportStatus GetRequiredKeywordSupport(
        CSharpParseOptions parseOptions)
    {
        if (parseOptions.LanguageVersion >= LanguageVersion.CSharp12)
            return SupportStatus.Supported;
        return SupportStatus.Unsupported;
    }

    private static SupportStatus GetInitKeywordSupport(
        Compilation compilation,
        CSharpParseOptions parseOptions)
    {
        if (parseOptions.LanguageVersion >= LanguageVersion.CSharp9
            && compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.IsExternalInit") != null)
        {
            return SupportStatus.Supported;
        }
        return SupportStatus.Unsupported;
    }
}
