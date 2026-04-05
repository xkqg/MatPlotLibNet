# MatPlotLibNet Core -- Architecture (v0.3.1)

## Package dependency graph

```
MatPlotLibNet (Core)                      net10.0 + netstandard2.1 (System.Text.Json on ns2.1)
    |
    +-- MatPlotLibNet.Skia                SkiaSharp (PNG + PDF export)
    |
    +-- MatPlotLibNet.Blazor              Microsoft.AspNetCore.SignalR.Client
    |
    +-- MatPlotLibNet.AspNetCore          Microsoft.AspNetCore.App framework ref
    |       |
    |       +-- MatPlotLibNet.Interactive  embedded Kestrel + SignalR
    |       |
    |       +-- MatPlotLibNet.GraphQL      HotChocolate.AspNetCore (GraphQL queries + subscriptions)
    |
    +-- MatPlotLibNet.Maui                Microsoft.Maui.Controls
    |
    +-- @matplotlibnet/angular (npm)      @microsoft/signalr + Angular
    |
    +-- @matplotlibnet/react (npm)        @microsoft/signalr + React 19
    |
    +-- @matplotlibnet/vue (npm)          @microsoft/signalr + Vue 3
```

## Core library structure

```
MatPlotLibNet/
  Plt.cs                              entry point: Plt.Create() / Plt.Figure()
  ChartServices.cs                    static DI defaults (IChartSerializer, IChartRenderer, ISvgRenderer)
  DisplayMode.cs                      enum: Inline, Expandable, Popup
  IChartSubscriptionClient.cs         shared SignalR client contract

  Builders/
    FigureBuilder.cs                  fluent API: Plt.Create().WithTitle().Plot().Build()
    AxesBuilder.cs                    subplot config: WithTitle(), SetXLabel(), Plot(), Scatter(), etc.
    SecondaryAxisBuilder.cs           secondary Y-axis: SetYLabel(), Plot(), Scatter()
    ThemeBuilder.cs                   custom themes: Theme.CreateFrom().WithFont().Build()

  Extensions/
    FigureExtensions.cs               figure.ToSvg(), figure.ToJson(), figure.Transform()

  Models/
    Figure.cs                         top-level container (Title, Width, Height, Theme, EnableZoomPan)
    Axes.cs                           subplot: series, annotations, ref lines, spans, secondary axis
    Axis.cs                           label, min/max, scale, ticks
    Annotation.cs                     text annotation at data coordinates with optional arrow
    ReferenceLine.cs                  horizontal/vertical reference line (AxHLine, AxVLine)
    SpanRegion.cs                     shaded horizontal/vertical region (AxHSpan, AxVSpan)

    Series/                           16 series types via ISeries + ChartSeries base + visitor pattern
      ISeries.cs                      interface: Label, Visible, ZOrder, Accept()
      ChartSeries.cs                  abstract base: implements ISeries common properties
      LineSeries.cs                   XData, YData, Color, LineStyle, LineWidth, Marker
      ScatterSeries.cs                XData, YData, Color, MarkerSize, Sizes[], Colors[]
      BarSeries.cs                    Categories, Values, Color, Orientation, BarWidth, StackBaseline
      HistogramSeries.cs              Data, Bins, Color, Alpha, ComputeBins()
      PieSeries.cs                    Sizes, Labels, Colors[], StartAngle
      HeatmapSeries.cs               Data[,], ColorMap
      BoxSeries.cs                    Datasets[][], Color, MedianColor, ShowOutliers
      ViolinSeries.cs                 Datasets[][], Color, Alpha
      ContourSeries.cs                XData, YData, ZData[,], Levels, ColorMap
      StemSeries.cs                   XData, YData, MarkerColor, StemColor, BaselineColor
      AreaSeries.cs                   XData, YData, YData2 (fill between), Alpha, FillColor
      StepSeries.cs                   XData, YData, StepPosition (Pre/Mid/Post)
      ErrorBarSeries.cs               XData, YData, YErrorLow/High, XErrorLow/High, CapSize
      CandlestickSeries.cs            Open, High, Low, Close, DateLabels, UpColor, DownColor
      QuiverSeries.cs                 XData, YData, UData, VData, Scale, ArrowHeadSize
      RadarSeries.cs                  Categories, Values, FillColor, Alpha, MaxValue
      DonutSeries.cs                  Sizes, InnerRadius, CenterText (Circular/)
      BubbleSeries.cs                 XData, YData, Sizes, Alpha (XY/)
      OhlcBarSeries.cs                Open, High, Low, Close, TickWidth (Financial/)
      WaterfallSeries.cs              Categories, Values, IncreaseColor, DecreaseColor (Categorical/)
      FunnelSeries.cs                 Labels, Values, Colors (Categorical/)
      GanttSeries.cs                  Tasks, Starts, Ends, BarHeight (Categorical/)
      GaugeSeries.cs                  Value, Min, Max, Ranges, NeedleColor (Circular/)
      ProgressBarSeries.cs            Value, FillColor, TrackColor (Categorical/)
      SparklineSeries.cs              Values, LineWidth (XY/)

  Indicators/                           polymorphic: IIndicator -> Indicator -> Indicator<TResult>
    IIndicator.cs                       interface: Apply(Axes)
    Indicator.cs                        abstract base: Color, Label, LineWidth, LineStyle, Offset
    Indicator<TResult>.cs               generic: typed Compute() for composability
    PriceSource.cs                      enum (Close/Open/HL2/HLC3/OHLC4) + PriceSources resolver
    Sma.cs, Ema.cs                      Moving averages (overlay)
    BollingerBands.cs                   Upper/lower bands + SMA middle (overlay)
    Vwap.cs                             Volume-weighted average price (overlay)
    FibonacciRetracement.cs             Horizontal levels at key ratios (overlay)
    Rsi.cs                              Relative Strength Index 0-100 (panel)
    Macd.cs                             MACD + signal + histogram (panel)
    Stochastic.cs                       %K + %D oscillator (panel)
    VolumeIndicator.cs                  Volume bars (panel)
    Atr.cs                              Average True Range (panel)
    Adx.cs                              Average Directional Index + DI (panel)
    KeltnerChannels.cs                  EMA + ATR bands (overlay)
    Ichimoku.cs                         Ichimoku Cloud 5-line system (overlay)
    BuySellSignal.cs                    Buy/sell triangle markers
    EquityCurve.cs                      Cumulative P&L line (panel)
    ProfitLoss.cs                       Per-trade colored bars (panel)
    DrawDown.cs                         % drawdown from peak (panel)

  Transforms/                         polymorphic export: IFigureTransform -> FigureTransform -> concrete
    IFigureTransform.cs               interface: Transform(Figure, Stream)
    FigureTransform.cs                abstract base: holds ChartRenderer, shared by all transforms
    SvgTransform.cs                   FigureTransform + ISvgRenderer: parallel SVG rendering
    TransformResult.cs                fluent record: ToStream(), ToFile(), ToBytes()

  Rendering/
    IChartRenderer.cs                 interface: Render(Figure, IRenderContext)
    ChartRenderer.cs                  orchestrator: background, layout, axes, series, annotations
    IRenderContext.cs                  drawing primitives: DrawLine, DrawRect, DrawText, etc.
    ISeriesVisitor.cs                 visitor pattern: Visit() for each of the 25 series types
    DataTransform.cs                  data space <-> pixel space coordinate mapping
    RenderArea.cs                     plot bounds + context container
    Primitives.cs                     record structs: Point, Size, Rect, DataRange, PathSegment

    SeriesRenderers/                    generic SeriesRenderer<T> per series type
      SeriesRenderContext.cs            record: Transform + Ctx + Color + Area + options
      SeriesRenderer.cs                 abstract base + generic SeriesRenderer<T>
      XY/                               Line, Scatter, Step, Area, ErrorBar, Bubble, Sparkline
      Categorical/                      Bar, Histogram, Waterfall, Funnel, Gantt, ProgressBar
      Circular/                         Pie, Radar, Donut, Gauge
      Grid/                             Heatmap, Contour
      Distribution/                     Box, Violin
      Financial/                        Candlestick, OhlcBar
      Field/                            Quiver, Stem

    Svg/
      ISvgRenderer.cs                 interface: Render(Figure) -> string (backward compat)
      SvgRenderContext.cs             IRenderContext impl: StringBuilder-based SVG emission
      SvgSeriesRenderer.cs            thin visitor dispatcher to SeriesRenderer<T> instances
      SvgInteractivityScript.cs       embedded JavaScript for zoom/pan via viewBox manipulation

  Serialization/
    IChartSerializer.cs               interface: ToJson(Figure), FromJson(string)
    ChartSerializer.cs                System.Text.Json round-trip with series type discriminator

  Styling/
    Color.cs                          readonly record struct (R, G, B, A) + named colors + hex
    Font.cs                           sealed record (Family, Size, Weight, Slant, Color)
    Theme.cs                          6 built-in themes + GridStyle sealed record
    LineStyle.cs                      enum: Solid, Dashed, Dotted, DashDot, None
    MarkerStyle.cs                    enum: None, Circle, Square, Triangle, Diamond, etc.
    DashPatterns.cs                   canonical dash ratios shared by SVG + MAUI + Skia renderers

    ColorMaps/
      IColorMap.cs                    interface: GetColor(double normalized) -> Color
      LerpColorMap.cs                 linear interpolation between color stops
      ColorMaps.cs                    built-in: Viridis, Plasma, Inferno, Magma, Coolwarm, etc.
```

