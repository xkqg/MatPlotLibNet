# Performance Benchmarks

The live dashboard section below and the headline figures of the wiki were measured for 1.30.0 on one box: an AMD Ryzen 9 9950X3D
(16C/32T), Windows 11, .NET 10.0.15, X64 RyuJIT, BenchmarkDotNet 0.15.8, Release, at below-normal priority. The sections after it are
named for the release they were measured on (v1.13.0 and older) and keep the figures of that release, taken on an AMD Ryzen 9 3950X
(16C/32T, 3.49 GHz) with .NET 10.0.6 and BenchmarkDotNet 0.14 / 0.15. Raw reports live under [`Benchmarks/MatPlotLibNet.Benchmarks/BenchmarkDotNet.Artifacts/results/`](Benchmarks/MatPlotLibNet.Benchmarks/BenchmarkDotNet.Artifacts/results/) (`*-report.csv`, `*-report-github.md`, `*-report.html`).

## A live dashboard, drawn again and again

A server that shows live charts draws the whole SVG of a figure every time its data moves and pushes it to the viewers.
`LiveWallBenchmarks` and the `--live-wall` modes of the benchmark program (the `LiveWall` folder) model one: a trend
figure fed a point every quarter second (two streaming lines of 720 points, two y-axes, a date axis, minor ticks, a grid
and browser interaction), and four figures built again on every draw: a row of eleven stat tiles with sparklines,
seven stacked line panels, four log-scale panels and a lit 40×40 3-D surface whose heights are a moving ripple. The
trend and the tiles are drawn every beat, the other three every two seconds.

### 1.29.0 against 1.30.0

Both releases ran the same benchmark program, the one of this release without the rows 1.29.0 cannot build (the server, MCP and
data-frame rows), built against the code of each (1.29.0 from its tag); the native control row, which 1.29.0 does not have, ran in
the whole program. Each measurement ran the two back to back, six rounds with the order alternating (1.30.0 first in three, 1.29.0
first in three), at below-normal priority. A figure is the mean of the six rounds, with the lowest and the highest in brackets.
What counts is the CPU time of the process, taken from the operating system, and the allocation; the clock is shown beside it.

`--live-wall-burst 400` (a round of the wall: eight beats of the trend and the tiles, then one of the slow parts):

| A round of the wall | 1.29.0 | 1.30.0 |
|---|---:|---:|
| CPU spent by the process | 22.8 ms (19.8–25.7) | **9.8 ms** (9.5–9.9), −57 % |
| time on the clock | 16.9 ms (16.4–17.3) | **7.0 ms** (6.6–7.2) |
| SVG written over 400 rounds | 922,809 KiB | **532,911 KiB**, −42 % |

One draw of each part (BenchmarkDotNet, short job, in process, memory diagnoser; 1 KB = 1,024 bytes):

| One draw | 1.29.0 | 1.30.0 | allocated 1.29.0 | allocated 1.30.0 |
|---|---:|---:|---:|---:|
| the trend, after one beat | 1.14 ms | **239 µs** (207–251) | 1,009.7 KB | **287.9 KB** |
| the tile row | 75.4 µs | **58.4 µs** (57.8–59.4) | 734.3 KB | **219.0 KB** |
| the seven panels | 3.84 ms | **532 µs** (531–535) | 3,575.6 KB | **856.6 KB** |
| the four log-scale panels | 2.19 ms | **295 µs** (294–296) | 2,038.8 KB | **494.7 KB** |
| the 3-D surface | 4.58 ms | **1.98 ms** (1.84–2.28) | 5,857.3 KB | **2,303.1 KB** |

Allocation per draw fell by 61 to 76 %, and with it the work of the garbage collector; the SVG is shorter too. What changed:

- a number is formatted straight into the document, instead of into a string of its own that is then copied in;
- a coordinate is written to two decimals, a hundredth of a pixel, by scaling it to a whole number of hundredths and writing
  that number's digits, with no floating-point formatting (a view attribute that a script reads back and an opacity keep every
  digit); an earlier try that wrote the two decimals through the double formatter saved only 2 % of the CPU and was left out;
- the document is copied into its string once instead of twice;
- which way a label reads is decided by its first strong character (rules P2 and P3 of the bidirectional algorithm),
  without resolving the whole algorithm for every label;
