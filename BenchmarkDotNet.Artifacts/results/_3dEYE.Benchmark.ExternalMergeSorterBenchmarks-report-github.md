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
| **Sort**   | **1048576**         | **3.725 m** |    **NA** | **8309000.0000** | **2390000.0000** | **2041000.0000** |  **79.82 GB** |
| **Sort**   | **10485760**        | **3.603 m** |    **NA** | **4921000.0000** |  **679000.0000** |  **299000.0000** |  **75.02 GB** |