## Data flow

### Static SVG rendering

```
Plt.Create()           user builds figure via fluent API
    |
FigureBuilder.Build()  produces Figure model
    |
figure.ToSvg()         calls ChartServices.SvgRenderer.Render(figure)
    |
SvgTransform           renders background sequentially
    |                  computes subplot layout
    |                  renders subplots in PARALLEL (Parallel.For)
    |                  each subplot gets its own SvgRenderContext
    |                  merges SVG output in order
    |
ChartRenderer          per subplot: computes data ranges, creates DataTransform
    |                  renders spans, reference lines (decorations)
    |                  renders grid, ticks, axis labels
    |                  for each series: creates SvgSeriesRenderer (visitor)
    |                  renders secondary Y-axis series with separate transform
    |                  renders annotations (text + arrows)
    |
SvgSeriesRenderer      visitor dispatches to type-specific rendering
    |                  transforms data points to pixels via DataTransform
    |                  emits SVG elements to SvgRenderContext
    |                  wraps data elements in <title> for tooltips when enabled
    |
SvgRenderContext       accumulates SVG markup in StringBuilder
    |
string                 complete <svg>...</svg> document (with optional zoom/pan script)
```

### Polymorphic figure transforms

```
IFigureTransform           common interface: Transform(Figure, Stream)
    |
FigureTransform            abstract base: holds ChartRenderer
    |
    +-- SvgTransform       writes UTF-8 SVG text (also implements ISvgRenderer)
    +-- PngTransform       writes PNG bytes via SkiaSharp (MatPlotLibNet.Skia)
    +-- PdfTransform       writes PDF bytes via SkiaSharp (MatPlotLibNet.Skia)
```

