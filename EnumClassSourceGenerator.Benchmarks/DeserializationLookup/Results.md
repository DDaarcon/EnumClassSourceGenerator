BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9711/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i7-1265U 1.80GHz, 1 CPU, 12 logical and 10 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


| Method                         | Categories    | ValueCount | Mean       | Error     | StdDev     | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |-------------- |----------- |-----------:|----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| IfChain_NearBeginning          | NearBeginning | 4          |   2.248 ns | 0.0235 ns |  0.0219 ns |   2.251 ns |  1.00 |    0.01 |         - |          NA |
| FrozenDictionary_NearBeginning | NearBeginning | 4          |   5.056 ns | 0.0367 ns |  0.0326 ns |   5.052 ns |  2.25 |    0.03 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_NearBeginning          | NearBeginning | 8          |   2.219 ns | 0.0367 ns |  0.0652 ns |   2.206 ns |  1.00 |    0.04 |         - |          NA |
| FrozenDictionary_NearBeginning | NearBeginning | 8          |   6.084 ns | 0.1266 ns |  0.3530 ns |   6.002 ns |  2.74 |    0.18 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_NearBeginning          | NearBeginning | 16         |   3.326 ns | 0.1049 ns |  0.2907 ns |   3.289 ns |  1.01 |    0.13 |         - |          NA |
| FrozenDictionary_NearBeginning | NearBeginning | 16         |   5.508 ns | 0.1102 ns |  0.2042 ns |   5.418 ns |  1.67 |    0.16 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_NearBeginning          | NearBeginning | 32         |   2.904 ns | 0.1368 ns |  0.4034 ns |   2.634 ns |  1.02 |    0.19 |         - |          NA |
| FrozenDictionary_NearBeginning | NearBeginning | 32         |   5.485 ns | 0.1076 ns |  0.2340 ns |   5.404 ns |  1.92 |    0.26 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_NearBeginning          | NearBeginning | 64         |   2.570 ns | 0.0508 ns |  0.0605 ns |   2.574 ns |  1.00 |    0.03 |         - |          NA |
| FrozenDictionary_NearBeginning | NearBeginning | 64         |   7.676 ns | 0.6017 ns |  1.7646 ns |   7.664 ns |  2.99 |    0.69 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_NearBeginning          | NearBeginning | 128        |   3.131 ns | 0.0582 ns |  0.0545 ns |   3.136 ns |  1.00 |    0.02 |         - |          NA |
| FrozenDictionary_NearBeginning | NearBeginning | 128        |   5.388 ns | 0.0443 ns |  0.0393 ns |   5.383 ns |  1.72 |    0.03 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_NearEnd                | NearEnd       | 4          |   2.876 ns | 0.0398 ns |  0.0372 ns |   2.872 ns |  1.00 |    0.02 |         - |          NA |
| FrozenDictionary_NearEnd       | NearEnd       | 4          |   9.534 ns | 0.8590 ns |  2.5329 ns |   8.839 ns |  3.32 |    0.88 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_NearEnd                | NearEnd       | 8          |   6.311 ns | 0.0929 ns |  0.1070 ns |   6.287 ns |  1.00 |    0.02 |         - |          NA |
| FrozenDictionary_NearEnd       | NearEnd       | 8          |   4.214 ns | 0.0395 ns |  0.0369 ns |   4.201 ns |  0.67 |    0.01 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_NearEnd                | NearEnd       | 16         |  13.183 ns | 0.1593 ns |  0.1330 ns |  13.150 ns |  1.00 |    0.01 |         - |          NA |
| FrozenDictionary_NearEnd       | NearEnd       | 16         |   9.512 ns | 0.6381 ns |  1.7892 ns |   9.064 ns |  0.72 |    0.14 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_NearEnd                | NearEnd       | 32         |  31.902 ns | 0.6279 ns |  1.3244 ns |  32.119 ns |  1.00 |    0.06 |         - |          NA |
| FrozenDictionary_NearEnd       | NearEnd       | 32         |   9.945 ns | 1.1918 ns |  3.4386 ns |   8.831 ns |  0.31 |    0.11 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_NearEnd                | NearEnd       | 64         |  61.155 ns | 1.7941 ns |  5.2050 ns |  62.622 ns |  1.01 |    0.12 |         - |          NA |
| FrozenDictionary_NearEnd       | NearEnd       | 64         |   7.867 ns | 0.4158 ns |  1.1998 ns |   8.429 ns |  0.13 |    0.02 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_NearEnd                | NearEnd       | 128        | 126.621 ns | 5.9774 ns | 17.0539 ns | 127.440 ns |  1.02 |    0.19 |         - |          NA |
| FrozenDictionary_NearEnd       | NearEnd       | 128        |  10.745 ns | 0.9344 ns |  2.6507 ns |   9.778 ns |  0.09 |    0.02 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_Random                 | Random        | 4          |   4.148 ns | 0.0876 ns |  0.2543 ns |   4.186 ns |  1.00 |    0.09 |         - |          NA |
| FrozenDictionary_Random        | Random        | 4          |  10.017 ns | 0.9201 ns |  2.7130 ns |  11.017 ns |  2.42 |    0.67 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_Random                 | Random        | 8          |   4.318 ns | 0.0591 ns |  0.0524 ns |   4.319 ns |  1.00 |    0.02 |         - |          NA |
| FrozenDictionary_Random        | Random        | 8          |   4.858 ns | 0.0653 ns |  0.0545 ns |   4.835 ns |  1.12 |    0.02 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_Random                 | Random        | 16         |   7.556 ns | 0.1013 ns |  0.0948 ns |   7.537 ns |  1.00 |    0.02 |         - |          NA |
| FrozenDictionary_Random        | Random        | 16         |   5.844 ns | 0.1157 ns |  0.2338 ns |   5.792 ns |  0.77 |    0.03 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_Random                 | Random        | 32         |  19.504 ns | 0.8188 ns |  2.2824 ns |  19.645 ns |  1.01 |    0.17 |         - |          NA |
| FrozenDictionary_Random        | Random        | 32         |   6.115 ns | 0.0411 ns |  0.0321 ns |   6.115 ns |  0.32 |    0.04 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_Random                 | Random        | 64         |  28.208 ns | 0.5301 ns |  0.4699 ns |  28.189 ns |  1.00 |    0.02 |         - |          NA |
| FrozenDictionary_Random        | Random        | 64         |   9.485 ns | 0.2924 ns |  0.8103 ns |   9.354 ns |  0.34 |    0.03 |         - |          NA |
|                                |               |            |            |           |            |            |       |         |           |             |
| IfChain_Random                 | Random        | 128        |  67.509 ns | 1.2510 ns |  2.4400 ns |  67.297 ns |  1.00 |    0.05 |         - |          NA |
| FrozenDictionary_Random        | Random        | 128        |   7.366 ns | 0.5267 ns |  1.5529 ns |   6.297 ns |  0.11 |    0.02 |         - |          NA |

