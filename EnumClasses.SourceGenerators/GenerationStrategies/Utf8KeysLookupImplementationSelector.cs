using EnumClasses.SourceGenerators.FeatureSupport;
using EnumClasses.SourceGenerators.Schema;

namespace EnumClasses.SourceGenerators.GenerationStrategies;

internal class Utf8KeysLookupImplementationSelector
{
    public static LookupImplementation Get(EnumClassCollector.Definition.WithConfig props, int enumValuesCount)
    {
        if (!props.Definition.FeatureSupport.AlternateLookup.IsSupported)
            return LookupImplementation.IfChain; // TODO .NET 8 is outside of the scope of Alternalte lookups, a different solution is needed to replace if's on a large number of values

        var fromConfig = LookupImplementationSelection.GetForcedOrUndefined(props);
        if (fromConfig is not LookupImplementation.Undefined)
            return fromConfig;

        if (enumValuesCount <= _dictionaryImplThreshold)
            return LookupImplementation.IfChain;

        return LookupImplementation.Dictionary;
    }

    private const int _dictionaryImplThreshold = 24; // based on results of a benchmark
}
