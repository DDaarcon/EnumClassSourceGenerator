using EnumClasses.SourceGenerators.Schema;

namespace EnumClasses.SourceGenerators.Deserialization;

internal static class DeserializationMethodProvider
{
    public static DeserializationMethod Get(EnumClass.Definition props, int enumValuesCount)
    {
        return props.Config.DeserializationMode switch
        {
            DeserializationMode.ForceIfChain => DeserializationMethod.IfChain,
            DeserializationMode.ForceDictionary => DeserializationMethod.Dictionary,
            DeserializationMode.Optimized or _ => GetOptimizedMethod(enumValuesCount)
        };
    }

    private const int _dictionaryAdvantageThreashold = 8;
    private static DeserializationMethod GetOptimizedMethod(int enumValuesCount)
    {
        if (enumValuesCount < _dictionaryAdvantageThreashold)
            return DeserializationMethod.IfChain;
        return DeserializationMethod.Dictionary;
    }
}
