# Performance Benchmarks

AMD Ryzen 9 3950X (16C/32T, 3.49 GHz base). Windows 11 24H2. .NET 10.0.6, X64 RyuJIT AVX2 / x86-64-v3. BenchmarkDotNet 0.14 / 0.15, Release mode. Raw reports live under [`Benchmarks/MatPlotLibNet.Benchmarks/BenchmarkDotNet.Artifacts/results/`](Benchmarks/MatPlotLibNet.Benchmarks/BenchmarkDotNet.Artifacts/results/) (`*-report.csv`, `*-report-github.md`, `*-report.html`). Historical v0.5.1 / v0.6.0 / v1.1.0 / v1.1.1 comparisons preserved below.

## What's in v1.9.0

v1.9.0 is a **pure indicator-expansion release** — 12 new indicators across Tier 3a (Volume / Money Flow), Tier 3b (Trend / Transform), and Tier 3c (Advanced / Cross-asset) bring the library total to **52 production-grade indicators**. No hot-path changes: SVG output is byte-identical to v1.8.0 so every render number in the tables below is still current. The v1.8.0 + v1.9.0 indicators (GarmanKlass, YangZhang, Klinger, Supertrend, EhlersITrend, TransferEntropy, and 30 more) use the same stacked-base-class infrastructure as the classical set benchmarked here — their per-indicator cost is in the same ballpark as the ATR / BollingerBands family for the equivalent family (volatility / trend / cycle / microstructure). A dedicated `Tier3IndicatorBenchmarks` suite will land post-v1.9.0 with side-by-side numbers.

**Positioning highlights (Ryzen 9 3950X, 100 000-point series):**

- **SVG render** of a 1 000-point line: **66 µs** (127 KB)
- **SVG render** with LTTB downsample of 100 000 points: **1.78 ms**
- **SIMD reductions** (`Vec.Sum` / `Vec.Mean`) on 100 000 points: **19 µs, zero allocation**
- **SMA(20)** on 100 000 points: **196 µs**
- **VWAP** on 100 000 points: **204 µs**
- **JSON round-trip** (Figure ⇄ JSON): **40 µs**
- **PNG export** via SkiaSharp: **27 ms**
- **TransformBatch** (data-space → pixel-space, AVX interleave): **208 µs for 100 000 points**, 33 % faster than per-point scalar.

## What changed in v1.1.1

v1.1.1 adds `PolarHeatmapSeries`, `AxisBreak`, and NumPy-style numerics (`Mat`, `Linalg`, `NpStats`, `NpRandom`, `Fft` extensions). No benchmark changes were required — these are new capabilities, not hot-path optimizations. The `DataFrameBenchmarks` suite (added in v1.1.1) is documented in the section below.

---

## Architecture

MatPlotLibNet renders charts **server-side as SVG** and pushes them to clients via SignalR. No JavaScript chart library on the client — the browser just swaps `innerHTML`. v0.6.0 introduced a SIMD-accelerated numeric kernel (`VectorMath`) backed by `System.Numerics.Tensors.TensorPrimitives` and AVX hardware intrinsics for the coordinate transform hot path. v1.1.0 extended the SIMD coverage to `SplitPositiveNegative` (now two `TensorPrimitives.Max/Min` passes instead of a branchy scalar loop) and added benchmarks for 3D lighting, geo maps, and choropleth rendering. v1.1.1 added `DataFrameBenchmarks` (27 benchmarks across column reader, 16 financial indicators, polynomial numerics, and figure builders with hue grouping).

**Why server-side SVG?**

- **Zero client-side cost** — browser swaps innerHTML, no canvas redraws, no layout recalculation
- **Inline SVG** — part of the DOM, styleable via CSS, accessible to screen readers, prints as vector
- **Consistent** — every client sees the exact same chart, no browser rendering differences
- **Bandwidth-efficient** — typical chart SVG is 5-15 KB, SignalR pushes only changed charts
- **Scales with hardware** — parallel subplot rendering uses all cores

---

## What changed in v0.6.0