- the subplots of a figure are drawn one after another: handing them to the thread pool cost more CPU than it shared out;
- the 3-D surface writes the vertex data of its faces without boxing twelve numbers per face, and an axes that shares no scale with
  another no longer builds a set to find that out;
- two questions a draw asks over and over (whether the axes has a set limit, and the colour slot of every series) are read straight
  from the axes instead of building a list each time they are asked.

Measured and left as it is: reusing the builders between draws saves 4 % at the price of memory held by every thread.
`WithBrowserInteraction` puts every interaction script into every document, the 3-D, treemap and sankey ones included: on a chart
drawn many times a second, switch on only the interactions it uses.

### Building a figure

A figure built again for every draw pays its build every time, so a build is measured on its own (`BuilderBenchmarks`, and
the two rows of the live wall that build without drawing). The same six rounds:

| Build | 1.29.0 | 1.30.0 | allocated 1.29.0 | allocated 1.30.0 |
|---|---:|---:|---:|---:|
| one line, a title, a label | 231 ns | **207 ns** (205–211) | 4.07 KB | **3.03 KB** |
| a Pareto chart | 618 ns | 615 ns (604–632) | 7.77 KB | **5.74 KB** |
| a 3 x 3 grid of two lines each | 2.53 µs | **2.32 µs** (2.28–2.35) | 35.8 KB | **26.6 KB** |
| the seven panels of the wall | 2.58 µs | **2.39 µs** (2.38–2.42) | 33.3 KB | **25.1 KB** |
| the tile row of the wall | 3.06 µs | 3.04 µs (3.01–3.08) | 49.2 KB | **40.0 KB** |
| 10,000 vertical lines | 25.4 µs | 25.8 µs (25.6–26.1) | 473.4 KB | 472.0 KB |
| a ridgeline | 1.58 ms | **1.44 ms** (1.43–1.45) | 241.8 KB | **78.8 KB** |
| a figure read from JSON | 194 µs | **190 µs** (188–192) | 302.1 KB | **145.0 KB** |

Every build allocates less than in 1.29.0, except the 10,000 vertical lines, which allocate the same (472 against 473 KB). Five
builds are faster; the Pareto chart and the tile row take as long as in 1.29.0 (604–632 ns against 605–638, 3.01–3.08 µs against
3.04–3.08), and the 10,000 vertical lines take 25.8 µs against 25.4 (a second six rounds: 25.9 against 25.5, the ranges
overlapping). Both were slower before two fixes: hiding the four spines of a tile made a copy of the spines for each edge (3.37 µs),
and the line of the segments was written through a line's own arrays with a check of room for every segment (28.5 µs).

What a build pays for in 1.30.0, and what it was given back:

- **The change marks.** Every change to a figure takes a mark from one counter the whole process shares (an atomic increment, about
  3 ns). It is what lets a figure drawn again draw only the subplots that changed; a cheaper counter (marks per thread, or marks
  handed out by the draws) would let a change go unseen now and then.
- **What an author gave, kept apart from what the theme decides.** A value an author named stays apart from the resting one a theme
  supplies, so a given 4 is drawn as 4 under every theme: a `double?` where a `double` stood, a `bool?`, an `int?`. Each is a named
  field of an object the figure holds, and none is a second field for a fact another one holds.
- **Shared resting objects.** Every axes and figure starts from one shared legend, grid, spines, panes, tick settings and spacing
  (records no one can change) instead of making its own, and the theme a style describes is made once per change of the style, not
  once per build. A Pareto chart sorts and reorders its categories through one array sort instead of LINQ.

### Math labels and the matplotlib themes

The rows of `MathTextBenchmarks` and `MatplotlibThemeSvgBenchmarks`, written in a program of their own against the calls both
releases have, the same six rounds (a theme row changes the title of every subplot before each draw, so the whole figure is drawn):

| | 1.29.0 | 1.30.0 | allocated 1.29.0 | allocated 1.30.0 |
|---|---:|---:|---:|---:|
| five labels parsed (`MathTextParser.Parse`) | 461 ns | 782 ns (751–840) | 3,048 B | **2,608 B** |
| a fraction parsed | 109 ns | 183 ns (179–188) | 768 B | **552 B** |
| nine panels with minor ticks under the v2 theme, as SVG | 371 µs | **176 µs** (176–177) | 2,133.4 KB | **512.3 KB** |
| the same nine panels without minor ticks | 294 µs | **128 µs** (127–129) | 1,742.5 KB | **324.3 KB** |

