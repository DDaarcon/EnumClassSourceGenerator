using EnumClasses.SourceGenerators.FeatureSupport;
using EnumClasses.SourceGenerators.Schema;
using System;

namespace EnumClasses.SourceGenerators.GenerationStrategies;

internal static class LookupImplementationSelector
{
    public enum Target
    {
        StringKeys,
        Utf8Keys,
        IntKeys
    }

    public static LookupImplementation Get(EnumClassCollector.Definition.WithConfig props, int enumValuesCount, Target target)
    {
        if (target is Target.Utf8Keys && !props.Definition.FeatureSupport.AlternateLookup.IsSupported)
            return LookupImplementation.IfChain; // TODO .NET 8 is outside of the scope of Alternalte lookups, a different solution is needed to replace if's on a large number of values

        var implementation = props.Configuration.LookupMode switch
        {
            LookupMode.ForceIfChain => LookupImplementation.IfChain,
            LookupMode.ForceDictionary => LookupImplementation.Dictionary,
            LookupMode.Automatic or _ => GetAutomaticImplementation(props, enumValuesCount, target)
        };

        return implementation;
    }

    // TODO confirm thresholds
    private const int _dictAdvThresholdForString = 8;
    private const int _dictAdvThresholdForUtf8 = 16;
    private const int _dictAdvThresholdForInt = 12;
    private static LookupImplementation GetAutomaticImplementation(EnumClassCollector.Definition.WithConfig props, int enumValuesCount, Target target)
    {
        int limit = target switch
        {
            Target.StringKeys => _dictAdvThresholdForString,
            Target.Utf8Keys => _dictAdvThresholdForUtf8,
            Target.IntKeys => _dictAdvThresholdForInt,
            _ => throw new InvalidOperationException($"{target} is not a valid value for {nameof(Target)}")
        };

        if (enumValuesCount <= limit)
            return LookupImplementation.IfChain;

        return LookupImplementation.Dictionary;
    }
}