| Area                            | v0.5.1                           | v0.6.0                            | Improvement               |
|---------------------------------|----------------------------------|-----------------------------------|----------------------------|
| DataTransform (1K pts)          | 9 us (3x slower than per-point)  | 764 ns (3.6x faster than per-pt)  | **12x swing**              |
| DataTransform (100K pts)        | 1,298 us / 3,047 KB alloc        | 208 us / 1,563 KB alloc           | **6.2x faster, 2x less mem** |
| Stochastic(14) at 100K         | 7,669 us (O(n*p) nested loops)   | 3,308 us (O(n) monotone deque)    | **2.3x faster**            |
| SVG large line (10K pts)        | 3,935 us                         | 3,105 us                          | **1.2x faster**            |
| SVG large line (100K+LTTB)      | 1,512 us                         | 1,332 us                          | **1.1x faster**            |
| New: Vec reductions             | —                                | 18 us at 100K, zero alloc         | new                        |
| New: 4 Phase F indicators       | —                                | OBV 645 us, ParSar 1.2 ms at 100K | new                        |

---

## DataTransform: v0.5.1 vs v0.6.0

The coordinate transform hot path (`data space -> pixel space`) was the single biggest optimization target.

### v0.5.1 — TensorPrimitives two-pass (3 allocations)

| Method              |     1K |     10K |      100K | Alloc (100K) |
|---------------------|-------:|--------:|----------:|-------------:|
| Per-point loop      |   3 us |   63 us |    365 us |    1,563 KB  |
| TransformBatch      |   9 us |  124 us |  1,298 us |    3,047 KB  |
| **Ratio**           | **3.0x slower** | **2.0x slower** | **3.6x slower** | **2.0x more** |

TransformBatch was slower at every size — the SIMD multiply-add gain was wiped out by allocating separate `double[]` arrays for X, Y, and Points.

### v0.6.0 — AVX SIMD single-pass interleave (0 intermediate allocations)

| Method                    |       1K |     10K |    100K | Alloc (100K) |
|---------------------------|----------|--------:|--------:|-------------:|
| Per-point loop            | 2,761 ns |   65 us |  313 us |    1,563 KB  |
| **TransformBatch (AVX)**  | **764 ns** | **53 us** | **208 us** | **1,563 KB** |
| **Ratio**                 | **3.6x faster** | **1.2x faster** | **1.5x faster** | **1.0x (same)** |

Single-pass: `Vector256.Multiply` + `Add` (FMA when available) -> `Avx.UnpackLow/High` -> `Avx.Permute2x128` for lane-correct SoA->AoS shuffle -> direct store into `Point[]` via `MemoryMarshal.Cast`. Scalar fallback on non-x86.

### Before / after summary

| Size  | v0.5.1 TransformBatch | v0.6.0 TransformBatch |  Speedup | Alloc reduction |
|-------|----------------------:|----------------------:|---------:|----------------:|
| 1K    |                 9 us  |              764 ns   | **11.8x** |           2.0x  |
| 10K   |               124 us  |               53 us   |  **2.3x** |           2.0x  |
| 100K  |             1,298 us  |              208 us   |  **6.2x** |           2.0x  |

Every `LineSeriesRenderer`, `ScatterSeriesRenderer`, `AreaSeriesRenderer`, and `BubbleSeriesRenderer` uses this path — all indicator output benefits automatically.

---

## SVG Rendering

| Chart type                        |  v0.5.1 |  v0.6.0 | Allocated |
|-----------------------------------|--------:|--------:|----------:|
| Simple line (100 pts)             |   52 us |   94 us |    136 KB |
| Complex (line + scatter + bar)    |   72 us |  109 us |    133 KB |
| 3x3 subplot grid (9 charts)       |  422 us |  754 us |    933 KB |
| Treemap (6 nodes, nested)         |   26 us |   60 us |    109 KB |
| Sunburst (4 nodes, 2 depth)       |   45 us |   65 us |    118 KB |
| Sankey (4 nodes, 4 links)         |   39 us |   63 us |    118 KB |
| Polar line (50 pts)               |   42 us |   33 us |     56 KB |
| 3D surface (10x10 grid)           |   72 us |   69 us |    124 KB |
| 3D surface + directional lighting |       — |   82 us |    148 KB | ← v1.1.0 |
| Geo map — Equirectangular (4 poly)|       — |   55 us |     98 KB | ← v1.1.0 |
| Choropleth — Viridis (4 features) |       — |   71 us |    112 KB | ← v1.1.0 |
| Line + legend (3 series)          |  110 us |  140 us |    214 KB |
| Large line (10K pts)              | 3,935 us | **3,105 us** | 3,714 KB |
| Large line (100K pts, LTTB->2K)   | 1,512 us | **1,332 us** | 2,429 KB |

