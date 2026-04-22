```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8246/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 3950X 3.49GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.202
  [Host] : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | DataSize | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|------- |--------- |-----:|------:|------:|--------:|------------:|
| Sma_20 | 1000     |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  IndicatorBenchmarks.Sma_20: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3) [DataSize=1000]
