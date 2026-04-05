```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8117)
AMD Ryzen 9 3950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method        | Mean     | Error     | StdDev   | Gen0     | Gen1     | Gen2     | Allocated  |
|-------------- |---------:|----------:|---------:|---------:|---------:|---------:|-----------:|
| ToPng_Simple  | 20.80 ms | 13.136 ms | 0.720 ms |        - |        - |        - |   78.42 KB |
| ToPng_Complex | 20.13 ms |  0.943 ms | 0.052 ms |        - |        - |        - |   53.61 KB |
| ToPdf_Simple  | 45.65 ms | 19.343 ms | 1.060 ms | 909.0909 | 909.0909 | 909.0909 | 3764.75 KB |
| ToPdf_Complex | 47.01 ms | 23.856 ms | 1.308 ms | 916.6667 | 916.6667 | 916.6667 | 3760.02 KB |