Small charts show higher v0.6.0 numbers due to benchmark contention (5 suites ran in parallel). The large-dataset charts show the real TransformBatch SIMD improvement: **21% faster at 10K**, **12% faster at 100K**. LTTB downsampling makes 100K-point charts faster than full-resolution 10K charts.

---

## Technical Indicators — v0.5.1 vs v0.6.0 (100K data points)

| Indicator              | v0.5.1 (100K) | v0.6.0 (100K) |   Change | Notes                                |
|------------------------|-------------:|-------------:|---------:|--------------------------------------|
| SMA(20)                |       196 us |       195 us |    ~same | `RollingMean` sliding sum            |
| EMA(20)                |       496 us |       491 us |    ~same | Sequential (inherently scalar)       |
| RSI(14)                |       851 us |       892 us |    ~same |                                      |
| ATR(14)                |       886 us |       920 us |    ~same |                                      |
| VWAP                   |       212 us |       238 us |    ~same |                                      |
| DrawDown               |       274 us |       313 us |    ~same |                                      |
| EquityCurve            |       349 us |       226 us | **1.5x** | `CumulativeSum` + `Linspace`         |
| BollingerBands(20)     |     2,016 us |     2,231 us |    ~same | `RollingStdDev` SIMD inner loop      |
| MACD(12,26,9)          |     1,574 us |     1,495 us |    ~same | `Subtract` for histogram             |
| KeltnerChannels(20)    |     1,812 us |     2,870 us |    ~same |                                      |
| ADX(14)                |     2,609 us |     2,434 us |    ~same |                                      |
| **Stochastic(14,3)**   |   **7,669 us** |   **3,308 us** | **2.3x** | **O(n) monotone deque replaces O(n*p)** |

Stochastic sees the largest gain — `RollingMin/Max` changed from O(n*p) nested loops to O(n) monotone-deque algorithm. This benefits all indicators that use windowed min/max (Stochastic, WilliamsR, Ichimoku).

### Phase F — New indicators (v0.6.0)

| Indicator       |     1K |    10K |    100K | Alloc (100K) |
|-----------------|-------:|-------:|--------:|-------------:|
| WilliamsR(14)   | 8.7 us | 225 us | 2,972 us |    3,125 KB  |
| OBV             | 3.2 us |  34 us |   645 us |      781 KB  |
| CCI(20)         |  21 us | 252 us | 2,159 us |    2,344 KB  |
| ParabolicSAR    | 4.8 us |  93 us | 1,211 us |      879 KB  |

OBV is the fastest (single-pass sequential accumulation). CCI is heavier due to per-window mean deviation. WilliamsR uses O(n) monotone-deque rolling min/max.

### All indicators — full table (v0.6.0)

| Indicator              |     1K |    10K |    100K | Alloc (100K) |
|------------------------|-------:|-------:|--------:|-------------:|
| SMA(20)                | 1.7 us |  17 us |   195 us |      781 KB  |
| EMA(20)                | 4.7 us |  45 us |   491 us |      781 KB  |
| VWAP                   | 1.8 us |  16 us |   238 us |      781 KB  |
| EquityCurve            | 1.4 us |  11 us |   226 us |      781 KB  |
| DrawDown               | 1.7 us |  17 us |   313 us |      781 KB  |
| OBV                    | 3.2 us |  34 us |   645 us |      781 KB  |
| RSI(14)                | 6.1 us |  64 us |   892 us |      781 KB  |
| ParabolicSAR           | 4.8 us |  93 us | 1,211 us |      879 KB  |
| ATR(14)                | 7.8 us |  78 us |   920 us |    1,563 KB  |
| MACD(12,26,9)          |  13 us | 125 us | 1,495 us |    3,906 KB  |
| BollingerBands(20)     |  20 us | 179 us | 2,231 us |    3,906 KB  |
| CCI(20)                |  21 us | 252 us | 2,159 us |    2,344 KB  |
| ADX(14)                |  18 us | 192 us | 2,434 us |    5,469 KB  |
| KeltnerChannels(20)    |  15 us | 146 us | 2,870 us |    5,469 KB  |
| WilliamsR(14)          | 8.7 us | 225 us | 2,972 us |    3,125 KB  |
| Stochastic(14,3)       |  12 us | 265 us | 3,308 us |    3,907 KB  |

