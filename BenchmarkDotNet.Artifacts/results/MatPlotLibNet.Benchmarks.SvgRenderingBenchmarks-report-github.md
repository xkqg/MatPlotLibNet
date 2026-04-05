```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8117)
AMD Ryzen 9 3950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|---------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| SimpleLine     |  52.48 μs |   4.328 μs | 0.237 μs |  1.00 |    0.01 | 10.0098 |  0.8545 |       - |  82.22 KB |        1.00 |
| ComplexChart   |  64.39 μs |  16.305 μs | 0.894 μs |  1.23 |    0.02 | 11.2305 |  0.9766 |       - |  92.99 KB |        1.13 |
| SubplotGrid3x3 | 224.38 μs | 104.045 μs | 5.703 μs |  4.28 |    0.10 | 64.4531 | 32.2266 | 32.2266 | 480.64 KB |        5.85 |
