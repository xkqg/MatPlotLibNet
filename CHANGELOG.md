# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.8.0] - Unreleased

### Added

**17 new series types (43 → 60)**

*Phase A — Statistical & categorical:*
- `RugplotSeries` — tick marks along X axis showing individual data distribution (`Vec Data`, `Height`, `Alpha`, `LineWidth`)
- `StripplotSeries` — jittered points per category (`double[][] Datasets`, `Jitter`, `MarkerSize`, `Alpha`)
- `EventplotSeries` — vertical tick lines per event row (`double[][] Positions`, `LineLength`, `Colors[]`)
- `BrokenBarSeries` — broken horizontal bars for Gantt-style ranges (`(double Start, double Width)[][]`, `BarHeight`)
- `CountSeries` — bar chart auto-counting category frequencies (`string[] Values`, `BarOrientation`)
- `PcolormeshSeries` — pseudocolor grid with irregular quad cells (`Vec X`, `Vec Y`, `double[,] C`, `IColorMap`)
- `ResidualSeries` — residual scatter from polynomial fit (`Vec XData`, `Vec YData`, `Degree`, `ShowZeroLine`)

*Phase B — Statistical helpers + dependent series:*
- `PointplotSeries` — mean + confidence interval per category dataset (`CapSize`, `ConfidenceLevel`)
- `SwarmplotSeries` — beeswarm-algorithm non-overlapping dot plot (`MarkerSize`, `Alpha`)
- `SpectrogramSeries` — STFT spectrogram heatmap (`Vec Signal`, `SampleRate`, `WindowSize`, `Overlap`, `IColorMap`)
- `TableSeries` — tabular data rendered inside axes (`string[][] CellData`, `ColumnHeaders`, `RowHeaders`)

*Phase C — Triangular mesh & field:*
- `TricontourSeries` — iso-contour lines on unstructured triangular mesh (`Vec X`, `Vec Y`, `Vec Z`, `Levels`)
- `TripcolorSeries` — pseudocolor fill on triangular mesh with auto-Delaunay (`int[]? Triangles`)
- `QuiverKeySeries` — reference arrow legend for quiver plots (axes-fraction position, `U`, `Label`)
- `BarbsSeries` — meteorological wind barbs with speed/direction flags (`Vec Speed`, `Vec Direction`, `BarbLength`)

*Phase D — 3D:*
- `Stem3DSeries` — vertical lines from XY-plane to 3D data points (`Vec X`, `Vec Y`, `Vec Z`, `MarkerSize`)
- `Bar3DSeries` — 3D rectangular prism bars with depth-sorted painter's algorithm (`BarWidth`)

**5 new numeric helpers**
- `Vec.Percentile(double p)` / `Vec.Quantile(double q)` — sorted linear-interpolation percentile on Vec
- `Fft` (public static) — Cooley-Tukey radix-2 DIT with Hann window; `Forward(double[])` + `Stft(...)` → `StftResult(Magnitudes, Frequencies, Times)`
- `BeeswarmLayout` (internal static) — greedy O(n²) circle-packing for swarm plots; falls back to deterministic jitter for N > 1000
- `Delaunay` (public static) — Bowyer-Watson incremental triangulation returning `TriMesh(int[] Triangles, double[] X, double[] Y)`
- `HierarchicalClustering` (public static) — Ward's method agglomerative clustering returning `Dendrogram(DendrogramNode[] Merges, int[] LeafOrder)`

**3 new FigureTemplates**
- `FigureTemplates.PairPlot(double[][] columns, string[]? columnNames, int bins)` — N×N grid; diagonal = histograms, off-diagonal = scatter
- `FigureTemplates.FacetGrid(double[] x, double[] y, string[] category, Action<AxesBuilder, double[], double[]> plotFunc, int cols)` — one subplot per unique category
- `FigureTemplates.Clustermap(double[,] data, string[]? rowLabels, string[]? colLabels)` — 2×2 GridSpec heatmap with row/column dendrograms

**Tests: 1924 → 2268 (+344)**

---

## [0.7.0] - Unreleased

### Added

**Feature 4a — KdeSeries + GaussianKde**
- `KdeSeries` (sealed, Distribution family) — kernel density estimation rendered as a filled area + density curve
  - Properties: `Data[]`, `Bandwidth` (double?, null = auto Silverman), `Fill` (bool, default true), `Alpha` (double, default 0.3), `LineWidth` (double, default 1.5), `Color`, `LineStyle`
  - Implements `ISeriesSerializable`, `IHasDataRange` (30% X padding, density curve Y range)
- `GaussianKde` (internal static, `Rendering/SeriesRenderers/Distribution/`) — Gaussian KDE math helper
  - `SilvermanBandwidth(double[] sortedData)` → `1.06 * σ * n^(-0.2)`, fallback 1.0 for constant/degenerate data
  - `Evaluate(double[] sortedData, double bandwidth, int numPoints=100)` → `(double[] X, double[] Density)` over [min-3h, max+3h]
- `KdeSeriesRenderer` — sorts data → bandwidth → `GaussianKde.Evaluate` → optional filled polygon + density polyline
- `Axes.Kde()`, `AxesBuilder.Kde()`, `FigureBuilder.Kde()` — fluent factory methods
- `SeriesRegistry` registration for `"kde"` type discriminator
- `SeriesDto.Bandwidth` (`double?`) added
- Series count: 40 → 41