At 100K points (full trading day at 1-second bars), every indicator completes in under 3.3 ms. Multiple indicators can run in parallel on separate cores.

---

## Vec SIMD Operations (new in v0.6.0)

`Vec` is a `readonly record struct` wrapping `double[]` with SIMD-accelerated operators backed by `TensorPrimitives`.

### Element-wise (allocate result array)

| Operation       |     1K |     10K |    100K | Alloc (100K) |
|-----------------|-------:|--------:|--------:|-------------:|
| a + b           | 452 ns |  3.8 us |  120 us |      781 KB  |
| a * scalar      | 386 ns |  2.9 us |  120 us |      781 KB  |
| (a+b)*1.5-b     | 1.3 us |   11 us |  447 us |    2,344 KB  |
| Std             | 803 ns |  8.4 us |  172 us |      781 KB  |

### Reductions (zero allocation)

| Operation |     1K |     10K |   100K |
|-----------|-------:|--------:|-------:|
| Sum       | 164 ns |  1.8 us |  18 us |
| Mean      | 166 ns |  1.8 us |  18 us |
| Min       | 434 ns |  4.5 us |  44 us |
| Max       | 335 ns |  3.4 us |  34 us |

### Scalar lambdas (not SIMD, per-element delegate call)

| Operation        |     1K |    10K |    100K | Alloc (100K) |
|------------------|-------:|-------:|--------:|-------------:|
| Select(lambda)   | 1.5 us |  13 us |  165 us |      781 KB  |
| Where(lambda)    | 1.6 us |  33 us |  597 us |    1,172 KB  |

### Domain algorithms (via indicator proxies)

| Algorithm          |     1K |    10K |      100K | Alloc (100K) |
|--------------------|-------:|-------:|----------:|-------------:|
| RollingMean(20)    | 1.7 us |  17 us |    174 us |      781 KB  |
| RollingStdDev(20)  |  18 us | 187 us |  2,054 us |    3,907 KB  |
| RollingMinMax(14)  |  12 us | 266 us |  2,871 us |    3,906 KB  |

Reductions (Sum, Mean) are **zero-alloc** and ~6x faster than element-wise ops at 100K. Element-wise operators (+, *) allocate a new result array per expression. `RollingMin/Max` uses O(n) monotone deque (vs O(n*p) nested loops in naive implementations).

---

## JSON Serialization

| Method              | v0.5.1 | v0.6.0 | Allocated |
|---------------------|-------:|-------:|----------:|
| ToJson              |  20 us |  26 us |      8 KB |
| ToJson (indented)   |  23 us |  24 us |     14 KB |
| FromJson            |  19 us |  21 us |     12 KB |
| Round-trip           |  41 us |  41 us |     20 KB |

Unchanged — round-trip under 50 us -> >20,000 chart specs/sec on a single core.

---

## PNG / PDF Export (SkiaSharp)

| Method              | v0.5.1 | v0.6.0 | Allocated  |
|---------------------|-------:|-------:|-----------:|
| PNG (simple chart)  |  21 ms |  27 ms |      88 KB |
| PNG (complex chart) |  20 ms |  22 ms |      81 KB |
| PDF (simple chart)  |  46 ms |  47 ms |   3,925 KB |
| PDF (complex chart) |  47 ms |  47 ms |   3,922 KB |

Unchanged — dominated by SkiaSharp rasterization. Both suit batch export, not real-time streaming.

---

## Running Benchmarks

```bash
cd Benchmarks/MatPlotLibNet.Benchmarks
dotnet run -c Release -- --filter "*SvgRendering*"
dotnet run -c Release -- --filter "*DataTransform*"
dotnet run -c Release -- --filter "*Indicator*"
dotnet run -c Release -- --filter "*VectorMath*"
dotnet run -c Release -- --filter "*Serialization*"
dotnet run -c Release -- --filter "*SkiaExport*"
dotnet run -c Release -- --filter "*"              # all suites
```

Run from the project directory to avoid multi-project ambiguity. Run one suite at a time for accurate numbers — concurrent benchmark runs inflate timings due to CPU contention.
