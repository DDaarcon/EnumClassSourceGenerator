using static EnumClasses.SourceGenerators.FeatureSupport.FeatureSupportState;

namespace EnumClasses.SourceGenerators.FeatureSupport;

internal record struct FeatureSupportState(
    Status AlternateLookup,
    Status RequiredKeyword,
    Status InitKeyword)
{
    internal enum Status
    {
        Supported,
        Unsupported
    }
}
