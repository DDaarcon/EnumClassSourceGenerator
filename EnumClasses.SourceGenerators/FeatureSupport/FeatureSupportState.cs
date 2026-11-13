using static EnumClasses.SourceGenerators.FeatureSupport.FeatureSupportState;

namespace EnumClasses.SourceGenerators.FeatureSupport;

internal record struct FeatureSupportState(
    Status AlternateLookup,
    Status DictionaryTryAdd,
    Status FrozenDictionary,
    Status Utf8StringLiteral,
    Status NullableReferenceTypes,
    Status UnconstrainedTypeParameterNullableAnnotation,
    Status SwitchExpression,
    Status SpanBasedApis,
    Status JsonConverter,
    Status NotPattern,
    Status RequiredKeyword,
    Status InitKeyword,
    Status NotNullWhenAttribute)
{
    internal enum Status
    {
        Supported,
        Unsupported
    }
}
