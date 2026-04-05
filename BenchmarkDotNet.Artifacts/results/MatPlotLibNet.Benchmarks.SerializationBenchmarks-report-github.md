```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8117)
AMD Ryzen 9 3950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean     | Error     | StdDev   | Gen0   | Allocated |
|--------------- |---------:|----------:|---------:|-------:|----------:|
| ToJson         | 20.06 μs |  5.142 μs | 0.282 μs | 0.9460 |   7.75 KB |
| ToJsonIndented | 23.34 μs |  7.396 μs | 0.405 μs | 1.7090 |   14.2 KB |
| FromJson       | 19.47 μs |  1.648 μs | 0.090 μs | 1.3123 |  10.92 KB |
| RoundTrip      | 41.36 μs | 14.365 μs | 0.787 μs | 2.1973 |  18.67 KB |
