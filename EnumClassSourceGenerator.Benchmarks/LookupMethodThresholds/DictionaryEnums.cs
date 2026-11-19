using EnumClasses;

namespace EnumClassSourceGenerator.Benchmarks.LookupMethodThresholds;

public partial class DictionaryEnums
{
    [EnumClass(
        LookupMode = LookupMode.ForceDictionary,
        IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
    internal partial class EnumWith1Value
    {
        public static readonly EnumWith1Value Value001 = new() { EnumIndex = 0 };
    }

    [EnumClass(
        LookupMode = LookupMode.ForceDictionary,
        IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
    internal partial class EnumWith5Values
    {
        public static readonly EnumWith5Values Value001 = new() { EnumIndex = 0 };
        public static readonly EnumWith5Values Value002 = new() { EnumIndex = 1 };
        public static readonly EnumWith5Values Value003 = new() { EnumIndex = 2 };
        public static readonly EnumWith5Values Value004 = new() { EnumIndex = 3 };
        public static readonly EnumWith5Values Value005 = new() { EnumIndex = 4 };
    }

    [EnumClass(
        LookupMode = LookupMode.ForceDictionary,
        IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
    internal partial class EnumWith10Values
    {
        public static readonly EnumWith10Values Value001 = new() { EnumIndex = 0 };
        public static readonly EnumWith10Values Value002 = new() { EnumIndex = 1 };
        public static readonly EnumWith10Values Value003 = new() { EnumIndex = 2 };
        public static readonly EnumWith10Values Value004 = new() { EnumIndex = 3 };
        public static readonly EnumWith10Values Value005 = new() { EnumIndex = 4 };
        public static readonly EnumWith10Values Value006 = new() { EnumIndex = 5 };
        public static readonly EnumWith10Values Value007 = new() { EnumIndex = 6 };
        public static readonly EnumWith10Values Value008 = new() { EnumIndex = 7 };
        public static readonly EnumWith10Values Value009 = new() { EnumIndex = 8 };
        public static readonly EnumWith10Values Value010 = new() { EnumIndex = 9 };
    }

    [EnumClass(
        LookupMode = LookupMode.ForceDictionary,
        IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
    internal partial class EnumWith30Values
    {
        public static readonly EnumWith30Values Value001 = new() { EnumIndex = 0 };
        public static readonly EnumWith30Values Value002 = new() { EnumIndex = 1 };
        public static readonly EnumWith30Values Value003 = new() { EnumIndex = 2 };
        public static readonly EnumWith30Values Value004 = new() { EnumIndex = 3 };
        public static readonly EnumWith30Values Value005 = new() { EnumIndex = 4 };
        public static readonly EnumWith30Values Value006 = new() { EnumIndex = 5 };
        public static readonly EnumWith30Values Value007 = new() { EnumIndex = 6 };
        public static readonly EnumWith30Values Value008 = new() { EnumIndex = 7 };
        public static readonly EnumWith30Values Value009 = new() { EnumIndex = 8 };
        public static readonly EnumWith30Values Value010 = new() { EnumIndex = 9 };
        public static readonly EnumWith30Values Value011 = new() { EnumIndex = 10 };
        public static readonly EnumWith30Values Value012 = new() { EnumIndex = 11 };
        public static readonly EnumWith30Values Value013 = new() { EnumIndex = 12 };
        public static readonly EnumWith30Values Value014 = new() { EnumIndex = 13 };
        public static readonly EnumWith30Values Value015 = new() { EnumIndex = 14 };
        public static readonly EnumWith30Values Value016 = new() { EnumIndex = 15 };
        public static readonly EnumWith30Values Value017 = new() { EnumIndex = 16 };
        public static readonly EnumWith30Values Value018 = new() { EnumIndex = 17 };
        public static readonly EnumWith30Values Value019 = new() { EnumIndex = 18 };
        public static readonly EnumWith30Values Value020 = new() { EnumIndex = 19 };
        public static readonly EnumWith30Values Value021 = new() { EnumIndex = 20 };
        public static readonly EnumWith30Values Value022 = new() { EnumIndex = 21 };
        public static readonly EnumWith30Values Value023 = new() { EnumIndex = 22 };
        public static readonly EnumWith30Values Value024 = new() { EnumIndex = 23 };
        public static readonly EnumWith30Values Value025 = new() { EnumIndex = 24 };
        public static readonly EnumWith30Values Value026 = new() { EnumIndex = 25 };
        public static readonly EnumWith30Values Value027 = new() { EnumIndex = 26 };
        public static readonly EnumWith30Values Value028 = new() { EnumIndex = 27 };
        public static readonly EnumWith30Values Value029 = new() { EnumIndex = 28 };
        public static readonly EnumWith30Values Value030 = new() { EnumIndex = 29 };
    }

    [EnumClass(
        LookupMode = LookupMode.ForceDictionary,
        IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
    internal partial class EnumWith50Values
    {
        public static readonly EnumWith50Values Value001 = new() { EnumIndex = 0 };
        public static readonly EnumWith50Values Value002 = new() { EnumIndex = 1 };
        public static readonly EnumWith50Values Value003 = new() { EnumIndex = 2 };
        public static readonly EnumWith50Values Value004 = new() { EnumIndex = 3 };
        public static readonly EnumWith50Values Value005 = new() { EnumIndex = 4 };
        public static readonly EnumWith50Values Value006 = new() { EnumIndex = 5 };
        public static readonly EnumWith50Values Value007 = new() { EnumIndex = 6 };
        public static readonly EnumWith50Values Value008 = new() { EnumIndex = 7 };
        public static readonly EnumWith50Values Value009 = new() { EnumIndex = 8 };
        public static readonly EnumWith50Values Value010 = new() { EnumIndex = 9 };
        public static readonly EnumWith50Values Value011 = new() { EnumIndex = 10 };
        public static readonly EnumWith50Values Value012 = new() { EnumIndex = 11 };
        public static readonly EnumWith50Values Value013 = new() { EnumIndex = 12 };
        public static readonly EnumWith50Values Value014 = new() { EnumIndex = 13 };
        public static readonly EnumWith50Values Value015 = new() { EnumIndex = 14 };
        public static readonly EnumWith50Values Value016 = new() { EnumIndex = 15 };
        public static readonly EnumWith50Values Value017 = new() { EnumIndex = 16 };
        public static readonly EnumWith50Values Value018 = new() { EnumIndex = 17 };
        public static readonly EnumWith50Values Value019 = new() { EnumIndex = 18 };
        public static readonly EnumWith50Values Value020 = new() { EnumIndex = 19 };
        public static readonly EnumWith50Values Value021 = new() { EnumIndex = 20 };
        public static readonly EnumWith50Values Value022 = new() { EnumIndex = 21 };
        public static readonly EnumWith50Values Value023 = new() { EnumIndex = 22 };
        public static readonly EnumWith50Values Value024 = new() { EnumIndex = 23 };
        public static readonly EnumWith50Values Value025 = new() { EnumIndex = 24 };
        public static readonly EnumWith50Values Value026 = new() { EnumIndex = 25 };
        public static readonly EnumWith50Values Value027 = new() { EnumIndex = 26 };
        public static readonly EnumWith50Values Value028 = new() { EnumIndex = 27 };
        public static readonly EnumWith50Values Value029 = new() { EnumIndex = 28 };
        public static readonly EnumWith50Values Value030 = new() { EnumIndex = 29 };
        public static readonly EnumWith50Values Value031 = new() { EnumIndex = 30 };
        public static readonly EnumWith50Values Value032 = new() { EnumIndex = 31 };
        public static readonly EnumWith50Values Value033 = new() { EnumIndex = 32 };
        public static readonly EnumWith50Values Value034 = new() { EnumIndex = 33 };
        public static readonly EnumWith50Values Value035 = new() { EnumIndex = 34 };
        public static readonly EnumWith50Values Value036 = new() { EnumIndex = 35 };
        public static readonly EnumWith50Values Value037 = new() { EnumIndex = 36 };
        public static readonly EnumWith50Values Value038 = new() { EnumIndex = 37 };
        public static readonly EnumWith50Values Value039 = new() { EnumIndex = 38 };
        public static readonly EnumWith50Values Value040 = new() { EnumIndex = 39 };
        public static readonly EnumWith50Values Value041 = new() { EnumIndex = 40 };
        public static readonly EnumWith50Values Value042 = new() { EnumIndex = 41 };
        public static readonly EnumWith50Values Value043 = new() { EnumIndex = 42 };
        public static readonly EnumWith50Values Value044 = new() { EnumIndex = 43 };
        public static readonly EnumWith50Values Value045 = new() { EnumIndex = 44 };
        public static readonly EnumWith50Values Value046 = new() { EnumIndex = 45 };
        public static readonly EnumWith50Values Value047 = new() { EnumIndex = 46 };
        public static readonly EnumWith50Values Value048 = new() { EnumIndex = 47 };
        public static readonly EnumWith50Values Value049 = new() { EnumIndex = 48 };
        public static readonly EnumWith50Values Value050 = new() { EnumIndex = 49 };
    }

    [EnumClass(
        LookupMode = LookupMode.ForceDictionary,
        IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
    internal partial class EnumWith100Values
    {
        public static readonly EnumWith100Values Value001 = new() { EnumIndex = 0 };
        public static readonly EnumWith100Values Value002 = new() { EnumIndex = 1 };
        public static readonly EnumWith100Values Value003 = new() { EnumIndex = 2 };
        public static readonly EnumWith100Values Value004 = new() { EnumIndex = 3 };
        public static readonly EnumWith100Values Value005 = new() { EnumIndex = 4 };
        public static readonly EnumWith100Values Value006 = new() { EnumIndex = 5 };
        public static readonly EnumWith100Values Value007 = new() { EnumIndex = 6 };
        public static readonly EnumWith100Values Value008 = new() { EnumIndex = 7 };
        public static readonly EnumWith100Values Value009 = new() { EnumIndex = 8 };
        public static readonly EnumWith100Values Value010 = new() { EnumIndex = 9 };
        public static readonly EnumWith100Values Value011 = new() { EnumIndex = 10 };
        public static readonly EnumWith100Values Value012 = new() { EnumIndex = 11 };
        public static readonly EnumWith100Values Value013 = new() { EnumIndex = 12 };
        public static readonly EnumWith100Values Value014 = new() { EnumIndex = 13 };
        public static readonly EnumWith100Values Value015 = new() { EnumIndex = 14 };
        public static readonly EnumWith100Values Value016 = new() { EnumIndex = 15 };
        public static readonly EnumWith100Values Value017 = new() { EnumIndex = 16 };
        public static readonly EnumWith100Values Value018 = new() { EnumIndex = 17 };
        public static readonly EnumWith100Values Value019 = new() { EnumIndex = 18 };
        public static readonly EnumWith100Values Value020 = new() { EnumIndex = 19 };
        public static readonly EnumWith100Values Value021 = new() { EnumIndex = 20 };
        public static readonly EnumWith100Values Value022 = new() { EnumIndex = 21 };
        public static readonly EnumWith100Values Value023 = new() { EnumIndex = 22 };
        public static readonly EnumWith100Values Value024 = new() { EnumIndex = 23 };
        public static readonly EnumWith100Values Value025 = new() { EnumIndex = 24 };
        public static readonly EnumWith100Values Value026 = new() { EnumIndex = 25 };
        public static readonly EnumWith100Values Value027 = new() { EnumIndex = 26 };
        public static readonly EnumWith100Values Value028 = new() { EnumIndex = 27 };
        public static readonly EnumWith100Values Value029 = new() { EnumIndex = 28 };
        public static readonly EnumWith100Values Value030 = new() { EnumIndex = 29 };
        public static readonly EnumWith100Values Value031 = new() { EnumIndex = 30 };
        public static readonly EnumWith100Values Value032 = new() { EnumIndex = 31 };
        public static readonly EnumWith100Values Value033 = new() { EnumIndex = 32 };
        public static readonly EnumWith100Values Value034 = new() { EnumIndex = 33 };
        public static readonly EnumWith100Values Value035 = new() { EnumIndex = 34 };
        public static readonly EnumWith100Values Value036 = new() { EnumIndex = 35 };
        public static readonly EnumWith100Values Value037 = new() { EnumIndex = 36 };
        public static readonly EnumWith100Values Value038 = new() { EnumIndex = 37 };
        public static readonly EnumWith100Values Value039 = new() { EnumIndex = 38 };
        public static readonly EnumWith100Values Value040 = new() { EnumIndex = 39 };
        public static readonly EnumWith100Values Value041 = new() { EnumIndex = 40 };
        public static readonly EnumWith100Values Value042 = new() { EnumIndex = 41 };
        public static readonly EnumWith100Values Value043 = new() { EnumIndex = 42 };
        public static readonly EnumWith100Values Value044 = new() { EnumIndex = 43 };
        public static readonly EnumWith100Values Value045 = new() { EnumIndex = 44 };
        public static readonly EnumWith100Values Value046 = new() { EnumIndex = 45 };
        public static readonly EnumWith100Values Value047 = new() { EnumIndex = 46 };
        public static readonly EnumWith100Values Value048 = new() { EnumIndex = 47 };
        public static readonly EnumWith100Values Value049 = new() { EnumIndex = 48 };
        public static readonly EnumWith100Values Value050 = new() { EnumIndex = 49 };
        public static readonly EnumWith100Values Value051 = new() { EnumIndex = 50 };
        public static readonly EnumWith100Values Value052 = new() { EnumIndex = 51 };
        public static readonly EnumWith100Values Value053 = new() { EnumIndex = 52 };
        public static readonly EnumWith100Values Value054 = new() { EnumIndex = 53 };
        public static readonly EnumWith100Values Value055 = new() { EnumIndex = 54 };
        public static readonly EnumWith100Values Value056 = new() { EnumIndex = 55 };
        public static readonly EnumWith100Values Value057 = new() { EnumIndex = 56 };
        public static readonly EnumWith100Values Value058 = new() { EnumIndex = 57 };
        public static readonly EnumWith100Values Value059 = new() { EnumIndex = 58 };
        public static readonly EnumWith100Values Value060 = new() { EnumIndex = 59 };
        public static readonly EnumWith100Values Value061 = new() { EnumIndex = 60 };
        public static readonly EnumWith100Values Value062 = new() { EnumIndex = 61 };
        public static readonly EnumWith100Values Value063 = new() { EnumIndex = 62 };
        public static readonly EnumWith100Values Value064 = new() { EnumIndex = 63 };
        public static readonly EnumWith100Values Value065 = new() { EnumIndex = 64 };
        public static readonly EnumWith100Values Value066 = new() { EnumIndex = 65 };
        public static readonly EnumWith100Values Value067 = new() { EnumIndex = 66 };
        public static readonly EnumWith100Values Value068 = new() { EnumIndex = 67 };
        public static readonly EnumWith100Values Value069 = new() { EnumIndex = 68 };
        public static readonly EnumWith100Values Value070 = new() { EnumIndex = 69 };
        public static readonly EnumWith100Values Value071 = new() { EnumIndex = 70 };
        public static readonly EnumWith100Values Value072 = new() { EnumIndex = 71 };
        public static readonly EnumWith100Values Value073 = new() { EnumIndex = 72 };
        public static readonly EnumWith100Values Value074 = new() { EnumIndex = 73 };
        public static readonly EnumWith100Values Value075 = new() { EnumIndex = 74 };
        public static readonly EnumWith100Values Value076 = new() { EnumIndex = 75 };
        public static readonly EnumWith100Values Value077 = new() { EnumIndex = 76 };
        public static readonly EnumWith100Values Value078 = new() { EnumIndex = 77 };
        public static readonly EnumWith100Values Value079 = new() { EnumIndex = 78 };
        public static readonly EnumWith100Values Value080 = new() { EnumIndex = 79 };
        public static readonly EnumWith100Values Value081 = new() { EnumIndex = 80 };
        public static readonly EnumWith100Values Value082 = new() { EnumIndex = 81 };
        public static readonly EnumWith100Values Value083 = new() { EnumIndex = 82 };
        public static readonly EnumWith100Values Value084 = new() { EnumIndex = 83 };
        public static readonly EnumWith100Values Value085 = new() { EnumIndex = 84 };
        public static readonly EnumWith100Values Value086 = new() { EnumIndex = 85 };
        public static readonly EnumWith100Values Value087 = new() { EnumIndex = 86 };
        public static readonly EnumWith100Values Value088 = new() { EnumIndex = 87 };
        public static readonly EnumWith100Values Value089 = new() { EnumIndex = 88 };
        public static readonly EnumWith100Values Value090 = new() { EnumIndex = 89 };
        public static readonly EnumWith100Values Value091 = new() { EnumIndex = 90 };
        public static readonly EnumWith100Values Value092 = new() { EnumIndex = 91 };
        public static readonly EnumWith100Values Value093 = new() { EnumIndex = 92 };
        public static readonly EnumWith100Values Value094 = new() { EnumIndex = 93 };
        public static readonly EnumWith100Values Value095 = new() { EnumIndex = 94 };
        public static readonly EnumWith100Values Value096 = new() { EnumIndex = 95 };
        public static readonly EnumWith100Values Value097 = new() { EnumIndex = 96 };
        public static readonly EnumWith100Values Value098 = new() { EnumIndex = 97 };
        public static readonly EnumWith100Values Value099 = new() { EnumIndex = 98 };
        public static readonly EnumWith100Values Value100 = new() { EnumIndex = 99 };
    }

}
