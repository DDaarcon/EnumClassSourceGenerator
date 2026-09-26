using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Collections.Frozen;
using System.Linq.Expressions;

namespace EnumClassSourceGenerator.Benchmarks.DeserializationLookup;

[MemoryDiagnoser(false)]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class DeserializationLookupBenchmarks
{
    private const int OperationsPerInvoke = 1_024;

    private sealed record LookupValue(int Index, string SerializedName);

    private Func<string, LookupValue?> _ifChainLookup = null!;
    private Func<string, LookupValue?> _dictionaryLookup = null!;
    private string[] _randomInputs = null!;
    private string _nearBeginningInput = null!;
    private string _nearEndInput = null!;

    [Params(4, 8, 16, 32, 64, 128)]
    public int ValueCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var values = Enumerable.Range(0, ValueCount)
            .Select(index => new LookupValue(index, $"Value{index:D3}"))
            .ToArray();

        _ifChainLookup = BuildIfChain(values);

        var valuesBySerializedName = values.ToFrozenDictionary(
            value => value.SerializedName);

        _dictionaryLookup = serializedValue =>
            valuesBySerializedName.TryGetValue(serializedValue, out var value)
                ? value
                : null;

        _nearBeginningInput = values[1].SerializedName;
        _nearEndInput = values[^2].SerializedName;

        var random = new Random(69);
        _randomInputs = Enumerable.Range(0, OperationsPerInvoke)
            .Select(_ => values[random.Next(values.Length)].SerializedName)
            .ToArray();

        ValidateLookups(values);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
    [BenchmarkCategory("Random")]
    public int IfChain_Random()
        => LookupRandomInputs(_ifChainLookup);

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    [BenchmarkCategory("Random")]
    public int FrozenDictionary_Random()
        => LookupRandomInputs(_dictionaryLookup);

    [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
    [BenchmarkCategory("NearBeginning")]
    public int IfChain_NearBeginning()
        => LookupRepeatedInput(_ifChainLookup, _nearBeginningInput);

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    [BenchmarkCategory("NearBeginning")]
    public int FrozenDictionary_NearBeginning()
        => LookupRepeatedInput(_dictionaryLookup, _nearBeginningInput);

    [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
    [BenchmarkCategory("NearEnd")]
    public int IfChain_NearEnd()
        => LookupRepeatedInput(_ifChainLookup, _nearEndInput);

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    [BenchmarkCategory("NearEnd")]
    public int FrozenDictionary_NearEnd()
        => LookupRepeatedInput(_dictionaryLookup, _nearEndInput);

    private int LookupRandomInputs(Func<string, LookupValue?> lookup)
    {
        var checksum = 0;

        for (var index = 0; index < _randomInputs.Length; index++)
        {
            checksum += lookup(_randomInputs[index])!.Index;
        }

        return checksum;
    }

    private static int LookupRepeatedInput(
        Func<string, LookupValue?> lookup,
        string serializedValue)
    {
        var checksum = 0;

        for (var index = 0; index < OperationsPerInvoke; index++)
        {
            checksum += lookup(serializedValue)!.Index;
        }

        return checksum;
    }

    private static Func<string, LookupValue?> BuildIfChain(
        IReadOnlyList<LookupValue> values)
    {
        var serializedValue = Expression.Parameter(
            typeof(string),
            "serializedValue");

        Expression body = Expression.Constant(null, typeof(LookupValue));

        for (var index = values.Count - 1; index >= 0; index--)
        {
            var value = values[index];
            var serializedName = Expression.Constant(value.SerializedName);

            body = Expression.Condition(
                Expression.Equal(serializedValue, serializedName),
                Expression.Constant(value, typeof(LookupValue)),
                body);
        }

        return Expression.Lambda<Func<string, LookupValue?>>(
            body,
            serializedValue).Compile();
    }

    private void ValidateLookups(IReadOnlyList<LookupValue> values)
    {
        foreach (var value in values)
        {
            if (!ReferenceEquals(_ifChainLookup(value.SerializedName), value) ||
                !ReferenceEquals(_dictionaryLookup(value.SerializedName), value))
            {
                throw new InvalidOperationException("Lookup setup returned an unexpected value.");
            }
        }

        const string missingValue = "Missing";

        if (_ifChainLookup(missingValue) is not null ||
            _dictionaryLookup(missingValue) is not null)
        {
            throw new InvalidOperationException("Lookup setup accepted a missing value.");
        }
    }
}
