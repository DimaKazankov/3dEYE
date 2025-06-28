```

BenchmarkDotNet v0.15.2, Linux Manjaro Linux
Intel Core i7-6820HQ CPU 2.70GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 9.0.105
  [Host]     : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2


```
| Method            | Mean        | Error     | StdDev      | Median      | Gen0         | Gen1      | Allocated  |
|------------------ |------------:|----------:|------------:|------------:|-------------:|----------:|-----------:|
| Generate1GBFile   | 11,988.6 ms | 777.03 ms | 2,291.09 ms | 10,493.3 ms | 1502000.0000 | 1000.0000 | 5992.26 MB |
| Generate100MBFile |  1,348.2 ms | 108.41 ms |   317.96 ms |  1,368.7 ms |  146000.0000 |         - |  585.49 MB |
| Generate10MBFile  |    168.5 ms |   8.61 ms |    24.69 ms |    164.4 ms |   14000.0000 |         - |   58.83 MB |