The parse of a label that holds math is the one call that costs more than in 1.29.0. It reads the label as matplotlib's mathtext
does and builds the boxes its layout places (`$\sqrt{x^2 + y^2}$` is a root over two letters with their scripts, where 1.29.0 kept
one run of text) and then writes the list of spans from them; per box or span it writes, it takes less than 1.29.0 took per span. A
draw reads only the boxes, once for as long as the figure draws the label.

### Turning a 3-D view, and drawing only what changed

A figure drawn again draws only the axes that changed and hands out the others as they were drawn. Turning a lit 40×40
surface one degree a frame beside three line panels (`--drag`, which goes on until the CPU clock is read to 1 %, about three thousand frames; CPU time of the
process per frame and allocation per frame; the same six alternated rounds):

| One frame | 1.29.0 | 1.30.0 |
|---|---:|---:|
| SVG (`svg4`) | 5.05 ms (4.45–5.73), 7,356 KiB | **0.97 ms** (0.94–1.02), 2,049 KiB |
| SVG with the Skia package loaded, text written as glyph outlines (`svg4g`) | 8.68 ms (8.44–9.58), 9,453 KiB | **1.06 ms** (0.99–1.17), 2,241 KiB |
| the same drawn onto a Skia canvas through the renderer (`skia4`) | 7.89 ms (7.81–7.97), 1,657 KiB | **5.07 ms** (5.03–5.14), 670 KiB |
| the same in a native control, through the painter it holds (`ctl4`) | not there | **4.97 ms** (4.92–5.04), 589 KiB |
| the surface alone onto a Skia canvas (`skia1`) | 4.10 ms (4.06–4.15), 800 KiB | **3.48 ms** (3.44–3.51), 563 KiB |

Four things make up the difference: the subplots drawn one after another (above), the Skia glyph writer keeping the
outline of each label instead of shaping it again, the surface keeping the colour, the light and the vertex data of its
faces while only the camera moves, and the three panels handed out as they were.

Over SignalR, a viewer that subscribes with `SubscribeToParts` is sent only the parts that changed: one moved panel of seven is one
part of the seven, not the whole document, and a 3-D surface turned beside three panels sends the surface, which is most of the
document. When nothing changed, nothing is sent at all.

### The same in a native control

The WPF, Avalonia and Uno controls keep a recording of every subplot they painted and replay it when nothing it draws
from changed; a replay is the same pixels as a direct draw at scale 1 and at scale 2. In 1.29.0 a control drew the whole figure
through the renderer on every frame (the `skia4` row above); the painter takes that to 4.97 ms (`ctl4`), 37 % less CPU and a third
of the allocation. The panels are replayed; the turned surface is drawn again on every frame and is most of what remains: drawing
the surface's faces took 57.7 % of the frame and the lines of the panels 22 % before the painter, and the painter takes away the
panels' share, not the surface's.

## What's in v1.13.0

v1.13.0 is a **refactor &amp; cleanup release** that touched three hot paths, so the headline numbers
were re-measured (same Ryzen 9 3950X, .NET 10, BenchmarkDotNet 0.15, Release):

| Claim | v1.9.0 era | v1.13.0 measured | Note |
|---|---:|---:|---|
| SVG render, 1 000-point line | 66 µs | **33.4 µs** | render-path work accumulated over v1.10–v1.13 + the `StrokeStyle`/`ShapeStyle` record conversion with centralized visibility guards |
| SVG render, 100 000 points via LTTB | 1.78 ms | **0.81 ms** | same accumulation |
| JSON round-trip (Figure ⇄ JSON) | 40 µs | **30.8 µs** | deserialize now dispatches through per-series `FromSeriesDto` method groups (ToJson 16.9 µs / FromJson 13.9 µs) |
| PNG export via SkiaSharp (simple) | 27 ms | **12.9 ms** | primarily the v1.13.0 typeface fix: `ResolveTypeface` is now process-cached and the per-call `SKFontStyle` native allocation is gone |
| PDF export (simple / complex) | — | 26.2 / 29.3 ms | first published PDF numbers |

