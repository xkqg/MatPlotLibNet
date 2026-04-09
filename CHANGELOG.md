# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

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
