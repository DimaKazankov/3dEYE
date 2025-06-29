```

BenchmarkDotNet v0.15.2, Linux Manjaro Linux
Intel Core i7-6820HQ CPU 2.70GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 9.0.105
  [Host]     : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2


```
| Method                        | Mean        | Error      | StdDev     | Median      | Gen0        | Gen1      | Gen2      | Allocated  |
|------------------------------ |------------:|-----------:|-----------:|------------:|------------:|----------:|----------:|-----------:|
| Original_Generate1GBFile      | 5,797.23 ms | 277.151 ms | 817.187 ms | 5,504.26 ms | 325000.0000 |         - |         - | 1298.62 MB |
| Optimized_Generate1GBFile     | 6,761.82 ms | 128.877 ms | 282.889 ms | 6,732.17 ms | 597000.0000 | 1000.0000 | 1000.0000 | 2389.82 MB |
| Parallel_Generate1GBFile      | 3,461.17 ms |  68.908 ms | 181.530 ms | 3,426.40 ms | 317000.0000 | 2000.0000 | 1000.0000 | 1268.77 MB |
| LargeParallel_Generate1GBFile | 4,138.85 ms |  99.531 ms | 290.336 ms | 4,123.83 ms | 318000.0000 | 1000.0000 |         - | 1272.19 MB |
| Original_Generate100MBFile    |   657.24 ms |  22.158 ms |  63.577 ms |   660.01 ms |  31000.0000 |         - |         - |   127.1 MB |
| Optimized_Generate100MBFile   |   794.77 ms |  24.511 ms |  71.112 ms |   800.97 ms |  58000.0000 | 1000.0000 | 1000.0000 |  238.85 MB |
| Parallel_Generate100MBFile    |   749.41 ms |  24.063 ms |  67.476 ms |   750.88 ms |  31000.0000 |         - |         - |  128.36 MB |
| Original_Generate10MBFile     |    68.58 ms |   2.599 ms |   7.458 ms |    68.16 ms |   3000.0000 |         - |         - |      13 MB |
| Optimized_Generate10MBFile    |    85.78 ms |   2.998 ms |   8.602 ms |    87.04 ms |   6000.0000 |  666.6667 |  666.6667 |    29.3 MB |
| Parallel_Generate10MBFile     |    78.08 ms |   2.716 ms |   7.482 ms |    78.70 ms |   3000.0000 |         - |         - |   14.14 MB |