// * Warnings *
MultimodalDistribution
  DeserializationLookupBenchmarks.IfChain_NearBeginning: Default          -> It seems that the distribution can have several modes (mValue = 3.11)
  DeserializationLookupBenchmarks.IfChain_NearBeginning: Default          -> It seems that the distribution is bimodal (mValue = 3.31)
  DeserializationLookupBenchmarks.FrozenDictionary_NearBeginning: Default -> It seems that the distribution is multimodal (mValue = 4.89)
  DeserializationLookupBenchmarks.FrozenDictionary_NearEnd: Default       -> It seems that the distribution can have several modes (mValue = 3.08)
  DeserializationLookupBenchmarks.FrozenDictionary_NearEnd: Default       -> It seems that the distribution can have several modes (mValue = 3.17)
  DeserializationLookupBenchmarks.IfChain_NearEnd: Default                -> It seems that the distribution is bimodal (mValue = 3.24)
  DeserializationLookupBenchmarks.IfChain_NearEnd: Default                -> It seems that the distribution is bimodal (mValue = 3.49)
  DeserializationLookupBenchmarks.FrozenDictionary_Random: Default        -> It seems that the distribution is bimodal (mValue = 3.78)
  DeserializationLookupBenchmarks.FrozenDictionary_Random: Default        -> It seems that the distribution can have several modes (mValue = 2.97)