No regression on any measured path; the Skia export halving is the direct payoff of the
disposable-hygiene fixes. Raw reports in the artifacts folder as usual.

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
- **Lean on the server** — a figure is drawn in one pass and its document copied once (see "A live dashboard, drawn
  again and again" above)

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

In 1.30.0 the kernel became the value less where the range starts, times the pixels per unit, plus where the plot starts, each step rounded once (no fused multiply-add), so every path and every processor give a point the same bits and a narrow range far from 0 keeps its digits. Measured back to back on the same box: 1,000 points cost 343 ns against 294 ns, 10,000 points 35.2 µs against 35.0 µs (bound by memory); one direction of 100,000 values 31 µs against 76 µs, now one pass instead of two.

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
dotnet run -c Release -- --filter "*SkiaExport*"     # pictures, PDF pages and SVG with glyph outlines; run on its own because it loads the Skia package
dotnet run -c Release -- --filter "*LiveWall*"
dotnet run -c Release -- --filter "*Builder*"       # what building a figure costs, without drawing it (the fluent builders, two composed verbs, FromJson)
dotnet run -c Release -- --filter "*"              # all suites

dotnet run -c Release -- --live-wall 60            # a live dashboard for 60 s on its own cadences: cost per part
dotnet run -c Release -- --live-wall-burst 400     # the same mix without pauses, for a profiler or a CPU count
dotnet run -c Release -- --live-wall-dump out      # one SVG per part of the wall, to read the documents
dotnet run -c Release -- --drag ctl4 300           # a 3-D surface turned a degree a frame: ms, CPU and allocation per frame
dotnet run -c Release -- --timer-idle 60 1         # what one streaming figure's timer costs a process with no data changing

dotnet run -c Release -- --filter "*ChartPublisher*"      # a publish of a wall figure: document, patch, send over a stand-in hub
dotnet run -c Release -- --filter "*ChartSession*"        # twenty pan events drained by a chart session
dotnet run -c Release -- --filter "*Hover*"               # the nearest point under a cursor, three lines of 360 points
dotnet run -c Release -- --filter "*MathTextBenchmarks*"  # five typical labels parsed, and four math-labelled panels written as SVG text
dotnet run -c Release -- --filter "*MathTextSkia*"        # the same panels as pictures and as SVG glyph outlines; run on its own because it loads the Skia package
dotnet run -c Release -- --filter "*DataTransformBreaks*" # the break-aware transform against the plain one
dotnet run -c Release -- --filter "*MatplotlibThemeSvg*" # the figures the matplotlib themes are judged by, drawn as SVG (grids on minor ticks, a grid that asks for minor lines with no minor ticks on, a colour bar with its extension, a zoom indicator, patches, a figure legend, a classic contour, an interactive 3-D picture)
dotnet run -c Release -- --filter "*MatplotlibThemeSkia*" # the nine-panel figure of both matplotlib themes as a raster picture and as a PDF page, and the ask a render context answers to put a mark on a pixel grid, with and without snapping; run on its own because loading the Skia package changes how SVG text is written
dotnet run -c Release -- --theme-rows-bytes         # the length in bytes of the SVG of each of those SVG rows
```

How the program reads a `--drag` mode from the command line is held by a small suite, `Tst/MatPlotLibNet.Benchmarks`, which CI runs; the
measurements themselves are not tests.

The drag modes are `svg1 svg4 svg4g skia1 skia4 ctl1 ctl4`: the paint (`svg` the document a browser is sent, `skia` the renderer onto
a Skia canvas, `ctl` the painter a native control holds), then `1` for the surface alone or `4` for the surface with three line
panels; a trailing `g` loads the Skia package first, which writes SVG text as glyph outlines. BenchmarkDotNet raises its own process
to high priority; hold it at below-normal when the numbers are compared with a run made at below-normal.

### Comparing two builds in one process

With `--inProcess` every row of a filter runs in one process, one after the other, and two things that process carries from row to
row change what a later row measures.

**The Skia package.** Loading it makes every later SVG of the process write its text as glyph outlines, and it cannot be unloaded.
An SVG row that runs after a picture row therefore measures a longer document. Four math-labelled panels under the classic theme
were measured at 86.3 µs and 324.8 KB a draw when their SVG row ran after a picture row, and at 12.4 µs and 83.7 KB on their own,
while the same panels under the v2 theme read 11.7 µs because their row ran before the picture row. The two looked like one figure
that was handed its kept subplots and one that was drawn whole; both were handed their kept subplots, and the difference was the
text. A class therefore either names no type of the Skia package (`MathTextBenchmarks`, `MatplotlibThemeSvgBenchmarks`, the SVG
classes) or loads it for every row (`MathTextSkiaBenchmarks`, `MatplotlibThemeSkiaBenchmarks`, `SkiaExportBenchmarks`), and a class
that loads it runs in a process of its own. Measured like for like, six runs each: as text the v2 panels take 11.76 µs and the
classic panels 12.40 µs (73.5 and 83.7 KB); as outlines 63.3 µs and 84.2 µs (238.1 and 324.8 KB). The classic document is the
longer one in both, by its Computer Modern outlines.

**The large object heap.** A row whose document passes 85 KB (`TrendBeat` 291 KB, `Panels` 883 KB, `LogPanels` 508 KB,
`V2Grid3x3` 526 KB) puts every SVG string on the large object heap, and a full collection follows every few dozen draws. A process
then runs in one of two modes for all its rows: in one, the memory those collections free has to be committed again and the rows
cost 11 to 27 % more, with 2 to 6 s of the process's time spent in the kernel instead of about a second or less. Which mode a process lands in does not
follow the code: two builds whose draws are the same were measured slow in 4 and in 16 of 18 processes, and on another day slow in
6 of 6 each. Telling the runtime to keep its memory (`DOTNET_GCRetainVM=1`) does not change it (6 of 6 slow on both builds). Raising
the size at which an object goes on the large object heap does: with `DOTNET_GCLOHThreshold=400000` (a number the runtime reads as
hexadecimal, 4 MiB) both builds ran fast in 6 of 6 processes, `TrendBeat` 62.9 against 63.3 µs (six runs 62.3 to 69.4 and 62.7 to
63.7) and `Panels` 282.2 against 283.0 µs (281.3 to 283.8 and 279.5 to 283.9), against 97.2 and 347 µs without it. Set it when two
builds are compared, in the environment of the benchmark process:

```powershell
$env:DOTNET_GCLOHThreshold = '400000'; dotnet run -c Release -- --filter "*LiveWall*" --job short --inProcess
```

The variable changes where the runtime puts the string, so a row measured with it says what a build costs against another build,
not what a server with the runtime's default settings pays for a draw; that is the row without it. Alternate the two builds and
start half the rounds with each, since the second of two processes started one after the other reads a few percent slower.

The processor time of a process is read from a clock that moves in whole steps of the scheduler's tick (15.625 ms on Windows), so a
run is only as good as the number of steps it spans: 300 frames of 0.85 ms are sixteen steps or seventeen, a difference of 6 %, and
printed multiples of 0.052 ms per frame (0.78, 0.83, 0.89, 0.94) that were read as a 17 % slowdown of the SVG drag. A `--drag` run
therefore measures the step of the clock first, begins on a step, and goes on past the frames asked for until one step is at most a
hundredth of the time it read (about 1.6 s of processor time on Windows). The line it prints says how many frames that took and how far
the figure can be off (`1,800 frames ... cpu 0.88 ms/frame (+/-1.0 %)`); a frame too cheap to reach that in twenty times the frames
asked for ends the run with its error stated as it is. Six runs of `--drag svg4 300` then read 0.88 to 0.91 ms per frame. Compare
processor time of runs whose error is printed, never of runs of a few hundred frames of a cheap mode. `Tst/MatPlotLibNet.Benchmarks`
holds the rule (`CpuMeterTests`) on a clock that is quantised the same way.

A figure that is drawn again with nothing changed is handed the drawing it kept, which costs a fraction of a draw. In `SvgRenderingBenchmarks` the older rows (`SimpleLine`, `SubplotGrid3x3`, `Surface3D` and the rest) draw an unchanged figure and so measure that kept drawing; `Pie`, `MinorGrid`, `ViolinDraw`, `KdeDraw`, `PersistentLogRedraw`, `PersistentSymlogRedraw` and the rows ending in `Changed` change the first subplot (its title flips between two words of the same length) before every draw, so each of them is a draw of that subplot, which is what a live chart pays when its data moved. A row that kept its figure and draws it five times (`Persistent…`) reports the cost of one draw. The matplotlib-theme rows (`MatplotlibThemeSvgBenchmarks`, `MatplotlibThemeSkiaBenchmarks`) change every subplot before each draw, so a multi-panel figure is drawn whole.

Run from the project directory to avoid multi-project ambiguity. Run one suite at a time for accurate numbers — concurrent benchmark runs inflate timings due to CPU contention.
