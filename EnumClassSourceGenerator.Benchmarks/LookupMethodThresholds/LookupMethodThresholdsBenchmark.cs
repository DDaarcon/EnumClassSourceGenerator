using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Text;
using System.Text.Json;

namespace EnumClassSourceGenerator.Benchmarks.LookupMethodThresholds;

[MemoryDiagnoser(false)]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class LookupMethodThresholdsBenchmark
{
    private const int OperationsPerInvoke = 1_024;

    private byte[][] _utf8Inputs = null!;
    private int[] _intInputs = null!;
    private string[] _stringInputs = null!;
    private int _expectedChecksum;

    [Params(1, 5, 10, 30, 50, 100)]
    public int ValueCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(69);

        _expectedChecksum = 0;
        _intInputs = new int[OperationsPerInvoke];
        _stringInputs = new string[OperationsPerInvoke];
        _utf8Inputs = new byte[OperationsPerInvoke][];

        for (var index = 0; index < OperationsPerInvoke; index++)
        {
            var enumIndex = random.Next(ValueCount);
            var serializedName = $"Value{enumIndex + 1:D3}";

            _intInputs[index] = enumIndex;
            _stringInputs[index] = serializedName;
            _utf8Inputs[index] = Encoding.UTF8.GetBytes($"\"{serializedName}\"");
            _expectedChecksum += enumIndex;
        }

        ValidateSelectedPair();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
    [BenchmarkCategory("StringKey")]
    public int StringKey_IfChain()
        => ValueCount switch
        {
            1 => LookupStringKeys<IfChain1>(),
            5 => LookupStringKeys<IfChain5>(),
            10 => LookupStringKeys<IfChain10>(),
            30 => LookupStringKeys<IfChain30>(),
            50 => LookupStringKeys<IfChain50>(),
            100 => LookupStringKeys<IfChain100>(),
            _ => throw InvalidValueCount()
        };

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    [BenchmarkCategory("StringKey")]
    public int StringKey_Dictionary()
        => ValueCount switch
        {
            1 => LookupStringKeys<Dictionary1>(),
            5 => LookupStringKeys<Dictionary5>(),
            10 => LookupStringKeys<Dictionary10>(),
            30 => LookupStringKeys<Dictionary30>(),
            50 => LookupStringKeys<Dictionary50>(),
            100 => LookupStringKeys<Dictionary100>(),
            _ => throw InvalidValueCount()
        };

    [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
    [BenchmarkCategory("Utf8Key")]
    public int Utf8Key_IfChain()
        => ValueCount switch
        {
            1 => LookupUtf8Keys<IfChain1>(),
            5 => LookupUtf8Keys<IfChain5>(),
            10 => LookupUtf8Keys<IfChain10>(),
            30 => LookupUtf8Keys<IfChain30>(),
            50 => LookupUtf8Keys<IfChain50>(),
            100 => LookupUtf8Keys<IfChain100>(),
            _ => throw InvalidValueCount()
        };

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    [BenchmarkCategory("Utf8Key")]
    public int Utf8Key_Dictionary()
        => ValueCount switch
        {
            1 => LookupUtf8Keys<Dictionary1>(),
            5 => LookupUtf8Keys<Dictionary5>(),
            10 => LookupUtf8Keys<Dictionary10>(),
            30 => LookupUtf8Keys<Dictionary30>(),
            50 => LookupUtf8Keys<Dictionary50>(),
            100 => LookupUtf8Keys<Dictionary100>(),
            _ => throw InvalidValueCount()
        };

    [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
    [BenchmarkCategory("IntKey")]
    public int IntKey_IfChain()
        => ValueCount switch
        {
            1 => LookupIntKeys<IfChain1>(),
            5 => LookupIntKeys<IfChain5>(),
            10 => LookupIntKeys<IfChain10>(),
            30 => LookupIntKeys<IfChain30>(),
            50 => LookupIntKeys<IfChain50>(),
            100 => LookupIntKeys<IfChain100>(),
            _ => throw InvalidValueCount()
        };

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    [BenchmarkCategory("IntKey")]
    public int IntKey_Dictionary()
        => ValueCount switch
        {
            1 => LookupIntKeys<Dictionary1>(),
            5 => LookupIntKeys<Dictionary5>(),
            10 => LookupIntKeys<Dictionary10>(),
            30 => LookupIntKeys<Dictionary30>(),
            50 => LookupIntKeys<Dictionary50>(),
            100 => LookupIntKeys<Dictionary100>(),
            _ => throw InvalidValueCount()
        };

    private int LookupStringKeys<TLookup>()
        where TLookup : ILookup
    {
        var checksum = 0;

        for (var index = 0; index < _stringInputs.Length; index++)
            checksum += TLookup.LookupStringKey(_stringInputs[index]);

        return checksum;
    }

    private int LookupUtf8Keys<TLookup>()
        where TLookup : ILookup
    {
        var checksum = 0;

        for (var index = 0; index < _utf8Inputs.Length; index++)
            checksum += TLookup.LookupUtf8Key(_utf8Inputs[index]);

        return checksum;
    }

    private int LookupIntKeys<TLookup>()
        where TLookup : ILookup
    {
        var checksum = 0;

        for (var index = 0; index < _intInputs.Length; index++)
            checksum += TLookup.LookupIntKey(_intInputs[index]);

        return checksum;
    }

    private void ValidateSelectedPair()
    {
        switch (ValueCount)
        {
            case 1: ValidatePair<IfChain1, Dictionary1>(); break;
            case 5: ValidatePair<IfChain5, Dictionary5>(); break;
            case 10: ValidatePair<IfChain10, Dictionary10>(); break;
            case 30: ValidatePair<IfChain30, Dictionary30>(); break;
            case 50: ValidatePair<IfChain50, Dictionary50>(); break;
            case 100: ValidatePair<IfChain100, Dictionary100>(); break;
            default: throw InvalidValueCount();
        }
    }

    private void ValidatePair<TIfChain, TDictionary>()
        where TIfChain : ILookup
        where TDictionary : ILookup
    {
        if (LookupStringKeys<TIfChain>() != _expectedChecksum ||
            LookupStringKeys<TDictionary>() != _expectedChecksum ||
            LookupUtf8Keys<TIfChain>() != _expectedChecksum ||
            LookupUtf8Keys<TDictionary>() != _expectedChecksum ||
            LookupIntKeys<TIfChain>() != _expectedChecksum ||
            LookupIntKeys<TDictionary>() != _expectedChecksum)
        {
            throw new InvalidOperationException("A lookup fixture returned an unexpected value.");
        }
    }

    private InvalidOperationException InvalidValueCount()
        => new($"Unsupported value count: {ValueCount}.");

    private interface ILookup
    {
        static abstract int LookupStringKey(string key);
        static abstract int LookupUtf8Key(byte[] json);
        static abstract int LookupIntKey(int key);
    }

    private readonly struct IfChain1 : ILookup
    {
        public static int LookupStringKey(string key) => IfChainEnums.EnumWith1Value.Deserialize(key)!.EnumIndex;
        public static int LookupUtf8Key(byte[] json) => ReadUtf8(json).EnumIndex;
        public static int LookupIntKey(int key) => IfChainEnums.EnumWith1Value.GetByEnumIndex(key)!.EnumIndex;

        private static IfChainEnums.EnumWith1Value ReadUtf8(byte[] json)
        {
            var reader = CreateReader(json);
            return _converter.Read(ref reader, typeof(IfChainEnums.EnumWith1Value), null!)!;
        }

        private static readonly IfChainEnums.EnumWith1Value.JsonConverter _converter = new();
    }

    private readonly struct IfChain5 : ILookup
    {
        public static int LookupStringKey(string key) => IfChainEnums.EnumWith5Values.Deserialize(key)!.EnumIndex;
        public static int LookupUtf8Key(byte[] json) => ReadUtf8(json).EnumIndex;
        public static int LookupIntKey(int key) => IfChainEnums.EnumWith5Values.GetByEnumIndex(key)!.EnumIndex;

        private static IfChainEnums.EnumWith5Values ReadUtf8(byte[] json)
        {
            var reader = CreateReader(json);
            return _converter.Read(ref reader, typeof(IfChainEnums.EnumWith5Values), null!)!;
        }

        private static readonly IfChainEnums.EnumWith5Values.JsonConverter _converter = new();
    }

    private readonly struct IfChain10 : ILookup
    {
        public static int LookupStringKey(string key) => IfChainEnums.EnumWith10Values.Deserialize(key)!.EnumIndex;
        public static int LookupUtf8Key(byte[] json) => ReadUtf8(json).EnumIndex;
        public static int LookupIntKey(int key) => IfChainEnums.EnumWith10Values.GetByEnumIndex(key)!.EnumIndex;

        private static IfChainEnums.EnumWith10Values ReadUtf8(byte[] json)
        {
            var reader = CreateReader(json);
            return _converter.Read(ref reader, typeof(IfChainEnums.EnumWith10Values), null!)!;
        }

        private static readonly IfChainEnums.EnumWith10Values.JsonConverter _converter = new();
    }

    private readonly struct IfChain30 : ILookup
    {
        public static int LookupStringKey(string key) => IfChainEnums.EnumWith30Values.Deserialize(key)!.EnumIndex;
        public static int LookupUtf8Key(byte[] json) => ReadUtf8(json).EnumIndex;
        public static int LookupIntKey(int key) => IfChainEnums.EnumWith30Values.GetByEnumIndex(key)!.EnumIndex;

        private static IfChainEnums.EnumWith30Values ReadUtf8(byte[] json)
        {
            var reader = CreateReader(json);
            return _converter.Read(ref reader, typeof(IfChainEnums.EnumWith30Values), null!)!;
        }

        private static readonly IfChainEnums.EnumWith30Values.JsonConverter _converter = new();
    }

    private readonly struct IfChain50 : ILookup
    {
        public static int LookupStringKey(string key) => IfChainEnums.EnumWith50Values.Deserialize(key)!.EnumIndex;
        public static int LookupUtf8Key(byte[] json) => ReadUtf8(json).EnumIndex;
        public static int LookupIntKey(int key) => IfChainEnums.EnumWith50Values.GetByEnumIndex(key)!.EnumIndex;

        private static IfChainEnums.EnumWith50Values ReadUtf8(byte[] json)
        {
            var reader = CreateReader(json);
            return _converter.Read(ref reader, typeof(IfChainEnums.EnumWith50Values), null!)!;
        }

        private static readonly IfChainEnums.EnumWith50Values.JsonConverter _converter = new();
    }

    private readonly struct IfChain100 : ILookup
    {
        public static int LookupStringKey(string key) => IfChainEnums.EnumWith100Values.Deserialize(key)!.EnumIndex;
        public static int LookupUtf8Key(byte[] json) => ReadUtf8(json).EnumIndex;
        public static int LookupIntKey(int key) => IfChainEnums.EnumWith100Values.GetByEnumIndex(key)!.EnumIndex;

        private static IfChainEnums.EnumWith100Values ReadUtf8(byte[] json)
        {
            var reader = CreateReader(json);
            return _converter.Read(ref reader, typeof(IfChainEnums.EnumWith100Values), null!)!;
        }

        private static readonly IfChainEnums.EnumWith100Values.JsonConverter _converter = new();
    }

    private readonly struct Dictionary1 : ILookup
    {
        public static int LookupStringKey(string key) => DictionaryEnums.EnumWith1Value.Deserialize(key)!.EnumIndex;
        public static int LookupUtf8Key(byte[] json) => ReadUtf8(json).EnumIndex;
        public static int LookupIntKey(int key) => DictionaryEnums.EnumWith1Value.GetByEnumIndex(key)!.EnumIndex;

        private static DictionaryEnums.EnumWith1Value ReadUtf8(byte[] json)
        {
            var reader = CreateReader(json);
            return _converter.Read(ref reader, typeof(DictionaryEnums.EnumWith1Value), null!)!;
        }

        private static readonly DictionaryEnums.EnumWith1Value.JsonConverter _converter = new();
    }

    private readonly struct Dictionary5 : ILookup
    {
        public static int LookupStringKey(string key) => DictionaryEnums.EnumWith5Values.Deserialize(key)!.EnumIndex;
        public static int LookupUtf8Key(byte[] json) => ReadUtf8(json).EnumIndex;
        public static int LookupIntKey(int key) => DictionaryEnums.EnumWith5Values.GetByEnumIndex(key)!.EnumIndex;

        private static DictionaryEnums.EnumWith5Values ReadUtf8(byte[] json)
        {
            var reader = CreateReader(json);
            return _converter.Read(ref reader, typeof(DictionaryEnums.EnumWith5Values), null!)!;
        }

        private static readonly DictionaryEnums.EnumWith5Values.JsonConverter _converter = new();
    }

    private readonly struct Dictionary10 : ILookup
    {
        public static int LookupStringKey(string key) => DictionaryEnums.EnumWith10Values.Deserialize(key)!.EnumIndex;
        public static int LookupUtf8Key(byte[] json) => ReadUtf8(json).EnumIndex;
        public static int LookupIntKey(int key) => DictionaryEnums.EnumWith10Values.GetByEnumIndex(key)!.EnumIndex;

        private static DictionaryEnums.EnumWith10Values ReadUtf8(byte[] json)
        {
            var reader = CreateReader(json);
            return _converter.Read(ref reader, typeof(DictionaryEnums.EnumWith10Values), null!)!;
        }

        private static readonly DictionaryEnums.EnumWith10Values.JsonConverter _converter = new();
    }

    private readonly struct Dictionary30 : ILookup
    {
        public static int LookupStringKey(string key) => DictionaryEnums.EnumWith30Values.Deserialize(key)!.EnumIndex;
        public static int LookupUtf8Key(byte[] json) => ReadUtf8(json).EnumIndex;
        public static int LookupIntKey(int key) => DictionaryEnums.EnumWith30Values.GetByEnumIndex(key)!.EnumIndex;

        private static DictionaryEnums.EnumWith30Values ReadUtf8(byte[] json)
        {
            var reader = CreateReader(json);
            return _converter.Read(ref reader, typeof(DictionaryEnums.EnumWith30Values), null!)!;
        }

        private static readonly DictionaryEnums.EnumWith30Values.JsonConverter _converter = new();
    }

    private readonly struct Dictionary50 : ILookup
    {
        public static int LookupStringKey(string key) => DictionaryEnums.EnumWith50Values.Deserialize(key)!.EnumIndex;
        public static int LookupUtf8Key(byte[] json) => ReadUtf8(json).EnumIndex;
        public static int LookupIntKey(int key) => DictionaryEnums.EnumWith50Values.GetByEnumIndex(key)!.EnumIndex;

        private static DictionaryEnums.EnumWith50Values ReadUtf8(byte[] json)
        {
            var reader = CreateReader(json);
            return _converter.Read(ref reader, typeof(DictionaryEnums.EnumWith50Values), null!)!;
        }

        private static readonly DictionaryEnums.EnumWith50Values.JsonConverter _converter = new();
    }

    private readonly struct Dictionary100 : ILookup
    {
        public static int LookupStringKey(string key) => DictionaryEnums.EnumWith100Values.Deserialize(key)!.EnumIndex;
        public static int LookupUtf8Key(byte[] json) => ReadUtf8(json).EnumIndex;
        public static int LookupIntKey(int key) => DictionaryEnums.EnumWith100Values.GetByEnumIndex(key)!.EnumIndex;

        private static DictionaryEnums.EnumWith100Values ReadUtf8(byte[] json)
        {
            var reader = CreateReader(json);
            return _converter.Read(ref reader, typeof(DictionaryEnums.EnumWith100Values), null!)!;
        }

        private static readonly DictionaryEnums.EnumWith100Values.JsonConverter _converter = new();
    }

    private static Utf8JsonReader CreateReader(byte[] json)
    {
        var reader = new Utf8JsonReader(json);
        reader.Read();
        return reader;
    }
}
