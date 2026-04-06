# Performance Benchmarks

Benchmarked on AMD Ryzen 9 3950X, .NET 10, BenchmarkDotNet, Release mode.

## Why SVG over SignalR?

MatPlotLibNet renders charts **server-side as SVG** and pushes the result to clients via SignalR. This is fundamentally different from client-side charting libraries that ship data to the browser and re-render on every update.

**Benefits of server-side SVG + SignalR:**

- **No client-side rendering cost** -- the browser just swaps innerHTML. No JavaScript chart library, no canvas redraws, no layout recalculation. A simple line chart renders in ~52us on the server and arrives as a ready-to-display SVG string.
- **Inline SVG** -- the chart is part of the DOM. It can be styled with CSS, picked up by screen readers, and printed without rasterization artifacts.
- **Works outside the visible viewport** -- unlike canvas-based charts that skip off-screen rendering, SVG content exists in the DOM regardless of scroll position. Charts in collapsed panels, tabs, or below the fold are always ready.
- **Consistent output** -- every client sees the exact same chart. No browser rendering differences, no missing fonts, no WebGL compatibility issues.
- **Bandwidth-efficient updates** -- a typical chart SVG is 5-15 KB. SignalR pushes only the charts that changed, only to subscribed clients. No full-page refresh, no REST polling.
- **Scales with server hardware** -- parallel subplot rendering uses all available cores. A 3x3 grid renders in ~224us. Adding more server CPU directly improves throughput.

## SVG Rendering

| Method | Mean | Allocated |
|--------|-----:|----------:|
| Simple line chart (100 points) | 52 us | 82 KB |
| Complex chart (line + scatter + bar) | 72 us | 111 KB |
| 3x3 subplot grid (9 subplots) | 422 us | 483 KB |
| Treemap (nested 6 nodes) | 26 us | 42 KB |
| Sunburst (4 nodes, 2 depth) | 45 us | 50 KB |
| Sankey (4 nodes, 4 links) | 39 us | 60 KB |
| Polar line (50 points) | 42 us | 56 KB |
| 3D surface (10x10 grid) | 72 us | 124 KB |
| Line chart with legend (3 series) | 110 us | 159 KB |

Treemap is the fastest chart type at 26us — simpler geometry than line charts. 3D surface is comparable to a complex 2D chart. Legend adds ~60us overhead for color swatch + text measurement.

## JSON Serialization

| Method | Mean | Allocated |
|--------|-----:|----------:|
| ToJson | 20 us | 8 KB |
| ToJson (indented) | 23 us | 14 KB |
| FromJson | 19 us | 11 KB |
| Round-trip (serialize + deserialize) | 41 us | 19 KB |

JSON round-trip under 50us means real-time chart specs can be exchanged at >20,000 charts/sec on a single core.

## PNG / PDF Export (via SkiaSharp)

| Method | Mean | Allocated |
|--------|-----:|----------:|
| PNG (simple chart) | 21 ms | 78 KB |
| PNG (complex chart) | 20 ms | 54 KB |
| PDF (simple chart) | 46 ms | 3,765 KB |
| PDF (complex chart) | 47 ms | 3,760 KB |

PNG export is dominated by SkiaSharp rasterization. PDF is ~2x slower due to vector path encoding. Both are suitable for batch export, not real-time streaming.

## Technical Indicators (per 100K data points)

| Indicator | 1K | 10K | 100K | Allocated (100K) |
|-----------|---:|----:|-----:|-----------------:|
| SMA(20) | 2.1 us | 20 us | 196 us | 781 KB |
| EMA(20) | 5.7 us | 51 us | 496 us | 781 KB |
| RSI(14) | 9.4 us | 85 us | 851 us | 781 KB |
| ATR(14) | 9.7 us | 89 us | 886 us | 1,563 KB |
| VWAP | 1.9 us | 19 us | 212 us | 781 KB |
| DrawDown | 2.0 us | 19 us | 274 us | 781 KB |
| EquityCurve | 3.5 us | 34 us | 349 us | 781 KB |
| Bollinger Bands(20) | 21 us | 198 us | 2,016 us | 2,344 KB |
| MACD(12,26,9) | 16 us | 157 us | 1,574 us | 3,907 KB |
| Keltner Channels(20) | 18 us | 175 us | 1,812 us | 4,688 KB |
| ADX(14) | 28 us | 259 us | 2,609 us | 5,469 KB |
| Stochastic(14,3) | 76 us | 751 us | 7,669 us | 1,563 KB |

All indicators scale linearly with data size. At 100K data points (typical for intraday trading with 1-second bars over a full trading day), even the slowest indicator (Stochastic) completes in under 8ms — well within real-time update budgets.

## Running Benchmarks

```
dotnet run --project Benchmarks/MatPlotLibNet.Benchmarks -c Release -- --filter "*SvgRendering*"
dotnet run --project Benchmarks/MatPlotLibNet.Benchmarks -c Release -- --filter "*Indicator*"
dotnet run --project Benchmarks/MatPlotLibNet.Benchmarks -c Release -- --filter "*"
```