**Feature 4b — RegressionSeries + LeastSquares**
- `RegressionSeries` (sealed, XY family) — polynomial regression line with optional confidence bands
  - Properties: `XData[]`, `YData[]`, `Degree` (int, default 1), `ShowConfidence` (bool, default false), `ConfidenceLevel` (double, default 0.95), `LineWidth` (double, default 2.0), `Color`, `BandColor`, `BandAlpha` (double, default 0.2), `LineStyle`
- `LeastSquares` (public static, `Numerics/`) — polynomial regression math helper
  - `PolyFit(double[] x, double[] y, int degree)` → coefficient array `[a₀, a₁, ..., aₙ]` via normal equations, degree 0–10
  - `PolyEval(double[] coefficients, double[] x)` → evaluated Y values via Horner's method
  - `ConfidenceBand(double[] x, double[] y, double[] coeff, double[] evalX, double level=0.95)` → `(double[] Upper, double[] Lower)` using leverage-based t-distribution intervals
- `RegressionSeriesRenderer` — 100 eval points on linspace, optional confidence-band polygon
- `Axes.Regression()`, `AxesBuilder.Regression()` — fluent factory methods
- `SeriesRegistry` registration for `"regression"` type discriminator
- `SeriesDto.Degree` (`int?`), `SeriesDto.ShowConfidence` (`bool?`), `SeriesDto.ConfidenceLevel` (`double?`) added
- Series count: 41 → 42

**Feature 4c — HexbinSeries + HexGrid**
- `HexbinSeries` (sealed, Grid family) — 2D hexagonal bin density plot
  - Properties: `X[]`, `Y[]`, `GridSize` (int, default 20), `MinCount` (int, default 1), `ColorMap`, `Normalizer`
  - Implements `IColormappable`, `INormalizable`, `IColorBarDataProvider`
- `HexGrid` (internal static, namespace `MatPlotLibNet.Numerics`) — flat-top hex bin math helper
  - `ComputeHexBins(...)` → `Dictionary<(int q, int r), int>` count map using axial (q,r) cube-coordinate rounding
  - `HexagonVertices(cx, cy, hexSize)` → 6 vertex coordinates for a flat-top hexagon
  - `HexCenter(q, r, hexSize, ...)` → (X, Y) center coordinates
- `HexbinSeriesRenderer` — renders colored hexagonal polygons with 5% visual gap; uses `HexGrid.ComputeHexBins`
- `Axes.Hexbin()`, `AxesBuilder.Hexbin()` — fluent factory methods
- `SeriesRegistry` registration for `"hexbin"` type discriminator
- `SeriesDto.GridSize` (`int?`), `SeriesDto.MinCount` (`int?`) added
- Series count: 42 → 43

**Feature 4d — JointPlotBuilder**
- `FigureTemplates.JointPlot(double[] x, double[] y, string? title = null, int bins = 30)` — scatter + marginal histogram template
  - 2×2 `GridSpec` with `heightRatios=[1,4]`, `widthRatios=[4,1]`
  - Top marginal: `Histogram(x)` at `GridPosition(0,1,0,1)`
  - Center: `Scatter(x, y)` at `GridPosition(1,2,0,1)`
  - Right marginal: `Histogram(y)` at `GridPosition(1,2,1,2)`

**Feature 5a — Data Attributes Foundation**
- `Figure.EnableLegendToggle`, `EnableRichTooltips`, `EnableHighlight`, `EnableSelection` (bool) — per-feature interactivity flags
- `Figure.HasInteractivity` (bool) — true when any flag is set; used to gate data-attribute emission
- `Axes.EnableInteractiveAttributes` (bool) — propagated by `SvgTransform` before parallel rendering
- `SvgRenderContext.BeginDataGroup(string cssClass, int seriesIndex)` — emits `<g class="..." data-series-index="N">`
- `SvgRenderContext.BeginLegendItemGroup(int legendIndex)` — emits `<g data-legend-index="N" style="cursor:pointer">`
- `AxesRenderer.RenderSeries()` — wraps each series in a `data-series-index` group when `EnableInteractiveAttributes`
- `AxesRenderer.RenderLegend()` — wraps each legend entry in a `data-legend-index` group when `EnableInteractiveAttributes`

**Feature 5b — Legend Toggle Script**
- `SvgLegendToggleScript` — click `[data-legend-index=N]` → toggles `display` on `g[data-series-index=N]` + dims legend entry opacity to 0.4
- `FigureBuilder.WithLegendToggle(bool enabled = true)` — fluent enable method
- Injected by `SvgTransform` when `Figure.EnableLegendToggle` is true

**Feature 5c — Rich Tooltips Script**
- `SvgCustomTooltipScript` — intercepts `<title>` elements and shows a styled floating `div` tooltip instead of native browser tooltip
- `FigureBuilder.WithRichTooltips(bool enabled = true)` — fluent enable method
- Injected by `SvgTransform` when `Figure.EnableRichTooltips` is true

**Feature 5d — Highlight Script**
- `SvgHighlightScript` — `mouseenter` on `g[data-series-index]` → dims siblings to 0.3 opacity; `mouseleave` → restores all to 1.0
- `FigureBuilder.WithHighlight(bool enabled = true)` — fluent enable method
- Injected by `SvgTransform` when `Figure.EnableHighlight` is true

**Feature 5e — Selection Script**
- `SvgSelectionScript` — Shift+mousedown draws a blue selection rectangle; mouseup dispatches `CustomEvent('mpl:selection', { detail: { x1, y1, x2, y2 } })` on the SVG element
- `FigureBuilder.WithSelection(bool enabled = true)` — fluent enable method
- Injected by `SvgTransform` when `Figure.EnableSelection` is true

