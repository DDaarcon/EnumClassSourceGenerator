using EnumClasses.SourceGenerators.Schema;

namespace EnumClasses.SourceGenerators.Deserialization;

internal static class SearchMethodProvider
{
    public static SearchMethod Get(EnumClass.Definition props, int enumValuesCount)
    {
        return props.Config.SearchMode switch
        {
            SearchMode.ForceIfChain => SearchMethod.IfChain,
            SearchMode.ForceDictionary => SearchMethod.Dictionary,
            SearchMode.Optimized or _ => GetOptimizedMethod(enumValuesCount)
        };
    }

    private const int _dictionaryAdvantageThreashold = 8;
    private static SearchMethod GetOptimizedMethod(int enumValuesCount)
    {
        if (enumValuesCount < _dictionaryAdvantageThreashold)
            return SearchMethod.IfChain;
        return SearchMethod.Dictionary;
    }
}
