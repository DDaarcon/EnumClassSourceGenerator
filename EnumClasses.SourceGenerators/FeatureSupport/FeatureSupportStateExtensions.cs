using System;
using System.Collections.Generic;
using System.Text;

namespace EnumClasses.SourceGenerators.FeatureSupport;

internal static class FeatureSupportStateExtensions
{
    extension(FeatureSupportState.Status status)
    {
        public bool IsSupported => status is FeatureSupportState.Status.Supported;
    }
}