**Notebooks package fix**
- `MatPlotLibNet.Notebooks.csproj` — added `<BuildOutputTargetFolder>interactive-extensions/dotnet</BuildOutputTargetFolder>` so Polyglot Notebooks auto-discovers `NotebookExtension` via `IKernelExtension`
- `Microsoft.DotNet.Interactive` reference now carries `PrivateAssets="all"` to prevent transitive dependency leakage

**Test suite:** 1924 tests (up from 1777), zero regressions.

**Feature 1 — Style Sheets / rcParams**
- `RcParams` global configuration registry — typed dictionary keyed by string (e.g., `"font.size"`, `"lines.linewidth"`, `"axes.grid"`), thread-safe via `AsyncLocal<T>` scoping
- `RcParams.Default` static instance with hard-coded defaults matching current behavior
- `RcParams.Current` resolves scoped override → Default (AsyncLocal per async flow)
- `RcParamKeys` static constants for all supported keys — compile-time safe, no string typos
- `StyleSheet` named bundle of `RcParams` overrides — `StyleSheet.FromTheme(Theme)` bridge converts existing 6 themes to style sheets
- `StyleContext : IDisposable` scoped override — pushes `RcParams` layer on construct, pops on `Dispose()`; nests arbitrarily
- `StyleSheetRegistry` thread-safe `ConcurrentDictionary` — all 6 built-in themes auto-registered as style sheets
- `Plt.Style.Use(name)` / `Plt.Style.Use(StyleSheet)` — modifies global defaults (matches `matplotlib.pyplot.style.use()`)
- `Plt.Style.Context(name)` / `Plt.Style.Context(StyleSheet)` — returns `StyleContext` for scoped overrides (matches `matplotlib.pyplot.style.context()`)
- `Theme.ToStyleSheet()` — converts any `Theme` to a `StyleSheet` for use with rcParams
- Precedence: explicit property > Theme > `RcParams.Current` > `RcParams.Default`
- `FigureBuilder`, `CartesianAxesRenderer`, `LineSeriesRenderer`, `ScatterSeriesRenderer` consult `RcParams.Current` for defaults when no explicit value is set

**Feature 2 — Filled Contours (ContourfSeries)**
- `ContourfSeries` (sealed, Grid family) — filled contour plot rendering colored bands between consecutive iso-levels
  - Properties: `XData[]`, `YData[]`, `ZData[,]`, `Levels` (int, default 10), `Alpha` (double, default 1.0), `ShowLines` (bool, default true), `LineWidth` (double, default 0.5), `ColorMap`, `Normalizer`
  - Implements `IColormappable`, `INormalizable`, `IColorBarDataProvider`
- `ContourfSeriesRenderer` — painter's algorithm: fills entire plot area with bottom band color, then paints ascending iso-level regions over previous using `DrawPolygon()`; optional iso-line overlay via `DrawLines()`
- `MarchingSquares.ExtractBands()` — new method producing `ContourBand[]` (closed polygon bands between iso-levels)
- `ContourBand` `readonly record struct` — `(double LevelLow, double LevelHigh, PointF[][] Polygons)`
- `ISeriesVisitor.Visit(ContourfSeries)` — new visitor overload
- `Axes.Contourf()`, `AxesBuilder.Contourf()`, `FigureBuilder.Contourf()` — fluent API methods
- `SeriesRegistry` registration for `"contourf"` type discriminator
- Series count: 39 → 40

**Feature 3 — Image Compositing**
- `IInterpolationEngine` strategy interface — `Resample(double[,] data, int targetRows, int targetCols)`
- `NearestInterpolation` (singleton) — identity / pixel duplication (existing behavior)
- `BilinearInterpolation` (singleton) — 2×2 neighborhood, linear weights
- `BicubicInterpolation` (singleton) — 4×4 neighborhood, Catmull-Rom / Keys kernel with output clamping to prevent ringing
- `InterpolationRegistry` thread-safe `ConcurrentDictionary` — maps `"nearest"` / `"bilinear"` / `"bicubic"` to engine instances (mirrors `ColorMapRegistry` pattern)
- `BlendMode` enum — `Normal`, `Multiply`, `Screen`, `Overlay`
- `CompositeOperation` static utility — `Color Blend(Color src, Color dst, BlendMode mode)`
- `ImageSeries.Alpha` (`double`, default 1.0) — overall opacity
- `ImageSeries.BlendMode` (`BlendMode`, default `Normal`) — alpha composite blend mode
- `ImageSeriesRenderer` enhanced — resolves `InterpolationRegistry.Get(series.Interpolation)` to resample data before rendering; upsampled grid capped at min(source×4, 256) to prevent SVG size explosion

**Test suite:** 1777 tests (up from 1668), zero regressions.

## [0.6.0] - 2026-04-09

### Added

**Batch 1 — VectorMath SIMD Kernel**
- `VectorMath` (`internal static`) — `System.Numerics.Tensors.TensorPrimitives` wrappers: `Add`, `Subtract`, `Multiply`, `Divide`, `Sum`, `Min`, `Max`, `Abs`, `Negate`, `MultiplyAdd`
- `VectorMath` domain algorithms: `Linspace`, `RollingMean`, `RollingMin`, `RollingMax` (O(n) monotone deque), `RollingStdDev`, `CumulativeSum`, `StandardDeviation`, `SplitPositiveNegative`
- `Vec` (`public readonly record struct`) — LINQ-style wrapper with SIMD-accelerated operators (`+`, `-`, `*`, `/`, unary `-`), reductions (`Sum`, `Min`, `Max`, `Mean`, `Std`), scalar lambdas (`Select`, `Where`, `Zip`, `Aggregate`), and implicit `double[]` conversions
- `System.Numerics.Tensors` NuGet dependency added to main package