All transforms share the same flow:
1. Create format-specific `IRenderContext` (SvgRenderContext or SkiaRenderContext)
2. Call `ChartRenderer.Render(figure, ctx)` -- shared rendering pipeline
3. Encode the context output into the target format (SVG string, PNG bitmap, PDF document)

Usage (fluent via TransformResult record):
```
figure.Transform(new SvgTransform()).ToFile("chart.svg");
figure.Transform(new PngTransform()).ToFile("chart.png");
figure.Transform(new PdfTransform()).ToFile("chart.pdf");

byte[] png = figure.Transform(new PngTransform()).ToBytes();
figure.Transform(new SvgTransform()).ToStream(stream);
```

### JSON round-trip

```
figure.ToJson()        calls ChartServices.Serializer.ToJson(figure)
    |
ChartSerializer        Figure -> FigureDto -> JsonSerializer.Serialize()
    |
string                 JSON with camelCase, null-ignoring, Color as hex

ChartServices.Serializer.FromJson(json)
    |
ChartSerializer        JsonSerializer.Deserialize() -> FigureDto -> Figure
```

### Real-time SignalR updates

```
Server: IChartPublisher.PublishSvgAsync(chartId, figure)
    |
ChartPublisher         renders to SVG, broadcasts via IHubContext
    |
ChartHub               routes to SignalR group by chartId
    |
    +-- Blazor client:   ChartSubscriptionClient (C#, implements IChartSubscriptionClient)
    +-- Angular client:  ChartSubscriptionClient (TypeScript, mirrors same pattern)
    +-- Interactive:     embedded JS in ChartPage.cs HTML template
```

## Key design patterns

| Pattern | Where | Why |
|---------|-------|-----|
| Fluent builder | FigureBuilder, AxesBuilder, ThemeBuilder, SecondaryAxisBuilder | matplotlib-style method chaining |
| Visitor | ISeriesVisitor + ChartSeries.Accept() | extensible per-series rendering without switch |
| Strategy | IRenderContext (SVG, MAUI, Skia) | multiple output targets from same model |
| Template method | FigureTransform base class | shared renderer, format-specific Transform() |
| Fluent result | TransformResult record | polymorphic ToStream/ToFile/ToBytes from any transform |
| DI interfaces | IFigureTransform, IChartRenderer, ISvgRenderer, IChartSerializer | testable, replaceable services |
| Static defaults | ChartServices | non-DI usage for console apps |
| Record types | Font, GridStyle, Legend, TickConfig, Color, Point, Rect, TransformResult | immutable value objects |
| Parallel rendering | SvgTransform + per-subplot SvgRenderContext | multi-core subplot rendering |
| Delegate extraction | SvgTransform.BuildSvgDocument, ChartSerializer.ApplyEnum | DRY via higher-order functions |
