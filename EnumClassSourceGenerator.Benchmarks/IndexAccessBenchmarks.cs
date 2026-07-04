using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;

namespace EnumClassSourceGenerator.Benchmarks;

[MemoryDiagnoser(false)]
public class IndexAccessBenchmarks
{
    private record Data(
        int EnumIndex,
        string Val);

    private static readonly Data _one = new(1, "One");
    private static readonly Data _two = new(1, "Two");
    private static readonly Data _three = new(1, "Three");
    private static readonly Data _four = new(1, "Four");

    private readonly static FrozenDictionary<int, Data> _frozenDic = new Dictionary<int, Data>()
    {
        [1] = _one,
        [2] = _two,
        [3] = _three,
        [4] = _four
    }.ToFrozenDictionary();

    const int _iterations = 1000;

    private readonly Random _random = new Random(69);

    [Benchmark]
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void WithIf()
    {
        Data dataShared;
        for (int i = 0; i < _iterations; i++)
        {
            var index = _random.Next(1, 5);
            var data = GetWithIf(index);

            dataShared = data;
        }
    }

    [Benchmark]
    public void WithDic()
    {
        Data dataShared;
        for (int i = 0; i < _iterations; i++)
        {
            var index = _random.Next(1, 5);
            var data = GetWithDic(index);

            dataShared = data;
        }
    }

    private static Data GetWithIf(int index)
    {
        if (index == _one.EnumIndex) return _one;
        if (index == _two.EnumIndex) return _two;
        if (index == _three.EnumIndex) return _three;
        if (index == _four.EnumIndex) return _four;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    private static Data GetWithDic(int index)
        => _frozenDic.TryGetValue(index, out var data)
            ? data
            : throw new ArgumentOutOfRangeException(nameof(index));
}