**Batch 2 — DataTransform Batch Path**
- `DataTransform.TransformX(ReadOnlySpan<double>)` — SIMD batch X coordinate transform
- `DataTransform.TransformY(ReadOnlySpan<double>)` — SIMD batch Y coordinate transform
- `DataTransform.TransformBatch(ReadOnlySpan<double>, ReadOnlySpan<double>)` — single-pass AVX SIMD interleave (FMA → UnpackLow/High → Permute2x128 → direct store), zero intermediate allocations, 3.6× faster than per-point loop at 1K points
- `VectorMath.TransformInterleave` — SoA→AoS affine transform kernel with AVX fast path and scalar fallback
- 8 series renderers refactored to pre-compute batch pixel coordinates: `LineSeriesRenderer`, `AreaSeriesRenderer`, `ScatterSeriesRenderer`, `StepSeriesRenderer`, `EcdfSeriesRenderer`, `StackedAreaSeriesRenderer`, `ErrorBarSeriesRenderer`, `BubbleSeriesRenderer`

**Batch 3 — Indicator Refactoring**
- All 15 indicators (`Sma`, `Ema`, `BollingerBands`, `Stochastic`, `Ichimoku`, `Adx`, `Atr`, `Rsi`, `Macd`, `KeltnerChannels`, `Vwap`, `EquityCurve`, `DrawDown`, `ProfitLoss`, `Indicator.ApplyOffset`) refactored to use `VectorMath` instead of scalar loops

**Batch 4 — Phase F Indicators**
- `WilliamsR` — Williams %R momentum indicator (-100..0), reference lines at -20 and -80
- `Obv` — On-Balance Volume, sequential cumulative indicator
- `Cci` — Commodity Channel Index, mean-deviation oscillator, reference lines at ±100
- `ParabolicSar` — Parabolic SAR trend indicator; returns `ParabolicSarResult(double[] Sar, bool[] IsLong)`
- `AxesBuilder` shortcuts: `WilliamsR()`, `Obv()`, `Cci()`, `ParabolicSar()`

**Batch 5 — Chart Templates**
- `FigureTemplates.FinancialDashboard()` — 3-panel chart (price/candlestick 60%, volume 15%, oscillator 25%) with shared X axis and custom GridSpec height ratios
- `FigureTemplates.ScientificPaper()` — N×M subplot grid, 150 DPI, tight layout, hidden top/right spines
- `FigureTemplates.SparklineDashboard()` — vertically stacked sparklines, one row per data series with Y label

**Batch 6 — Contour Labels (Marching Squares)**
- `MarchingSquares` (`internal static`) in `Rendering/Algorithms/` — 4-bit cell classification, edge interpolation, greedy segment joining into polylines
- `ContourSeries.LabelFormat` (`string?`, default `"G4"`) — format string for contour level labels
- `ContourSeries.LabelFontSize` (`double`, default `10`) — font size for contour level labels
- `ContourSeriesRenderer` — now draws iso-lines via marching-squares; `ShowLabels = true` renders centered labels with white background rectangles

**Batch 7 — Polyglot Notebooks**
- New package `MatPlotLibNet.Notebooks` — `IKernelExtension` for Polyglot Notebooks / Jupyter
- `NotebookExtension` — registers `Figure` as an inline SVG display type via `Formatter.Register<Figure>`
- `FigureFormatter` — wraps `figure.ToSvg()` in a `<div>` for notebook cell output

**Batch 8 — Benchmarks**
- `VectorMathBenchmarks.cs` — benchmarks Vec SIMD operators, reductions, and domain algorithm proxies
- `DataTransformBenchmarks.cs` — per-point loop vs TransformBatch comparison
- Extended `IndicatorBenchmarks.cs` — added WilliamsR, OBV, CCI, ParabolicSar
- Extended `SvgRenderingBenchmarks.cs` — added 10K-point line chart and 100K-point LTTB chart
- Updated `BENCHMARKS.md` with new sections

### Fixed
- `Macd.Compute()` — guard against out-of-range slice when MACD data is shorter than the signal period

## [0.5.1] - 2026-04-09

### Added

**Phase C — Text & Annotation**
- `Annotation.Alignment` (`TextAlignment`) — horizontal text alignment; default `Left`
- `Annotation.Rotation` (`double`) — text rotation in degrees; default 0
- `Annotation.ArrowStyle` (`ArrowStyle` enum) — `None`, `Simple` (existing line), `FancyArrow` (line + triangular arrowhead); default `Simple`
- `Annotation.BackgroundColor` (`Color?`) — optional fill rect drawn behind annotation text
- `ArrowStyle` enum — `None`, `Simple`, `FancyArrow`
- `BarSeries.ShowLabels` / `.LabelFormat` — auto-label bars with their values; format string is optional (defaults to G4)
- `ContourSeries.ShowLabels` — reserves property for future contour line labeling (rendering deferred to v0.6.0; requires marching-squares)
- `IRenderContext.DrawText(text, position, font, alignment, rotation)` — overload with rotation; default interface method ignores rotation (backward-compatible)
- `SvgRenderContext.DrawText(..., rotation)` — emits `transform="rotate(…)"` on the SVG text element

