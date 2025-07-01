```

BenchmarkDotNet v0.15.2, Linux Manjaro Linux
Intel Core i7-6820HQ CPU 2.70GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 9.0.105
  [Host]     : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
  Job-OYOULF : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2

Runtime=.NET 9.0  IterationCount=3  WarmupCount=1  

```
| Method | FileSizeBytes | BufferSizeBytes | Mean    | Error    | StdDev   | Gen0         | Gen1        | Gen2       | Allocated |
|------- |-------------- |---------------- |--------:|---------:|---------:|-------------:|------------:|-----------:|----------:|
| Sort   | 1073741824    | 10485760        | 2.452 m | 0.2543 m | 0.0139 m | 4841000.0000 | 506000.0000 | 84000.0000 |  22.71 GB |
