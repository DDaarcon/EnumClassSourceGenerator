using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnumClasses.SourceGenerators.FeatureSupport;

internal static class FeatureSupportStateExtensions
{
    extension(FeatureSupportState.Status status)
    {
        public bool IsSupported => status is FeatureSupportState.Status.Supported;

        public string? IfSupportedOrNull(string value)
            => status.IsSupported ? value : null;

        public string GetNullableReferenceType(string? typeName)
        {
            if (typeName is null)
                throw new ArgumentNullException(nameof(typeName));

            return status.IsSupported ? $"{typeName}?" : typeName;
        }
    }

    extension(CSharpParseOptions options)
    {
        public FeatureSupportState.Status SupportedIfAtLeast(LanguageVersion version)
            => options.LanguageVersion >= version
                ? FeatureSupportState.Status.Supported
                : FeatureSupportState.Status.Unsupported;
    }

    extension(INamedTypeSymbol? symbol)
    {
        public bool HasMethod(string name, Func<IMethodSymbol, bool> predicate)
            => symbol?.GetMembers(name)
                .OfType<IMethodSymbol>()
                .Any(predicate) ?? false;

        public bool HasProperty(string name, Func<IPropertySymbol, bool> predicate)
            => symbol?.GetMembers(name)
                .OfType<IPropertySymbol>()
                .Any(predicate) ?? false;

        public bool HasProperty(string name)
            => symbol?.GetMembers(name)
                .OfType<IPropertySymbol>()
                .Any() ?? false;
    }
}