**Phase D — Tick System**
- `ITickLocator` interface — `double[] Locate(double min, double max)` strategy for axis tick positions
- `AutoLocator(int targetCount = 5)` — extracts the existing nice-number algorithm as a reusable locator
- `MaxNLocator(int maxN)` — nice numbers capped to at most `maxN` ticks
- `MultipleLocator(double baseValue)` — ticks at exact multiples of base in `[min, max]`
- `FixedLocator(double[] positions)` — returns exactly the provided positions filtered to range
- `LogLocator` — powers of 10 within range
- `EngFormatter` — SI prefix formatting: 1000→"1k", 1M→"1M", 1e-3→"1m", 1e-6→"1µ" etc.
- `PercentFormatter(double max)` — `value/max*100` + "%" suffix
- `Axis.TickLocator` (`ITickLocator?`) — per-axis custom locator; overrides default algorithm
- `Axis.MajorTicks` and `Axis.MinorTicks` are now settable (changed from `{ get; }` to `{ get; set; }`)
- Minor tick rendering — when `Axis.MinorTicks.Visible = true`, 5 minor subdivisions per major interval are drawn at half the tick length (3 px vs 5 px), no labels
- `TickConfig.Spacing` is now respected: auto-creates `MultipleLocator(spacing)` when no explicit locator is set
- `AxesBuilder.SetXTickLocator()` / `SetYTickLocator()` — fluent tick locator configuration
- `AxesBuilder.WithMinorTicks(bool)` — enables minor ticks on both axes
- Bug fix: secondary Y-axis tick labels now correctly use `Axes.SecondaryYAxis.TickFormatter` (was calling `FormatTick` unconditionally)
- Bug fix: `PolarAxesRenderer` ring labels now use `Axes.YAxis.TickFormatter` when set

**Phase E — Performance**
- `IDownsampler` interface — `(double[] X, double[] Y) Downsample(double[] x, double[] y, int targetPoints)`
- `LttbDownsampler` — Largest-Triangle-Three-Buckets O(n) algorithm; preserves visual peaks/troughs; always keeps first and last point
- `ViewportCuller` (static) — filters XY data to `[xMin, xMax]` keeping one point on each side for correct line clipping
- `XYSeries.MaxDisplayPoints` (`int?`) — opt-in downsampling for `LineSeries`, `AreaSeries`, `ScatterSeries`, `StepSeries`; viewport culling followed by LTTB when enabled
- `DataTransform.DataXMin/XMax/YMin/YMax` — public properties exposing the current viewport bounds (needed by renderers to pass to `ViewportCuller`)
- `AxesBuilder.WithDownsampling(int maxPoints = 2000)` — fluent downsampling configuration on last XY series
- `AxesBuilder.WithBarLabels(string? format = null)` — fluent bar label configuration on last bar series

### Changed

- `CartesianAxesRenderer` tick computation now calls `ComputeTickValues(min, max, Axis)` — respects `TickLocator` and `Spacing`
- `BarSeriesRenderer` — appends value text above vertical bars / beside horizontal bars when `ShowLabels = true`
- `LineSeriesRenderer`, `AreaSeriesRenderer`, `StepSeriesRenderer` — apply viewport culling + LTTB before rendering when `MaxDisplayPoints` is set
- `ScatterSeriesRenderer` — applies viewport culling when `MaxDisplayPoints` is set

## [0.5.0] - 2026-04-09

### Added

- `GridSpec` model — unequal subplot layouts with row/col height/width ratios and cell spanning
- `SpinesConfig` — per-spine show/hide/position (`Edge`, `Data`, `Axes` fraction) via `AxesBuilder.WithSpines()`, `.HideTopSpine()`, `.HideRightSpine()`
- Shared axes (`ShareX`/`ShareY`) with union range computation across linked subplots
- Inset axes — `AddInset(x, y, w, h)` on `AxesBuilder` with recursive rendering (depth guard = 3)
- `ImageSeries` (imshow) — display 2D data as colored pixels with colormap + `VMin`/`VMax`, implements `IColormappable`, `INormalizable`, `IColorBarDataProvider`
- `Histogram2DSeries` — 2D density histogram binning scatter data into a grid, implements `IColormappable`, `INormalizable`, `IColorBarDataProvider`
- `StreamplotSeries` — vector field streamlines with configurable `Density` and `ArrowSize`
- `EcdfSeries` — empirical cumulative distribution function (sorted XY series)
- `StackedAreaSeries` — stacked filled areas (stackplot) with `X[]`, `YSets[][]`, `StackLabels`, `FillColors`
- `ICategoryLabeled` — polymorphic tick-label resolution for bar/candlestick series; eliminates per-type casts in renderers
- `IColorBarDataProvider` — colorbar auto-detection from series data range + colormap; eliminates type dispatch in `AxesBuilder.WithColorBar()`
- `IStackable` — stacking offset computation for bar series
- `IRenderContext.BeginGroup`/`EndGroup` — default interface methods; eliminated 6 type casts across renderers
- `PathSegment.ToSvgPathData()` — polymorphic SVG path rendering; eliminated 5-case `switch` in `SvgSeriesRenderer`
- Series count increased from 34 to 39 chart types
- `IColormappable` interface — `IColorMap? ColorMap { get; set; }` — implemented by all 7 series that support colormaps (`HeatmapSeries`, `ImageSeries`, `Histogram2DSeries`, `ContourSeries`, `SurfaceSeries`, `ScatterSeries`, `HierarchicalSeries`)
- `INormalizable` interface — `INormalizer? Normalizer { get; set; }` — implemented by `HeatmapSeries`, `ImageSeries`, `Histogram2DSeries`
- **20 new colormaps** (52 base total, 104 with reversed `_r` variants):
  - Sequential: `Hot`, `Copper`, `Bone`, `BuPu`, `GnBu`, `PuRd`, `RdPu`, `YlGnBu`, `PuBuGn`, `Cubehelix`
  - Diverging: `PuOr`, `Seismic`, `Bwr`
  - Qualitative: `Pastel2`, `Dark2`, `Accent`, `Paired`
  - Special: `Turbo` (perceptually-uniform rainbow), `Jet` (legacy rainbow), `Hsv` (cyclic hue)
