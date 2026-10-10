using static EnumClasses.SourceGenerators.FeatureSupport.FeatureSupportState;

namespace EnumClasses.SourceGenerators.FeatureSupport;

internal record struct FeatureSupportState(
    Status AlternateLookup,
    Status RequiredKeyword, // TODO apply in template
    Status InitKeyword) // TODO apply in template
{
    internal enum Status
    {
        Supported,
        Unsupported
    }
}
