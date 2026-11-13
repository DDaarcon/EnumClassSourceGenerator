using static EnumClasses.SourceGenerators.FeatureSupport.FeatureSupportState;

namespace EnumClasses.SourceGenerators.FeatureSupport;

internal record struct FeatureSupportState(
    Status AlternateLookup,
    Status Utf8StringLiteral,
    Status NullableReferenceTypes,
    Status UnconstrainedTypeParameterNullableAnnotation,
    Status SwitchExpression,
    Status SpanBasedApis,
    Status NotPattern,
    Status RequiredKeyword,
    Status InitKeyword)
{
    internal enum Status
    {
        Supported,
        Unsupported
    }
}
