# MatPlotLibNet Core -- Architecture

## Package dependency graph

```
MatPlotLibNet (Core)                      zero external dependencies
    |
    +-- MatPlotLibNet.Blazor              Microsoft.AspNetCore.SignalR.Client
    |
    +-- MatPlotLibNet.AspNetCore          Microsoft.AspNetCore.App framework ref
    |       |
    |       +-- MatPlotLibNet.Interactive  embedded Kestrel + SignalR
    |
    +-- MatPlotLibNet.Maui                Microsoft.Maui.Controls
    |
    +-- @matplotlibnet/angular (npm)      @microsoft/signalr + Angular
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
    ThemeBuilder.cs                   custom themes: Theme.CreateFrom().WithFont().Build()

  Extensions/
    FigureExtensions.cs               figure.ToSvg(), figure.ToJson()

  Models/
    Figure.cs                         top-level container (Title, Width, Height, Theme, SubPlots)
    Axes.cs                           subplot with series collection + axis config
    Axis.cs                           label, min/max, scale, ticks

    Series/
      ISeries.cs                      interface: Label, Visible, ZOrder, Accept()
      ChartSeries.cs                  abstract base: implements ISeries common properties
      LineSeries.cs                   XData, YData, Color, LineStyle, LineWidth, Marker
      ScatterSeries.cs                XData, YData, Color, MarkerSize, Sizes[], Colors[]
      BarSeries.cs                    Categories, Values, Color, Orientation, BarWidth
      HistogramSeries.cs              Data, Bins, Color, Alpha, ComputeBins()
      PieSeries.cs                    Sizes, Labels, Colors[], StartAngle
      HeatmapSeries.cs               Data[,], ColorMap
      BoxSeries.cs                    Datasets[][], Color, MedianColor, ShowOutliers
      ViolinSeries.cs                 Datasets[][], Color, Alpha
      ContourSeries.cs                XData, YData, ZData[,], Levels, ColorMap
      StemSeries.cs                   XData, YData, MarkerColor, StemColor, BaselineColor

  Rendering/
    IChartRenderer.cs                 interface: Render(Figure, IRenderContext)
    ChartRenderer.cs                  orchestrator: background, layout, axes, series
    IRenderContext.cs                  drawing primitives: DrawLine, DrawRect, DrawText, etc.
    ISeriesVisitor.cs                 visitor pattern: Visit(LineSeries), Visit(BarSeries), etc.
    DataTransform.cs                  data space <-> pixel space coordinate mapping
    RenderArea.cs                     plot bounds + context container
    Primitives.cs                     record structs: Point, Size, Rect, DataRange

    Svg/
      ISvgRenderer.cs                 interface: Render(Figure) -> string
      SvgRenderer.cs                  parallel subplot rendering, SVG assembly
      SvgRenderContext.cs             IRenderContext impl: StringBuilder-based SVG emission
      SvgSeriesRenderer.cs            ISeriesVisitor impl: renders each series type to SVG

  Serialization/
    IChartSerializer.cs               interface: ToJson(Figure), FromJson(string)
    ChartSerializer.cs                System.Text.Json-based round-trip serialization

  Styling/
    Color.cs                          readonly record struct (R, G, B, A) + named colors + hex
    Font.cs                           sealed record (Family, Size, Weight, Slant, Color)
    Theme.cs                          6 built-in themes + GridStyle sealed record
    LineStyle.cs                      enum: Solid, Dashed, Dotted, DashDot, None
    MarkerStyle.cs                    enum: None, Circle, Square, Triangle, Diamond, etc.
    DashPatterns.cs                   canonical dash ratios shared by SVG + MAUI renderers

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
SvgRenderer            renders background sequentially
    |                  computes subplot layout
    |                  renders subplots in PARALLEL (Parallel.For)
    |                  each subplot gets its own SvgRenderContext
    |                  merges SVG output in order
    |
ChartRenderer          per subplot: computes data ranges, creates DataTransform
    |                  renders grid, ticks, axis labels
    |                  for each series: creates SvgSeriesRenderer (visitor)
    |
SvgSeriesRenderer      visitor dispatches to type-specific rendering
    |                  transforms data points to pixels via DataTransform
    |                  emits SVG elements to SvgRenderContext
    |
SvgRenderContext       accumulates SVG markup in StringBuilder
    |
string                 complete <svg>...</svg> document
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
| Fluent builder | FigureBuilder, AxesBuilder, ThemeBuilder | matplotlib-style method chaining |
| Visitor | ISeriesVisitor + ChartSeries.Accept() | extensible per-series rendering without switch |
| Strategy | IRenderContext (SVG, MAUI, custom) | multiple output targets from same model |
| DI interfaces | IChartRenderer, ISvgRenderer, IChartSerializer | testable, replaceable services |
| Static defaults | ChartServices | non-DI usage for console apps |
| Record types | Font, GridStyle, Legend, TickConfig, Color, Point, Rect | immutable value objects with `with` |
| Parallel rendering | SvgRenderer + per-subplot SvgRenderContext | multi-core subplot rendering |
