using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
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
}
