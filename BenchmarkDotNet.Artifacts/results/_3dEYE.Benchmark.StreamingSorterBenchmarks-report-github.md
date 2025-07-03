```

BenchmarkDotNet v0.15.2, Linux Manjaro Linux
Intel Core i7-6820HQ CPU 2.70GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 9.0.105
  [Host]     : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
  Job-EGQROE : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2

Runtime=.NET 9.0  IterationCount=1  WarmupCount=1  

```
| Method | Mean    | Error | Gen0         | Gen1        | Gen2        | Allocated |
|------- |--------:|------:|-------------:|------------:|------------:|----------:|
| Sort   | 2.794 m |    NA | 1534000.0000 | 944000.0000 | 328000.0000 |   7.34 GB |
