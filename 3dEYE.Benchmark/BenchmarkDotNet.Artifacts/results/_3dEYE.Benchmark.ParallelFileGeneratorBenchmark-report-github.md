```

BenchmarkDotNet v0.15.2, Linux Manjaro Linux
Intel Core i7-6820HQ CPU 2.70GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 9.0.105
  [Host]     : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2


```
| Method                        | Mean       | Error     | StdDev    | Median     | Gen0         | Gen1      | Gen2      | Allocated  |
|------------------------------ |-----------:|----------:|----------:|-----------:|-------------:|----------:|----------:|-----------:|
| Original_Generate1GBFile      | 9,803.3 ms | 192.01 ms | 379.02 ms | 9,791.1 ms | 1502000.0000 | 1000.0000 |         - | 5992.14 MB |
| Optimized_Generate1GBFile     | 8,382.7 ms | 165.47 ms | 294.12 ms | 8,397.4 ms | 1594000.0000 | 1000.0000 | 1000.0000 |  6362.1 MB |
| Parallel_Generate1GBFile      | 4,240.9 ms | 143.07 ms | 419.60 ms | 4,122.0 ms | 1412000.0000 | 2000.0000 | 1000.0000 | 5611.25 MB |
| LargeParallel_Generate1GBFile | 4,595.0 ms |  90.24 ms | 190.35 ms | 4,615.5 ms | 1416000.0000 | 1000.0000 |         - | 5620.72 MB |
| Original_Generate100MBFile    |   993.3 ms |  35.00 ms | 100.43 ms | 1,015.6 ms |  146000.0000 |         - |         - |  585.51 MB |
| Optimized_Generate100MBFile   |   955.2 ms |  28.26 ms |  80.18 ms |   968.7 ms |  156000.0000 | 1000.0000 | 1000.0000 |  626.71 MB |
| Parallel_Generate100MBFile    |   706.9 ms |  26.37 ms |  75.67 ms |   705.9 ms |  139000.0000 |         - |         - |  554.85 MB |