- 502 new tests (1502 total); category-specific theories: monotonic brightness, diverging midpoint neutrality, cyclic start≈end, qualitative color distinctness

### Changed

- `FigureBuilder.WithGridSpec()` / `AddSubPlot(GridPosition, ...)` for GridSpec-based unequal subplot layouts
- `AxesBuilder.WithSpines()`, `.HideTopSpine()`, `.HideRightSpine()` for spine control
- `AxesBuilder.ShareX(key)` / `.ShareY(key)` for shared-axis range synchronization
- `AxesBuilder.AddInset(x, y, w, h, configure)` for inset axes
- `AxesBuilder.WithColorMap(IColorMap)` — replaced 4-branch `if/else if` type chain with `if (last is IColormappable c)` — now covers all 7 colormappable series (previously missed `SurfaceSeries`, `ScatterSeries`, `HierarchicalSeries`)
- `AxesBuilder.WithNormalizer(INormalizer)` — replaced 3-branch `if/else if` type chain with `if (last is INormalizable n)`

## [0.4.1] - 2026-04-06

### Added

- `ISeriesSerializable` interface on all 34 series — each series serializes itself, eliminating the 152-line `SeriesToDto` switch in `ChartSerializer`
- `SeriesRegistry` for deserialization with `ConcurrentDictionary`-based type lookup
- `IHasDataRange` interface for series that expose their own data bounds
- `IPolarSeries` interface for polar coordinate series
- `I3DGridSeries` and `I3DPointSeries` interfaces for 3D series families
- `IPriceSeries` interface for financial OHLC series
- Generic base classes: `XYSeries`, `PolarSeries`, `GridSeries3D`, `HierarchicalSeries`
- Color constants: `Tab10Blue`, `Tab10Orange`, `Tab10Green`, `GridGray`, `EdgeGray`, `Amber`, `FibonacciOrange` — replacing magic hex strings throughout the codebase
- `IAnimation<TState>` interface and `AnimationController<TState>` for typed animation pipelines
- `LegacyAnimationAdapter` bridges `AnimationBuilder` to `IAnimation<TState>` contract
- `ConfigureAwait(false)` in `AnimationController` for library-safe async

### Changed

- Target frameworks changed to `net10.0;net8.0` (dropped `netstandard2.1`)
- Removed `IsExternalInit` polyfill (no longer needed without netstandard2.1)
- `FigureBuilder` SRP: `Save()`, `Transform()`, `ToSvg()` moved to `FigureExtensions` — builder only builds
- `FigureExtensions.RegisterTransform()` replaces `FigureBuilder.RegisterGlobalTransform()` for startup-time format registration
- `GlobalTransforms` registry uses `ConcurrentDictionary` for thread safety
- `AxesRenderer` registry uses `ConcurrentDictionary` for thread-safe coordinate system dispatch
- Volatile fields used for thread-safe state in animation and rendering pipelines
- Publish workflow fix: build before pack for Skia/MAUI projects
- Warning cleanup: xUnit1051 `CancellationToken` warnings and CS8604 nullable reference warnings resolved

## [0.4.0] - 2026-04-06

### Added

