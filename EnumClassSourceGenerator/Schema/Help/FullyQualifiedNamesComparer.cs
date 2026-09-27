using System;

namespace EnumClasses.SourceGenerators.Schema.Help;

internal static class FullyQualifiedNamesComparer
{
    public static bool AreEqual(ReadOnlySpan<char> name1, ReadOnlySpan<char> name2)
    {
        var name1Prefixless = GetPrefixless(name1);
        var name2Prefixless = GetPrefixless(name2);

        return name1Prefixless.Equals(name2Prefixless, StringComparison.Ordinal);

        static ReadOnlySpan<char> GetPrefixless(ReadOnlySpan<char> name)
            => name.StartsWith(_globalPrefix)
                ? name.Slice(_globalPrefix.Length)
                : name;
    }

    private const string _globalPrefix = "global::";
}
