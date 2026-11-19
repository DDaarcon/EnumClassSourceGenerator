using EnumClasses.SourceGenerators.Schema;

namespace EnumClasses.SourceGenerators.GenerationStrategies;

internal static class LookupImplementationSelection
{
    // TODO benchmark tresholds

    internal static LookupImplementation GetForcedOrUndefined(EnumClassCollector.Definition.WithConfig props)
        => props.Configuration.LookupMode switch
        {
            LookupMode.ForceIfChain => LookupImplementation.IfChain,
            LookupMode.ForceDictionary => LookupImplementation.Dictionary,
            LookupMode.Automatic or _ => LookupImplementation.Undefined
        };
}
