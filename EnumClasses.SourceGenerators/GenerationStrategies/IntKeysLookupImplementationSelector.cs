using EnumClasses.SourceGenerators.Schema;

namespace EnumClasses.SourceGenerators.GenerationStrategies;

internal class IntKeysLookupImplementationSelector
{
    public static LookupImplementation Get(EnumClassCollector.Definition.WithConfig props, int enumValuesCount)
    {
        var fromConfig = LookupImplementationSelection.GetForcedOrUndefined(props);
        if (fromConfig is not LookupImplementation.Undefined)
            return fromConfig;

        if (enumValuesCount <= _dictionaryImplThreshold)
            return LookupImplementation.IfChain;

        return LookupImplementation.Dictionary;
    }

    private const int _dictionaryImplThreshold = 2; // based on results of a benchmark
}