- `Projection3D` class for 3D-to-2D projection with elevation/azimuth rotation and depth sorting
- `DataRange3D` record struct for 3D data bounds
- `SurfaceSeries` — colored quadrilateral surface with optional wireframe overlay
- `WireframeSeries` — 3D wireframe grid rendering
- `Scatter3DSeries` — 3D scatter with depth-based size variation
- `ChartRenderer.Render3DAxes()` — 3D bounding box wireframe, axis labels, painter's algorithm
- `ColorBar` record with auto-detect from heatmap/contour data range and colormap
- `AxesBuilder.WithColorBar()` and `WithProjection(elevation, azimuth)` fluent methods
- `FigureBuilder.Save(path)` with auto-detect format from extension (no extension = SVG)
- `AnimationBuilder` class for frame-based animation (FrameCount, Interval, Loop, GenerateFrames)
- `InteractiveFigure.AnimateAsync()` for pushing animation frames via SignalR
- `CoordinateSystem` enum (`Cartesian`, `Polar`, `ThreeD`) on `Axes` for alternative rendering paths
- `PolarTransform` class for (r, theta) to pixel coordinate conversion
- `PolarLineSeries`, `PolarScatterSeries`, `PolarBarSeries` in new Polar family
- `ChartRenderer.RenderPolarAxes()` — circular grid, radial axis lines, angle labels
- `FigureBuilder.ToSvg()`, `ToJson()`, `SaveSvg()`, `Transform()`, `Save(path)` — output directly from the builder without `.Build()`
- `FigureBuilder.Save(path)` auto-detects format from file extension (.svg, .png, .pdf, .json)
- `TreeNode` record for hierarchical data (Label, Value, Color, Children with recursive TotalValue)
- `HierarchicalSeries` abstract base class with shared Root, ColorMap, ShowLabels properties
- `TreemapSeries` — nested rectangle layout with configurable padding
- `SunburstSeries` — concentric ring segments with configurable inner radius
- `TreemapSeriesRenderer` — squarified slice-and-dice layout algorithm
- `SunburstSeriesRenderer` — arc-based radial rendering with recursive depth
- `SankeySeries` — flow diagram with nodes and bezier-curved links
- `SankeyNode` and `SankeyLink` records for Sankey data model
- `SankeySeriesRenderer` — BFS column layout, curved link rendering, node labels
- Legend rendering in `ChartRenderer.RenderLegend()` with color swatches and position control
- `SubPlotSpacing` record with configurable margins, gaps, and `TightLayout` flag
- `ITickFormatter` interface for pluggable axis tick formatting
- `DateTickFormatter` — formats OLE Automation dates with configurable format string
- `LogTickFormatter` — superscript notation for powers of ten
- `NumericTickFormatter` — extracted from existing `FormatTick` logic
- `AxisScale.Date` enum value for date axes
- `Axis.TickFormatter` property for custom tick formatting
- `AxesBuilder.WithLegend()`, `SetXDateFormat()`, `SetYDateFormat()`, `SetXTickFormatter()`, `SetYTickFormatter()` fluent methods
- `FigureBuilder.TightLayout()` and `WithSubPlotSpacing()` fluent methods
- `SvgRenderContext.BeginGroup()` / `EndGroup()` for CSS-classed SVG groups

### Changed

- Subplot layout margins are now configurable via `Figure.Spacing` (was hardcoded constants)
- `ChartRenderer.RenderTicks` uses `Axis.TickFormatter` when set (falls back to default formatting)
- GitHub Actions updated to v5 (Node.js 24 compatibility)
- Series count increased from 25 to 34
- `ChartRenderer` refactored from ~1100 lines to ~100 lines — all axes rendering moved to polymorphic `AxesRenderer` subclasses
- `AxesRenderer` abstract base with `CartesianAxesRenderer`, `PolarAxesRenderer`, `ThreeDAxesRenderer` — no more `private static` methods with repeated parameters
- `ChartRenderer.RenderAxes` is now a one-liner: `AxesRenderer.Create(axes, plotArea, ctx, theme).Render()`
- Tests refactored to use builder output methods (`.ToSvg()`) instead of explicit `.Build()`

## [0.3.2] - 2026-04-05

### Added

- `IIndicatorResult` marker interface — all indicator result types must implement it
- `SignalResult` record for single-line indicators (SMA, EMA, RSI, ATR, etc.) with implicit `double[]` conversion
- `BandsResult` record for band indicators (Bollinger Bands, Keltner Channels)
- `MacdResult` record for MACD (MacdLine, SignalLine, Histogram)
- `StochasticResult` record for Stochastic (%K, %D)
- `IchimokuResult` record for Ichimoku Cloud (5 lines)
- 92 new tests: SkiaRenderContext (18), MAUI RenderContext (12), ColorMaps (17), SvgRenderContext (19), ChartRenderer (10), JSON serialization round-trip (9), indicator type assertions (7)
- BenchmarkDotNet project with 23 benchmarks: SVG rendering, JSON serialization, Skia export, 12 indicators at 1K/10K/100K data points
- CHANGELOG.md, BENCHMARKS.md with real performance numbers
- DocFX scaffolding (docfx.json, toc.yml, articles/intro.md)
- 4 runnable sample projects (Console, Blazor, WebApi, GraphQL)
- howTo.md for React, Vue, GraphQL packages
- Skia README.md and NuGet pack metadata
- `dotnet-coverage` tooling for code coverage with xUnit v3
- `GenerateDocumentationFile` enabled globally — XML ships with NuGet packages
- `InternalsVisibleTo` on core library for test access

### Changed

- All 16 indicators refactored to `Indicator<TResult> where TResult : IIndicatorResult` — no more untyped `Indicator` or raw `double[]` generics
- Static `Compute` methods removed from all indicators — computation lives in instance `override Compute()` only
- Tuple return types replaced with named records (`BandsResult` instead of `(double[], double[], double[])`)
- JSON serialization fixed for 9 series types that previously fell through to `Type = "unknown"` (DonutSeries, BubbleSeries, OhlcBarSeries, WaterfallSeries, FunnelSeries, GanttSeries, GaugeSeries, ProgressBarSeries, SparklineSeries)

## [0.3.1] - 2026-04-05

### Added

- `@matplotlibnet/react` npm package: React 19 hooks (`useMplChart`, `useMplLiveChart`), components (`MplChart`, `MplLiveChart`), TypeScript SignalR client
- `@matplotlibnet/vue` npm package: Vue 3 composables (`useMplChart`, `useMplLiveChart`), components (`MplChart`, `MplLiveChart`), TypeScript SignalR client
- `MatPlotLibNet.GraphQL` package: HotChocolate integration with `ChartQueryType`, `ChartSubscriptionType`, `GraphQLChartPublisher`, `IChartEventSender`
- `AddMatPlotLibNetGraphQL()` and `MapMatPlotLibNetGraphQL()` extension methods for DI and endpoint registration
- `netstandard2.1` target on core library for broader ecosystem compatibility
- `IsExternalInit` polyfill and conditional `System.Text.Json` package reference for netstandard2.1