// * Hints *
Outliers
  DeserializationLookupBenchmarks.FrozenDictionary_NearBeginning: Default -> 1 outlier  was  removed (5.19 ns)
  DeserializationLookupBenchmarks.IfChain_NearBeginning: Default          -> 9 outliers were removed (2.49 ns..2.93 ns)
  DeserializationLookupBenchmarks.FrozenDictionary_NearBeginning: Default -> 10 outliers were removed (7.50 ns..13.58 ns)
  DeserializationLookupBenchmarks.IfChain_NearBeginning: Default          -> 11 outliers were removed, 13 outliers were detected (2.43 ns, 2.50 ns, 4.27 ns..6.31 ns)
  DeserializationLookupBenchmarks.FrozenDictionary_NearBeginning: Default -> 11 outliers were removed (9.03 ns..13.03 ns)
  DeserializationLookupBenchmarks.FrozenDictionary_NearBeginning: Default -> 6 outliers were removed (6.41 ns..8.16 ns)
  DeserializationLookupBenchmarks.IfChain_NearBeginning: Default          -> 2 outliers were removed (2.84 ns, 2.97 ns)
  DeserializationLookupBenchmarks.FrozenDictionary_NearBeginning: Default -> 1 outlier  was  removed (14.35 ns)
  DeserializationLookupBenchmarks.IfChain_NearBeginning: Default          -> 2 outliers were detected (3.01 ns, 3.02 ns)
  DeserializationLookupBenchmarks.FrozenDictionary_NearBeginning: Default -> 1 outlier  was  removed (5.64 ns)
  DeserializationLookupBenchmarks.IfChain_NearEnd: Default                -> 3 outliers were removed (6.78 ns..7.29 ns)
  DeserializationLookupBenchmarks.IfChain_NearEnd: Default                -> 3 outliers were removed (13.76 ns..14.35 ns)
  DeserializationLookupBenchmarks.FrozenDictionary_NearEnd: Default       -> 9 outliers were removed (15.50 ns..18.53 ns)
  DeserializationLookupBenchmarks.IfChain_NearEnd: Default                -> 6 outliers were removed, 11 outliers were detected (27.71 ns..29.96 ns, 36.04 ns..57.94 ns)
  DeserializationLookupBenchmarks.FrozenDictionary_NearEnd: Default       -> 4 outliers were removed (20.29 ns..21.12 ns)
  DeserializationLookupBenchmarks.IfChain_NearEnd: Default                -> 3 outliers were removed (76.81 ns..80.33 ns)
  DeserializationLookupBenchmarks.FrozenDictionary_NearEnd: Default       -> 4 outliers were removed, 19 outliers were detected (5.19 ns..5.78 ns, 10.06 ns..11.67 ns)
  DeserializationLookupBenchmarks.IfChain_NearEnd: Default                -> 6 outliers were removed (184.73 ns..313.76 ns)
  DeserializationLookupBenchmarks.FrozenDictionary_NearEnd: Default       -> 7 outliers were removed (19.92 ns..22.22 ns)
  DeserializationLookupBenchmarks.IfChain_Random: Default                 -> 3 outliers were removed, 10 outliers were detected (3.15 ns..3.69 ns, 4.90 ns..7.16 ns)
  DeserializationLookupBenchmarks.IfChain_Random: Default                 -> 1 outlier  was  removed (4.48 ns)
  DeserializationLookupBenchmarks.FrozenDictionary_Random: Default        -> 2 outliers were removed (5.20 ns, 5.41 ns)
  DeserializationLookupBenchmarks.FrozenDictionary_Random: Default        -> 3 outliers were removed (6.66 ns..7.03 ns)
  DeserializationLookupBenchmarks.IfChain_Random: Default                 -> 10 outliers were removed, 15 outliers were detected (14.13 ns..14.52 ns, 25.22 ns..37.23 ns)
  DeserializationLookupBenchmarks.FrozenDictionary_Random: Default        -> 3 outliers were removed (6.33 ns..6.57 ns)
  DeserializationLookupBenchmarks.IfChain_Random: Default                 -> 1 outlier  was  removed (30.85 ns)
  DeserializationLookupBenchmarks.FrozenDictionary_Random: Default        -> 11 outliers were removed, 14 outliers were detected (6.41 ns..7.44 ns, 12.54 ns..19.56 ns)
  DeserializationLookupBenchmarks.IfChain_Random: Default                 -> 4 outliers were removed, 5 outliers were detected (60.38 ns, 75.74 ns..80.59 ns)

// * Legends *
  Categories  : All categories of the corresponded method, class, and assembly
  ValueCount  : Value of the 'ValueCount' parameter
  Mean        : Arithmetic mean of all measurements
  Error       : Half of 99.9% confidence interval
  StdDev      : Standard deviation of all measurements
  Median      : Value separating the higher half of all measurements (50th percentile)
  Ratio       : Mean of the ratio distribution ([Current]/[Baseline])
  RatioSD     : Standard deviation of the ratio distribution ([Current]/[Baseline])
  Allocated   : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  Alloc Ratio : Allocated memory ratio distribution ([Current]/[Baseline])
  1 ns        : 1 Nanosecond (0.000000001 sec)

// * Diagnostic Output - MemoryDiagnoser *


// ***** BenchmarkRunner: End *****
Run time: 00:42:04 (2524.97 sec), executed benchmarks: 36

Global total time: 00:42:24 (2544.64 sec), executed benchmarks: 36
// * Artifacts cleanup *
Artifacts cleanup is finished