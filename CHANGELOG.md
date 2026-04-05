# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

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
