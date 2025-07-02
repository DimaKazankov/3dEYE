```

BenchmarkDotNet v0.15.2, Linux Manjaro Linux
Intel Core i7-6820HQ CPU 2.70GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 9.0.105
  [Host]     : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
  Job-EGQROE : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2

Runtime=.NET 9.0  IterationCount=1  WarmupCount=1  

```
| Method | BufferSizeBytes | Mean    | Error | Gen0         | Gen1         | Gen2         | Allocated |
|------- |---------------- |--------:|------:|-------------:|-------------:|-------------:|----------:|
| **Sort**   | **1048576**         | **2.587 m** |    **NA** | **8357000.0000** | **2450000.0000** | **2092000.0000** |  **79.81 GB** |
| **Sort**   | **10485760**        | **2.524 m** |    **NA** | **4925000.0000** |  **693000.0000** |  **294000.0000** |  **75.02 GB** |
