using EnumClasses.SourceGenerators.Configurations;
using System;

namespace EnumClasses.SourceGenerators.Deserialization;

internal static class SearchMethodProvider
{
    public enum Target
    {
        StringKeys,
        Utf8Keys
    }

    public static SearchMethod Get(Configuration config, int enumValuesCount, Target target)
    {
        return config.SearchMode switch
        {
            SearchMode.ForceIfChain => SearchMethod.IfChain,
            SearchMode.ForceDictionary => SearchMethod.Dictionary,
            SearchMode.Optimized or _ => GetOptimizedMethod(enumValuesCount, target)
        };
    }

    private const int _dictAdvThresholdForString = 8;
    private const int _dictAdvThresholdForUtf8 = 16;
    private static SearchMethod GetOptimizedMethod(int enumValuesCount, Target target)
    {
        int limit = target switch
        {
            Target.StringKeys => _dictAdvThresholdForString,
            Target.Utf8Keys => _dictAdvThresholdForUtf8,
            _ => throw new InvalidOperationException($"{target} is not a valid value for {nameof(Target)}")
        };

        if (enumValuesCount <= limit)
            return SearchMethod.IfChain;
        return SearchMethod.Dictionary;
    }
}
