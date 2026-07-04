using BenchmarkDotNet.Running;

namespace EnumClassSourceGenerator.Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<IndexAccessBenchmarks>();
    }
}
