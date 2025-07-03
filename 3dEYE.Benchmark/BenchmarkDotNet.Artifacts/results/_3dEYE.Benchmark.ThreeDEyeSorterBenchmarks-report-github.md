```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4351/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.205
  [Host]     : .NET 9.0.6 (9.0.625.26613), X64 RyuJIT AVX2
  Job-EGQROE : .NET 9.0.6 (9.0.625.26613), X64 RyuJIT AVX2

Runtime=.NET 9.0  IterationCount=1  WarmupCount=1  

```
| Method | Mean    | Error | Gen0         | Gen1       | Gen2       | Allocated |
|------- |--------:|------:|-------------:|-----------:|-----------:|----------:|
| Sort   | 1.956 m |    NA | 2565000.0000 | 31000.0000 | 28000.0000 |  14.76 GB |
