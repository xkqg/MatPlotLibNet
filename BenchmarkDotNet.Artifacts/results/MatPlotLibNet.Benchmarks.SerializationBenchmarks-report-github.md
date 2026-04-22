```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8246/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 3950X 3.49GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3


```
| Method         | Mean     | Error    | StdDev   | Gen0   | Gen1   | Allocated |
|--------------- |---------:|---------:|---------:|-------:|-------:|----------:|
| ToJson         | 22.42 μs | 0.407 μs | 0.380 μs | 1.0681 |      - |   8.88 KB |
| ToJsonIndented | 24.93 μs | 0.471 μs | 0.440 μs | 1.8616 |      - |  15.33 KB |
| FromJson       | 18.80 μs | 0.293 μs | 0.274 μs | 1.6785 | 0.0305 |  13.92 KB |
| RoundTrip      | 44.04 μs | 0.878 μs | 1.833 μs | 2.6855 |      - |   22.8 KB |
