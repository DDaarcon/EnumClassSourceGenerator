using EnumClasses.SourceGenerators.Schema;
using System;

namespace EnumClasses.SourceGenerators.Deserialization;

internal static class LookupImplementationSelector
{
    public enum Target
    {
        StringKeys,
        Utf8Keys
    }

    public static LookupImplementation Get(EnumClassCollector.Definition.WithConfig props, int enumValuesCount, Target target)
    {
        return props.Configuration.LookupMode switch
        {
            LookupMode.ForceIfChain => LookupImplementation.IfChain,
            LookupMode.ForceDictionary => LookupImplementation.Dictionary,
            LookupMode.Automatic or _ => GetAutomaticImplementation(props, enumValuesCount, target)
        };
    }

    private const int _dictAdvThresholdForString = 8;
    private const int _dictAdvThresholdForUtf8 = 16;
    private static LookupImplementation GetAutomaticImplementation(EnumClassCollector.Definition.WithConfig props, int enumValuesCount, Target target)
    {
        if (!props.Definition.Meta.IsAlternateLookupSupported)
            return LookupImplementation.IfChain; // TODO .NET 8 is outside of the scope of Alternalte lookups, a different solution is needed to replace if's on a large number of values

        int limit = target switch
        {
            Target.StringKeys => _dictAdvThresholdForString,
            Target.Utf8Keys => _dictAdvThresholdForUtf8,
            _ => throw new InvalidOperationException($"{target} is not a valid value for {nameof(Target)}")
        };

        if (enumValuesCount <= limit)
            return LookupImplementation.IfChain;
        return LookupImplementation.Dictionary;
    }
}