### Changed

- Core `MatPlotLibNet.csproj` now targets `net10.0;netstandard2.1` (was `net10.0` only)
- Solution file updated to include GraphQL source and test projects

## [0.3.0] - 2026-04-05

### Added

- 9 new series types organized into chart families: `DonutSeries`, `BubbleSeries`, `OhlcBarSeries`, `WaterfallSeries`, `FunnelSeries`, `GanttSeries`, `GaugeSeries`, `ProgressBarSeries`, `SparklineSeries`
- 13 technical indicators: SMA, EMA, Bollinger Bands, VWAP, RSI, MACD, Stochastic, Volume, Fibonacci Retracement, ATR, ADX, Keltner Channels, Ichimoku Cloud
- Trading analytics: `EquityCurve`, `ProfitLoss`, `DrawDown` panel indicators
- Buy/sell signal markers (`BuySellSignal`, `SignalMarker`)
- Generic `SeriesRenderer<T>` base class with `SeriesRenderContext` for type-safe rendering
- `Indicator<TResult>` generic base for composable indicator computation
- `PriceSource` enum (`Close`, `Open`, `HL2`, `HLC3`, `OHLC4`) for flexible price source selection
- Fluent indicator API on `AxesBuilder`: `.Sma(20)`, `.Ema(9)`, `.BollingerBands()`, `.BuyAt()`, `.SellAt()`, `.Rsi()`, `.AddIndicator()`
- `figure.SaveSvg(path)` convenience method
- Per-family series renderer directories (XY/, Categorical/, Circular/, Grid/, Distribution/, Financial/, Field/)
- Offset parameter and LineStyle customization for all overlay indicators

### Changed

- `SvgSeriesRenderer` refactored from monolithic visitor to thin dispatcher over `SeriesRenderer<T>` instances
- Series model classes reorganized from flat `Series/` directory into family subdirectories

## [0.2.0] - 2026-04-05

### Added

- 6 new series types: `AreaSeries`, `StepSeries`, `ErrorBarSeries`, `CandlestickSeries`, `QuiverSeries`, `RadarSeries`
- Stacked bars via `BarMode.Stacked` on `Axes`
- Annotations: `Annotation` model with text positioning and optional arrow
- Reference lines: `ReferenceLine` model for `AxHLine` / `AxVLine`
- Shaded regions: `SpanRegion` model for `AxHSpan` / `AxVSpan`
- Secondary Y-axis via `WithSecondaryYAxis()` / `SecondaryAxisBuilder`
- SVG tooltips via `<title>` elements (`WithTooltips()`)
- SVG zoom/pan via embedded JavaScript (`WithZoomPan()`, `SvgInteractivityScript`)
- Polymorphic export transforms: `IFigureTransform`, `FigureTransform` (abstract), `SvgTransform`, `TransformResult` (fluent `ToStream()`, `ToFile()`, `ToBytes()`)
- `MatPlotLibNet.Skia` package: `PngTransform`, `PdfTransform`, `SkiaRenderContext`
- Convenience extensions: `figure.Transform(t).ToFile()` / `.ToBytes()` / `.ToStream()`

### Changed

- `SvgRenderer` replaced by `SvgTransform` (also implements `ISvgRenderer` for backward compatibility)
- `FigureExtensions` expanded with `Transform()` method
- `ChartRenderer` expanded for annotations, decorations, and secondary axis rendering

## [0.1.0] - 2026-04-04

### Added

- Core library with fluent builder API: `Plt.Create()`, `FigureBuilder`, `AxesBuilder`, `ThemeBuilder`
- 10 series types: Line, Scatter, Bar, Histogram, Pie, Heatmap, Box, Violin, Contour, Stem
- `Figure`, `Axes`, `Axis` model hierarchy with `ISeries` / `ChartSeries` base
- Parallel SVG rendering via `SvgRenderer` with per-subplot `SvgRenderContext`
- JSON round-trip serialization via `ChartSerializer` / `IChartSerializer` (System.Text.Json)
- 6 built-in themes: Default, Dark, Seaborn, Ggplot, Bmh, FiveThirtyEight
- Custom theme builder with immutable records (`Theme`, `GridStyle`)
- `Color` readonly record struct with named colors, hex, and RGBA support
- 8 built-in color maps: Viridis, Plasma, Inferno, Magma, Coolwarm, Blues, Reds, Greens
- `DashPatterns`, `LineStyle`, `MarkerStyle` for consistent styling
- `IChartRenderer`, `IRenderContext`, `ISeriesVisitor` interfaces
- `DataTransform` for data-to-pixel coordinate mapping
- `ChartServices` static DI defaults
- `DisplayMode` enum (Inline, Expandable, Popup)
- `IChartSubscriptionClient` shared SignalR contract
- `MatPlotLibNet.Blazor` package: `MplChart`, `MplLiveChart` Razor components, `ChartSubscriptionClient`
- `MatPlotLibNet.AspNetCore` package: `ChartHub`, `ChartPublisher`, `IChartPublisher`, REST endpoints, SignalR hub
- `MatPlotLibNet.Interactive` package: `ChartServer`, `BrowserLauncher`, `ShowAsync()` extension
- `MatPlotLibNet.Maui` package: `MplChartView`, `MauiGraphicsRenderContext`
- `@matplotlibnet/angular` npm package: Angular components + TypeScript SignalR client
