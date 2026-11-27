# MatPlotLibNet Core -- Architecture (v1.31.0)

What the library is made of today and why it is shaped that way. Read it in this order: the packages, how a figure is
drawn, where each file of the core library sits, which class owns each shared rule, where a chart verb lives, the design
patterns, the few places that are shared by the whole process, what is left out on purpose, and the satellite packages. What each release changed is in CHANGELOG.md.

## Package dependency graph

```
MatPlotLibNet (Core)                      net10.0 + net8.0
    |
    +-- MatPlotLibNet.Skia                SkiaSharp (PNG + PDF export)
    |       |
    |       +-- MatPlotLibNet.Wpf          SkiaSharp.Views.WPF (net8.0-windows)
    |       |
    |       +-- MatPlotLibNet.Avalonia     Avalonia + Avalonia.Skia
    |       |
    |       +-- MatPlotLibNet.Uno          Uno.WinUI + SkiaSharp.Views.Uno.WinUI (net8.0)
    |
    +-- MatPlotLibNet.DataFrame           Microsoft.Data.Analysis (the indicators over a DataFrame)
    |
    +-- MatPlotLibNet.Geo                 pocketken.H3 (map projections, Natural Earth data embedded)
    |
    +-- MatPlotLibNet.Blazor              Microsoft.AspNetCore.SignalR.Client
    |
    +-- MatPlotLibNet.AspNetCore          Microsoft.AspNetCore.App framework ref
    |       |
    |       +-- MatPlotLibNet.Interactive  embedded Kestrel + SignalR
    |       |
    |       +-- MatPlotLibNet.GraphQL      HotChocolate.AspNetCore (GraphQL queries + subscriptions)
    |
    +-- MatPlotLibNet.Mcp                 ModelContextProtocol (stdio MCP server, net10.0, dotnet tool)
    |       + MatPlotLibNet.Skia           (PNG/PDF for the agent's tools)
    |
    +-- MatPlotLibNet.Verso               Verso.Abstractions ONLY (net8.0) -- a notebook loads an extension in
    |                                       its own AssemblyLoadContext, so the chart type is matched BY NAME
    |                                       and rendered through the assembly the cell's own value came from.
    |                                       Shipping the core alongside would load it twice and give the
    |                                       extension a different Figure from the cell's
    |
    +-- MatPlotLibNet.Notebooks           Microsoft.DotNet.Interactive (Polyglot Notebooks) -- the runtime
    |                                       underneath has been ended by Microsoft. Frozen at the version it
    |                                       reached, built and pushed by nothing; new work goes to
    |                                       MatPlotLibNet.Verso
    |
    +-- MatPlotLibNet.Maui                Microsoft.Maui.Controls
    |
    +-- @matplotlibnet/client (npm)       @microsoft/signalr; the hub client, the SVG mount and the document in parts
    |                                       the three framework packages below share
    |
    +-- @matplotlibnet/angular (npm)      @matplotlibnet/client + Angular
    |
    +-- @matplotlibnet/react (npm)        @matplotlibnet/client + React 19
    |
    +-- @matplotlibnet/vue (npm)          @matplotlibnet/client + Vue 3
```

## Repository-wide build files

```
Directory.Build.props — imported BEFORE every project: authorship, the repository URL, deterministic builds, Source
    Link, EnableWindowsTargeting for non-Windows hosts, and the package icon all fifteen packages carry
icon.png — the 128x128 package icon, at the repository root so the props file can pack it into every package. Drawn BY
    the library (sample 43 of the console sample generator), so the mark cannot drift from what the library produces.
    ReleasePackagingContractTests checks the format, the size and the 1 MB ceiling nuget.org enforces at push time
Directory.Build.targets — imported AFTER every project, so the version is already known. Holds the one release-notes
    sentence all fifteen packages carry, with the version and the changelog anchor built from that version (fifteen
    hand-kept copies would drift), and the exact dependency of a package that reads internals: a package that sets
    DependsOnInternals (Skia, AspNetCore, Geo, Maui, Wpf, Avalonia, Uno, GraphQL; each reads internals of a package it
    references through InternalsVisibleTo) depends on every package it references at exactly its own version, [x.y.z],
    not at the floor NuGet writes for a project reference, so a newer build cannot move an internal member under it.
    InternalsDependencyContractTests holds the list to the grants
Src/.editorconfig — the analyzer rules the packages build with: IDE0052 (a private member written and never read) is a
    warning, run in the build by EnforceCodeStyleInBuild in the Src group of Directory.Build.props, so dead state fails a
    -warnaserror build
```

## How a figure is drawn

### Static SVG rendering

```
Plt.Create()           user builds figure via fluent API
    |
FigureBuilder.Build()  produces Figure model
    |
figure.ToSvg()         calls SvgRendererDefault.Current.Render(figure)
    |
SvgTransform           RenderDocument draws through ChartRenderer.Draw onto an SvgFrameTarget (the one drawing sequence of
    |                  every target): the frame laid out once (FigureFrame), the background, the figure underlays
    |                  (RenderFigureUnderlays: the images), the subplots one after another (handing them to the thread
    |                  pool cost more CPU than it shared out: measured on the live-wall benchmark, Benchmarks/.../LiveWall),
    |                  each into its own SvgRenderContext carrying the SvgEmit of this draw, unless nothing it draws from
    |                  changed since the figure's last draw: then the markup that draw made is handed out again
    |                  (KeptAxesDraw<SvgRenderContext>, kept on Figure.DrawnSvgAxes; its AxesDrawKey and, from the second
    |                  draw under one key, the DataSnapshot taken before the draw, so a value changed in place in an
    |                  array is drawn); then the figure overlays (RenderFigureOverlays: colour bar, texts, legend) into a
    |                  context of their own
    |                  returns the document in its parts (SvgFigureDocument: root tag, title/desc, background, one part
    |                  per subplot, overlay, scripts); Render writes it plain, joining the parts with ONE copy into the
    |                  finished string (SvgDocumentParts), every number written straight into its builder
    |                  (StringBuilder.AppendSvgNumber: a coordinate to two decimals, its digits written as an integer's;
    |                  AppendExactSvgNumber for a view attribute or an opacity), never as a string of its own first; ToWrappedSvg writes the same
    |                  with every drawn part in a <g data-mpl-part> group, for a viewer that patches
    |
ChartRenderer          per subplot: computes data ranges, creates DataTransform
    |                  renders spans, patches, reference lines (decorations)
    |                  renders grid, ticks, axis labels
    |                  for each series: creates SvgSeriesRenderer (visitor)
    |                  places every Y axis slot of the container once (CartesianAxisPlacement: range, transform,
    |                  ticks, band, offset) and draws each with the one slot part, on either side, through its own scale
    |                  places the top X axis once (CartesianAxisPlacement.PlaceTopAxis: its own range and scale, or,
    |                  linked, the primary's), and stands each tick of a linked axis through a LinkedTickPlacement
    |                  renders annotations (text + arrows)
    |                  renders each inset, the inset's zoom indicator first (over the parent's marks, under the
    |                  inset: matplotlib's 4.99 against 5; ZoomIndicatorPath, only when both axes are Cartesian)
    |
SvgSeriesRenderer      visitor dispatches to type-specific rendering
    |                  transforms data points to pixels via DataTransform
    |                  emits SVG elements to SvgRenderContext
    |                  wraps data elements in <title> for tooltips when enabled
    |
SvgRenderContext       the public entry: hands each call to SvgElementBuffer (the markup, the clips, the paint servers and the
    |                  one envelope of an element) and to the mark, text and markup writers over it
    |
string                 complete <svg>...</svg> document
                       optional scripts appended at end of body:
                         GetZoomPanScript()      (EnableZoomPan)
                         LegendToggleScript      (EnableLegendToggle)
                         LegendDragScript        (EnableLegendToggle — co-emitted)
                         CustomTooltipScript     (EnableRichTooltips)
                         HighlightScript         (EnableHighlight)
                         SelectionScript         (EnableSelection)
                       data-series-index / data-legend-index attributes emitted when Figure.HasInteractivity or
                         the axes' own EnableInteractiveAttributes (SvgEmit, decided per draw; the draw writes
                         nothing into the figure)
```

### Native controls

```
WPF / Avalonia / Uno control      paint event, on the control's own thread
    |
SkiaChartPainter                  one per control (internal); Paint(canvas, figure, band) under its own lock
    |                             (Avalonia paints on its render thread); starts over for another figure;
    |                             frees every recording on Unloaded / detach
SkiaFrameTarget                   KeepingFrameTarget<SKPicture>: a subplot that changed is recorded under the
    |                             canvas' matrix (its LayerKey), every other one is replayed with the matrix set
    |                             aside, so a replay is the same pixels as a direct draw at any scale
ChartRenderer.Draw                the same sequence as the SVG writer; the layout it returns goes to the
                                  control's InteractionHost (host.Painted: the host asks whether the control is
                                  interactive, builds the ChartLayout, and runs it on the control's thread when the
                                  control paints on another one)
MAUI                              paints through a DirectFrameTarget over its canvas (MauiGraphicsRenderContext)
Streaming controls                apply the scale modes before they draw (new data, a gesture, a subscribe);
                                  a paint writes nothing into the figure. All four compose one
                                  StreamingFigureFollower (core, internal), hand it their UI-thread
                                  dispatch and their invalidate, and give its Redraw to their host as
                                  the repaint after a gesture; every streaming control raises DataPointClicked
```

### Polymorphic figure transforms

```
IFigureTransform           common interface: Transform(Figure, Stream), and Transform(Figure, Stream, SaveOptions)
    |                      for the transforms that read a save setting
FigureTransform            abstract base: holds the IChartRenderer it was made with (FigureRenderer), any renderer
    |
    +-- SvgTransform       writes UTF-8 SVG text (also implements ISvgRenderer); indentation, decimals, title, author
    +-- PngTransform       writes PNG bytes via SkiaSharp (MatPlotLibNet.Skia) at the dpi of the options or the figure; title, author
    +-- PdfTransform       writes PDF bytes via SkiaSharp (MatPlotLibNet.Skia); title, author
```

All transforms share one drawing sequence, `ChartRenderer.Draw`:
1. The SVG writer draws onto an `SvgFrameTarget` (a context per subplot, kept subplots handed out again); PNG, PDF and a
   GIF frame draw onto a `DirectFrameTarget` over one `SkiaRenderContext` (`ChartRenderer.Render`)
2. `Draw` lays the frame out once and draws the background, the underlays, each subplot and the overlays
3. The transform encodes the result into its format (SVG string, PNG bitmap, PDF document)

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
figure.ToJson()        calls ChartSerializerDefault.Current.ToJson(figure)
    |
ChartSerializer        FigureDto.Of -> AxesDto.Of -> AxisDto.Of, each series via ISeriesSerializable -> JsonSerializer.Serialize()
    |                  the Y axis container -> yAxis (primary), secondaryYAxis (right slot 0), the slots past slot 0 of
    |                  each side in extraRightYAxes / leftYAxes (the old caps) and rightYAxesOverflow / leftYAxesOverflow
    |                  (every one past them), yAxisTitleGap only when it was given
    |                  the figure's own texts, images and legend -> texts, images, legend; an inset's zoom
    |                  indicator -> zoomIndicator; a subplot's patches -> patches (Patch.Describe, by "type"), its
    |                  signals -> signals, its AddSubPlot cell -> subplot (only for a grid of more than one cell); an
    |                  axis' link -> link (the named AxisTransform leaves describe themselves, one made with Of is
    |                  left out); each left out when there is none
    |
string                 JSON with camelCase, null-ignoring, Color as hex, a number that is not finite as the string "NaN",
                       "Infinity" or "-Infinity"; a value at its default is left out

ChartSerializerDefault.Current.FromJson(json)
    |
ChartSerializer        JsonSerializer.Deserialize() -> FigureDto.ToFigure -> AxesDto.RestoreOnto -> SeriesRegistry resolves type -> Figure
    |                  each list of numbers is read into an array of its own length (DoubleArrayReadConverter)
    |                  each frozen list, then its overflow list, stacks its slots on the container in outward order
    |                  a subplot with a subplot cell is added with Figure.AddSubPlot(rows, cols, index); a patch or a
    |                  link of a kind this build does not know reads as the base record, restores nothing and reports
    |                  on ChartDiagnostics, so the rest of the figure loads
    |
SeriesRegistry         one NamedTable<string, Func<...>> maps type discriminator to factory
```

What an older reader does with a newer file:

- **The Y axes** are the one part of the wire an older reader shapes. The 1.23.0 fields keep their meaning (`yAxis` and `series`
  the primary, `secondaryYAxis` and `secondarySeries` right slot 0, `extraRightYAxes` and `leftYAxes` the slots past slot 0 in
  outward order); each frozen list holds at most one slot (`YAxisSlotsPastSlot0Dto.FrozenCap`) and every further slot goes to
  `rightYAxesOverflow` / `leftYAxesOverflow`, which a 1.23.0 reader skips. A figure within the old caps writes the bytes 1.23.0
  wrote (`Compat/v1.23.0-four-axes.json` is pinned), and `AxesDrawnPropertiesRoundTripTests` has a row for every seat.
- **Package data.** `SeriesDto.PackageData` carries what a package writes of its own series; core writes and reads it for none,
  and the package registers the reader of its series with `SeriesRegistry.Register` (Geo: a `[ModuleInitializer]` and
  `GeoPolygonSeries.RegisterReader()`). A 1.23.0 core drops such a series with the diagnostic for an unknown kind.
- **The figure's texts, images and legend, and an inset's `zoomIndicator`:** a 1.27.0 reader skips them.
- **A subplot's `subplot` cell, its `signals`, its `patches`, and an axis' `link`:** a 1.28.0 reader skips them.

### Real-time SignalR updates

```
Server: IChartPublisher.PublishSvgAsync(chartId, figure)
    |
ChartPublisher         renders the document in its parts (SvgTransform.RenderDocument) and keeps per chart the one it
    |                  last sent and who holds it (with the hub's ChartSubscriptions, as the container makes it):
    |                  a viewer subscribed with Subscribe gets the plain document (GroupExcept the ones that patch),
    |                  a viewer subscribed with SubscribeToParts the whole wrapped document once, then UpdateChartParts
    |                  with only what changed (ChartSvgParts: root tag, background, overlay, changed subplots); a new
    |                  title, other scripts or another number of subplots resend the whole; nothing changed: nothing sent,
    |                  except to a viewer that joined since; what it keeps for a chart is let go of (internal
    |                  IKeepingChartPublisher.ForgetAsync) when the chart is unregistered and when its last viewer leaves
    |
GraphQLChartPublisher  decorates the publisher: it sends its subscribers the plain SVG the library's ChartPublisher hands
    |                  back from the document it just published (internal IKeepingChartPublisher.PublishSvgAndHandOnAsync), so a
    |                  publish draws the figure once; over a publisher of the caller's it draws the SVG itself; the
    |                  view change and the forgetting of a chart it hands to the publisher it wraps
    |
FigureRegistry         one session per chart id, still (ChartSession) or streaming (StreamingChartSession), both an
    |                  internal IRegisteredChart; registering under a taken id replaces it, UnregisterAsync ends either
    |
ChartHub               routes to SignalR group by chartId; SubscribeToParts records a viewer that patches
    |
    +-- Blazor client:   ChartSubscriptionClient (C#, implements IChartSubscriptionClient); keeps the last whole
    |                    document per chart (SvgPartsDocument), puts each part in place, hands on the plain SVG;
    |                    builds its connection and listens through an internal IHubConnector (SignalRHubConnector)
    +-- React/Vue/Angular clients: one ChartSubscriptionClient (TypeScript, the same pattern, svg-parts-document.ts) in
    |                    @matplotlibnet/client, which the three framework packages depend on and re-export
    +-- Interactive:     embedded JS in the HTML page ChartServer writes, which puts each part in place in the DOM

Client gesture (server-interactive chart): the SVG's script sends a zoom, pan, reset or legend event over the hub
    |
ChartHub               writes the event to the chart's channel and returns
    |
ChartSession           ONE reader task per chart drains the channel in order and applies each event to the figure
                       (TryApplyTo), publishing once per drained batch: a ChartEventBatch holds what the batch left
                       (whether the figure changed, the viewer who moved the view last, one tooltip per viewer) and
                       is published once after it. One event costs the chart only itself: an event
                       that names a subplot the figure lacks is dropped and counted (DroppedEventCount), one that throws
                       (an application event, a handler) is counted (FaultedEventCount, LastFault) and the reader goes
                       on with the next. Only cancellation and a closed channel end the reader
```

## Core library map

One line per file or group of files: what it is and its one responsibility. A rule a file owns for others is in "One owner per
rule"; a pattern is in "Key design patterns". Enums and the small values a type hands out are found by their names and are not
listed.

- A type of its own carries a concept more than one type uses. A part only one type uses is part of that type (its members, or a
  nested type when two of its members hand a value to each other); `SingleUserTypeContractTests` names the few exceptions with the
  rule their dissolving would break, and a part file (`DataTransform.RoomCut.cs`) stands only where the user and the part pass 600
  lines together.
- A file is named after the type it declares, up to its first dot (`SourceFileNamesContractTests`); a file holds one main type and at
  most what it hands out to one other file.
- A file this map names stands where the map puts it (`ArchitectureMapContractTests`); a type folded into another is named by its
  owner from then on in every document (`DeletedNameTests`), and its tests are the owner's (`TestClassContractTests`).

### Root, builders, extensions

```
MatPlotLibNet/
  Plt.cs — entry point: Plt.Create() / Plt.Figure()
  Plt.Style — nested static class: Use(), Context() for rcParams configuration
  ChartSerializerDefault.cs, ChartRendererDefault.cs, SvgRendererDefault.cs — GlyphPathProviderDefault.cs
  NamedTable.cs — internal sealed NamedTable<TKey, TValue>: the one name -> value table behind the six registries
  IChartSubscriptionClient.cs — shared SignalR client contract
  SaveOptions.cs — sealed record: Dpi, PrettifySvg, SvgDecimalPrecision, SvgIdPrefix, Title, Author — Figure.Save(path, options)
  SaveOptionsExtensions.cs — internal: DpiFor (the options' resolution unless it is the default, else the figure's own)
  FigureTemplates.cs — pre-built layouts, each a factory `new XFigure(…) { … }.Build()` over the faceted figure that owns its layout
  Builders/
    FigureBuilder.cs — fluent API: Plt.Create().WithTitle().Plot().Build()
    MosaicFigureBuilder.cs — Plt.Mosaic("AB\nCD"): panels by letter (Parse and GetDimensions, members of MosaicFigureBuilder)
    AxesBuilder.cs — the builder itself: the Axes it builds (internal Axes), the views X, Y and Z of its primary axes
    SecondaryXAxisBuilder.cs — the builder of the top X axis (matplotlib's secondary_xaxis('top'))
    AxisVerbs.cs — the verbs of one axis, written once
    XAxisSlotBuilder.cs — the builder of one linked X axis below the plot (matplotlib's secondary_xaxis('bottom'))
    YAxisSlotBuilder.cs — ONE builder for any Y axis, either side, over one YAxisSlot
    CategoryTickExtensions.cs — internal: SetXCategoryTicks(names) / SetYCategoryTicks(names, topDown)
    CategoricalAxesExtensions.cs, DistributionAxesExtensions.cs, XYAxesExtensions.cs — the 38 chart verbs
    DumbbellParts.cs, ParetoParts.cs, QQPlotParts.cs, ControlChartParts.cs, RiskTableParts.cs — what a verb's callback receives when it draws several series types
    ControlLimits.cs — readonly record struct (Center, Lower, Upper)
    ControlChartData.cs — readonly record struct: what ShewhartChart plots and how it estimates the limits, made by a factory
    ControlRules.cs — [Flags] enum, Nelson's eight tests numbered 1 to 8 (None, All)
    GappedLine.cs — internal: the points of ONE line drawn as several runs, with the one separator between two runs; Segments, the line of many segments over arrays of their exact length
    SegmentPath.cs — internal readonly record struct (X, Y): many segments as the points of ONE line
    XYAxesExtensions.cs (Stairs) — a staircase as the points of ONE line: two points for each bin
    CategoricalAxesExtensions.cs (Marimekko, MarimekkoCells) — the cells of a Marimekko chart
    DistributionAxesExtensions.ProductLimit.cs — internal: ProductLimit() — the product-limit estimate of a group
    GridAxesExtensions.cs (CalendarGrid) — internal: a span of days laid out as a calendar, where a day stands
    QQReference.cs — readonly record struct: the distribution ProbabilityPlot compares a sample with
    SpectralOptions.cs — readonly record struct (QQReference shape: nullable backing fields, so default = matplotlib's psd defaults)
    SpectrumOptions.cs — readonly record struct (QQReference shape, default = matplotlib's magnitude_spectrum defaults)
    SpectralOptionExtensions.cs — internal: AsSampleRate, AsPadTo, AsCenterFrequency and AsSides
    CorrelationOptions.cs — readonly record struct (QQReference shape, default = matplotlib's acorr/xcorr defaults): MaxLags 10
    ThemeBuilder.cs — custom themes: Theme.CreateFrom().WithFont().WithName().WithAlarmPalette().Build()
    OpsDashboardBuilder.cs — Plt.OpsDashboard(): tiles + timelines + topology panels + trend panel on ONE pinned time window
  Extensions/
    EnumMemberExtensions.cs — internal: ThrowIfNotDefined(name, sentence) and NotAMember(name, sentence) on any enum
    FigureExtensions.cs — Save(), Transform(), ToSvg(), ToJson(), RegisterTransform() — the output methods
    FigureDataTableExtensions.cs — ToDataTables() on Figure and on Axes: the figure's data as tables
    FileReplaceExtensions.cs — internal: ReplaceFileWith on a path — the one way the library writes a file
    AngleExtensions.cs — internal extension(double): ToRadians / ToDegrees — the one answer to "this angle in the other unit"
    ArrayLengthExtensions.cs (ThrowIfEmpty and ThrowIfNoCell, beside the length checks) — internal: ThrowIfEmpty(message) on an IReadOnlyCollection<T>
    ArrayElementExtensions.cs — internal: ElementOr / ElementOrNull on an optional array, a list or a Vec
    ArrayLengthExtensions.cs — internal: ThrowIfMisaligned — the one check, in one wording
    InvariantTextExtensions.cs — internal: a number's text and the number a text holds, the same in every culture: InvariantText
    LineEndExtensions.cs — internal: the line end of every text the library and its packages write, LineEnd
    MissingValueExtensions.cs — internal: NaN is the one missing value, and only a finite value can be drawn
```

### Models: the figure, its axes and what is drawn on them

```
  Models/
    Figure.cs — top-level container: Title, TitleStyle (the title's font over the theme's, and extra room below it), Width, Height
    FigureText.cs — a text on the figure itself (matplotlib's figtext): Text, X
    FigureImage.cs — an image on the figure itself (matplotlib's figimage): Data[,] (row 0 on top)
    Axes.cs — subplot: series, the overlays
    Camera3D.cs — the camera of a 3-D axes: where it stands at rest
    Axis.cs — label, min/max, scale, ticks, Inverted
    AxisTransform.cs — abstract class, internal constructor (only the library derives one)
    AxesGridExtensions.cs — internal extension(Axes): GridUnder / ShapeGridUnder / WallGridUnder
    AxesOverlays.cs — internal, one per Axes (Axes.Overlays): the eight overlay lists, the lines, spans
    ColorBar.cs (AskedFor) — internal: ColorBar.AskedFor(bar, configure), the colour bar a WithColorBar call leaves
    YAxisContainer.cs — every stacked axis of an Axes: Left and Right (the Y axes) and Bottom
    YAxisStack.cs — the Y axes of ONE side, in stack order, 0 nearest the plot
    YAxisSlot.cs — sealed class (Axis, Side, Location, Series, AddSeries): one stacked axis and the series plotted against it
    SpineEdgeExtensions.cs — internal: the SpineEdge enum and the one edit of spines by edge: On(edge), WithEdge(edge, change), Hiding(edges) (one copy)
    AxesLastSeriesExtensions.cs — internal: LastSeriesOf<T>() (the series of a kind added last) and LastSeriesIs<T>()
    Annotation.cs — text annotation: Text, X, Y, ArrowTargetX / ArrowTargetY, Font (null = the theme's data text font), Alignment
    ReferenceLine.cs — horizontal/vertical reference line (AxHLine, AxVLine)
    SpanRegion.cs — shaded horizontal/vertical region (AxHSpan, AxVSpan)
    IStrokedTool.cs — internal interface beside the public models of a drawn annotation line
    GridSpec.cs — unequal subplot layout: Rows, Cols, HeightRatios, WidthRatios
    LayoutRect.cs — sealed record (Left, Bottom, Right, Top) in fractions of the figure, matplotlib's rect of tight_layout
    ValueObjects/InsetBounds.cs — inset position: X, Y, Width, Height
    ZoomIndicator.cs — the zoom an inset indicates on its parent (matplotlib's indicate_inset_zoom)
    SpinesConfig.cs — per-spine visibility/position/width: Top, Bottom, Left, Right
    TreeNodeExtensions.cs — TreeNode.Walk() — DFS pre-order enumerable
    DendrogramExtensions.cs — public Dendrogram.ToTree(leafLabels?): a clustering as the one tree a chart draws
    ValueObjects/PanelWrap.cs — internal (Rows, Columns): how panels wrap into a grid, Filled
    TreeNodePartitionExtensions.cs — internal: Shares() → PartitionShare(Node, Share)[] and Levels()
    ChartDataTable.cs — the accessible alternative to the picture: Caption, Columns, Rows, and ToHtml() / ToMarkdown() / ToCsv()
    AxisNames.cs — internal readonly record struct (Axes, OnPrimaryX, OnPrimaryY)
    ChartDataColumn.cs — readonly record struct (Header, DataColumnKind, DataAxis) — the kind decides how a cell PRINTS
    NumberColumn.cs — internal readonly record struct (Header, Values, DataAxis): a column of numbers a table takes after its point
    DataCell.cs — readonly record struct: Empty | Number | Text
    ValueObjects/OpsCondition.cs — what an operations reading is in, on TWO axes that must never be one
    Patches/ — shapes drawn on an axes in data coordinates, matplotlib's patches
      Patch.cs — abstract base, a private protected constructor (only the library derives one), an IEdgedPatch: EdgeColor
      FilledPatch.cs — abstract: a patch with a face, FaceColor
      Rectangle.cs — sealed: Anchor (Interaction.DataPoint), Width, Height (negative = left of / below the anchor), Angle
      RotationPoint.cs — public readonly record struct, matplotlib's rotation_point as one value: Anchor
      Polygon.cs — sealed: Vertices (IReadOnlyList<DataPoint>, a copy kept), Closed (true by default)
      Arrow.cs — sealed: matplotlib's FancyArrow (Axes.arrow) in data units: Start, Dx, Dy, Width (0.001), HeadWidth
      Circle.cs — sealed: Center, Radius; matplotlib's unit circle (8 cubics, 26 vertices) scaled and moved
      Ellipse.cs — sealed: Center, Width and Height (FULL diameters, as matplotlib's), Angle
      Wedge.cs — sealed: Center, Radius, Theta1, Theta2 (degrees counter-clockwise; a first angle past the second sweeps through 0)
      Arc.cs — sealed, derives from Patch (no face: matplotlib refuses to fill an arc; DrawnFaceColor = none, a black edge): Center
      EllipticArc.cs — internal readonly record struct (Center, Width, Height, Angle): UnitCircle (matplotlib's Path.unit_circle)
```

### Models/Series: the series types

Each kind of series is one file named after it; its settings are in the API reference.

```
    Series/ — 91 series types across 16 categories (Streaming is one of them)
      ISeries, IPolarSeries, I3DGridSeries, I3DPointSeries, IPriceSeries — one file each
      IHasDataRange.cs — interface: series that expose their own data bounds
      Streaming/StreamRing.cs — internal StreamRing<TSample>: the samples of a streaming series in a RingBuffer<T>
      Streaming/IVersionedSeries.cs — internal, beside the public IStreamingSeries and IStreamingOhlcSeries: Version
      IColormappable.cs — interface: ColorMap { get; set; } — polymorphic colormap assignment
      INormalizable.cs — interface: Normalizer { get; set; } — polymorphic normalizer assignment
      ICategoryLabeled.cs — interface: CategoryLabels for polymorphic tick-label rendering, on the axis that carries the categories
      INamesValueAxis.cs — internal interface: the name a seaborn verb gives the axis its values run along
      ISaturatedFill.cs — internal interface: how much of its colour's saturation a kind keeps under a theme (SaturationUnder)
      ICategoryPlacement.cs — internal interface beside ICategoryLabeled: CategoriesAlongY, CategoriesTopDown (default false)
      ITableNamedByAxes.cs — internal interface: ToDataTable(AxisNames)
      IColorBarDataProvider.cs — interface: GetColorBarRange() + ColorMap for colorbar auto-detection
      IStackable.cs — interface: Values a stacked render sums (on two running sums, StackableExtensions) + StackBaseline
      IKeepsBarStack.cs — internal interface beside the public IAxesContext: BarStack
      IHasColor.cs — interface: Color? Color — polymorphic color access (~20 series)
      IMapSeries.cs — internal interface, OutlineIn(transform): a series that draws a map
      IHasAlpha.cs — interface: double Alpha — polymorphic alpha access (~7 series)
      IHasEdgeColor.cs — interface: Color? EdgeColor — polymorphic edge-color access (~3 series)
      ILabelable.cs — interface: bool ShowLabels; string? LabelFormat — series with data-point labels
      ILabelFormatted.cs — internal, beside ILabelable: DefaultLabelFormat of a kind
      IHasLabelPlacement.cs — interface: SeriesLabelPlacement LabelPlacement { get; set; }
      LinePoints.cs — internal readonly record struct: one consistent snapshot of a line's x and y values
      ChartSeries.cs — abstract base: implements ISeries + ISeriesSerializable
      KeptEstimate.cs — internal sealed generic: an estimate a series makes from its observations
      LegendSwatch.cs — internal: enum LegendSwatchKind
      MatplotlibArtist.cs — internal readonly record struct (Call, ZOrder, ThemeColor, ClipOn) + enum ThemeColorRole
      LegendPart.cs — internal readonly record struct (Label, Swatch, ColorSlotOffset, Color): one entry of a legend
      RadialExtent.cs — internal readonly record struct (Min, Max, Sticky): the radii a series reaches on a polar axes, below zero too
      XY/
        XYSeries.cs — generic base: XData, YData, MaxDisplayPoints (Line, Scatter, Step, Area, ErrorBar, Bubble, Sparkline, Stem, Ecdf)
        YAxisSlotXYVerbs.cs — internal: YAxisSlot.Plot / Scatter (lengths checked, series made, series put on the slot)
        IndexRange.cs — readonly record struct(StartInclusive, EndExclusive) + Count/IsEmpty — returned by IMonotonicXY.IndexRangeFor
        LineSeries, ScatterSeries, AreaSeries, StepSeries, ErrorBarSeries, BubbleSeries, SparklineSeries, EcdfSeries,
            StackedAreaSeries, RegressionSeries, ResidualSeries, RelativeRotationSeries — one file each
      Categorical/
        BarSeries, HistogramSeries, WaterfallSeries, FunnelSeries, GanttSeries, ProgressBarSeries, StatTileSeries,
            BulletGraphSeries, StateTimelineSeries, EventplotSeries, BrokenBarSeries, CountSeries, PointplotSeries — one file each
        CategorySlot.cs — internal readonly record struct (Left, Centre, Right): category k owns [k, k + 1], centred CentreOffset
        CategoryPlacementExtensions.cs — internal: OnAxes(range, orientation) puts a range measured upright
        BarHeightExtensions.cs — internal: AcrossRows(rows) on a bar height
        StackableExtensions.cs — internal: StackedFeet(layers) / Stack(layers) → BarStack — stacking bars on TWO running sums
        BarStack.cs — internal readonly record struct (Feet, Ends): a stack worked out once, each layer's feet and ends
        BarArrangementExtensions.cs — internal: Axes.ArrangeBars(), the one rule for where the bars of an axes stand in a draw
        BarGroup.cs — public readonly record struct (Label, Values): a name and one value per place
        BarGroupExtensions.cs — internal: ThrowIfMisaligned(count, paramName, places) — one value per place
        FloatingBars.cs — internal sealed class: bars that float
      Circular/
        WedgeSeries.cs — public abstract (private protected ctor: only the library derives), matplotlib's pie wedges
        PieSeries, DonutSeries, WaffleSeries, GaugeSeries, RadarSeries — one file each
      Distribution/
        DatasetSeries.cs — distribution base: Datasets[][], default ComputeDataRange
        BoxSeries, ViolinSeries, KdeSeries, RugplotSeries, StripplotSeries, SwarmplotSeries — one file each
        DatasetPositionExtensions.cs — internal: PositionOf(index, DatasetStanding) — where a dataset stands
        ViolinBody.cs — internal: ViolinSlice(Y, HalfWidth), the width of a violin's body at one height, and ViolinBody(Sample, Outline)
        NamedSample.cs — public readonly record struct (Label, Values): a sample of any length with a name, the group type of Ridgeline
        NamedSampleExtensions.cs — internal: Observations / SortedObservations — the observations of a sample that can be drawn
        SurvivalGroup.cs — public readonly record struct (Label, Durations, Observed): one group of survival data
      Grid/
        HeatmapSeries, ImageSeries, Histogram2DSeries, ContourSeries, ContourfSeries, HexbinSeries, PcolormeshSeries,
            SpectrogramSeries, TricontourSeries, TripcolorSeries, ClustermapSeries, PairGridSeries — one file each
        GridContourSeries.cs — public abstract (private protected ctor: only the library derives), matplotlib's QuadContourSet
        ../IThemedColorBar.cs — internal, beside the public IColorBarDataProvider, implemented by the line, filled and triangle contours
        ../IColorScaled.cs — internal, beside the public IColormappable and INormalizable: ColourRangeOver(ColorMapping)
      Financial/
        OhlcSeries.cs — financial base: Open, High, Low, Close, DateLabels, UpColor, DownColor, PriceData (Candlestick, OhlcBar)
        CandlestickSeries, OhlcBarSeries, PointAndFigureSeries — one file each
      Field/
        StemSeries, QuiverSeries, StreamplotSeries, QuiverKeySeries, BarbsSeries — one file each
      Polar/
        PolarSeries.cs — generic base: RData, ThetaData (PolarLine, PolarScatter, PolarBar)
        PolarLineSeries, PolarScatterSeries, PolarBarSeries — one file each
      ThreeD/
        GridSeries3D.cs — generic base: XData, YData, ZData[,] (Surface, Wireframe)
        SurfaceSeries, WireframeSeries, Scatter3DSeries, Stem3DSeries, Bar3DSeries — one file each
      Hierarchical/
        HierarchicalSeries.cs — abstract base: Root, ColorMap, ShowLabels, IColormappable (Treemap, Sunburst, Dendrogram)
        TreemapSeries, SunburstSeries, IcicleSeries, DendrogramSeries — one file each
      Graph/
        NetworkGraphSeries — one file each
        GraphNode.cs — readonly record struct: Id, X, Y, ColorScalar, SizeScalar, Label
        GraphEdge.cs — readonly record struct: From, To, Weight, IsDirected
      Flow/
        SankeySeries, ChordSeries — one file each
        ChordLayout.cs — pure geometry of a chord diagram in radians
      Text/
        WordCloudSeries — one file each
        AxesTextVerbs.cs — extension block: Axes.WordCloud(words, weights) refuses a weight that is not above zero
      Ternary/
        TernarySeries.cs — abstract base of the ternary series: Top, Left, Right as the author gave them
        TernaryScatterSeries.cs, TernaryLineSeries.cs — a marker, or a polyline, on each composition of three parts
        PiperSeries — one file each
        WaterSample.cs — readonly record struct: Label, Ca, Mg, Na, K, HCO3, CO3, Cl, SO4 in mg/L
        AxesTernaryVerbs.cs — extension block: TernaryPlot, TernaryScatter, Piper
      Special/
        TableSeries — one file each
```

### Rendering: the figure, the frame and the primitives

```
  Rendering/
    IChartRenderer.cs — interface: Render(Figure, IRenderContext), ComputeLayout
    ChartRenderer.cs — figure-level orchestrator
    IDataPlacement.cs — internal interface: where a point of an axes' data stands, PlaceUnclipped
    PatchDrawing.cs — internal readonly struct: the one drawing of an axes' patches, whatever its coordinate system
    FigureRanges.cs — internal: the data range of each subplot for one draw, worked out the first time the draw asks
    Frames/ — what a draw is drawn ONTO
      FrameTarget.cs — abstract: the surface (Overlay) and DrawAxes(AxesDrawRequest)
      KeepingFrameTarget.cs — abstract KeepingFrameTarget<TLayer>: the reuse rule ONCE
      KeptAxesDraw.cs — KeptAxesDraw<TLayer>: one kept draw of an axes — its AxesDrawKey
      FrameTargetPaintExtensions.cs — internal: target.Paint(figure, band) — the one paint of a native control
    ImageCells.cs — internal readonly record struct (Scale, Alpha): the image's colour scale
    PlotAspectExtensions.cs — owns Axes.Aspect: the length of a unit of Y over a unit of X on the page
    IRenderContext.cs — drawing primitives: DrawLine/DrawRect/etc
    IThemedRenderContext.cs — internal, beside IRenderContext (A15): UseLooks(ThemeLooks)
    ThemedSurface.cs, ISurfaceRenderContext.cs — internal: what a drawing surface keeps of what a draw tells it
    CanvasShapeExtensions.cs — internal interface ICanvasShape
    ClipRegion.cs — internal readonly record struct (Bounds, Outline): where the marks of a plot are cut off
    RenderContextScopeExtensions.cs — internal: OpacityScope, ClipScope and TextClipScope
    MarkClips.cs — internal sealed class MarkClips<TClip> + internal interface IClipSurface<TClip> (Apply, Lift)
    IPathSink.cs — internal interface: what a path becomes on a target, five members (MoveTo, LineTo, CubicTo, ArcTo, Close)
    ISeriesVisitor.cs — visitor pattern: Visit() for each of the 91 series types
    AxisBreakMap.cs — internal: the breaks of one axis, the range they are cut from and the axis' scale, merged into their union
    AxisMapping.cs — internal readonly record struct: how one direction of a transform maps its values
    CalloutBox.cs — internal readonly record struct: the box behind an annotation's text (style, padding, corner radius, face, edge)
    ArrowHead.cs — internal: ArrowHead.Of(style), one sealed head per kind of ArrowStyle
    ConnectionPath.cs — internal: ConnectionPath.Of(style), one sealed path per ConnectionStyle
    MarkerShape.cs — internal: MarkerShape.Of(style), one sealed shape per MarkerStyle
    DataTransform.cs — data space <-> pixel space, one PixelDirection per direction
    PixelRoom.cs — internal readonly record struct (Lo, Hi, Jump): a room on the screen of an axis drawn in parts
    PixelDirection.cs — internal readonly struct: one direction of a transform, the X and the Y one rule
    DataTransform.StartEdge.cs — private readonly record struct (Edge, PerUnit, AlongX): the edge of the plot an axis begins at
    PixelSpreadExtensions.cs — extension(Span<double>): the kernels that compute Spread a vector at a time
    PathSegmentExtensions.cs — internal: an outline in data units (a patch's)
    RenderArea.cs — plot bounds + context container
    ValueObjects/Point.cs, Size.cs, Rect.cs, DataRange.cs — record structs Point, Size, Rect and DataRange, one file each
    ValueObjects/PointExtensions.cs, Heading.cs — internal: Point.StepTo / Length / SquaredLength / DistanceTo / DistanceSquaredTo / HeadingTo
    Rect.Center reads AlongExtensions.CentreAlong, the one place half a side is added to a rectangle's start; every renderer reads Rect.Center
    Range1D.Around(centre, length) and Range1D.CentredIn(start, room, length) — internal: a span of a length centred on a point
    ValueObjects/RoundFrame.cs — internal: RoundFrame(Centre, Radius), the circle a round chart is drawn in
    Normalized3DPoint.cs — readonly record struct(Nx, Ny, Nz) — returned by Projection3D.Normalize()
    MarkObstacles.cs — internal: the marks a chart drew on one axes, in pixels as drawn: boxes (a bar, a text's box), paths
    MarkCollectingContext.cs — an IRenderContext that measures through the one it wraps
    TextBox.cs — the rectangle a text occupies (TextPlacement, TextExtent)
    ThemeFontExtensions.cs — the theme's fonts as members of Theme, every role made from one private BodyFont(size, weight, colour)
    TickLocators/SteppedTicks.cs — internal: TickLadder
    ValueObjects/LineSegmentExtensions.cs — a straight line cut to the area it is drawn in
    ValueObjects/LinkedRange.cs — internal readonly record struct (Interval, Increasing): the range a linked axis shows
```

### Rendering: the axes renderers

```
    AxesRenderer.cs — abstract base for coordinate-system-specific rendering (a NamedTable of factories per coordinate system)
    SeriesPass.cs — one pass over series, made only by its factories
    LayerOrder.cs — the one orderer: the steps of a pass (SeriesPass.Order: grid, patches, series) or of a Cartesian data layer
    LayerStep.cs — one step of an axes' data layer
    ZOrderExtensions.cs — extension(Theme): where each thing of a Cartesian data layer stands in the z-order
    CartesianAxesRenderer.cs — Cartesian (X,Y): keeps the drawing order
    CartesianAxesRenderer.CartesianDirectLabelsPart.cs — the end labels: placed ONCE per axes
    CartesianEdges.cs, OppositeTicks.cs — what the edge opposite each primary axis draws: Mirrored
    PlacedAxis.cs — ONE axis as one render places it, on any edge: Axis, Edge, Range, Transform, Placement, Ticks, Source
    CartesianAxisPlacement.cs — internal readonly struct over the axes, the plot area, the context, the theme and the draw's ranges
    GridLineExtensions.cs — extension(IRenderContext): RenderGrid
    CartesianInsetAxesRenderer.cs — inset axes (Axes.InsetBounds; chosen by the Cartesian factory entry)
    CartesianParts/ — one thing drawn per part
      CartesianPartContext.cs — the axes, plot area, context, theme
      CartesianDataLayer.cs — the host of the primary series' pass, a value on the render's stack: the grid, spans, patch runs
      CartesianAxisPart.cs — ONE part for EVERY axis
      CartesianSpansPart.cs — shaded spans behind the series, cut at the plot edge
      CartesianPatchesPart.cs — the patches (Axes.Patches), each at its place in the data layer
      CartesianReferenceLinesPart.cs — full-width reference lines
      LineLabelPlacement.cs — where the label of a drawn line stands
      CartesianToolsPart.cs — trendlines, levels, Fibonacci overlays
      CartesianBreakMarkersPart.cs — the axis break marks, drawn over the series: per break
      CartesianAnnotationsPart.cs — text, arrows, callouts: the text in the theme's data text font on its point's baseline, centre
      CartesianSignalsPart.cs — buy/sell markers
    PolarAxesRenderer.cs — Polar (r,theta): eight spokes every 45 degrees with their angle labels
    RadialScale.cs — internal readonly record struct (Extent, Rings, Labels, Feet) with Min (rmin) and Origin
    TernaryAxesRenderer.cs — Ternary: the triangle, a grid at every fifth, ticks that run counter-clockwise
    TernaryPoint.cs — readonly record struct TernaryPoint(Top, Left, Right), TernaryFrame (the frame) and TernaryPointExtensions
    WordCloudLayout.cs — internal static: each word's size (the square root of its weight between two limits)
    Projection3D.cs — matplotlib's 3-D pipeline (world, view, perspective) composed into one matrix, kept as four rows
    DepthQueue3D.cs — the one place the marks of a 3-D scene are put back to front
    DepthMarks.cs — IDepthMark (a mark's depth)
    SeriesGroup.cs — internal readonly record struct (Axes, Series, Slot): the group a series is drawn in
    Projected3DPoint.cs — internal readonly record struct Projected3DPoint(X, Y, Z), a point of a 3-D scatter after the projection
    ThreeDAxesRenderer.cs — 3D (X,Y,Z): the order a draw follows
    ThreeDAxesRenderer.CubeAxisRow.cs — internal readonly record struct, ONE per axis a draw (Of): the tick values
    ValueObjects/CubeFaceSelection.cs — which of each axis' two parallel cube faces points AWAY from the camera
    Lighting/ — per-face 3D lighting model
      ILightSource.cs — interface: ComputeIntensity(nx, ny, nz) → [0,1] + the default member Shade(color, normal)
      DirectionalLight.cs — sealed record: Dx/Dy/Dz direction + Ambient/Diffuse
      Vec3.cs — readonly record struct Vec3(X, Y, Z) + static FaceNormal(v0, v1, v2) cross-product face normal
      ../../Styling/ColorExtensions.cs — Color.Modulate(intensity), Color.Shade(normal, lightDir) — extensions on Styling.Color
```

### Rendering: series colour, ticks, axis titles and ranges

```
    SeriesSlot.cs — a series' colour AND its stroke: SeriesAxisPair
    SeriesGroupExtensions.cs — the group a series is drawn in
    AxisTickExtensions.cs — the one tick drawing on a surface, extension(IRenderContext): RenderAxisTicks
    TickDrawContext.cs — internal readonly struct: where and how one axis' ticks are drawn
    LabelReach.cs — internal readonly record struct (Away, Before, After): how far a turned label reaches
    TickRun.cs — internal readonly record struct (Ticks, Placement, Edge): the major ticks of one axis on the edge they lie on
    TickInk.cs — internal [Flags] enum (Labels, Marks, Both): what a drawing of ticks puts on the page
    TickMarkSpan.cs — internal readonly record struct (Outer, Inner)
    TickMark.cs — internal readonly record struct (Stroke, Drawn, Marker): the mark of one tick look, worked out once per look
    TickLabelExtensions.cs — the labels of a set of ticks: UniformFormat
    AxesTitleExtensions.cs — the one title rule of an axes: TitleOf(axis, theme)
    MinorWalk.cs — internal readonly struct, a struct enumerable: the minor values between the major ticks
    ITickPlacement.cs — internal interface (OnPlot, ScaleAlong, ViewAlong): where the tick drawing stands a tick of a given value
    LinkedTickPlacement.cs — internal sealed ITickPlacement of a linked axis (Axis.Link)
    FacingInk.cs — internal readonly record struct (Facing, Outward, Dip)
    TitleClearance.cs — internal readonly record struct (TickLabelBand, Gap, LabelSize, Ink; WithoutTickLabels)
    YAxisTitleExtent.cs — YAxisTitleInput (tick label band, side, gap, theme title font)
    EdgeTerms.cs — internal sealed record, one per edge (Bottom, Top, Left, Right; Of(TickSide), Of(AxisSide))
    AxisPair.cs — internal readonly struct AxisPair<T> (X, Y as readonly fields): one value per direction of the plot
    SpineInsets.cs — internal readonly record struct (Left, Right, Top, Bottom)
    AlongExtensions.cs — the one place a direction picks the X or the Y member of a pair, whatever its flag is called
    ClosedStrokeExtensions.cs — a closed outline drawn with a stroke: its shape when solid (DrawsAsShape)
    ScaleRule.cs — internal abstract class + one sealed instance per AxisScale (Linear, Log, SymLog, Logit, Date): what a scale does
    AxisScaleExtensions.cs — scale.Rule, the one switch from AxisScale to its ScaleRule
    AutolimitRule.cs — internal abstract class, where the free ends of an axis stop, one sealed leaf per theme word: Data
    AxisAutoscale.cs — internal readonly struct over an Axis and its rule: what an axis does with the range its data reach
    SymlogTransform.cs — public: matplotlib's SymmetricalLogTransform and its inverse, the linear part stretched by linscale /
    AxesTickSourceExtensions.cs — TickSource (Locator, Formatter, NamesCategories) + PrimaryTickSource(isX, theme)
    AxesRange.cs — internal readonly record struct (Range, PatchesMayReachPastPlot)
    AxesAsk.cs — internal readonly record struct (Tight, RowsDown, Aspect, MarginRaise) and the internal IAsksOfAxes
    AutoscaleAskExtensions.cs — whether something asks for the axes to be scaled under the matplotlib themes: a series
    AxesRangeExtensions.cs — the automatic range of an axes: SnapshotContributions evaluates every series' ComputeDataRange once
    TickLocators/ — ITickLocator strategy for axis tick placement
      ITickLocator.cs — interface: double[] Locate(double min, double max)
      IRoundingLocator.cs — internal interface beside it: RoundedOut(range), where the locator ends an axis under round_numbers
      ITickSpaceLocator.cs — internal interface beside it: LocateWithSpace(min, max, tickSpace, theme)
      IMinorTickLocator.cs — internal interface beside it: MinorsOver(view, majors, tickSpace, theme)
      LogitLocator.cs — internal: the scale's locator of a logit axis
      TickRequest.cs — internal readonly record struct (Span, LengthPx, LabelPx, AlongX, Theme, Kind; Given, Spacing, LibraryLengthPx)
      TickBudget.cs — internal readonly record struct (Fewest, Most): how many ticks the room along an axis asks for
      TickEdge.cs — internal readonly record struct (Step, Tolerance): matplotlib's _Edge_integer(step, offset)
      ArithmeticTicks.cs — internal readonly record struct (First, Step, Limit): the one walk of a regular ladder of ticks
      TickAxisKind.cs — internal abstract class, the kind of axis a request asks for, one sealed leaf per kind: LibraryMajors
      AutoMinorLocator.cs — internal readonly record struct (Divisions): matplotlib's AutoMinorLocator(n)
      AutoLocator.cs — nice-number algorithm, the nearest step
      MaxNLocator.cs — nice numbers capped to at most N ticks
      MultipleLocator.cs — ticks at multiples of a fixed base (ArithmeticTicks.Multiples)
      FixedLocator.cs — exactly the provided positions within range
      LogLocator.cs — TicksNamedOver(lo, hi, tickSpace, theme), the one rule of the ticks a log axis and a log-norm colour bar name
      AutoDateLocator.cs — OA date range → DateInterval → aligned tick positions
    TickFormatters/
      ITickFormatter.cs — interface: string Format(double value)
      NumericTickFormatter.cs — G5 with scientific fallback
      DateTickFormatter.cs — OLE dates with configurable format string
      AutoDateFormatter.cs — reads ChosenInterval from AutoDateLocator, selects matching format
      LogTickFormatter.cs — a power of 10 as 10ⁿ, any other value as c×10ⁿ (SciNotation, matplotlib's LogFormatterSciNotation)
      ITickSetFormatter.cs — internal interface beside ITickFormatter: ForTicks(ticks, view, linThresh, theme), matplotlib's set_locs
      LogitFormatter.cs — internal: matplotlib's LogitFormatter
      EngFormatter.cs — SI prefix engineering notation (k, M, G, m, µ, n)
      PercentFormatter.cs — value/max*100 + "%" suffix
      CategoryFormatter.cs — a tick from k to just below k + 1 → label k
      DateTimeTickFormatter.cs — FromArray (index-based) + FromEpochMs (Unix ms) factories
```

### Rendering: layout and legend

```
    Layout/ — margin computation
      ConstrainedLayoutEngine.cs — the layout of a figure on both paths
      AxesEdgeNeeds.cs — what one axes needs at the edges of its plot, as both paths measure it, holding the layout's pads
      ConstrainedLayoutEngine.AxisMargins.cs — internal struct, one per direction
      AxisNeeds.cs — readonly record struct (Start, End, StartBand, EndBand, PastEnd)
      ConstrainedLayoutEngine.SettleColorBars.cs — SettleColorBars(needs, …, stage) keeps the room of the colour bars the figure's ColorBarRoom settles at that stage
      FigureGrid.cs — internal readonly struct, built once per draw
      Sides.cs — readonly record struct Sides<T>(Left, Bottom, Top, Right): one value per side
      SupTitleBand.cs — internal static: TopPad (8), BottomPad (12) and Needed(titleHeight)
      AxisStackingExtensions.cs — StackedOffsets(gap)/TotalStackedBand(gap) on IReadOnlyList<double>
      LabelLayoutEngine.cs — Place(candidates, bounds): continuous repulsion of text boxes that stay inside the bounds
      LayoutPads.cs — internal readonly record struct (Edges, Shared, Bounds): the pads of a layout
      LayoutBounds.cs / MarginBounds.cs — the floor and the cap of each margin and gap
      AxesLeftEdgeExtensions.cs — internal extension(Axes): LeftNeed
      EdgeBands.cs — readonly record struct
      BandsBelowAxis.cs — readonly record struct (TickLabels, Title): what one X axis draws below the edge it hangs from
      RightBand.cs — readonly record struct (SecondaryAxis, MirroredYTicks, EndLabels): the right edge in drawing order
      DirectLabelMetrics.cs — what the end labels say, where each line ends (last finite point inside the visible X range), their font
      Interval1D.cs — readonly record struct (Start, Size): a span on one axis
      Interval1DExtensions.cs — SpreadApart: order-preserving collision sweep over spans
    LegendRendering/ — the legend
      LegendRenderer.cs — internal: one legend from a list of entries in a rectangle, under a Legend config, a theme and a surface
      LegendLookExtensions.cs — the one rule for each setting of a legend its author may leave alone and its theme decides
      Styling/LegendBox.cs — how a legend's box is laid out round its rows, a value of LegendLook.Box
      LegendLayoutCalculator.cs — the legend's geometry from the Legend config, its labels and the plot area
      LegendPositionStrategy.cs — one strategy per place
      LegendEntry.cs — abstract record LegendEntry(Label): one row of a legend, which says for itself its ToggleIndex
      LegendValueTextExtensions.cs — internal: how a legend writes a series' value (Legend.LegendValues)
      SeriesLegendEntry.cs — sealed record (Series, OriginalIndex, ColorSlotIndex, Label, Swatch): ToggleIndex = OriginalIndex
      PatchLegendEntry.cs — sealed record (Patch, Label, Index): the label of the patch
      FigureLegendExtensions.cs — the legends of a figure (Figure.Legends)
```

### Rendering: text

```
    TextMeasurement/ — how much room a text takes
      DefaultFontMetrics.cs (the width of each character) — internal static: per-char width factors for Helvetica/Arial at 1em
      IFontMetrics.cs — interface: Measure(text, font) — DefaultFontMetrics here, SkiaFontMetrics in Skia
      IGlyphInkMetrics.cs — internal interface beside it (A15): InkOf(text, face, size) → GlyphInk, BandOf(font) → FontBand
      GlyphInk.cs — internal readonly record struct (Advance, PixelAdvance, Rise, Drop): a text's shaped advance
      FontBand.cs — internal readonly record struct (Ascender, Descender): a font's typographic band (OS/2) at its size
      TextExtent.cs — internal readonly record struct: Width, Height, Ascent (Descent, Box derived)
      TextExtentExtensions.cs — internal extension(IFontMetrics) and extension(IRenderContext)
      SurfaceFontMetrics.cs — internal sealed IFontMetrics over a surface
      IGlyphPathProvider.cs — interface: GetPathData(text, font) — text as SVG <path>; null → <text> fallback
      IShapedGlyphPathProvider.cs — internal interface beside it: GetOutline(text, SpanFont) → ShapedOutline
      ShapedOutline.cs — internal readonly record struct: PathData, Advance
      GlyphPathProviderExtensions.cs — internal extension(IGlyphPathProvider): OutlineOf(text, font, alignment)
    Text/ — the Unicode Bidirectional Algorithm, UAX #9
      BidiClassTable.cs / .g.cs — internal static: GetClass(codePoint) by binary search over 1 267 generated ranges
      BidiBrackets.cs / .g.cs — internal static: the 128 paired brackets for rule N0, canonical equivalents
      BidiAlgorithm.cs — internal static: the paragraph
      BidiAlgorithm.BidiExplicitLevels.cs — ref struct: X1–X8, the directional status stack and its three counters
      BidiAlgorithm.BidiRunSequence.cs — readonly record struct: an isolating run sequence (X10, BD7, BD13: AllOf(paragraph))
      CodePointExtensions.cs — extension(ReadOnlySpan<char>): UnitsAt(i), CodePointAt(ref i)
      BidiParagraph.cs — readonly record struct: Levels per UTF-16 unit (Removed for X9), ParagraphLevel, Order (the visual order)
    MathText/ — mathtext → a tree of boxes (MathBox)
      MathTextParser.cs — Parse(text): the flat view (MathSpanWriter.Write) of MathTextCursor.Parse
      TextLabel.cs — internal readonly record struct: a text and, when it holds $...$, the tree (LineBox) it parses to
      KeptMathTexts.cs — internal sealed: the trees of the math texts a figure's draws read (Figure.MathTexts)
      MathTextRenderContextExtensions.cs — extension(IRenderContext): Draw(TextLabel, font, TextPlacement)
      MathTextCursor.cs — internal ref struct: the parse into boxes
      KeptByThread.cs — internal static, generic: the one instance a thread keeps between two uses (Take, Return emptied)
      MathTextCursor.MathPendingText.cs — internal struct: the run a parse is reading, a stretch of the label cut without a copy
      MathNames.cs — internal sealed, generic: a table of math names read by the characters of a name where it stands in a label
      MathSource.cs — internal readonly record struct: the label's text and where its math starts
      MathGrammar.cs + MathGrammar.Data.cs — internal: the grammar of matplotlib 3.11.2's mathtext
      MathCommand.cs — internal: the command table (Find), one sealed leaf per \cmd family
      MathBox.cs + ScriptsBox.cs + LimitsBox.cs + FractionBox.cs + SizedGlyph.cs + DelimitedBox.cs + RadicalBox.cs + OverlineBox.cs — + UnderlineBox.cs + StackBox.cs + MatrixBox.cs
      MathExtent.cs + MathMeasure.cs + MathStyle.cs + MathInkMark.cs + MathInk.cs — internal: MathExtent (width, height, depth)
      MathConstants.cs — internal sealed record: the font set's constants, matplotlib's FontConstantsBase leaves
      MathSpanWriter.cs — internal: MathSpanWriter
      RichText.cs — RichText(IReadOnlyList<TextSpan>) + internal Box (the LineBox it was written from)
      MathFontSet.cs — internal abstract: the faces math is set in (TextLook.MathFonts, matplotlib's mathtext.fontset)
      StixFallback.cs (+ StixFallback.Data.cs, generated) — internal sealed: the STIX glyphs matplotlib takes what a set has no glyph for
      MathRun.cs — internal readonly record structs: MathFace
      SpanFont.cs — internal readonly record struct: Face, Size, as a run is measured
      GreekLetters.cs — public TryGet: the names of MathGrammar's symbol table whose character is a Greek letter
      MathSymbols.cs — public TryGet: every name of MathGrammar's symbol table
```

### Rendering: series renderers, images, large data, the colour bar

```
    SeriesRenderers/ — generic SeriesRenderer<T> per series type
      SeriesRenderContext.cs — record: Transform + Ctx + Color + Area + options
      SeriesRenderer.cs — abstract base, no virtual member (a renderer replaces nothing of it): ColorOf(series)
      PixelSegment.cs — readonly record struct: one stroked segment as drawn, for AddSegment
      DrawStyleInterpolation.cs — internal: points.SteppedBy(style)
      XY/ — Line (LTTB), Scatter (viewport cull), Step (LTTB), Area (LTTB)
      Categorical/ — Bar (ShowLabels), Histogram, Waterfall, Funnel, Gantt, ProgressBar
        PixelBox.cs — PixelBox.OnFoot(transform, slot, foot, end, rising, orientation) → PixelBox(From, To): a box that stands on a foot
        BarSeriesRenderer.cs (PixelBar, nested in BarSeriesRenderer) — FootToEnd(orientation, footColour, endColour) → the LinearGradient of a bar
      Circular/ — RingWedge (the slice of a ring: Path() as arcs for a donut and a sunburst, Polygon() / PolygonPath() as a polyline at one resolution, StepsFor, for a pie, a dial band and a polar heat map cell; CounterClockwise() makes one from radians laid as a pie lays them)
      Grid/ — Heatmap (its cells stand at their data coordinates, so a limit or a zoom moves them with the axes; RenderInArea fills the plot rectangle for a heatmap that is a panel of a larger chart: the clustermap's matrix, a filled contour's backdrop)
        PairGridSeriesRenderer.cs (the cells) — pure geometry: ComputeCellRects(plotBounds, n, cellSpacing) → Rect[n,n]
        IPairGridOffDiagonalPainter.cs — IPairGridOffDiagonalPainter strategy
        PairGridCellFrame.cs — AxisSpan (Of(data, count): the NaN-safe span of one variable), PairGridCellFrame
        PairGridHue.cs — series.HasValidHue, series.HueColors(), series.HueColorOf(group), series.SamplesByHue(variable)
      Graph/ — NetworkGraph (composite: nodes + edges in 2D, directed-edge arrowheads, optional weight labels; the nodes are placed by Numerics/GraphLayoutAlgorithm.cs once for each change of the series, series.Placement, which the data range and the drawing both read)
      Distribution/ — Box, Violin, Kde (density math delegated to Numerics/GaussianKde.cs — the theme's bandwidth rule)
        SwarmplotSeriesRenderer.cs — internal static: Compute(sortedValues, markerRadius, categoryCenter)
        BoxSeriesRenderer.cs (BoxStatistics, nested in BoxSeriesRenderer) — internal readonly record struct
      Financial/ — Candlestick, OhlcBar
      Polar/ — PolarLine, PolarScatter, PolarBar (stacked on the RadialScale feet)
      Field/ — Quiver, Stem, Streamplot, QuiverKey (reference arrow), Barbs
        StreamplotSeriesRenderer.cs (StreamlinesOf) — internal: StreamlinesOf(series) → Streamline[]
      Hierarchical/ — Treemap, Sunburst, Dendrogram
        HierarchicalLayout.cs — LabelGapPx (the gap from a panel to the names beside it: dendrogram leaves, clustermap rows and columns, pair grid variables)
        TreemapSeriesRenderer.cs (the tags of a tile) — private: TagTreemapNode(ctx, nodeId, depth, parentId, label?)
        DendrogramFrame.cs — internal: DendrogramFrame.Of(orientation), one sealed frame per DendrogramOrientation
      Flow/ — Sankey, Chord (a ring of arcs and one closed path for each ribbon, bent through the centre)
      Text/ — WordCloud (the boxes WordCloudLayout packs, each with a title and a requested text length)
      Ternary/ — TernaryScatter and TernaryLine (through the frame, no clip), Piper
      Special/ — Table (measured column widths, header+data rows, borders)
      ThreeD/ — every renderer derives SceneSeriesRenderer<T>
    Interpolation/ — image interpolation engines
      IInterpolationEngine.cs — interface: Resample(double[,], int, int) strategy
      NearestInterpolation.cs — singleton: identity / pixel duplication (default)
      BilinearInterpolation.cs — singleton: 2x2 neighborhood, linear weights
      BicubicInterpolation.cs — singleton: 4x4 Catmull-Rom kernel with output clamping
      HanningInterpolation.cs — singleton: Agg's hanning filter
      ColourSample.cs — internal readonly struct: a cell's premultiplied red, green, blue and its opacity in one Vector256
      IColourResampler.cs — internal interface beside IInterpolationEngine: ResampleColours, the four channels in one pass
      GridSpanExtensions.cs — internal: AsRows, a T[,] grid as its row-by-row span without a copy
      ImageFootprint.cs — internal: how large an image is shown against its values
      InterpolationRegistry.cs — thread-safe NamedTable, case-insensitive, mirrors ColorMapRegistry pattern
    Downsampling/ — performance for large datasets
      IDownsampler.cs — interface: Downsample(x, y, targetPoints)
      LttbDownsampler.cs — Largest-Triangle-Three-Buckets O(n) algorithm
      ViewportCuller.cs — static: filter to [xMin,xMax] + one padding point each side
      XYData.cs (AtMost) — internal: XYData.AtMost(maxPoints), the one place a line over its point budget is reduced (LTTB)
    ColorBarRendering/ — the colour bar
      ColorBarRenderer.cs — abstract base (range, colour map, extend flags) of the orientation renderers
      ColorBarStrip.cs — internal readonly record struct (Bar, Vertical, Extend, DrawEdges, Steps): the body of every colour bar
      ColorBarScale.cs — internal readonly record struct
      ResolvedColorBar.cs — internal readonly record struct (Provider, Map, Range, Normalizer, Steps, Lines)
      ColorBarRoom.cs — internal abstract class: where a figure's colour bars stand and the room they take
      ColorBarRoomExtensions.cs — internal: what a colour bar has whatever its room: BarRoom on a figure
      FigureBarStand.cs — internal sealed: where the figure's own colour bar stands in one draw
      Layout/IAxesBands.cs, Layout/MeasuredBands.cs — what an axes draws past its spines, measured when a colour bar's room asks
      ColorBarMarks.cs — internal readonly record struct (Major, Minor), the tick marks of a colour bar under a theme
```

### Rendering: SVG

```
    Svg/
      ISvgRenderer.cs — interface: Render(Figure) -> string
      SvgIdScope.cs — internal: the ids a render context writes (Clip, Hatch, Gradient): the first subplot keeps the plain ids
      SvgElementBuffer.cs (SvgGradientRegistry, nested in SvgElementBuffer) — gradient ids and the <defs> emission
      SvgWriterExtensions.cs — internal: the attribute writers every SVG writer shares
      SvgRenderContext.cs — the public SVG surface
      SvgElementBuffer.cs — internal sealed: the markup, what was asked of the next element (aria-level, class, data-*, text length)
      ISvgElement.cs — internal: one element as the envelope writes it (WriteOpening, WriteClosing), a value per kind
      ISvgGeometry.cs — internal: the geometry of a shape (HasCorners, WriteGeometry)
      SvgRenderContext.SvgMarkupWriter.cs — internal struct over the buffer: groups, links, series/patch data groups, legend entry
      SvgEmit.cs — internal readonly record struct (Subject, InteractiveAttributes, DataCursorMapping, ServerViewIndex, VertexData3D)
      SvgFrameTarget.cs — internal sealed KeepingFrameTarget<SvgRenderContext>: a context per subplot is its layer
      SvgFigureDocument.cs — the document in its parts (see "Static SVG rendering"): ToSvg writes it plain
      SvgXml.cs — internal static: EscapeForXml(this string) — XML escaping used by SvgRenderContext and SvgTransform
      SvgDocumentExtensions.cs — internal: WithSaveOptions(svg, options) — the settings of SaveOptions an SVG can use
      SvgSeriesRenderer.cs — thin visitor dispatcher to SeriesRenderer<T> instances
      SvgScriptPreamble.cs — internal: the pieces every script shares, each written once and put into the script text: Root
      SvgAttributes.cs — internal: the names the writers put on a chart for its scripts
      SvgPartMarker.cs — public: the marker of a part of a document sent in parts (Attribute, Opening, Closing, Start(name))
```

### Transforms, animation

```
  Transforms/ — polymorphic export: IFigureTransform -> FigureTransform -> concrete
    IFigureTransform.cs — interface: Transform(Figure, Stream) and Transform(Figure, Stream, SaveOptions)
    FigureTransform.cs — abstract base: holds the IChartRenderer it was made with (protected FigureRenderer)
    SvgTransform.cs — FigureTransform + ISvgRenderer: SVG rendering; applies the SVG settings of SaveOptions (SvgDocumentExtensions)
    SvgTransform.SvgInteractivityScript.cs — embedded JS: zoom/pan via viewBox
    SvgTransform.SvgLegendDragScript.cs (LegendDragScript, a member of SvgTransform) — embedded JS: press-and-hold legend item to translate <g class="legend"> via transform
    SvgTransform.Svg3DRotationScript.cs — embedded JS for mouse/keyboard interactive 3D rotation
    SvgTransform.ServerInteractionScript.cs — embedded JS of a server-interactive figure
    TransformResult.cs — fluent record: ToStream(), ToFile() (through FileReplaceExtensions), ToBytes()
    IAnimationTransform.cs — the animation contract, Transform(frames, interval, loop, stream)
    StepThroughSvgTransform.cs — an IAnimationTransform: an animation as one SVG that steps through its frames
  Animation/
    IAnimation<TState>.cs — interface: typed animation state pipeline
    AnimationController<TState>.cs — controller: runs animation loop with ConfigureAwait(false)
    AnimationBuilder.cs — the one mechanism that makes an animation's files: frames (FrameCount, Interval, Loop)
    AnimationTimer.cs — public abstract base of every IAnimationTimer of the library
```

### Indicators

```
  Indicators/ — stacked base classes
    IIndicator.cs — interface: Apply(Axes)
    IndicatorLook.cs, IchimokuColors.cs — internal: the colours an indicator chooses for a part its caller gave none, one look
    IndicatorSpec.cs — internal readonly record struct (Label, Lines, FirstValueIndex, Warmup)
    Indicator.cs — abstract base: Color, Label, LineWidth, LineStyle, Offset
    IndicatorLevels.cs — public sealed: the two guide lines an oscillator draws beside its signal
    Indicator<TResult>.cs — generic: typed Compute() with IIndicatorResult constraint
    PaneRange.cs — internal readonly record struct (Min, Max, a side null to leave it to the data; Percent, Unit, Signed)
    CandleIndicator<TResult>.cs — OHLCV base: Open, High, Low, Close, Volume, BarCount, ComputeTrueRange(), ComputeTypicalPrice()
    PriceIndicator<TResult>.cs — single-price base: Prices, PriceSource constructor overload
    IIndicatorResult.cs — marker interface for all result types
    SignalResult.cs — single-line result (SMA, EMA, RSI, ATR, etc.) with implicit double[] conversion
    BandsResult.cs — band result (Bollinger Bands, Keltner Channels): Middle, Upper, Lower
    MacdResult.cs — MACD result: MacdLine, SignalLine, Histogram
    AdxResult.cs — sealed record: Adx[], PlusDi[], MinusDi[] — returned by Adx.ComputeFull()
    StochasticResult.cs — Stochastic result: K, D
    IchimokuResult.cs — Ichimoku result: TenkanSen, KijunSen, SenkouSpanA, SenkouSpanB, ChikouSpan
    Sma.cs, Ema.cs, Roc.cs — PriceIndicator: moving averages / rate-of-change (overlay)
    BollingerBands.cs — PriceIndicator: upper/lower bands via PlotBand() (overlay)
    Vwap.cs — PriceIndicator: volume-weighted average price (overlay), from the first bar or from an anchor bar
    VolumeProfile.cs, VolumeProfileResult.cs — CandleIndicator: volume by price — the closes' range cut into equal bins
    FibonacciRetracement.cs — Horizontal levels at key ratios (overlay): the inner ratios and the prices of the retracement tool
    Rsi.cs — PriceIndicator: Relative Strength Index 0-100 (panel)
    Macd.cs — PriceIndicator: MACD + signal (the exponential mean of the line, an EmaStep: pandas-ta's and TA-Lib's) + histogram
    Stochastic.cs — CandleIndicator: %K + %D oscillator as pandas-ta and TA-Lib have it
    VolumeIndicator.cs — Volume bars (panel)
    Atr.cs — CandleIndicator: Average True Range, folded over AtrStep (panel)
    Adx.cs — CandleIndicator: Average Directional Index (+DI, -DI and the ADX, all Wilder's smoothing), folded over AdxStep (panel)
    KeltnerChannels.cs — CandleIndicator: EMA + ATR bands via PlotBand() (overlay)
    Ichimoku.cs — CandleIndicator: Ichimoku Cloud via ComputeDonchianMid() (overlay)
    BuySellSignal.cs — Buy/sell triangle markers
    EquityCurve.cs — Cumulative P&L line (panel)
    ProfitLoss.cs — Per-trade colored bars (panel)
    DrawDown.cs — % drawdown from peak (panel)
    volatility, momentum, cycle, microstructure, entropy, change-point, dispersion: — GarmanKlass.cs, YangZhang.cs
    volume / money-flow, trend / transform, advanced + cross-asset: — KlingerVolumeOscillator.cs + KlingerResult — CandleIndicator
    Steps/ — internal step structs: the recursion of an indicator
    Streaming/ — 57 of the 59 indicators have a streaming form
```

### Interaction

```
  Interaction/ — the managed interaction layer — in the CORE package
    IInteractionController.cs — what a host talks to: five Handle* methods, three read-only state properties, UpdateLayout
    InteractionContext.cs — what a controller is made from: figure, chart id, layout slot, toolbar, and the one sink that hands an
                            event on, then raises the clicked point and tells the handler, whichever controller sent it
    InteractionControllerFactory.cs — public delegate: context → IInteractionController; a control's ControllerFactory
    InteractionController.cs — the built-in one: the first modifier that claims a gesture takes it, every other is told of each move
                               (OnPointerTracked), what is shown is the first modifier's that shows it; DefaultModifiers = the fifteen
    InteractionHost.cs — where a native control's wiring lives: it is handed the figure and the layout of every paint (Painted),
                         makes the context and asks its ControllerFactory for the controller
    IServerInteractive.cs, ServerRouteExtensions.cs (internal) — a control says whether it is interactive and holds its host
    AnimationSourceFollower.cs (internal) — what a native control does with its IAnimationSource: let go of the one before
    StreamingFigureFollower.cs (internal) — what a native streaming control does with its StreamingFigure: listen
    InputArgs/ — the three neutral records
    LayoutSlot.cs — the holder the modifiers read the current layout through: a repaint replaces it, so they are built once
    IChartEventHandler.cs, ChartEventContext.cs — one handler that receives every FigureInteractionEvent once
    HubMethodNames.cs — public: the name of every method of the chart hub (the events, the messages to a viewer, the subscriptions)
    ZoomNotch.cs — internal: one notch of the wheel, matplotlib's 0.85 in and 1/0.85 out, for the modifier, a pinch and the scripts
    RowSelectionEvent.cs — the keys of the points a brush covers
    TouchGestureTranslator.cs — what a touch surface reports
    Modifiers/IInteractionModifier.cs — one gesture, one class: Handles*(args) claims, On*(args) acts
    Modifiers/InteractionModifier.cs — the base of all of them: it holds the chart id, the layout and the event sink, and the roles
                                       a controller asks every modifier for (OnPointerTracked, ActiveBrushSelect, ActiveCrosshair,
                                       ActiveTooltip); a caller's own modifier derives from it
    ChartLayout.cs / IChartLayout.cs — what a modifier measures against: plot rectangles, HitTestAxes(x, y), PixelToData
    ChartLayoutViewExtensions.cs — ViewOf(layout, axes): that transform for every modifier
    NearestPointFinder.cs — which data point is under the cursor in one axes, with the pixel distance it was found at
    IHoverTarget.cs — internal: Nearest(HoverSearch, seriesIndex, best), one answer per series family: XYSeries
    FigureInteractionEvent.cs — the abstract event with ApplyTo(figure)
    FigureNotificationEvent.cs — the events that ask a question rather than change anything: ApplyTo is a sealed no-op
    SignalREventSink.cs — the sink that publishes events to a hub instead of applying them
    InteractionToolbar.cs — which drawing tool is active; ToolbarButton
    ViewHistoryManager.cs — back/forward over axis limits
    PanEvent.cs / AxisRangeEvent.cs — apply to the target axes AND every axes linked to it by ShareX
    PinnedAnnotation.cs — a clicked point: series label, data x/y, pixel x/y, axes index
```

**The outward numbering.** THE WIRE IS OUTWARD-ONLY, and stays so. `SecondaryAxisRange(Side, SlotIndex, Range)`,
`GetSecondaryDataRanges` and the `data-sec-right-N` / `data-sec-left-N` attributes number only the slots PAST the
primary: (Left, 0) is the first left axis past the primary Y axis, (Right, 0) is the first right axis, and the primary is
never addressable through it. The container's own index counts the primary as left slot 0, so the left side shifts by
one between the two; `YAxisStack.OutwardOffset` (left 1, right 0) and `SlotAtOutward(i)` are the ONLY place that converts,
read by `ApplySecondaryRanges`, by the renderer's views and by the SVG writer (`SvgMarkupWriter`'s data-sec attributes);
the 1.23.0 JSON fields use the same outward numbering. The numbering is a contract with every browser tab that holds an
older drawing, which sends these indices back after an upgrade; the names keep "secondary" for the same reason (they mean
"an axis past the primary"). The server-interaction script reads `data-sec-*-count` and the loop index as the slot, and
needs no knowledge of the container.

### Data, numerics

```
  Data/
    OhlcBar.cs, OhlcvBar.cs — a bar of four prices, and the same with the volume (NaN when the bar came without one)
    InputWindow.cs — internal struct InputWindow<T>: the one ring a window of inputs is kept in, no lock
    RingBuffer.cs — public sealed RingBuffer<T> — fixed-capacity circular SEQUENCE, oldest first
    RingBufferExtensions.cs — Min()/Max() over INumber<T> folded under one read lock without allocating
  Numerics/
    MarchingSquares.cs — internal static: Extract (iso-lines) and ExtractBands (filled bands) over ONE case index (CaseOf)
    ContourLevels.cs — which levels a contour draws, ONE rule for a line, a filled and a triangle contour (Under)
    SquarifiedLayout.cs — internal static: Of(node, bounds, pad, fromBottom) → SquarifiedCell(Node, Bounds)[]
    HaarDwt.cs + DwtResult — internal: Decompose
    HexLattice.cs — internal: HexExtent
    PointDensity.cs — how crowded each point's neighbourhood is, counted on a grid rather than estimated with a kernel
    LeastSquares.cs — public static: PolyFit (normal equations, solved by Linalg.Solve), PolyEval (Horner), ConfidenceBand
    Linalg.cs — public static: Solve, Inv and Det through one LU with partial pivoting (the largest entry of the column, found once)
    ArgumentRangeExtensions.cs — internal: the one owner of what a number given to the library must be, one sentence each
    DataCountExtensions.cs — internal extension(int held): ThrowIfFewerThan(least, name, need) and ThrowIfMoreThan(most, name, need)
    MomentsExtensions.cs — internal extension(ReadOnlySpan<double>): Mean, Variance(ddof) and StdDev(ddof)
    DistributionDomainExtensions.cs — internal extension(double): IsUndefinedShape
    ConfidenceBand.cs — sealed record(Upper[], Lower[]) — returned by LeastSquares.ConfidenceBand()
    StandardNormal.cs — internal static: Quantile(p) (Wichura AS 241, ~1e-16); saturates to ±∞ outside (0, 1), NaN → NaN
    StudentT.cs — internal static: Cdf (through the incomplete beta) and Quantile
    Gamma.cs, Beta.cs — internal static: LogGamma (Lanczos), Digamma
    LentzExtensions.cs — internal static: Tiny (1e-300) and double.AwayFromZero()
    ChiSquare.cs — internal static: Survival(x, degrees) — the p-value of a chi-square statistic
    Exponential.cs — internal static: Quantile(p) with an accurate ln(1 + x) (the naive sum keeps seven digits of 1e-9)
    KraskovEstimator.cs — internal static: the Kraskov-Stoegbauer-Grassberger conditional mutual information
    LogRank.cs — public readonly record struct LogRankResult(Statistic, DegreesOfFreedom, PValue)
    NelsonRules.cs — internal static: Flagged(rules, values, limits) — Nelson's eight tests as a table of (flag, predicate)
    ControlChartConstants.cs — internal static: A2, D3, D4 for subgroups of 2 to 10 to five decimals (exact integrals)
    PriceStepExtensions.cs — internal: FloorStep / CeilingStep / AtLeast / LevelFrom
    WholeStepExtensions.cs — internal: a value in whole steps, extension(double): DivMod (numpy's divmod, a WholeStepDivision)
    DecadeExtensions.cs — internal: extension(double): DecadeAtOrBelow / DecadeAtOrAbove / NearestDecade
    PointAndFigureExtensions.cs — internal: PointAndFigureColumns(boxSize, reversal) → PointAndFigureColumn[]
    RenkoExtensions.cs — internal: RenkoBricks(brickSize) → RenkoBrick(Rising, Bottom, Top, SourceIndex)[]
    KagiExtensions.cs — internal: KagiLines(reversal) → KagiLines(Segments, Connectors): the pieces of one width and the connectors
    StemAndLeafExtensions.cs — internal: StemAndLeaf(leafUnit?) → StemAndLeafDisplay(Exponent, Rows; LeafUnit, Key, LeafColumns, Cells() — the table of text the verb draws)
    ProbabilityFit.cs — internal: QQReference.FitTo(ordered) → ProbabilityFit(Positions, Theoretical, StartY, EndY)
    LargestRemainderExtensions.cs — internal: Apportion(seats) — Hamilton's method
    SortOrderExtensions.cs — internal: Percentile(p) of values in ascending order (a box, a violin and Vec.Percentile read it)
    StackedBaselineExtensions.cs — internal: ComputeFor(baseline, ySets, n) — the first line of a stack under Zero, Symmetric
    Vec.cs — readonly record struct: Data[], Length, Min/Max/Mean/Std/Sum/Percentile(p)/Quantile(q), element-wise ops
    RollingWindowExtensions.cs — internal extension(ReadOnlySpan<double>): RollingMean
    Fft.cs — public static: Forward (SpectralWindow.Hann, zero-pad to power-of-2, Transform), Stft
    SpectralWindow.cs — public readonly record struct (QQReference shape, no enum switch): Hann (= default), Hamming, Blackman
    SpectralDetrend.cs — public readonly record struct (QQReference shape): None (= default, matplotlib's detrend_none), Mean
    SpectralSidesExtensions.cs — extension(SpectralSides): the ONE rule for which places a spectrum has and in what order
    Welch.cs — internal static: Psd(x, options) → SpectralDensity(Frequencies, Power)
    Spectrum.cs — internal static: Of(signal, SpectrumOptions) → SignalSpectrum(Frequencies, Complex[] Bins, WindowSum)
    Correlation.cs — internal static: Auto(signal, options) and Cross(x, y, options) → Correlogram(Lags, Values), matplotlib's xcorr
    Delaunay.cs — public static: Triangulate(x, y) → TriMesh — Bowyer-Watson algorithm, CCW super-triangle
    TriangleListExtensions.cs — internal: what a flat list of triangles must be
    DecibelExtensions.cs — internal: ToDecibels(decibelsPerDecade) — 10 for a power, 20 for a magnitude
    MatrixExtensions.cs — internal on double[,]: CellsAbove, Permute, Transposed, RowDistances (Euclidean, symmetric, zero diagonal)
    HierarchicalClustering.cs — public static: Cluster(double[,] distanceMatrix) → Dendrogram — Ward's method (Lance-Williams)
    MinMaxRange.cs — public readonly record struct MinMaxRange(Min, Max) + MinMaxRangeExtensions.ScanColorBarRange(this double[,])
    MatShape.cs — public readonly record struct MatShape(Rows, Cols) — 2-D dimensions
    XYCurve.cs — public readonly record struct XYCurve(X, Y) — paired sample arrays (spline / KDE results)
    GaussianKde.cs — internal static: Bandwidth(sorted, factor)
    DensityRule.cs — internal readonly record struct (BandwidthFactor, Points)
    GraphLayoutAlgorithm.cs — internal: GraphLayoutAlgorithm.For(series), one sealed layout per GraphLayout
```

### Serialization, diagnostics

```
  Serialization/
    IChartSerializer.cs — interface: ToJson(Figure), FromJson(string)
    ISeriesSerializable.cs — interface: each series serializes itself
    ISeriesKind.cs, SeriesKindExtensions.cs — internal: the name a kind of series is written under
    ChartSerializer.cs — the options a figure is written and read with, ToJson and FromJson, and nothing else
    FigureDto.cs — the figure's own settings
    AxesDto.cs — one axes: Of(axes, theme) and ONE RestoreOnto(axes), for a subplot and an inset alike
    AxisDto.cs — one axis seat (the primary X, Y and Z, every Y slot, the top axis): Of(axis) with its ticks, locator, formatter
    SpinesConfigDto.cs, SpineConfigDto.cs — the four spines, and one spine with its position named in lower case
    GridSpecDto.cs, GridPositionDto.cs, InsetBoundsDto.cs — a figure's grid specification, a subplot's cells of it
    SecondaryAxisSlotDto.cs, YAxisSlotsPastSlot0Dto.cs — a Y axis slot past slot 0 (its axis and its series)
    XAxisSlotDto.cs — a linked X axis below the plot (an entry of bottomXAxes): its axis and its location, no series field
    SeriesSharedFieldsExtensions.cs — extension(ISeries): the fields every series shares
    AxesSerializationExtensions.cs — extension(Axes): a series is made from its JSON object in ONE place, CreateFromDtoOrReport
    SeriesDto.cs — the one flat record every series kind is written as (frozen: a field is added, never moved)
    DoubleArrayReadConverter.cs — internal: reads a JSON list of numbers into a double[] of exactly its length through one pooled buffer
    ThemeDto.cs — a theme as data: every property written where it differs from the plain Theme.Default and read back over it
    IThemeKeys.cs — internal interface IThemeKeys<TLook>: WriteKeys(ThemeDto) and static ReadKeys(ThemeDto)
    SubPlotSpacingDto.cs, InteractionThemeDto.cs — the figure's spacing (and a theme's default spacing) and the interaction tokens
    IJsonDescribed.cs — internal interface IJsonDescribed<TDescription>: Describe()
    TickLocatorDto.cs, TickFormatterDto.cs, NormalizerDto.cs — the described families: one base record per family
    NormalizableExtensions.cs — DescribeForJson (a built-in normaliser describes itself, anything else is left out)
    ColorMapDto.cs — a colour map as data, closed like the tick locators: linear
    ColorMapDescriptionExtensions.cs — Describe (a shipped map by its name; a map built in code as what it is made of; a map of the user's own type by a registered name only) and ColorMapOnTheWire
    RestingValueExtensions.cs — OrNullAtRest(resting): a value is written only while it differs from what a new object holds
    GivenStrokeDtoExtensions.cs — a series' given stroke in the JSON: StyleName
    HierarchicalSeriesExtensions.cs — DescribeSharedSettings / RestoreSharedSettings: the settings treemap
    LegendDto.cs, ColorBarDto.cs, GridDto.cs, TickConfigDto.cs, TextStyleDto.cs — the record parts of an axes
    FigureTextDto.cs, FigureImageDto.cs — the figure's own texts and images
    ZoomIndicatorDto.cs — an inset's zoom indicator (AxesDto.ZoomIndicator)
    SubplotCellDto.cs — the cell of a rows-by-columns grid a subplot was added to
    SignalMarkerDto.cs — a buy or sell signal (the "signals" list of AxesDto, null while empty): place, direction, and colour
    ReferenceLineDto.cs, SpanRegionDto.cs, TrendlineDto.cs, HorizontalLevelDto.cs — Pane3DConfigDto.cs, AxisBreakDto.cs
    PatchDto.cs — the patches of an axes (the "patches" list of AxesDto, null while empty)
    AxisTransformDto.cs — the link of an axis (the "link" field of AxisDto, null for an axis with a range of its own)
    SeriesRegistry.cs — NamedTable<string, Func<Axes, SeriesDto
    SeriesRead.cs — internal readonly record struct (Series, Why): what a reader made of a DTO, or why it made none
  Diagnostics/
    ChartDiagnostics.cs — process-wide diagnostic event bus — static event Emitted + Emit(ChartDiagnostic)
```

### Styling

```
  Styling/
    RcParamKeys.cs — static constants for all supported rcParams keys
    ThemeRcKey.cs, ThemeRcParts.cs — the rc parameters a theme carries, one list
    RcParams.cs — global config registry: typed Dictionary + AsyncLocal scoping
    StyleSheet.cs — named bundle of RcParams overrides + Theme bridge
    StyleContext.cs — IDisposable scoped override: push on construct, pop on Dispose
    StyleSheetRegistry.cs — thread-safe NamedTable, case-insensitive: name -> StyleSheet
    ValueObjects/Color.cs — readonly record struct (R, G, B, A) + named colors + hex
    Colors.cs — the named colours: the basic ones and the CSS4 aliases read Css4Colors
    ColorExtensions.cs — internal Color.Desaturated(share) (seaborn's utils.desaturate: HLS, the saturation times the share)
    ValueObjects/CyclePair.cs — internal readonly record struct (First, Second): the first two colours of the standard cycle
    ValueObjects/UpDownColors.cs — internal readonly record struct (Up, Down): the colours of a rise and a fall
    UnitIntervalExtensions.cs — internal: double.Saturate(), a value held between 0 and 1
    BoxLook.cs — internal sealed record: what a theme draws a box plot with, every theme (ThemeLooks.Box): the Outline
    BoxStroke.cs, BoxCaps.cs, BoxMarker.cs, BoxStanding.cs — the parts of a BoxLook, one readonly record struct each
    Font.cs — sealed record (Family, Size, Weight, Slant, Color)
    Theme.cs — 30 built-in themes (incl. OpsNight/OpsPanel/OpsWarm/OpsContrast operator backgrounds)
    LineWidthRule.cs — internal sealed record (Resting, Cycled, BeatsTheme, Scale), each made once
    GuideLine.cs — internal readonly record struct (Color, Opacity, Style, Width): a line a series draws as a guide to its marks
    GroundInk.cs — internal readonly record struct (Contrast, On): how a series text on a ground it paints is inked
    DatasetStanding.cs — internal readonly record struct
    Looks/ — internal sealed records, one per concern
      ThemeLooks.cs — the composition: Ticks, TickLabels, Range, Frame, Cube, Text, Figure, Patch, ColorBar, ColorMap, Raster, Strokes
      AxisTickLook.cs — the tick ladder, the 2-D ticks' direction, length, width, pad and minor width and pad, ticks on all sides
      TickLabelLook.cs — log label thresholds, formatter limits (PowerLimits, in Styling/), offset threshold, the true minus sign
      AxisRangeLook.cs — autolimit mode (AutolimitRule), the symlog threshold of an axis given none
      AxesFrameLook.cs — the spine width, the axis z-order, no background without axes, whether every artist is clipped to the axes
      CubeLook.cs — the 3-D wall grid, the surface stride, the box's axis line width
      TextLook.cs — title and suptitle scale, title weight, title and label pads, series text at the font size
      FigureLook.cs — a figure legend that lists every entry, no pad above a fixed layout's titles
      PatchLook.cs — draws like a collection, forced edge, edge colour and width, alpha, the hatch colour rule
      ColorBarLook.cs — takes room, a step per colour, extend triangles, tick marks
      ColorMapLook.cs — the default colour map, matplotlib's LogNorm and CountsFromTheLeastDrawn
    PointLengthExtensions.cs — internal: a length in points in pixels (PointsInPixels, PixelsPerPoint = 100 / 72) and back
    ValueObjects/AlarmPalette.cs — readonly record struct: the colours a theme reserves for abnormal states
    GivenStroke.cs — internal readonly record struct (Width, Style): the width and dash a caller gave a series' line
    GridChange.cs — internal readonly record struct: what an author changed of the grid as seven nullable fields
    PatchEdge.cs — internal readonly record struct: the colour (null for none) and width of a patch's edge
    IDotSeries.cs — internal interface of the five kinds of dots (strip, swarm, point plot, residual plot, pair grid): Given
    IWidthGiven.cs — internal interface: a kind whose line keeps whether its author gave it a width: GivenLineWidth and WidthRule
    IGivenStroked.cs — internal interface, an IWidthGiven: a kind that keeps the width and dash it was given as one GivenStroke
    IGivenTextSize.cs — internal interface: a kind that writes text at a size its author may give
    IGivenAlpha.cs — internal interface: a series that keeps whether its author gave it an opacity
    IHatched.cs, HatchFill.cs — whatever names a hatch
    IEdgedPatch.cs — internal interface: what a kind of patch says about its edge, GivenEdge
    GivenPatchEdge.cs — internal readonly record struct (Color? Color, double? Width, double? Alpha)
    ResolvedPatchEdge.cs — internal readonly record struct: what a theme decides about one series' edge, worked out once
    DashPatterns.cs — GetPattern (public: the library look's ratios) and Lengths(style, thickness, DashLook)
    ScaledDashPattern.cs — internal readonly ref struct over a look's ratios and a scale (Count, this[i], ToSingles)
    PropCycler.cs — LCM-based multi-property cycler
    PropCyclerBuilder.cs — fluent builder: WithColors(), WithLineStyles(), WithMarkerStyles(), WithLineWidths()
    ColorMaps/
      IColorMap.cs — interface: GetColor(double normalized) -> Color
      LinearColorMap.cs — public sealed: linear interpolation between Color[] hex stops
      ReversedColorMap.cs — public decorator: inverts normalized input, appends _r suffix
      ColorMapRegistry.cs — thread-safe NamedTable, case-insensitive, auto-registers _r variants
      ColorMapShelf.cs — internal: the maps one class declares, in order
      ColorMapping.cs — internal: the colour scale every colour-mapped kind is drawn with (ColorMapping, ColormapContext)
      LogNorm.cs — internal: matplotlib's LogNorm
      INormalizer.cs — interface + 8 implementations: LinearNormalizer (singleton), LogNormalizer, TwoSlopeNormalizer(center)
      RangeFractionExtensions.cs — internal extension(IRangeFraction): Normalized(value, min, max)
      ColorMaps.cs — 9 perceptually-uniform + rainbow: Viridis, Plasma, Inferno, Magma, Coolwarm, Blues, Reds, Turbo (prefer over Jet)
      MatplotlibColorTables.cs — internal, partial: the one owner of matplotlib's colour tables
      MatplotlibColorTables.Data.cs — the data, written by tools/mpl_reference/generate_colormaps.py from matplotlib 3.11.2
      SequentialColorMaps.cs — 21 sequential: Cividis, Greens, Oranges, Purples, Greys, YlOrBr, YlOrRd, OrRd, PuBu, YlGn, BuGn, Hot
      DivergingColorMaps.cs — 9 diverging: RdBu, RdYlGn, RdYlBu, BrBG, PiYG, Spectral, PuOr, Seismic, Bwr
      CyclicColorMaps.cs — 3 cyclic (start≈end): Twilight, TwilightShifted, Hsv
      QualitativeColorMaps.cs — 14 qualitative (the ten colours of Tab10 are written here once; Tab10Cycle() hands each reader of the cycle, the themes and HueGrouper, an array of its own)
      AdditionalColorMaps.cs — 13 more: Gray, Spring, Summer, Autumn, Winter, Cool, AfmHot, PRGn, RdGy, Rainbow, Ocean, Terrain
      PerceptualColorMaps2.cs — 5 Seaborn perceptually-uniform: Rocket, Mako, Crest, Flare, Icefire Total: 74 base colormaps × 2
      ColormapExtensions.cs — INormalizable.NormalizerOrLinear() + int.ColormapFraction(count, singletonT)
```

## One owner per rule

A rule that two places would otherwise each work out is worked out in one class, and every reader asks that class, so the
readers cannot disagree. The rows run from drawing and layout, through axes, series and the legend, to numbers, JSON and
interaction. `OneOwnerSourceContractTests` holds a rule to its owners in source, one row per rule in the area files of
`Tst/MatPlotLibNet/Packaging/OwnedRules/`, each row with the lines it must find and the lines it must pass; `ResponsibilityBudgetContractTests` holds every
type to 20 methods of its own, a type over it named with the reason it holds them, so a class does not take on a second concern.

| Rule | Owner | Read by |
|---|---|---|
| a span of a length centred on a point, and one centred in a room (matplotlib's `Bbox.anchored(c='C')`) | `Range1D.Around`, `Range1D.CentredIn` | `CategorySlot.Around`, the bullet graph's measure, a contour label's chord |
| whether a draw is laid out by a layout that keeps room for what is drawn (tight, constrained) or keeps the margins it is given | `FigureRanges.KeepsRoom` | `ChartRenderer.PrepareSpacing` (the layout path), `ChartRenderer.FrameOf` |
| the drawing sequence of every target, with the frame (spacing, title) laid out once | `ChartRenderer.Draw`, `ChartRenderer.FrameOf` → `FigureFrame` | `ChartRenderer.Render` (a `DirectFrameTarget`), `SvgFrameTarget`, `SkiaFrameTarget` |
| the plot areas of the subplots, the cell of each axes and the grid edges it touches | `FigureGrid` (`PlotRects`, `CellOf`, `EdgeOf`, `CellHeight`; one per draw) | the drawing, the layout (every loop of `ConstrainedLayoutEngine`), the hit test |
| the gap between neighbouring subplots under the matplotlib themes: matplotlib's `figure.subplot.wspace` and `hspace` | `SubPlotSpacing.HorizontalGapFraction` / `VerticalGapFraction` → `SubPlotSpacing.Resolve(width, height, rows, columns)` | `FigureGrid.PlotRects`, `ConstrainedLayoutEngine.ReserveEdgeBands` |
| the pad a tight or constrained layout keeps past what an axes needs on each edge and in a gap between two subplots | `LayoutPads` (`Of`, `Stacked`), held by `ConstrainedLayoutEngine` for the layout it works out | the engine's margins and gaps, `AxesLeftEdgeExtensions.LeftNeed` |
| the body of a colour bar: the gradient steps, the wedges of its extend | `ColorBarStrip` (`Of`) | `HorizontalColorBarRenderer`, `VerticalColorBarRenderer` |
| the tick marks of a colour bar and how far its tick labels stand from it: matplotlib's colour-bar axis under the matplotlib themes | `ColorBarMarks` (`Under`, `Draw`, `LabelGap`; `ColorBarLook.TickMarks`) | `VerticalColorBarRenderer`, `HorizontalColorBarRenderer`, `ColorBarRoomExtensions` |
| the data range of a subplot in one draw: worked out once, then passed down | `FigureRanges` (made by `ChartRenderer.FrameOf`, carried by `FigureFrame`) | `AxesEdgeNeeds` (`EdgeBandsOf`, `SlotBand`), `ChartRenderer.RenderAxes` |
| the marks of the series in one render: collected at most once | `CartesianSeriesMarks` (`OfSeries`, `WithTextBoxes`), asked by `CartesianAxesRenderer.Render` | `AxesRenderer.PlaceLegend`, `CartesianAnnotationsPart` |
| the fold of an axis past the primary (a Y slot and the top X axis): its set limits, its own series along it | `CartesianAxisPlacement.DrawnOnAxisPastPrimary` | `ComputeSecondaryDataRanges`, `ComputeSecondaryXDataRanges` |
| how a renderer draws onto a target, and how it paints one with its layout, whichever renderer it is | `ChartRendererFrameExtensions.Draw`, `.Paint` on any `IChartRenderer` | `SvgTransform` (any renderer it was made with), `FrameTargetPaintExtensions.Paint` |
| whether an SVG writer draws a figure as a document in its parts | `SvgRendererDocumentExtensions.RenderDocument` on any `ISvgRenderer` | `ChartPublisher` |
| whether a kept draw of an axes is placed again or drawn into a new layer | `KeepingFrameTarget<TLayer>`, `KeptAxesDraw<TLayer>.Fits` | `SvgFrameTarget`, `SkiaFrameTarget` |
| the paint of a native control, and the band of a brush selection | `FrameTargetPaintExtensions.Paint`, `DrawBrushSelect` | `SkiaChartPainter`, the MAUI control |
| what a figure draws itself, under its subplots (its images) and over them (its colour bar, texts and legend) | `ChartRenderer.RenderFigureUnderlays`, `ChartRenderer.RenderFigureOverlays` | `ChartRenderer.Draw` (so every target), also for a figure with no subplot |
| what one draw of a subplot emits for the browser | `SvgEmit.Of` (a figure's subplot, and an axes in a context: reads `IMarkupRenderContext.Emit`) | `AxesRenderer`, `SeriesGroupExtensions`, `ThreeDAxesRenderer` |
| the markup a surface adds around what it draws | `RenderContextMarkupExtensions` on any `IRenderContext` | `AxesRenderer`, `LegendRenderer`, `SeriesRenderer`, `WordCloudSeriesRenderer` |
| the envelope of every SVG element | `SvgElementBuffer` (`WriteMark`, `WriteOutsideClips`, `OutsideClips`) | `SvgMarkWriter`, `SvgTextWriter`, `SvgMarkupWriter` |
| how one SVG shape element is written (two layers for a gradient under a hatch, the paint server before the tag) | `SvgMarkWriter.Shape<TGeometry>` over an `ISvgGeometry` | `DrawPolygon`, `DrawCircle`, `DrawRectangle`, `DrawEllipse`, `DrawPath` |
| the colour of a hatch's strokes (the shape's own, else the contrast against its fill) | `ShapeStyleLayers` (`HatchStroke`) | the SVG hatch registry, the Skia hatch paint |
| how a shape is painted on a canvas (fill, hatch over it, stroke, each when it shows) | `CanvasShapeExtensions.PaintIn` over an internal `ICanvasShape` (the layers a surface paints) | the five shape draws of `SkiaRenderContext` and of `MauiGraphicsRenderContext` |
| the view attributes the browser scripts read (plot area, data range, scale name and linear threshold, with every digit) | `SvgWriterExtensions` (`AppendView`, `AppendPlotArea`, `AppendSecondaryViews`) | a series group's mapping, `WriteAxesView` |
| how a `<text>` element opens (position, family, size, anchor and direction, rotation, italic, bold, fill), an accessible name | `SvgTextWriter` (its text element), `SvgMarkupWriter` | plain and rich text |
| the ids an SVG render context writes | `SvgIdScope` | every subplot's `SvgRenderContext`, then `SaveOptions.SvgIdPrefix` |
| a colour's alpha in the SVG, and a stroke drawn from a colour | `SvgWriterExtensions` (`AppendAlpha`, `AppendStroke`) | every SVG writer |
| which renderer a series has | `SvgSeriesRenderer` (`Dispatch`) | drawing and collect mode (`AddMarks`) |
| what a save setting means for a format | `SaveOptionsExtensions` (`DpiFor`, `TitleOrNull`, `AuthorOrNull`) | every export |
| the line end of every text the library writes (a line feed on every operating system), and what a reader breaks a text's lines at | `LineEndExtensions` (`LineEnd`, `AppendLineLf`, `LineBreaks`) | `SvgRenderContext`, `SvgGradientRegistry`, `SvgHatchRegistry`, `SvgTransform` |
| which exporter a file extension gets | the transform registry of `FigureExtensions`, the extension strings in `ExportExtension` | `Save`; MatPlotLibNet.Skia replaces the refusal for .png and .pdf |
| how an image is scaled to its plot where its author names no interpolation | `ImageLook.Interpolation` → `ImageLook.InterpolationOver(given)`, `ImageFootprint` | `ImageSeriesRenderer` |
| whether a smoothed image is interpolated in its values or in its colours | `ImageLook.ColoursStageWhenSmall` → `ImageLook.InterpolatesColours(footprint, engine)` | `ImageSeriesRenderer` |
| the aspect a plot is drawn at: the one given (`SetAspect`; `SetAspectAuto` gives 0, which fills the cell) | `AxesRangeExtensions.AspectUnder` over `AxesRangeExtensions.AskUnder` (`IAsksOfAxes`, `AxesAsk`) | `PlotAspectExtensions.FittedTo` (every layout), `SvgTransform` (`data-aspect-lock`) |
| how a name registry stores, replaces and lists its entries (thread safety, case rule, a null name refused) | `NamedTable<TKey, TValue>`; each registry says only which comparer it wants | `InterpolationRegistry`, `ColorMapRegistry`, `StyleSheetRegistry`, `SeriesRegistry` |
| how big a raster export's bitmap is and how the figure is scaled onto it | `RasterRenderingExtensions` (`RasterSize`) | a PNG, a frame of a GIF |
| the accessible name of a figure (alt text -> title -> tile labels) | `FigureExtensions.AccessibleName` | the SVG `<title>`, the data-table caption |
| what NaN and infinity mean to a chart: a line breaks at the gap | `MissingValueExtensions` | every renderer, every data range, a colormap |
| a line reshaped run by run: split at its gaps, each run downsampled or smoothed on its own, joined again by one missing point | `MissingValueExtensions.JoinedAcrossGaps` | `LttbDownsampler`, `MonotoneCubicSpline` |
| the regions a fill with a `Where` predicate fills: asked of the data points (matplotlib's `cbook.contiguous_regions(where)`) | `AreaSeriesRenderer` | `AreaSeriesRenderer.Render`, the legend's marks (`AddMarks`) |
| which scale clips a value it cannot place and which masks it, where a clipped value stands, and what is drawn of a mark through it | `PixelDirection.ClipsBase` / `Beyond` / `PlaceUnclipped` | every Cartesian series renderer that draws a line, a fill or a two-point mark |
| the marks a chart drew, the score of a candidate place against them, and the first least covered place | `MarkObstacles` (`Builder`, `Badness`, `FirstLeastCovered`) | the legend at `Best` (`BestStrategy`), `CartesianAnnotationsPart.PlaceText` |
| how long, in which direction and of which shape a quiver's arrow is, and what its axes range over | `QuiverSeries.AutoScaledUnder(theme)` (`AutoScale`, `Scale`, `VectorFieldLook.QuiverAutoScale`) | `QuiverSeriesRenderer`, `QuiverKeySeriesRenderer` |
| how a wind barb is drawn: under the matplotlib themes matplotlib's `Barbs` glyph | `VectorFieldLook.BarbsLikeMatplotlib`, `BarbsSeries.BarbLengthIn` | `BarbsSeriesRenderer` |
| where the streamlines of a streamplot run and where their arrows stand: matplotlib's `streamplot` layout under every theme | `StreamplotSeriesRenderer.StreamlinesOf`, `VectorFieldLook.StreamArrowsLikeMatplotlib` | `StreamplotSeriesRenderer` |
| where a quiver key's arrow and label stand for each label position: the arrow's pivot (middle, head or tail) | `QuiverKeySeriesRenderer` (`LabelAt`) over `QuiverKeyLabelPosition` | `QuiverKeySeries` drawn by every output |
| how long a quiver key's arrow is | `QuiverKeySeriesRenderer` (`QuiverBefore`), `SeriesPass.Series` | `QuiverKeySeries` drawn by every output |
| the rectangle a text occupies, and where a text centred on, hung from or standing on a point has its baseline | `TextBox` over the text's `TextExtent`, the vertical alignment in `TextPlacement.Vertical` | every tick label, the offset text, colour bar labels, line labels, annotations |
| how much room a text takes (its advance, its line's height, how far it rises above the baseline): the line rule | `TextExtent` (`OfLine`, `Of`, `OfInk`) | every site that measures a text: tick labels and their reach |
| which face each character of math is set in, and how that face codes it | `TextLook.MathFonts` (a `MathFontSet`, the value) with `MathFontSet.RunsOf` | the one layout (`TextExtentExtensions.LayOut`) |
| where each part of math stands and how big it is | one leaf of `MathBox` per construction, laid out by TeX's rules as matplotlib 3.11.2 runs them | the one layout (`MathInk`) |
| which face a font family names on the Skia surfaces: a registered or bundled family first, then one the machine has, `sans-serif` | `SkiaFonts.Resolve` | `SkiaRenderContext` (PNG, PDF, GIF, the native controls), `SkiaFontMetrics` |
| the minus sign of a negative number a `ScalarFormatter` writes (a tick label, the offset text, a colour bar's label) | `TickLabelLook.Signed` (one rule, on a string, over `UnicodeMinus`) and `TickLabelLook.Labelled` | `TickDrawContext.LabelText` / `AxisTickExtensions.RenderTickOffset` |
| the offset a linear axis' plain-number labels are written less, the power of ten they are divided by, and where both are written | `TickLabelExtensions.OffsetIn` / `OrderOfMagnitudeIn` / `OffsetText` / `ScaleText` | the primary X axis, every Y axis part, the top axis |
| how the ticks of a 2-D axis are drawn under a theme: what the author gave, else the theme's direction, length, width and pad | `TickConfig.DrawnUnder` | every 2-D axis' tick drawing and measuring |
| how a fill between two curves that names no alpha, line width or edge is drawn and ranged | `AreaSeries.IsFillBetweenUnder` over `PatchLook.LikeCollections`, with `IGivenAlpha.OpacityUnder` | `AreaSeriesRenderer`, `StackedAreaSeriesRenderer`, the range pipeline |
| the colour map a series, a colour bar or a figure image that names none is drawn with | `ColorMapLook.Of` over `ColorMapLook.Default` | the colour-scale owners: `ColorScaledExtensions.ColorMapUnder` |
| the colour scale of a kind that maps its values to colours: its map, the normaliser the theme reads its own as | `ColorScaledExtensions` | every colour-mapped renderer, `ImageCells`, the colour bar's range |
| matplotlib's table of a named colour map (the colours of `plasma`, `Blues`, `jet`, ... where the library ships the name) | `MatplotlibColorTables.Of(name)` over `MatplotlibColorTables.Data.cs` | `ColorMaps`, `SequentialColorMaps`, `DivergingColorMaps`, `CyclicColorMaps` |
| the size and weight of an axes title and of a figure title: the theme's `TitleScale`, `SupTitleScale` and `TitleWeight` | `Theme.TitleFont()`, `Theme.SupTitleFont()` (ThemeFontExtensions) | every axes renderer, the figure renderer, the layout engine's measures |
| the colour a hatch is drawn in where its author names none | `PatchLook.HatchOver(given, HatchGround)` over `PatchLook.Hatch` (`HatchColorRule`) | the bar, histogram (bars and filled steps), area, stacked area, pie |
| how the markers of a line (and of a step) are outlined | `SeriesSlot.LineMarkerStyleOf` over `MarkerLook.LineEdge` (`LineMarkerLook`) and `MarkerShape.Join` | `LineSeriesRenderer`, `StepSeriesRenderer`, `StemSeriesRenderer` (a stem's head) |
| how far an error bar's caps reach | `SeriesLineLook.CapSizeOver` over `SeriesLineLook.ErrorBarCapSize`, `ErrorBarSeries.GivenCapSize` | `ErrorBarSeriesRenderer`, `LegendRenderer` (the error-bar handle) |
| the size of a series text that names none | `TextLook.SeriesTextAtFontSize` with `Theme.SeriesTextSizeOver(given, resting)` | the fourteen series' renderers, the JSON (a size is written when it was given) |
| the font of the data texts | `Theme.DataTextFont()` (ThemeFontExtensions) | the annotations, the radar's and the network graph's labels, the bar's value labels |
| how an outline in data units is drawn | `PathSegmentExtensions.ToPixels` over `IDataPlacement.PlaceUnclipped` and | `CartesianPatchesPart` (every kind of patch, straight or curved) |
| the rectangle between two data corners | `Rect.FromCorners` | every renderer that draws a bar, box, cell or segment from two corners |
| a straight line cut to the area it is drawn in | `LineSegmentExtensions` (`ClippedTo`, `HorizontalAcross`, `VerticalAcross`, `LineThrough`) | `CartesianReferenceLinesPart`, `CartesianToolsPart`, `ResidualSeriesRenderer` |
| a mark with a fill and no outline, an outline over no fill, and the line an outline is drawn with | `ShapeStyle.Filled`, `ShapeStyle.Outlined`, `ShapeStyle.OutlineStroke()` | every series renderer and axes renderer; the SVG, Skia and MAUI strokes of a shape |
| a solid line, and a grid or spine becoming a line | `StrokeStyle.Solid`, `GridStyle.Stroke` (the minor look: `StrokeStyle.Faded(0.5)`) | every renderer that draws a line, the grid, the spines of Cartesian |
| matplotlib's plain grid, and the grid nothing was said about | `GridStyle.Plain`, `GridStyle.AtRest` | `Theme.Default`, `Theme.ColorBlindSafe`, `RcParams`, the matplotlib theme factory |
| the length of a step in the plane (a segment, a quiver's vector, a force) and its square, the distance between two pixels | `PointExtensions` (`StepTo`, `Length`, `SquaredLength`, `DistanceTo`, `DistanceSquaredTo`, `HeadingTo`; `OnRim`, `AtAngle`, `AtAngleCounterClockwise`, `AtBearing`, `BelowTurnedBy`) and `Heading` | arrow heads, the arrow patch, the connector curves, the 3-D cube's tick directions |
| the outline of a slice of a ring (a pie's slice, a donut's or sunburst's wedge, a dial's band, a polar heat map's cell) | `RingWedge` (`Path`, `OuterArc`, `Polygon`, `PolygonPath`, `StepsFor`, `CounterClockwise`, `FromBearings`) | pie, donut, sunburst, gauge, polar heat map |
| whether a pie or a donut has a whole to draw: the total of the slices that can be drawn, above nothing | `PartsOfAWholeExtensions.DrawableTotal` | `PieSeriesRenderer`, `DonutSeriesRenderer` |
| where the label of a cell of a hierarchy stands | `CellLabel.Standard` | `TreemapSeriesRenderer`, `IcicleSeriesRenderer` |
| the data attributes a treemap tile and its text carry for the drill-down script | `TreemapSeriesRenderer.TagTreemapNode` | the tile and its text, drawn by the same renderer |
| the text of a value a series labels: the format it was given, else the default of its kind, in the invariant culture | `ILabelFormatted.DefaultLabelFormat`, `LabelFormatExtensions.LabelOf` | `BarSeriesRenderer`, `ContourSeriesRenderer`, `HeatmapSeriesRenderer` |
| the shorter side of a rectangle | `RoundFrame` (`RoundFrame.In(bounds, share, inset, down)`; `DiscShare`, `WebShare`, `FullShare`, `DialCentreDown`) | pie, donut, gauge, radar, sunburst, chord, the polar axes and its transform |
| the i-th value of an optional array, else a resting value, and the name of an unnamed item (`column 3`) | `ArrayElementExtensions` | every series that takes a value for each part |
| where a point of an axes' data stands, for a patch's outline and an inset's zoom indicator | `IDataPlacement` (`TransformPlacement`, `PolarPlacement`; a 3-D axes stands a patch on its plane) | `PatchDrawing`, `ZoomIndicatorPath.Of` |
| the region an inset zooms into and the connectors kept, from the limits the inset draws | `ZoomIndicatorPath.Of` over the inset's frame and the transform its renderer draws with | the parent's render of an inset that has a `ZoomIndicator` (`IndicateZoom`) |
| the grid of cells that fills a rectangle with an image's values in its colour scale | `ImageCells` (`Draw`, over the image's `ColorScaleUnder`; `DrawScaled`, the interpolation named or the theme's, `'auto'` by the footprint, value or colour stage) | `ImageSeriesRenderer`, `SpectrogramSeriesRenderer` (under `SpectrogramLook.Specgram`) |
| what a light does to a face | `ILightSource.Shade` | `Surface`, `Cuboid` (a 3-D bar, a voxel), `Color.Shade` |
| the six faces of a box (a 3-D bar, a voxel), their corners, normals and depths | `Cuboid` (`AddFacesTo`, `ShadesOf`; `DirectionalLight.MatplotlibDefault`) | `Bar3DSeriesRenderer`, `VoxelSeriesRenderer` (the faces that border empty space) |
| the order the marks of a 3-D scene are drawn in | `DepthQueue3D` over `DepthMarks<TMark, TDrawer>` | the 12 3-D renderers and the 3-D axes' patches |
| where a 3-D camera stands at rest (elevation 30, azimuth -60, distance 10) and how it moves (a turn, a return to a kept view) | `Camera3D` (`RestingElevation`, `RestingAzimuth`, `RestingDistance`, `Turn`, `LookFrom`, `DrawnView`) | `Axes`, the camera verbs' defaults, `AxesDto`, `Rotate3DEvent`, `Rotate3DModifier` |
| the rows and columns a surface patch spans | `SurfaceFaces.Strides.Of` | `SurfaceSeriesRenderer` |
| the colour of a surface patch | `SurfaceFaces` (made once per data, kept across camera turns) | `SurfaceSeriesRenderer` |
| the lengths a broken line is dashed at: a pattern per style, whether it grows with the width and how long a unit is | `DashPatterns.Lengths(style, thickness, DashLook)` over `StrokeLook.Dashes` | the SVG writer (`AppendDashArray`), Skia |
| how two segments of a stroke meet (matplotlib: a line and a collection 'round', a patch 'miter'): a value of the theme | a LINE (`DrawLine`/`DrawLines`, whatever builds its stroke: 60 sites, the slot, contour, streamplot) names no join | the SVG context's one `AppendStroke` |
| the looks of the theme a surface draws under, so it knows how its strokes are dashed and joined | `IThemedRenderContext.UseLooks` | `SvgRenderContext`, `SkiaRenderContext`, `MauiGraphicsRenderContext` |
| a colour's opacity times a factor, an opacity in place of a colour's own | `ColorExtensions.ScaleAlpha`, `WithOpacityGiven` (every site; `WithOpacity` beneath it) | a series' alpha, the legend frame, a span, a voxel's faces, the minor grid |
| a value held between 0 and 1 | `UnitIntervalExtensions` (`double.Saturate()`) | an opacity, an intensity, a position on a colour map, a survival bound |
| a patch's opacity given in place of its colours' own | `Patch.WithTheOpacityGiven` | the face, the edge and the hatch of every patch |
| the colours a patch is drawn in, and how wide its edge is | `Patch.DrawnFaceColorUnder`, `Patch.DrawnStyleUnder(theme)` | the plot (`CartesianPatchesPart`), the legend swatch (`PatchLegendEntry`) |
| what an overlay adds to the range of an axes and whether it asks for its axes to be scaled | `AxesRangeExtensions.AddOverlayContributions` (values), `AutoscaleAskExtensions.HoldsWhatAsksAlong` | the 2-D range of the primary axes, the axes that share its scale |
| the curves of a curved patch, in data units | `EllipticArc` (`UnitCircle`, `UnitArc`, `Curves`, `Place`) | `Circle`, `Ellipse`, `Wedge`, `Arc` |
| a line that is `LineStyle.None` draws nothing, in the plot and in the legend | `SeriesRenderer.DrawPolyline`, `LegendRenderer.DrawSwatchLine` | every line-like series |
| a data value mapped into the space an axis is drawn in and back (log10, symlog, logit, identity) | `ScaleRule` (`Forward`, `ForwardAll`, `Inverse`, `HasPlaceFor`, `ShapesItsSpace`; one sealed leaf per scale, picked by `scale.Rule`; the symmetric-log formula in `SymlogTransform`, which the symlog normaliser and the browser script follow too) | `DataTransform`, the range the renderer draws, the server-interaction view, a pan |
| the pixel of a value in the drawn space: the value less where the range starts, times the pixels per unit | `PixelDirection.Spread` (a vector at a time: `PixelSpreadExtensions`) | `DataToPixel`, `TransformX` / `TransformY`, `TransformBatch` |
| a reversed axis: the range runs high to low, the ticks are the ticks of its interval, each drawn where its value is | `Range1D` (`Interval`, `OrientedLike`), `AxisAutoscale.Oriented` | every axis |
| what an axis does with the range its data reach: put on its scale | `AxisAutoscale` with `ScaleRule.Limited` | the primary axes, a Y slot, the top X axis, the 3-D cube, a polar radius |
| how what series report folds into the range an axis draws, and what an empty range | `Range1D` (`Merge`, `Normalized`: the library's own themes), `AxisAutoscale` | the primary axes, a Y slot past the primary, the top X axis, the 3-D axes |
| what a series that holds no data adds to its range and whether it still asks for its axes to be scaled | `ChartSeries` (`HoldsNoData`, `AsksWhenEmpty`, `NoDataUnderTheLibrary`: 0 to 1 under the library's own themes, nothing under the matplotlib themes) | every series with an empty-data arm |
| what a series that bins or grids its sample adds to its range, and the bins of a sample of one value | the bins it draws, not the sample: `HistogramSeries` | a histogram (and the pair grid's diagonal, the data frame's verb), a hexbin |
| how the ranges a series reaches along X and along Y become its contribution: spanned with no sticky edge | `DataRangeContribution` (`Spanning`, with Z in 3-D; `Over`; `Pinned`; `PinnedOver`), `ChartSeries` | the line and point series, hexbin, regression, a streaming series |
| which edges of the figure's grid an axes' cell touches, and whether it stands on the edge the figure's colour bar stands at | `Sides<bool>` (`FigureGrid.EdgeIn`), of the cell `FigureGrid.CellOf` draws the axes in | `AxisMargins`, the figure's bar clearance |
| where an axes' need goes in one direction of the grid: the margin of an edge its cell touches, else the gap beside it | `AxisMargins` (`Across`, `Down`; `Demand`, `NeedAtStart`, `NeedAtEnd`, `NeedAtStartEdge`, `BesideEnd`, `Widen`, `AtEnd`, `Stacked`) | `ConstrainedLayoutEngine` |
| how far a margin or a gap of a tight or a constrained layout may shrink and grow: a floor and a cap | `MarginBounds.Clamp`, `LayoutBounds.Library` on `LayoutPads.Bounds`, `AxisMargins.Bound` | `ComputeSides`, `ComputeEnds` |
| what each axes draws past its top and bottom spines in one pass of a layout: measured once, at the plots the sides settle on | `ConstrainedLayoutEngine.SettleSides` | `ComputeEnds` (tight), `ReserveEdgeBands` (default) |
| the shaping of a text with the Skia package loaded (its width, its outline and the glyphs a canvas paints) | `ShapedTextStore` (`Shared`, the one the package installs its pair over and every `SkiaRenderContext` reads) | `SkiaFontMetrics` (every layout pass and every draw), `SkiaGlyphPathProvider` |
| the visual order of items that carry bidi levels (rule L2) | `BidiAlgorithm.ReverseByLevels<T>` | `BidiAlgorithm.Resolve` (code units), `TextShaper.Shape` (the runs it shapes) |
| a code point of a UTF-16 text (two units for a well-formed surrogate pair, one otherwise, a lone surrogate its own value) | `CodePointExtensions` (`UnitsAt`, `CodePointAt`) | the bidi algorithm (`BidiText.Of`, its scans of a text), `TextShaper` |
| the direction of a bidi paragraph, and of what a first strong isolate holds (rules P2, P3) | `BidiAlgorithm.IsRightToLeftParagraph` | `BidiText.Of` (the paragraph level), `BidiExplicitLevels` (X5c), `SvgTextWriter` |
| the characters rule X9 keeps, and the isolate controls among the bidi classes | `BidiText.Kept`, `BidiAlgorithm.IsIsolateInitiator` and `IsIsolateControl` | the level runs (BD7), X10's sos and eos, L1, L2, the weak and neutral rules |
| the strong type before a position of an isolating run sequence, the end of a run of like types in it, and the direction of a level | `BidiRunSequence` (`PrecedingStrong`, `RunEnd`, `DirectionOf`) | W2, W7, N0 (`PrecedingStrong`), W5, N1–N2 (`RunEnd`), X10, N0, N1–N2 (`DirectionOf`) |
| the width that places a glyph outline for its alignment | `GlyphPathProviderExtensions.OutlineOf` | `SvgTextWriter.Write` |
| the band the figure's title takes at the top (pad above, the title as drawn, pad below, the style's pad) | `SupTitleBand` (`Needed`, `Figure.TitleHeight`, `Figure.TitleBand`) | `ChartRenderer` (the frame, the title's baseline), `ConstrainedLayoutEngine` |
| matplotlib's ten-colour default cycle | `QualitativeColorMaps.Tab10Cycle` (an array of the caller's own) and `Tab10At` (one colour) | the default, dark and matplotlib-v2 themes, `HueGrouper.DefaultPalette` |
| which colour maps the library ships | the static map properties of `ColorMaps`, `SequentialColorMaps`, `DivergingColorMaps` | `ColorMapRegistry` |
| the value of a basic colour name CSS4 also has (red, green, blue, white, black, yellow, cyan, magenta, orange, gray, light gray) | `Css4Colors` | `Colors` (read from it; `ColorsReadCss4Tests` holds it) |
| the rows of a mosaic pattern | `MosaicFigureBuilder.RowsOf` | `Parse`, `GetDimensions` |
| where a value lands on an axis with breaks, the range the breaks leave, which value a place on that axis reads back | `AxisBreakMap` (`Remap`, `CompressedRange`, `IsInBreak`, `Inverse`, `Shifted`, `Cuts`, `Edges`) | `DataTransform` (per point, the drawn range in `DataTransform.Of`, and `PixelToData`; the break marks and the ticks ask it through `DataToPixel`) |
| the room between the parts of a broken axis, and that the axis is drawn in parts | `Theme.BreakSpacingAlong` | `CartesianAxisPlacement.PrimaryTicks` (a run per part) |
| a wheel zoom about the pointer: the edges move toward it by the notch, read through the view | `DataTransform.ZoomedAbout` | `ZoomModifier` (the primary), `ChartLayoutViewExtensions.SecondaryRangesZoomedAbout` |
| the colour of the figure's ground: the background the figure was given, else its theme's | `Figure.Ground` (guard row `FigureGround`) | `ChartRenderer.RenderBackground`, `AxesRenderer.FigureGround` |
| what the cells of a spectrogram hold and where they stand: matplotlib's `specgram` under the matplotlib themes | `SpectrogramLook` (`CellsOf`, `ExtentOf`; key `spectrogramsLikeSpecgram`) over `StftFrames` | `SpectrogramSeries` (`CellsUnder`, its range, its colour range) |
| the one-sided fold of a spectrum | `SpectralSides.FoldOf`, `SpectralScaling.DivisorOf` | `Welch`, `StftResult.OneSidedDensity` |
| a spectrum's decibels (ten times the logarithm of a power, twenty of a magnitude; no number where it is not above zero) | `DecibelExtensions` (`PowerInDecibels`, `MagnitudeInDecibels`, the grid form; guard row `Decibels`) | the `Psd`, `Csd` and `MagnitudeSpectrum` verbs, `SpectrogramLook` |
| the box behind an annotation's text, and the leader line of a text that moved | `CalloutBox` (`Draw`), `DrawLeaderLine` | `CartesianAnnotationsPart` |
| the span of a pair-grid variable and where a sample lands in its cell | `AxisSpan.Of`, `PairGridCellFrame.Map` | the scatter and hexbin painters |
| a brush said as the rows it covers, to the viewer who brushed | `RowSelectionExtensions.RowsCoveredOn` | `InteractionHost` (a native control), `ChartSession` (a server) |
| the point a hover finds in a series, and how far it is on screen | each family's `IHoverTarget.Nearest` | `NearestPointFinder.Find`: the hover tooltip, the data cursor, the native controls |
| what one read cycle of a server session publishes: one render, the viewer who moved the view last, one tooltip per viewer | `ChartEventBatch` (`Applied`, `Faulted`, `Tooltip`, `Mover`) | `ChartSession` (it records each event there and publishes the batch once) |
| how a series of a package (a map) is drawn: by its renderer, made over the context of the series pass | `ISeriesRendererHost` (`Draw(series, renderer)`; the SVG series renderer's `Dispatch`) | `GeoPolygonSeries.Accept` -> `GeoPolygonRenderer` |
| the colour and shape of a series' legend handle when it wears a colour of the theme or draws a colour of its own | the series (`ChartSeries.LegendPartsUnder(theme)`) | the legend's one entry list |
| the theme the style parameters describe over a base theme, and when it has to be laid over again (a parameter written since) | `RcParams.ThemeOver` | `FigureBuilder.Build` |
| the rc parameters a theme carries, both ways: what a theme states for each | `ThemeRcKey.OfATheme` (`ValueOf`, `LaysOver`, over `ThemeRcParts`) | `StyleSheet.FromTheme`, `RcParams.ThemeOver` (`ThemeRcKeyTests`, a scan row) |
| what a theme says about how things are drawn beyond its colours, font, grid and margins | `ThemeLooks` and its looks (`Styling/Looks/`) | every reader of a look, the copy `Theme(name, other)` |
| the flat keys of a look in the theme's record of the figure JSON: what stands under each, which value is left out | each look's `IThemeKeys` pair (`WriteKeys`, `ReadKeys`), composed by `ThemeLooks` | `ThemeDto.From`, `ThemeDto.ToTheme` |
| whether a number can be written as a date, and the dates a date axis' window holds | `OleDateExtensions` (`IsOleDate`, `ClampedToOleDates`, `ToOleDateText`) | `ChartDataTable` (a date cell), `StateTimelineSeries` (the hover text) |
| where an axis' ticks stand and whether they label numbers or names, and the name an axis shows at a position | `AxesTickSourceExtensions` (`PrimaryTickSource`, `NamesAt`) | the axes renderer (draws and measures by it), the data table |
| the tick values of every 2-D axis, the primary X and Y included: the locator, else the spacing, else the theme's locator | `AxesTickSourceExtensions.TickValuesBetween` over the axis' `TickRequest` | `CartesianAxisPlacement.PrimaryTicks` (which leaves out the ticks inside a break) |
| the major ticks of every kind of axis (a 2-D axis, an axis of a 3-D cube, the rings of a polar axes, a colour bar) | `TickRequest.Majors` (`MajorsAtAnyLength` for a bar's reserve) | `AxesTickSourceExtensions.TickRequestOver`, `CubeAxisRow.ComputeAxisTicks` |
| the plain number format of a tick, and the one format all the ticks of an axis are written in | `NumericTickFormatter.FormatValue`, `TickLabelExtensions.UniformFormat` (a table of F0..F10) | `TickDrawContext.For` (every 2-D axis), the 3-D axes' labels (`ThreeDAxesRenderer`) |
| the axes title: its font, its style's pad, the gap below it | `AxesTitleExtensions` | `AxesRenderer` (the axes title and the X title it draws), the Y titles |
| the tick drawing: a run's marks and labels, or one of the two (`TickInk`), its minor marks and its offset text | `AxisTickExtensions` | the primary axes, their mirrored side and the sides a theme marks, the top axis |
| where an axis title stands past the tick labels of its edge, on all four edges, and the room it takes | `TitleClearance` (`Beside` an edge's `EdgeTerms`: its facing ink and `TitleWithoutTickLabels`; `TitleOffset`, `TitleExtent`) | the X title (`Axis.XTitleBeside`, `BottomTitleExtent`), the top axis' title |
| how an axis title is drawn beside its edge: upright and centred along a top or a bottom edge | `EdgeTerms.DrawTitle` over `EdgeTitle` (`UprightEdgeTitle`, the one X title site; `TurnedEdgeTitle`) | `AxesRenderer.RenderAxisLabels`, `CartesianAxisPart` (every axis) |
| how an axis' tick drawing is made and hung from its edge, and from the edge across the plot | `PlacedAxis.TickDraw`, `OppositeDraw` (`TickDrawContext.For`, `EdgeTerms.Hang`) | `CartesianAxisPart`, `CartesianAxisPlacement` (bands), the measure of the bands |
| what the edge opposite a primary axis draws: its mirrored ticks, a theme's every-side marks | `CartesianEdges` (`OppositeTicks.Of`), `Axes.DrawnSecondaryXAxis` | `CartesianAxesRenderer` (drawing, `MeasureSideBands`, `MeasureEndBands`) |
| how a series pass is made and drawn: its kind (over a Cartesian plot, on the unit square, layered), the one loop, the one orderer | `SeriesPass` (`OverPlot`, `OnUnitSquare`, `Layered`; `DrawSteps`; `UnitSquare`, the mapping a polar or 3-D axes also collects the marks of a Best legend through) | every renderer, `CartesianAxisPart` |
| what every part of a Cartesian render draws with | `CartesianPartContext` | every part; held by `OneOwnerSourceContractTests` |
| how far a tick mark reaches out of the plot and into it, by its direction | `TickMarkSpan.Of`, held per look with its stroke by `TickMark.Of` | `DrawTickMark` (the drawn mark and matplotlib's marker), `TickConfig.OutsideLength` |
| where a tick of a run stands, the scale and the view it is drawn over, and the minors that scale places | `TickRun` (`PlaceOnPlot`, `HoldsAny`, `Scale`, `View`, `ScaleMinors`) | the tick drawing, the grid lines, the break marks, `TickDrawContext.For` |
| what a drawing of ticks puts on the page | `TickInkExtensions` (`DrawsLabels`, `DrawsMarks`, `WithoutLabels`) | `RenderAxisTicks`, `CartesianAxesRenderer.RenderTicks`, a Y slot's part |
| the order a Cartesian axes draws its data in: the grid, spans, patches, reference lines, drawing tools | `ZOrderExtensions`, `LayerOrder` (`CartesianAxesRenderer.DataLayers`, `SeriesPass.Order`) | `SeriesPass.DrawSteps` (every renderer and every axis part) |
| the 2-D grid drawing: a line across the plot at each major tick the plot holds | `GridLineExtensions` (`RenderGrid`, `RenderGridLines`), `AxisTickLook` | `CartesianAxesRenderer.Render`, `CubeAxisRow` |
| where a spine stands, the stretch it spans and how the four are drawn, so a moved spine takes its ticks with it | `SpineLineExtensions` | `CartesianAxesRenderer.Render`, every axis' edge line |
| what a band hung from a spine reaches past the plot's edge | `EdgeTerms.InsetOf` → `SpineInsets` (`PastLeft`, `PastRight`, `PastTop`, `PastBottom`) | `EdgeBands` (`Insets`: `TopPastEdge`, `RightPastEdge`, `BottomExtent`) |
| how wide a spine is drawn | `SpineConfig.LineWidthIn(theme)`, from `AxesFrameLook.SpineWidth`; `SpineConfig.ColorIn(theme)` | `SpineLineExtensions.RenderSpines`, the circle that closes a polar axes |
| a length in points in pixels (100 / 72 to the point, the 100 dpi matplotlib draws at) | `PointLengthExtensions` (`PointsInPixels`, `PixelsInPoints`, `PixelsPerPoint`, `AreaAcrossInPoints`) | `MatplotlibThemeFactory`, `TickConfig`, `LegendRenderer` |
| how opaque a 3-D surface is drawn | `IGivenAlpha.OpacityUnder(theme)`, from `PatchLook.Alpha` | `SurfaceSeriesRenderer`, `Trisurf3DSeriesRenderer` |
| how opaque a series is drawn under a theme (fill, surface and dot alike): the opacity its author gave | `IGivenAlpha` (`GivenAlpha`, `RestingAlpha`, `ThemeAlphaOf(looks)`) and `GivenAlphaExtensions.OpacityUnder(theme)` | `AreaSeriesRenderer`, `StackedAreaSeriesRenderer`, `PolarBarSeriesRenderer` |
| that an opacity is a number from 0 to 1, refused otherwise (matplotlib's `Artist.set_alpha`): every setting named after an opacity | `UnitIntervalExtensions.ThrowIfNotOpacity(name)` | the 23 series' alphas, the indicators', a figure image's, a span's |
| the opacity and the clip of a group of marks, set and put back: a scope that resets on `Dispose` | `RenderContextScopeExtensions` | the surface, trisurf, voxel, contour band, image cell |
| the style of a patch-like shape: its fill, its edge as the theme decides it, the join of its corners and the colour of its hatch | `ResolvedPatchEdge.StyleOf(fill, follows or edge, hatch)` over `PatchEdge.StyleOf(fill)` | bar, histogram, broken bar, count, Gantt, waterfall, donut, pie, polar bar, violin |
| what bounds a 3-D box | `CubeLook.AxisLineWidth`, read once in `ThreeDAxesRenderer.Render` | `CubeFrame.DrawAxisLines`, `CubeFrame.DrawEdges` |
| how a 3-D surface's cells are outlined | `PatchLook.SurfaceEdgeOver(shown, given)`, from `PatchLook.ForceEdge`, `EdgeColor` | `SurfaceSeriesRenderer`, `Trisurf3DSeriesRenderer` |
| the edge of a bar-like patch and of its legend handle | `Theme.PatchEdgeOf(patch, fill)`: the matplotlib half is `PatchLook.EdgeOver(GivenPatchEdge)` | `BarSeriesRenderer`, `HistogramSeriesRenderer`, `CountSeriesRenderer` |
| the width and dash a caller gave a series' line, kept apart from what the line reads at rest (a given 1.5 or solid is given) | `GivenStroke` (`StyleOrResting`, `Over`) | `SeriesSlot.StrokeOf(color, series)` |
| how wide a line of a series is drawn when it was given no width: one rule over the width given | `LineWidthRule.WidthOf`, read through `SeriesSlot.LineWidthOf(series)` and `StrokeOf(color, series)` | the line, step, area, error-bar, ECDF, KDE, signal, sparkline, regression, polar |
| what a model keeps of a setting while its author gave it | one field-backed slot `internal T? GivenX { get; set => Change(ref field, value); }` (C# `field`) | every model, its writer and its reader |
| how a value a theme's look names meets the value its author gave and the value its owner rests at | the look's own member, named after the setting | the ticks drawn under a theme, the spines, a Y axis title's gap, the legend |
| the width the library draws its own outlines, hairlines and marks at | `LineWidthRule.OwnPixel`, `OwnHairline`, `OwnMark` (own rules: the width beats every theme) | the series renderers named, `ArrowHead`, `CubeFrame`, `TernaryAxesRenderer` |
| which samples of a line are drawn: the cull to the view, the LTTB point budget, the slice of a series over monotonic x | `SeriesRenderer.CulledToView`, `ApplyDownsampling`, `ApplyMonotonicDownsampling` | the line, step, area, scatter and monotonic line renderers |
| where a rectangle stands on the pixel grid of a raster export, under a theme that asks (`RasterLook.StrokesOnPixelGrid`) | `RenderContextPixelGridExtensions.DrawSnappedRectangle` (which asks `OnPixelGrid(Rect, ShapeStyle)`) | `BarSeriesRenderer`, `HistogramSeriesRenderer` (its bars), `CountSeriesRenderer` |
| where a line of horizontal and vertical segments | `RenderContextPixelGridExtensions.OnPixelGrid(IReadOnlyList<Point>, double)` | `SeriesRenderer.DrawPolyline` |
| where a horizontal or vertical stroke stands on the pixel grid of a surface that has one | `RenderContextPixelGridExtensions.OnPixelGrid` on any `IRenderContext` | `SpineLineExtensions.RenderSpines`, `GridLineExtensions` (a grid line) |
| how a horizontal or vertical line covers the pixels of a raster export: each pixel by the share of it the line takes | `SkiaRenderContext.DrawLine` over `AggPixelGrid.TryFillLine` | every straight line a PNG or a GIF frame draws under a matplotlib theme: spines |
| where a rectangle with an edge stands and is covered on the pixel grid of a raster export, under a theme that asks | `RenderContextPixelGridExtensions.DrawRectangleOnPixelGrid(Rect, ShapeStyle)` | `TableSeriesRenderer` (each cell), `TreemapSeriesRenderer` (each tile) |
| where a tick mark stands on the pixel grid of a surface that has one: as matplotlib's Agg draws a marker | `RenderContextPixelGridExtensions.MarkerOnPixelGrid` | `AxisTickExtensions.DrawTickMark` |
| where a tick of a given value stands, so a linked axis draws through the same code as every other axis | `ITickPlacement` (`DataTransform`; `LinkedTickPlacement` at the primary's place of `AxisTransform.Inverse(v)`) | `RenderAxisTicks`, `MeasureAxisTicks`, `RenderMinorTicks`, the grid lines |
| the minor values between the major ticks: the axis' own minor spacing or the major step in parts | `MinorWalk`, made by `AxisTickLook.MinorValuesBetween` over `AutoMinorLocator.Between` | `RenderMinorTicks`, the minor grid (`GridLineExtensions.RenderGridLines`) |
| whether the plot holds a tick, so a major mark and its label, a grid line or a minor mark is drawn there | `TickRun.PlaceOnPlot` (`AxisTickExtensions`) over `ITickPlacement.OnPlot` | `RenderAxisTicks` and `MeasureAxisTicks` |
| which ticks a log or logit axis names are its scale's minor ticks with a label, under the matplotlib themes | `ITickSpaceLocator.NamesAsMinor` | `TickDrawContext.For` (`MinorNamed`, `MinorLook`, `LookOf`) → `RenderAxisTicks` |
| where a tick of a 3-D axis stands, so a mark, a label or a grid line is drawn there: on its edge | `CubeAxisRow.Holds` | the major marks and their labels, the minor marks, the title's clearance (`TitlePad`) |
| whether the X tick labels turn, at the width the plot is drawn at, judged on the labels drawn (the ticks the plot holds) | `CartesianAxisPart.TurnedToFit` | the drawing, the layout (`MeasureEdgeBands`) |
| the fill behind a rectangular plot, in the theme's axes background | `AxesRenderer.FillAxesBackground` | `CartesianAxesRenderer`, `ThreeDAxesRenderer` |
| where each Y slot stands: its range, transform, ticks, band and outward offset, placed once per render | `CartesianAxisPlacement.PlaceSlot` / `PlaceYSlots` → `PlacedAxis`, `SlotBand` | `CartesianAxisPart`, the end labels, the layout's reserve (`ConstrainedLayoutEngine`) |
| where the top X axis stands: its own range, scale, locator, spacing and minor ticks, or, linked, the primary's range and transform | `CartesianAxisPlacement.PlaceTopAxis` / `PlaceDrawnTopAxis` → `PlacedAxis` | `CartesianAxisPart`, the band `MeasureEndBands` reserves, the legend's marks |
| where a linked X axis below the plot stands: the primary's range and transform, its ticks by the one rule of a linked axis | `CartesianAxisPlacement.PlaceBottomAxes` → `PlacedAxis` (one stack for the drawing and the measure) | `CartesianAxesRenderer.Render`, `MeasureEndBands` |
| the band of a Y slot and where its title stands | `YAxisTitleExtensions` (`YTitleExtent`, `DrawYAxisTitle`) over `TitleClearance.Beside` the edge | the layout's reserve, the renderer's stacking of the slots, each slot's title |
| everything that differs between the four edges an axis hangs its ticks from: along X, which way is out, where the edge stands | `EdgeTerms` (one per edge; `Facing`, `Hang`, `AlignmentFor`, `LabelAnchor`, `OffsetTextAt`, `TurnedAway`, `SpineAt`) | the tick drawing (`TickDrawContext.Edge`, `DrawTickMark`), the primary, mirrored |
| which member of an X/Y pair a direction picks, whatever its flag is called or whichever orientation says it | `AlongExtensions`; a type's own members over its own X and Y fields | the tick placement and drawing, the grid lines, the range folds |
| what no theme means: a member that draws, ranges or orders under a theme takes one, and a caller with none passes `Theme.Default` | the callers (`Theme.Default`), `SeriesPass.OnUnitSquare` | `RadialScale`, `ColorBarScale`, `PrimaryTickSource`, `MinorValuesBetween` |
| the looks a surface was told, the library's own for a surface that holds none, and the math texts and box metrics it reads by | `RenderContextLooksExtensions` | the text measuring seam, the verb, the mark collector |
| where the tree of a math label rests between draws, and when it is parsed | `KeptMathTexts` (held by `Figure.MathTexts`, told to each surface per stage) read by `TextLabel.Parsed` | the verb and the measuring seam, for every label a figure draws |
| the stroke of a drawn annotation line: the colour it names, else the one its kind takes | `IStrokedTool` + `StrokedToolExtensions.StrokeUnder` | reference lines, spans' borders, trendlines, levels, retracements, a zoom indicator |
| where the label of a drawn line stands (2 px off the line) and its font (the tick font in the line's colour) | `LineLabelPlacement` (`DrawLineLabel`), the font `ThemeFontExtensions.LineLabelFont` | the reference line, span and tools parts (they hand over the theme, never a font) |
| the font of an axis title | `Axis.TitleFontUnder(theme)` | the X, top, linked bottom and ternary titles, the bottom title's extent |
| whether a closed stroked outline is its shape or a polyline, and the walk of a broken circle | `ClosedStrokeExtensions` (`DrawCircleStroke`, `DrawPolygonStroke`) | the polar rings and spines, the radar rings |
| where each slot's outward offset starts and how much a stack reserves | `AxisStackingExtensions` (`StackedOffsets`, `TotalStackedBand`) | `ConstrainedLayoutEngine`, the render |
| whether a Y slot past the primary is drawn, and so owns the edge on the right | `YAxisSlot.IsDrawn` (series OR linked) | the renderer |
| the conversion between the container's numbering and the outward numbering | `YAxisStack.OutwardOffset`, `YAxisStack.SlotAtOutward` | `ApplySecondaryRanges`, the renderer's views, the SVG writer's `data-sec` attributes |
| the colour of an axis and of the spine on its edge | `AxisVerbs.ApplyAxisColor` | `SetYAxisColor`/`SetXAxisColor` on `AxesBuilder` |
| a linked axis holds no series of its own: a link is refused on an axis that holds a series, and a series on an axis that is linked | `AxisVerbs` (`LinkThrough`, `RefuseSeriesWhenLinked`, asked through its `Link` and `Seat`) | `YAxisSlotBuilder` (`Link`, `Plot`, `Scatter`, `StreamingPlot`) |
| whether a limit on a Y slot or the top axis counts (a limit set, no link) | `Axis.LimitsItsOwnRange` | `Axes.HasExplicitLimit` (a polar axes counts its radial limit alone) |
| the range a linked axis shows, or none | `AxisTransform.ImageOf` → `LinkedRange` | the drawing of a linked axis |
| the scale axes that share one read | `AxesRangeExtensions.LinkedContributions` | `CartesianAxisPlacement.ComputeDataRanges` (range, margins and sticky edges) |
| where bars that stand in rows reach in the automatic range (a Gantt chart, a broken-bar chart) | `BarHeightExtensions.AcrossRows` (rows 0 .. n - 1), `AcrossSlots` (the same in category slots) | `BrokenBarSeries.ComputeDataRange`, `GanttSeries.ComputeDataRange` |
| how far a patch reaches in the automatic range: its vertices and a curve's turning points | `AxesRangeExtensions.PatchContributions` over `PathSegmentExtensions.ExtremePoints` | `Axes.ComputeDataRanges`, so the axes that share a scale (`LinkedContributions`) too |
| the grid an axes draws | `AxesGridExtensions` | the Cartesian axes, the polar rings and spokes, the 3-D walls, the JSON |
| the radial scale of a polar axes: low end, extent, origin, rings, stacked feet | `RadialScale` (`RangeOf`, matplotlib's polar autoscale under the matplotlib themes; `Over` the rings of a span) | the polar grid and every polar series |
| the place of a composition in the ternary frame, and back | `TernaryPoint` / `TernaryPointExtensions` | the ternary axes renderer, the hit test, the table, the Piper diagram |
| the ticks of a 3-D axis (its `TickRequest`, `TickAxisKind.Cube`), worked out once a draw into its row | `CubeAxisRow` (`Of`, `ComputeAxisTicks`, `OutwardPerp`) | the 3-D grid, the tick marks and labels, the axis titles and their pad |
| where the free end of an axis that scales itself stops (matplotlib's `axes.autolimit_mode`) | `AxisRangeLook.Autolimit`, picked once as an `AutolimitRule` leaf | `AxesRangeExtensions.ComputeDataRanges`, `ComputeCubeRange` |
| the colour of a series a verb coloured where its caller named none | `Indicator.WearColor` | `SeriesPass.DrawSteps` (every pass, the top X axis' too) |
| the colour a series is drawn in, asked by a renderer: the colour it names, else the theme's colour for its kind | `SeriesRenderer.ColorOf` → `SeriesSlot.ColorOf` | every series renderer; the legend swatch asks `SeriesSlot.ColorOf` itself |
| the colours of a rise and a fall (green and red, the CSS colours, not the cycle's) a part is given none of | `UpDownColors.Standard` | the waterfall, OHLC and point-and-figure series, the renko verb |
| the fill of a span and of a patch that was given no colour: the first colour of the theme's cycle (matplotlib's `C0`) | `Theme.FirstCycleColor` (`SeriesSlot` at slot 0) | `CartesianSpansPart`, `CartesianPatchesPart`, the 3-D patches |
| the colour a part has that is chosen before any theme is known, and so cannot be the theme's cycle | `CyclePair.Standard` (`First`, `Second`) | the progress bar, waterfall, `FilledPatch.FaceColor`, `PropCycler`, `Pareto` |
| the colours a series that colours its groups, clusters or assets by a map wears where it names no palette | `Theme.CycleMap` (a `ListedColorMap`, made once per theme) | the pair grid's hue groups (`PairGridHue.HueColorOf`), a dendrogram's clusters |
| the colour of a quiver, a quiver key and barbs that name none and share no colour group: black under the matplotlib themes | `VectorFieldLook.Color` → `SeriesSlot.ColorOf` | the quiver, quiver-key and barbs renderers |
| the least margin of an axes that holds a scatter, a bubble or | `AxisRangeLook.ScatterMargin` → `AxesRangeExtensions.ScatterMarginIn` | `ComputeDataRanges`, `ComputeCubeRange` |
| what a box plot is drawn with where its author names nothing (matplotlib's `boxplot.*` rcParams under a style) | `ThemeLooks.Box` (`BoxLook`) → `SeriesSlot.ColorOf`, `BoxSeries` | `BoxSeriesRenderer` (`StylesOf`, `DrawStraight`, `DrawMean`, `DrawFliers`) |
| what a scatter marker is drawn with where its author names nothing | `ThemeLooks.Scatter` (`ScatterLook`) → `SeriesSlot` | `ScatterSeriesRenderer`, `BubbleSeriesRenderer`, `PolarScatterSeriesRenderer` |
| the cube of a 3-D axes | `AxesRangeExtensions.ComputeCubeRange` → `Box3D` | `ThreeDAxesRenderer.ComputeScene` (the projection), the rows' edges (`Box3D.RangeOf`) |
| where a 3-D or a polar axes stands: its cell under the figure's subplot params shrunk to a centred square | `PlotAspectExtensions.FittedTo` (the layout's plot area), `PolarTransform.MaxRadius` | the drawing, the hit test, the title, the legend, the background |
| the shape and the limits of a map axes: equal units centred in its cell unless the axes was given an aspect | `IMapSeries` (the series says it draws a map; its ask is `AxesAsk.Map`: tight, aspect 1) | every layout, the JSON and the MCP reader alike |
| the outline of a map (cartopy's `projection.boundary`): what the ocean fills | `ProjectionOutlineExtensions.Outline` | `Ocean`, `CartesianAxesRenderer` (`MapLook.LikeCartopy`) |
| where a ring or a line is cut at the horizon of a projection that shows one side of the globe | `HorizonClip` (`IProjectionHorizon`, the orthographic) through `GeoPixelMapping.Pieces` | `GeoPolygonRenderer` (`DrawArea`, `DrawLines`): the land |
| the geometry of a region made from polygons: a Polygon for one, a MultiPolygon for any other number | `GeoGeometry.OfPolygons` | the flow cartogram, `GeoAxesExtensions.ScaledAbout`, the H3 cells, `Ocean` |
| the look of a map that draws each region by a value (a neutral outline 0.3 wide, a label) and what the verb chooses | `GeoAxesExtensions.RegionMap` (private) | `Choropleth`, `H3Cells`, `H3Bins`, `Cartogram`, `ContiguousCartogram` |
| the width of a map's coastlines, borders and region outlines where the verb is given none | `GeoAxesExtensions` (`CoastlineWidth`, `BorderWidth`, `RegionEdgeWidth`) | the defaults of `Coastlines` and `Borders`, `RegionMap.Of` |
| a place off a projection's domain (the far side of a globe, a singularity): both coordinates missing | `ProjectedPointExtensions` (`ProjectedPoint.OffDomain`, `IsOffDomain`) | the projections, `GeoPixelMapping`, `HorizonClip`, `Place`, `ProjectedBy` |
| the numbers a projection is made with, by their PROJ names, and the projection made again with others | `ProjectionParameterSet` (`Centred`, `Conic`, `Meridian`, `With`) | the six parametric projections, `GeoMapData` (the JSON's order) |
| the centre of an azimuthal projection and a place seen from it | `CentredFrame`, `CentredPlace` | `Orthographic`, `AzimuthalEquidistant`, `Stereographic` |
| a projection's bounds (the limits a map asks its axes for, the square its outline circle is drawn in) | `GeoBoundsExtensions` (`WorldEdgeBounds`, `GeoBounds.Symmetric`, `BoxRound`) | the 13 projections |
| the outline of a map, traced once for each projection | `ProjectionOutlineExtensions.Outline` (`IProjectionOutline.TraceOutline`) | `IMapSeries.OutlineIn`, `Ocean`, `HorizonClip`; held by `OneOwnerSourceContractTests` |
| where a 3-D box stands in its plot: the square showing the projected points from -0.95/dist to 0.9/dist | `Projection3D` (the fit, worked out in its constructor) | every 3-D drawing, the browser's turn and zoom |
| which side of each axis a 3-D cube shows at the back, and the panes, edges and wall grid on those faces | `CubeFrame` | `ThreeDAxesRenderer.Render` |
| the vertex data of a 3-D scene: six significant digits, which components follow the face selection, a text's edge and pad | `SceneVertexWriter` | `CubeFrame`, the tick rows, the axis titles |
| how many steps a colour bar's gradient is drawn in: one for each colour of its map under the matplotlib themes | `ColorBarStrip.StepsFor` (`ColorBarLook.StepPerColour`) | the axes' bar (`AxesRenderer.RenderColorBar`), the figure's bar |
| where a colour bar stands and the room it takes | `ColorBarRoom` (`Under`; the leaves `MarginRoom`, `SlotRoom`, `ConstrainedMarginRoom`: `BodyOf`, `ClearanceOf`, `ReachBeside`, `ReachBelow`, `TakesCell`, `AnchorsAxes`, `SettlesAt`, `AreasAt`, `FigureAreaAt`) | `FigureGrid.Cells` and `FigureBarCell`, `PlotAspectExtensions.FittedTo` |
| which values a colour bar names, and where each stands along the gradient | `ColorBarScale` (`For`, `WidestLabel`, `GradientFraction`, `FractionOf`), the bar's `TickRequest` | the vertical and horizontal bar, the figure's bar, the layout that reserves its room |
| which series a colour bar reads | `ColorBar.Resolve` (`ResolvedColorBarExtensions`: the first provider that has a bar's data) | the axes' bar, the figure's bar, the layout |
| how a series that names a `LogNormalizer` colours its values: under the matplotlib themes matplotlib's `LogNorm` | `INormalizerRule` of each normaliser through `NormalizerRuleExtensions.Rule`: `NormalizerUnder` | every renderer that colours through a map, the colour bar, `ContourBandNormalizer` |
| what the end labels say, where each line ends, their font and gap | `DirectLabelMetrics` | the layout (reserves with it), `CartesianDirectLabelsPart` (draws with it) |
| spans spread apart in order | `Interval1DExtensions.SpreadApart` | the end labels, the Sankey node padding |
| the value labels of a bar, a pie, a sunburst and a sankey, placed so they do not stand on one another | `LabelLayoutEngine.Place` over `SurfaceFontMetrics` (`SeriesRenderer.PlaceLabels`) | the bar, pie, sunburst and sankey renderers |
| the .NET format a pie prints a wedge's share with | `PieSeriesRenderer.ToNetPercentFormat` | the wedges' labels, drawn by the same renderer |
| where a text starts along its baseline for its alignment | `TextAlignmentExtensions.StartOffset` | `TextBox`, the label layout's boxes (`LabelLayoutEngine`), the SVG glyph outlines |
| a number written into an SVG, a path's data and a glyph outline of the Skia package included | `StringBuilder.AppendSvgNumber` (two decimals, an integer's digits) | every SVG writer; `AppendSvgPath` is the one loop over a path's segments |
| a colour's hex in an SVG attribute | `StringBuilder.AppendHex` (the alpha is `AppendAlpha`'s) | the fill, stroke, text, gradient stop and hatch writers |
| the rim of an arc at an angle, and the two points it begins and ends on | `ArcSegment.PointAt`; `ArcSegment.StartPoint` and `EndPoint` | the arc's path data (its end point), `ArcSegmentExtensions.ToBeziers` |
| whether a text is drawn and measured as math (rich text) or as plain text, and how it stands | `TextLabel.Of` decides once | every text the renderers and the layout draw or measure: the axes and figure titles |
| the plain text of a rich text | `RichText.PlainText` | `TextLabel.OfRich`, `MathSpanWriter.TextOf` (the bidi order of a label) |
| the height and ascent of one line of a font | `ctx.LineExtentOf(font)` (a digit, on the surface the labels are measured on) | the Cartesian and polar axes, the constrained layout |
| which family reads a math-mode `\command`, and the character a symbol name stands for | `MathCommand.Find` | `MathTextCursor.Parse` (`TextLabel.Of`, `MathTextParser.Parse`) |
| the space of math: a typed space is none, a brace draws nothing, 0.2 em around a binary operator, a relation and an arrow | `MathGrammar.SpacesAround` | every row of math, at the top level and in every argument |
| what a parse of a label writes on before it knows how long a row or a run of text is | `MathParseScratch` | `MathTextCursor.Parse` (every row, group, argument and cell) |
| the font a run of math is drawn in: its face at its size, in the label's colour | `MathInk.AddGlyphs` through `SpanFont.Colored` | the verb (`MathTextRenderContextExtensions`, `DrawMath`) on every surface |
| the cells of an iso-line (the marching-squares case index): a corner is above a level when it is greater than it | `MarchingSquares.CaseOf` (`CollectSegments`, `RegionAbove`) | the contour lines (`Extract`), the filled bands (`ExtractBands`) |
| a run of round ticks over a range (matplotlib's `MaxNLocator._raw_ticks`) | `SteppedTicks.Over` with a `TickLadder` | `AxisAutoscale.RoundedOutToTicks` |
| one legend drawn from a list of entries in a rectangle | `LegendRenderer` (`Place`, `Draw`, `ItemBounds`) | the legend of every axes, the figure's legends (`FigLegend`) |
| where a figure's legends stand and the room the layout keeps at the figure's edges for them | `FigureLegendExtensions` (`OnTheFigure`, `PlacedLegends`, `RoomFor`) | `ChartRenderer.FrameOf` (the frame's `Legends`), `RenderFigureOverlays` |
| which rows a legend lists and how each kind of row is drawn, toggled and compared: series rows | `AxesRenderer.LegendEntries` over `SeriesLegendEntry.RowsOf` and `PatchLegendEntry.Among` | `RenderLegend`, `ComputeLegendBounds`, `LegendLayoutCalculator.MeasureLegendBox` |
| what a legend writes after a series' label when it shows values | `LegendValueTextExtensions` (`ISeries.LegendLabel`, `string.WithLegendValue`) | the legend rows (`SeriesLegendEntry.RowsOf`), the end-of-line labels |
| what a legend is drawn with where its author names nothing | `ThemeLooks.Legend` (`LegendLook`, `LegendFace`, `LegendScatterHandle`, `LegendFrameOpacity`) | `LegendLayoutCalculator`, `LegendRenderer` (`DrawFrame`, the handles) |
| how a legend's box is laid out round its rows: its padding, how far inside a corner it stands | `LegendLook.Box` (`LegendBox`: `LibraryLegendBox`, `MatplotlibLegendBox`, `InnerWidthOf`, `LineFloor`; `LegendRow`, `LegendCell`, `LegendSlot`) | `LegendLayoutCalculator` |
| the gap between a Y axis title and its tick labels, and between stacked slots | `YAxisContainer.TitleGapUnder`, `AxesTitleExtensions.XLabelGapUnder` | every Y axis part, the layout, the end-label column |
| where a legend stands (the place named, or the theme's default for a legend left at Best) | `Legend.PositionUnder(theme)` in `LegendLookExtensions` | the layout of the box, the renderer's decision to collect the marks |
| how large a marker in a legend handle is drawn | `LegendLook.MarkerSizeIn(points, handleHeight)` | `LegendRenderer.DrawSwatch` |
| what a point plot draws for a category: its mean and the two ends of its interval | `PointplotSeries.EstimateOf(index, theme)`, `DotLook.PointplotLine`, `CapSizeIn`, `IsJoinedIn` | `PointplotSeriesRenderer`, the series' range |
| how wide a dot of a strip plot, a swarm plot, a point plot, a residual plot or a pair grid is drawn where its author names no size | `DotLook` (seaborn's `Alpha`, `Jitter`, `Diameter`, `MeanPerLine`, `CapSize`, `JoinsPoints` as values) → `IDotSeries` | the five series' renderers |
| the shape of a marker, chosen from its style | `MarkerShape.Of(style)` / `Of(style, theme)`, `MarkerLook.FillTheirBox` | the line, step and scatter renderers (resolved once per series), the legend swatch |
| the head of an arrow and the path from an annotation's text to its target | `ArrowHead.Of`, `ConnectionPath.Of` | `CartesianAnnotationsPart`, the directed edges of `NetworkGraphSeriesRenderer` |
| where a network graph's nodes stand, once for each change of the series | `GraphLayoutAlgorithm.For` (`NetworkGraphSeries.Placement`) | `NetworkGraphSeries.ComputeDataRange`, `NetworkGraphSeriesRenderer` |
| how a dendrogram's orientation maps a leaf and a merge to pixels and places a label | `DendrogramFrame.Of` | every leg, the cut line and the labels of `DendrogramSeriesRenderer` |
| whether a legend stands outside the plot, where it stands there and the room the layout keeps for it | `LegendPositionStrategy` | `AxesEdgeNeeds` (`MeasureSides`, `MeasureEnds`) and `ConstrainedLayoutEngine` |
| what a subplot shows, in an identity order that never follows the order slots were added in | `Axes.AllSeries` in `YAxisContainer.IdentityOrder` | `SeriesSlotExtensions.SlotIndex`, `data-series-index`, a legend toggle's target |
| a series' colour and stroke, so a line on any axis and its swatch cannot wear different colours, dashes | `SeriesSlot` (`ColorOf`, `LineStyleOf`, `LineWidthOf`, `LineMarkerOf`, `ScatterMarkerOf`, `StrokeOf`: three values, the colour, the cycler's set and the theme, which the looks are read through) | the primary passes, the part of every Y slot, the line renderers, the legend swatch |
| what a kind of series is to matplotlib: the call its colour is asked with (which of the axes' two cycles) | `MatplotlibArtist` | `ChartSeries.CycleCall`, `ChartSeries.ArtistZOrder`, `SeriesSlot.ThemeColorOf` |
| the colour-slot table of an axes: made once by the renderer that draws it and handed to every reader | `AxesRenderer.ColorSlots`, `CartesianPartContext.ColorSlots`, `SeriesLegendEntry.RowsOf` | `SeriesPass.DrawSteps` through `ILayerHost.ColorSlots` |
| whether a series' or a patch's marks are cut off at the plot: under the matplotlib themes matplotlib's `clip_on` of its artist | `SeriesGroupExtensions.ClipsItsMarksUnder(theme, artist, reachesPastRange)` over `AxesFrameLook.ClipsEveryArtist` | `ClippedForSeries` |
| a clip cuts the marks drawn under it, never a text: a series' label, value or name stands whole past the plot | `MarkClips<TClip>` | `SvgElementBuffer` |
| the group a series is drawn in (`data-series-index` = slot + aria-label when interactive, aria-label otherwise) | `SeriesGroupExtensions` (`IRenderContext.BeginSeriesGroup`, `ClippedForSeries`) | the primary passes and the part of every Y slot, so the legend toggle |
| where the marks of a plot that is not a rectangle are cut off (the disc or ring of a polar axes, the triangle of a ternary one) | `ClipRegion` pushed by `RenderContextScopeExtensions.ClippedTo` (a `ClipScope` that pops it) | `PolarAxesRenderer` (`PolarTransform.Clip`), `TernaryAxesRenderer` (its patches) |
| what a path is on a target (a Skia path, a MAUI path, the vertices of the marks) and what a polyline is | the one walk `PathSinkExtensions.WalkInto` | `SkiaPathSink` (`SkiaRenderContext.PathOf`, `DrawLines`, `DrawPolygon`) |
| how one series of a pass is drawn: its group, its clip where the axes cuts it off | `SeriesPass.DrawSteps` (the one loop), made by `OverPlot`, `OnUnitSquare`, `Layered` | every renderer (Cartesian, polar, ternary, 3-D) and `CartesianAxisPart` |
| the handle of a series drawn as a line, and of a bar-like patch: from the value the series holds for its own drawing | `LegendSwatch.Line(stroke)`, `LegendSwatch.Line(series)` | the line kinds' `LegendSwatch`, the Geo map's line handle, the edged kinds |
| how far one X axis reaches below the edge it hangs from: its title when it has one (it stands past the tick labels) | `BandsBelowAxis.Extent` | `EdgeBands.BottomExtent`, `CartesianAxesRenderer` (`MeasureEndBands`) |
| the band past the right spine: the right Y axes stacked with the container's gap, the mirrored Y tick labels, the end labels | `RightBand.Of` | the layout (`AxesEdgeNeeds.MeasureRightBand`, over the bands it measures; `RightNeed` on both paths) |
| where a category and a bar on it sit, so a label, a mark and the bar it belongs to cannot drift apart | `CategorySlot` (`Of`, `FromTop`, `Centres`, `CentresFromTop`, `Width`, `Bar`, `Around`) | the bar and count renderers and data ranges, the category labels |
| categories written as an axis's ticks, on slot centres, on some slots, or at positions of their own | `CategoryTickExtensions` | every verb that names categories, the clustermap, the axes renderer |
| which axis carries the categories: a horizontal bar chart is an upright one with its axes swapped | `BarOrientation.ValuesAlongX` | the `BarSeries`, `CountSeries` and (a box that lies down) `BoxSeries` ranges |
| whether an axis' tick labels stand on its own side | `TickConfig.LabelsOnOwnSide` → `OnOwnSide(ink)` (drawing), `LabelsOwnSide` (layout) | `CartesianAxisPart` |
| where a polar axes puts its spoke labels and its title | `PolarLook` (`ThetaLabelPad`, `TitleRise`, `BarWidth`) | `PolarAxesRenderer` (the drawing and `MeasureEndBands`), `PolarBarSeriesRenderer` |
| a group of values that carries no values, named by its label | `BarGroupExtensions` | the verbs that take a group |
| the cells of a Marimekko chart: the width of a column, the stack of its segments, which columns are named | `Marimekko`, `MarimekkoCells` | `Marimekko` |
| the ranking of a Pareto chart: the counts from the largest, the running share of the total | `Pareto` (`SortOrderExtensions.DescendingOrder`) | `Pareto` |
| the columns of a parallel-coordinates chart: each scaled by its own range, its ticks in one number format, the line of a class | `ParallelCoordinates`, `ColumnTicks` | `ParallelCoordinates` |
| a sample fitted to a reference distribution: the plotting positions, the quantiles, the least-squares line | `ProbabilityFit` (`QQReference.FitTo`) | `ProbabilityPlot` (so `QQPlot`) |
| the table of cells of a stem-and-leaf display | `StemAndLeafDisplay.Cells` | `StemAndLeaf` |
| bars that float, as one series of one width and one colour | `FloatingBars` | `Renko` (a series for each direction), `CalendarEvents` (a series for each kind) |
| where the events and the days of a calendar stand | `GridAxesExtensions` (`StripeOf`, `DayCells`, `EventMarks` on a `CalendarGrid`) | `CalendarEvents` |
| how a horizon chart folds a series: the band height and each band's values | `Horizon`, `HorizonBand` | `Horizon` |
| the cells of a matrix above a threshold | `MatrixExtensions.CellsAbove` | `Spy` |
| what a flat list of triangles must be, and the edges it draws | `TriangleListExtensions` (`ThrowIfNotCornersOf`, `EdgesAmong`) | `Triplot` |
| a spectrum in decibels | `DecibelExtensions.ToDecibels` | `Psd`, `Csd`, `MagnitudeSpectrum` |
| the sample rate, the padded length, the centre frequency and the sides of a spectrum | `SpectralOptionExtensions` (`AsSampleRate`, `AsPadTo`, `AsCenterFrequency`, `AsSides`) | `SpectralOptions`, `SpectrumOptions` |
| which places a spectrum has and in what order (one-sided, or two-sided from the most negative frequency) | `SpectralSidesExtensions` (`FrequenciesOf`, `Arranged`) | `Welch`, `Spectrum` |
| whether a number a share or a width can be taken of is there (finite and above 0), as a test that branches | `MissingValueExtensions.IsPositiveFinite` | the treemap's and the apportioning's totals, a gauge's range |
| what a number given to the library must be, refused in one sentence and one exception type: finite, other than a value | `ArgumentRangeExtensions` | every setter and verb that takes a number |
| whether a value is a member of its enum, refused in the site's sentence | `EnumMemberExtensions` (`ThrowIfNotDefined`, `NotAMember` for a switch's last arm) | the spectral options, the stacking baseline, a calendar's first day |
| the lowest and the highest finite value of a sample | `double[].DrawableRange` (`MissingValueExtensions`) | the bins of a histogram, the span of a pair grid's variable, a polar series' reach |
| the area of a bubble on a map, and which places have one | `GeoAxesExtensions.BubblesAt` | `Bubbles`, `Dorling` |
| the line of a flow on a map: its great circle projected, each end once | `GeoFlow.PathOn` | `Flows` |
| the region of an H3 cell: its boundary through the H3 port, closed, cut at the antimeridian | `GeoAxesExtensions` (`AsH3Cells`, `BinnedIntoH3Cells`) | `H3Cells`, `H3Bins` |
| where a ring is cut at the antimeridian | `GeoClipping.SplitAtDateline` | the regions of `H3Cells` and `H3Bins` |
| regions resized by a value so that they still meet at their borders: the share each takes | `FlowCartogram.Equalized` (over `CosineLattice`, the cosine series on the library's FFT) | `ContiguousCartogram` |
| whether a projection can place a place | `GeoProjectionExtensions.Place` | the places of `Points`, `Bubbles`, `HexBins` and the ends and steps of a flow |
| what a bar stands on in a stack, on two sums (above zero, below zero), worked out once per range | `StackableExtensions` | `BarArrangementExtensions.ArrangeBars`, the stacked bar range (`BarSeries`) |
| where the bars of an axes stand in a draw: stacked first, then grouped side by side within each orientation | `BarArrangementExtensions.ArrangeBars` | `AxesRenderer.RenderSeries(DataTransform)`, `CartesianAxesRenderer.LegendObstacles` |
| the top edge of each stacked layer | `StackedBaselineExtensions.TopEdgesFor` | the stacked-area series and `StreamGraph` |
| where a box or a violin stands, and the range that follows | `DatasetStanding` (the first position, one value both looks hold), `DatasetPositionExtensions` | the box and violin renderers and ranges, the ticks at the boxes (`PrimaryTickSource`) |
| what a table's cells are where their author names nothing (fills, rule, row height, alignments, the pad of an aligned text) | `TableLook` (`HeaderFillOver` ... `PadOver`) | the table renderer |
| where a table's cells stand on a plot | `TableGrid` (`Of`, `TextSize`, `WithTablesOf`, `AnyStretchIn`), read off `TableLook` | `TableSeriesRenderer` (the cells drawn), `AxesRenderer.EndBands` |
| how far a layout moved an axes from where the figure's subplot parameters put it | `FigureRanges.MadeOn` | `AxesRenderer.Stretch` (a draw: its own plot; an inset its parent's) |
| where the wedges of a pie or a donut start, which way they follow, and how a pie's name is aligned beside its wedge | `WedgeLook` (`StartAngleOver`, `CounterClockwiseOver`, `NameCentreBand`) through `WedgeSeries.StartAngleUnder` | the pie and donut renderers |
| the width of a violin's body at each height | `ViolinSeries.Outline` | `ViolinSeries.BodiesUnder`, which the violin renderer and its range read |
| the copy of an object's data a draw takes, and whether it changed since | `DataSnapshot.Of` (the copy kept on `IKeepsDataCopy`: `KdeSeries`, `ViolinSeries`, `PairGridSeries`, `RelativeRotationSeries`) | `DrawnFrom.Of` (`KeptAxesDraw`), `KeptEstimate<T>` |
| when an estimate a series makes from its observations is made again: only when a setting changed | `KeptEstimate<T>` | `KdeSeries.CurveUnder`, `ViolinSeries.BodiesUnder` |
| an asset's RS-Ratio and RS-Momentum against its benchmark, by each formula | `RelativeRotation` | `RelativeRotationSeries.ComputeRsData` (a fresh copy) and `Rotation` |
| which levels a contour draws | `ContourLevels.Under` (each series' `LevelsUnder(theme)`) | the line, filled and triangle contours, the colour bar of each |
| the colour and the width of a contour's line at a level | `ContourSeries.LineColorOf` / `LineColourSpan` / `LineWidthUnder` | `ContourSeriesRenderer`, `TricontourSeriesRenderer` |
| the fill of a contour: the lowest band over the grid, each band above it the region above its lower level | `ContourBandFillExtensions.FillContourBands` over `MarchingSquares.RegionAbove` | `ContourSeries` with `Filled` and `ContourfSeries` |
| where a contour line's label stands and how it is turned (the same for the line traced backwards) | `ContourSeriesRenderer.LabelAlong` | `ContourSeries` with `ShowLabels` |
| a parent's extent divided among its children | `TreeNodePartitionExtensions.Shares` | `IcicleSeries`, the sunburst |
| a parent's area laid out among its children in near-square cells | `SquarifiedLayout.Of`, `TreemapLook.LikeSquarify` | the treemap renderer |
| where a day stands in a calendar | `CalendarGrid` | `CalendarHeatmap` |
| where a share becomes whole cells | `LargestRemainderExtensions.Apportion` | `WaffleSeries` |
| the neutral grey of reference graphics | `Theme.Neutral` | reference lines, trendlines, price levels, retracements |
| whether a collection a verb needs at least one of holds none, or a matrix has no row or no column | `ArrayLengthExtensions.ThrowIfEmpty`, `ThrowIfNoCell` | the verbs of the three verb families, `ChartDataTable`, the numerics |
| whether the data holds enough items for what a parameter needs of it, or no more than it takes | `DataCountExtensions` | Welch's method, the spectrum, the correlation, the Haar transform, `Cohere` |
| a column of a series' table whose values are positions along an axis, and the bare header x or y of one the series does not name | `ChartDataColumn.Position(axis, header)` | every series' `ToDataTable`, the figure's table (`Resolve` renames the bare x) |
| the series added last, of a kind | `Axes.LastSeriesOf` / `LastSeriesIs` (`AxesLastSeriesExtensions`) | the `AxesBuilder` verbs that act on it, the indicator shortcuts' prices |
| one spine of an axes changed, spines hidden | `SpinesConfig.WithEdge` / `Hiding` (`SpineEdgeExtensions`) | the colour rule, `HideTopSpine`, `HideRightSpine`, `HideAllAxes` |
| which arrays of a series fit, in one wording (an x and a y: "there must be one y per x"; an array a setter takes per point) | `ArrayLengthExtensions` | every verb that builds a series, `FromJson` through them, the MCP reader |
| which observations of a sample can be drawn, and how a refusal is worded | `NamedSampleExtensions` | `Ridgeline`, `QQPlot`, `ControlChart` |
| the drawable observations of a sample, ascending | `MissingValueExtensions.SortedDrawableValues` | the box, the violin, the density curve, the pair grid's diagonal, the ECDF |
| the stable largest-first order, and reading parallel arrays in it | `SortOrderExtensions.DescendingOrder`, `Pick` | `Pareto`, `DotPlot`, the treemap's squarified layout, a Sankey column |
| the separator between the runs of a line drawn in runs | `GappedLine` | `SegmentPath`, `JoinedAcrossGaps`, the Kagi path |
| many segments as the points of one line | `SegmentPath.Of` | `VLines`, `HLines`, `Triplot`, `Acorr` and `Xcorr` (their stems) |
| a staircase as the points of one line | `Stairs` | `Stairs` (its outline, and the top edge of its fill) |
| which triangles a point set has: the caller's own when given (even none), the Delaunay triangulation of the points otherwise | `Delaunay.TrianglesOf` | `TripcolorSeriesRenderer`, `Triplot` |
| the product-limit estimate of survival data | `SurvivalGroup.ProductLimit` (`DistributionAxesExtensions`) | `KaplanMeier` |
| the number of subjects at risk, and the test of k survival groups | `SurvivalGroup.AtRiskAt` (`DistributionAxesExtensions`), `LogRank.Test`, `ChiSquare.Survival` | the survival curve, `RiskTable`, the log-rank line |
| the confidence band around a survival curve, the level it is drawn at a time, and the times a group was censored | `SurvivalCurve.GreenwoodBand`, `SurvivalCurve.LevelAt`, `SurvivalGroup.CensoringTimes` | `KaplanMeier` (the band, and the plus marks on the curve) |
| the line that says how to read a stem-and-leaf display | `StemAndLeafDisplay.Key` | `StemAndLeaf` |
| one bandwidth for several groups, a shared grid, a density at given points, the densities of all the groups on one grid | `GaussianKde` (`JointBandwidth`, `Grid`, `DensityAt`, `JointDensities`) | `Ridgeline`, the density curve, the violin's body |
| the density curve of one sample: its drawable observations sorted, the bandwidth given or the theme's rule | `GaussianKde` (`DensityCurve`), `ThemeLooks.DensityCurveRule` | `KdeSeries.CurveUnder`, `PairGridSeries.DiagonalDensitiesUnder` |
| the bandwidth a density rests at where its author names none: Silverman's rule of thumb under the library's own themes | `DensityLook.BandwidthFactor` over `GaussianKde.Bandwidth(sorted, factor)` | the density curve, a violin's body, a pair grid's diagonal |
| the title an axis draws: the label it was given | `AxesTitleExtensions.TitleOf(axis, theme)` over `INamesValueAxis` | the Cartesian edge titles, the layout's bands, the left edge, the 3-D cube rows |
| what seaborn's verbs do beyond their marks where their author names nothing | `SeabornVerbLook` (`ThemeLooks.Verbs`) | `AxesTitleExtensions.TitleOf`, `CountSeries`, `KdeSeries`, `RugplotSeries` |
| a colour desaturated as seaborn's `desaturate`, and which kinds keep less of their colour's saturation | `ColorExtensions.Desaturated` and `ISaturatedFill`, applied once by `SeriesSlot.ColorOf` | a count plot's bars and its legend handle |
| how much a series raises the margins of its axes (a rug's twice its height across it) | `AxesAsk.MarginRaise` | `RugplotSeries` |
| a guide line a series draws over its marks (its colour or the series', opacity, dash, width rule) | `GuideLine.StrokeIn` | a residual plot's zero line (`SeabornVerbLook.ResidualZeroLine`) |
| the inverse of the normal CDF | `StandardNormal.Quantile` | `ProbabilityPlot` (so `QQPlot`), the regression band |
| Student's t: its CDF and its quantile | `StudentT` | the regression band, `QQReference.StudentT` |
| this angle in the other unit | `AngleExtensions.ToRadians`, `.ToDegrees` | the pie, donut, sunburst, polar, radar, chord and barbs renderers |
| the mean, the variance and the standard deviation of a run about its mean | `MomentsExtensions` (`Mean`, `Variance`, `StdDev`) | `Vec.Mean`, `Vec.Std`, `RollingStdDev` (so `BollingerBands`), `StreamingBollinger` |
| where a value stands in a range, and a range of no width (the middle of the colour map) | `RangeFractionExtensions.Normalized` over `IRangeFraction.Fraction` | `LinearNormalizer`, `LogNormalizer`, `SymLogNormalizer`, `PowerNormNormalizer` |
| where a value stands on a scale with a centre, each half filling its half of the colour map, a half of no width the middle | `CenteredNormNormalizer.Between` | `CenteredNormNormalizer`, `TwoSlopeNormalizer` |
| the edge of a distribution's domain: an input that is no shape, a probability at an end | `DistributionDomainExtensions` (`IsUndefinedShape`, `Saturated`) | `Beta`, `Gamma`, `ChiSquare`, `StudentT`, `Exponential`, `StandardNormal` |
| a power of two and its logarithm | the standard library's `int.IsPow2` and `int.Log2` (no bit trick, no cast of `Math.Log2`) | `Fft`, `HaarDwt`, `WaveletEnergyRatio`, `WaveletEntropy` |
| a simple moving average of a series an indicator derives | `RollingWindowExtensions.RollingMean` | `AmihudIlliquidity`, `CorwinSchultz`, `EaseOfMovement`, `GarmanKlass` |
| a value of each bar worked out from it and the bar before, rolled into a mean | `CandleIndicator.RollingMeanOfPairs` | `AmihudIlliquidity`, `CorwinSchultz`, `EaseOfMovement` |
| the solution of a linear system, the inverse of a matrix and the tolerance a pivot is singular by | `Linalg.Solve`, `Linalg.Inv` (LU, `SingularPivot`) | `LeastSquares.PolyFit`, `LeastSquares.ConfidenceBand`, `TurbulenceIndex`, `LogRank` |
| Nelson's eight tests, and the constants of the control charts | `NelsonRules`, `ControlChartConstants`, `ControlChartData` (the estimator of each kind) | `ShewhartChart` (and so `ControlChart`) |
| price arithmetic in whole steps from a fixed origin | `PriceStepExtensions` (its tolerance, 1e-9 of a step), counting through `WholeStepExtensions` | `Renko`, Point and Figure, `Kagi` |
| a value in whole steps: numpy's `divmod`, the multiple of a step at or below and at or above a value | `WholeStepExtensions` (`DivMod`, `StepsAtOrBelow`, `StepsAtOrAbove`) | `TickEdge` (matplotlib's `_Edge_integer(step, offset)`: the step and its tolerance worked out once) |
| the power of ten at or below, at or above and nearest a value, its exponent, and matplotlib's signed decades | `DecadeExtensions` (Numerics) | `SteppedTicks` (scale, offset), `AutoLocator`, `LogLocator` |
| how many ticks the room along an axis asks for (matplotlib's `np.clip(get_tick_space(), fewest, most)`) | `TickBudget` (`Clamp`; `Standard` 2..9, `Logit`, a ladder's `Budget`) | `MaxNLocator.ForTickSpace`, `LogLocator`, `LogitLocator`, `TickLadder.ForTickSpace` |
| a regular ladder of ticks: each tick from its index, rounded to ten decimals, a tick on the one before left out | `ArithmeticTicks` (`Through`, `Over`, `Multiples`, `Holding`, `Rounded`) | `AutoLocator` (`Locate`, `ExpandToNiceBounds`), the cube's ticks |
| how many label sizes a tick takes along an axis (3 along X, 2 along Y: matplotlib's `get_tick_space`), and how many labels fit | `MaxNLocator.LabelSpanAlong`, `MaxNLocator.LabelsAlong` | `TickRequest` (a caller says `AlongX`), `Axis.DensestTickLength` |
| the pieces of a Kagi path in the order it is walked, and the line one width draws | `KagiLines.Pieces`, `KagiLines.Path` | `Kagi` |
| the whole-number ticks of an axis that numbers cells: the step, and the name of each tick | `GridAxesExtensions.StepOf`, `NamesOf` | `MatShow`, `Spy` |
| the canvas a treemap's labels need | `FigureBuilder.WithAutoSize` | `FigureBuilder.WithAutoSize` |
| the guide lines an oscillator draws beside its signal, and how a caller styles or removes them | `IndicatorLevels` (`Visible`, `UpperColor`, `LowerColor`, `LineStyle`, `LineWidth`) | `Cci.Levels`, `WilliamsR.Levels` |
| the typical price, and the volume-weighted mean | `PriceSources.TypicalPrice`, `VolumeWeightedMean` | HLC3, `CandleIndicator`, `AdaptiveStochastic`; `Vwap`, `StreamingVwap`, `VwapZScore` |
| the arithmetic of an indicator's recursion, so it exists once | the step structs of `Indicators/Steps/` | the batch indicator (folds over it) and its streaming form (one input at a time) |
| the default of an indicator's parameter | a `public const Default<Parameter>` on the batch class | the batch and streaming constructors, the `AxesBuilder` verb |
| an indicator's label, the labels of its lines, the checks of its parameters and its warm-up, for both forms | the batch class's `Spec` factory (an `IndicatorSpec`, made nowhere else) | the batch base (`Indicator<TResult>`: `Label`, `FirstValueIndex`, `ValueCount`, `LineLabel`) and `Apply` |
| the bars before a batch indicator's first value: what its step says there is dropped | `Indicator<TResult>.FoldBars` (through `FoldPrices`, `FoldHlc`, `FoldCloseVolume`, `FoldHlcv`) | the 16 indicators whose recursion is a step (one value or a point of lines) |
| how many values a batch indicator has, and nothing when it has none | `Indicator<TResult>.ValueCount` (from its spec's first value) | every batch indicator |
| the pairs of consecutive bars an indicator walks (none below two bars) | `Indicator<TResult>.PairsIn` | `CandleIndicator.ComputeTrueRange`, `RollingMeanOfPairs`, `KaufmanEfficiencyRatio` |
| the true range of a bar | `PriceSources.TrueRange` | `CandleIndicator.ComputeTrueRange` (and so `SqueezeMomentum`), the ATR and ADX steps |
| the last inputs of a series kept in a ring: where the next goes, where the oldest sits | `InputWindow<T>` | `RingBuffer<T>` (behind its lock), `WindowedStreamingIndicator` |
| the bars of an indicator read from named columns | `DataFrameIndicatorExtensions` | every indicator class, made by the caller over the bars |
| the opacity of a band indicator's fill | `IndicatorLook.BandOpacity` | `BollingerBands.Alpha`, `KeltnerChannels.Alpha`, `Indicator.PlotBands` |
| where an indicator's value is drawn: at the bar it is worked out at, the last at the last bar given | `Indicator<TResult>` (`BarsIn`, `Positions`, `PlotLine`, `PlotBand`) | every line, band, histogram, area and dot of the library's indicators |
| the Y range an indicator's pane is pinned to, and the act that pins it | the indicator's static `Pane` (a `PaneRange`), `PaneRange.Pin` | the batch `Apply`, and `WithStreaming` for its streaming form (`IPinsItsPane`) |
| the highest or lowest of the last period values | `RollingExtremeStep<KeepHighest / KeepLowest>` | `RollingWindowExtensions.RollingMax` and `RollingMin` |
| the raw %K of the stochastic, and what smooths it | `StochasticStep.RawK` and `StochasticStep` (smoothK, d) | the batch `Stochastic` |
| Williams %R | `WilliamsRStep` | `WilliamsR`, `StreamingWilliamsR` |
| the colour of a reference line a verb coloured where its caller named none: `ReferenceLine.VerbColor` | `SeriesSlot.ChosenOrCycleColor` (the same rule as a series' `UnnamedColorOf`) | `CartesianReferenceLinesPart`, `FibonacciRetracement` |
| the averaged periodogram, and its NaN rule | `Welch` (`Psd`, `Csd`, `Coherence`) | `Psd`, `Csd`, `Cohere` |
| the spectrum of a whole signal in one transform, and its NaN rule | `Spectrum` (`Of`, `Unwrap`) and the `SignalSpectrum` it returns (`Magnitudes`, `Angles`, `Phases`) | `MagnitudeSpectrum`, `PhaseSpectrum`, `AngleSpectrum` |
| the Hann window, and a transform of exactly the length it is given | `SpectralWindow.Hann` (`Coefficients`), `Fft.Transform` | `Fft.Forward`, `Fft.Stft`, `Welch`, `Spectrum` |
| the correlation of two signals at each lag, and its NaN rule | `Correlation` (`Auto`, `Cross`) | `Acorr`, `Xcorr` |
| what a power or a magnitude is drawn as in decibels: ten (a power) or twenty (a magnitude) times its logarithm | `DecibelExtensions.ToDecibels` | `Psd`, `Csd`, `MagnitudeSpectrum` |
| the one projection of a map axes | `GeoAxesExtensions` (recorded by `WithProjection`, read by every layer) | `Land`, `Ocean`, `Borders`, `Coastlines`, `Choropleth`, `Points`, `Bubbles` |
| the JSON fields every series shares (label, placement, z-order, visibility, point budget) | `SeriesSharedFieldsExtensions` | every series' `ToSeriesDto` / `FromSeriesDto` |
| what a contract's sentences say: what a series' `FromSeriesDto` takes and returns, what an indicator's `Spec` factory is for | the thing that owns the contract: `SeriesRegistry.Register`, `IndicatorSpec`, `ColorMapShelf` | every member that follows it |
| what an axes draws besides its series, in JSON | `AxesDto.Of`, `AxesDto.RestoreOnto`, each overlay's own `From` / `AddTo` | a subplot and an inset alike |
| the fields of a figure record under their wire names, and the kinds of a described family, as the MCP server reads them | `WireContractExtensions` | `AxesSeatShape` (the seats), `ChartSchemaDescription` (describe_chart_schema) |
| a number that is not finite, in JSON | `ChartSerializer.ReadOptions` | the writer, the MCP reader, `DoubleArrayReadConverter` |
| a list of numbers read from JSON: an array of exactly its length, no growing list | `DoubleArrayReadConverter`, in `FromJson`'s own options only | `ChartSerializer.FromJson`, so also the last hop of the MCP reader |
| a normaliser in JSON | `NormalizableExtensions.DescribeForJson`, read only through `ColorScaleWire` | every `INormalizable` series, a figure image |
| a colour map in JSON: the name field or the description field, and the two put together again | `ColorMapDescriptionExtensions.ColorMapOnTheWire` and `ColorMapWire.Read` | every series drawn through a colour map, the colour bars, a theme |
| the settings treemap, sunburst and dendrogram share, in JSON | `HierarchicalSeriesExtensions` (`DescribeSharedSettings`, `RestoreSharedSettings`) | `TreemapSeries`, `SunburstSeries`, `DendrogramSeries` |
| how a patch is written to the figure JSON and read back: its kind, its geometry | `PatchDto` and its leaves (`Patch.Describe()` writes, `AddTo` reads) | `AxesDto.Of`, `AxesDto.RestoreOnto` (so a subplot and an inset alike) |
| how the link of an axis is written to the figure JSON and read back: the named transform's kind and what it was made with | `AxisTransformDto` and its leaves | `AxisDto.Of`, `AxisDto.ApplyTo` (every axis seat), the MCP reader (`NormaliseAxis`) |
| how a gesture's change is written onto a Y slot | `AxisRangeEventExtensions.ApplySecondaryRanges` | `PanEvent`, `AxisRangeEvent` |
| the transform a modifier converts a pixel through, for the primary and for every slot | `ChartLayoutViewExtensions` (`ViewOf`, `SecondaryViewsOf`) | every modifier |
| the chart id, the layout and the sink of a modifier, and its "claims nothing, does nothing" answers | `InteractionModifier` (`ChartId`, `Layout`, `Emit`, the virtual `Handles*`/`On*`) | every modifier, which overrides only the gestures it acts on |
| the roles a controller asks every modifier for: each move of the pointer, and the rubber band, crosshair and tooltip it shows | `InteractionModifier` (`OnPointerTracked`, `ActiveBrushSelect`, `ActiveCrosshair`, `ActiveTooltip`) | `InteractionController`, which tests no modifier's class (the crosshair, the hover and the brush override them; so can a caller's own) |
| the gestures a chart has, and their order | `InteractionController.DefaultModifiers` (a list the caller edits) | `InteractionController.Create(context, modifiers)`, a control's `ControllerFactory` |
| where an event goes once a controller sent it: the figure or the server, the clicked point, the handler | `InteractionContext.Sink` (made by `InteractionContext.For`) | `InteractionHost`, every controller and modifier, built in or the caller's own |
| the name of every hub method: an event, a message to a viewer, a subscription | `HubMethodNames` (public; held to the hub and its client by the server package's routing contract) | the hub, the sink of a native control, the server script, the Interactive page |
| the names a chart carries for its scripts (chart id, axes, series, legend, an axes' view, the namespace) and the marker of a part | `SvgAttributes`, `SvgPartMarker` | the SVG writers, the diff engine, every script, the Blazor client |
| how a script finds its chart, holds a pointer, reads a pointer in chart units and reads a token | `SvgScriptPreamble` | every embedded script (the one-owner scan refuses each spelled elsewhere) |
| a token of the interaction theme: written when changed, read with the default | `InteractionToken` | the root writer, the highlight, Sankey, tooltip, treemap and server scripts |
| one notch of the wheel | `ZoomNotch` (matplotlib's `0.85 ** step`) | `ZoomModifier`, `TouchGestureTranslator`, the zoom and server scripts |
| the state a rubber band shows while its drag is in progress | `DragTrack.Showing` | `RectangleZoomModifier`, `SpanSelectModifier`, `BrushSelectModifier` |
| a number a script reads back off the chart | `SvgWriterExtensions.AppendExactAttribute` | the root view and tokens, every axes' view, an alpha |
| where the local chart server is reached | `ChartServer` (Loopback, HubPath; its page reaches the hub on its own origin) | the listen URL, the figure URL, the page |
| a native control's wiring: figure, layout, controller, server sink, handler, tooltip | `InteractionHost` (its controller through `IInteractionController`, made by its `ControllerFactory`) | the WPF, Avalonia, Uno and MAUI controls |
| what a native control is apart from where its figure comes from: its host made with its facts, the five gestures, its handler | `MplChartSurface` of each of the WPF, Avalonia and Uno packages | the sealed leaves `MplChartControl` |
| whether a native control may act on a gesture, and the hand-off of the layout of a paint to its host | `InteractionHost.Listening` and `InteractionHost.Painted` | the five inputs of each WPF |
| wiring a native control to a server | `ServerRouteExtensions.RouteToServer` over `IServerInteractive` | each package's one `WithServerInteraction`, generic over its surface |
| following an animation source: let go, listen, show a frame on the control's thread | `AnimationSourceFollower` | the `AnimationSource` of the WPF, Avalonia |
| what an animation timer does: 16 ms to begin with, one start however often it is asked, a tick with the timer as sender | `AnimationTimer` (public abstract, only the library derives: a `private protected` constructor and `Begin` / `End` / `Retime` hooks) | the sealed leaves `SystemThreadingAnimationTimer` and `MauiAnimationTimer` |
| what a native streaming control does with its figure: listen, let go, the limits before the first draw | `StreamingFigureFollower` | the WPF, Avalonia, Uno and MAUI streaming controls |
| which keys are held, which button a move is made with, how far a notch of the wheel turns | `NativeInputExtensions` (`ModifierKeys.Held`, `PointerButton.MovedWith`, `ScrollInputArgs.OfWheel`) | the `MplChartSurface` of the Wpf and of the Avalonia package, `UnoInputAdapter` |
| how a file is written: made whole in memory, then the file replaced | `FileReplaceExtensions.ReplaceFileWith` | every `Save*` of core and the packages |
| the samples of a streaming series and the count and mark of their changes | `StreamRing<TSample>` | `StreamingSeries` (line, scatter), `StreamingCandlestickSeries` |
| whether a streaming figure's data changed: every streaming kind on every seat (each Y axis, the top X axis, the insets) | `StreamingFigure.DataVersion` over the internal `IVersionedSeries` | the figure's clock (`RenderRequested`), `RequestRender` |
| when a streaming figure asks for a render: one tick at a time, and a render that throws reported, never the end of the process | `StreamingFigure` (its clock, reporting to `ChartDiagnostics`) | the server's `StreamingChartSession`, `StreamingFigureFollower` |
| the range a streaming scale mode follows: each series asked through the axes it is on, as the renderer asks it | `StreamingFigure.ApplyAxisScaling` over `AxesContextAdapter` | the scale modes |
| what a streaming scale mode does to an axis, along X and along Y | the `AxisScaleMode` records | `StreamingFigure` |
| the look a style describes | `RcParams.ThemeOver` | `FigureBuilder.Build`, for a figure without a theme of its own |
| the default axes of a figure built twice: each build gets an axes of its own | `FigureBuilder` (the steps of the figure-level verbs, taken again at every `Build`) | every figure-level verb that draws on the default axes |
| what a figure-level verb does to the default axes | the `AxesBuilder` verb of the same name, recorded through `FigureBuilder.OnDefaultAxes` | `Plt.Create().Plot(...)` and every other verb of `FigureBuilderDefaultAxesVerbs` |
| the spacing a layout verb of the figure edits: the one the calls before it left, a new one when none did | `FigureBuilderLayoutVerbs` (its member `SpacingSoFar`; `FigureBuilder.Spacing` holds the value) | `TightLayout`, `ConstrainedLayout`, `WithSubPlotSpacing` |
| a builder's export: build the figure, then the figure's own export | `FigureBuilderExportVerbs` (and `MosaicFigureBuilder`, which hands its built figure builder to it) | `ToSvg`, `ToJson`, `Save` of the builders |
| what a figure-level call asks of the figure (a setting, a text, an image, an inset): taken in the order of the calls | `FigureBuilder.OnFigure` | every figure-level verb of `FigureBuilder` and its verb classes but `WithTheme` |
| a mosaic built again: a figure builder of its own, the same panels once | `MosaicFigureBuilder.Build` (its `Configure` steps and panels taken on a new `FigureBuilder`) | `MosaicFigureBuilder.ToSvg`, `.Save`, the Verso formatter |
| a row of an operations dashboard: what it draws on its panel and how the panel is dressed | the `Action<AxesBuilder>` step `OpsDashboardBuilder` keeps for it | `OpsDashboardBuilder.Build` |
| a date axis with automatic ticks: the date scale, an `AutoDateLocator` and the `AutoDateFormatter` that reads that locator | `AxisVerbs.AutoDates` | `AxesBuilder.SetXDateAxis`, the date `Plot` and `Scatter` of `AxesBuilder` |
| a symmetric-log axis with its locator | `AxisVerbs.Symlog` | `AxesBuilder.WithSymlogYScale`, `AxesBuilder.WithSymlogXScale` |
| a date axis with one fixed format | `AxisVerbs.DateFormat` | `AxesBuilder.SetXDateFormat`, `AxesBuilder.SetYDateFormat` |
| the title of an axis with an edit of its style | `AxisVerbs.SetLabel` | `AxesBuilder.SetXLabel`, `YAxisSlotBuilder.SetYLabel` |
| a colour bar that is asked for starts visible; the one already set is kept | `ColorBar.AskedFor` | `AxesBuilder.WithColorBar`, `FigureBuilder.WithColorBar` |
| a clustermap and a pair plot: what they draw and how | `ClustermapSeries`, `PairGridSeries` (and their renderers) | `FigureTemplates.Clustermap` (`ClustermapFigure`) |
| the clustering of a matrix's rows and columns, and the tree a chart draws of it | `MatrixExtensions.RowDistances` / `ColumnDistances`, `HierarchicalClustering.Cluster` | `ClustermapFigure`, the one caller of the clustering |
| the layout of a template | its faceted figure | every member of `FigureTemplates` |
| how panels wrap into a grid (filled or balanced) and the cell of a panel's index | `PanelWrap` (`Filled`, `Balanced`, `At`) | `FacetGridFigure`, `SmallMultiplesBuilder`, `OpsDashboardBuilder` (its tiles) |
| the look of a built-in style sheet | the `Theme` of its name, made into parameters by `StyleSheet.FromTheme` | `StyleSheet.Default`, `.Dark`, `.Seaborn`, `.Ggplot` |
| the gap between a composite panel and the names beside it | `HierarchicalLayout.LabelGapPx` | the dendrogram's leaf labels, the clustermap's row and column names |
| a hue label per observation as the number of its group, in the order first seen | `HueGrouper.Codes` (the order `GroupBy` keeps) | `PairPlotFigure`, the data frame's `PairGrid` |
| observations split by a hue drawn as one series per group, in the group's colour and label (a histogram's at the overlap opacity) | `HueSplit` (`Draw`, `DrawOverlapping`, `Lines`, `Scatters`, `Histograms`) over `HueGrouper.GroupBy` | `EnumerableFigureExtensions`, `FacetedFigure` |
| the JSON record of a stream of points (kind, one snapshot's x and y, capacity, colour) and its restore | `StreamingSeries` (`StreamDto`, `FromStreamDto`) | `StreamingLineSeries`, `StreamingScatterSeries` |
| whether a press opens a drawing tool (its tool active, the left button, over an axes) and the press it reads | `ToolGate.Opens` (the press it reads: `ChartLayoutViewExtensions.IsLeftPressOnAxes`) | the trendline, level and Fibonacci modifiers |
| an animation of frames played by a controller | `AnimationBuilder`, an `IAnimation<int>` itself | `AnimationBuilder.ToController`, `InteractiveFigure.AnimateAsync` |
| a time window pinned on a panel: exact limits, a date axis with time-aware ticks | `AxesBuilderScaleVerbs.PinTimeWindow` | `OpsDashboardBuilder` (trend and timelines), `SmallMultiplesBuilder.WithWindow` |
| the open frame of a strip or facet panel: no top and no right spine | `AxesBuilderFrameVerbs.OpenFrame` | `OpsDashboardBuilder`, `SmallMultiplesBuilder` |
| a line or scatter series of x and y on a Y axis: the lengths agree, the series is made and put on the slot | `YAxisSlotXYVerbs.Plot`, `.Scatter` | `Axes.Plot`, `Axes.Scatter`, `YAxisSlotBuilder.Plot`, `YAxisSlotBuilder.Scatter` |
| a series put on the primary Y axis is not null, and is refused at the call | `YAxisSlot.AddSeries` | `Axes.AddSeries`, `AxesBuilder.AddSeries` |
| a series that is added and configured is on the axes before its callback runs | `AxesBuilder.AddSeries(series, configure)` | the four streaming verbs (`AxesBuilderStreamingVerbs`) |
| the tail of a verb whose axes made what it adds: the caller's callback gets the new thing, the builder is returned | `AxesBuilder.Configured` | every series and overlay verb of the `AxesBuilder*Verbs` classes |
| the primary axes a builder verb works on (the bottom X axis, slot 0 of the left Y axes, the depth axis) | `AxesBuilder.X`, `.Y`, `.Z` | every setting verb of the `AxesBuilder*Verbs` classes (`BuilderRules`) |
| the tail of an indicator verb: the axes places the indicator, then the callback | `AxesBuilderIndicatorVerbs.ApplyIndicator` | every indicator verb of the four indicator classes (`BuilderRules`) |
| the end label of the series added last | `YAxisSlotBuilder.WithDirectLabel` | `AxesBuilder.WithDirectLabel` (over the primary Y axis) |
| the nudge of an indicator onto the category slot's centre in a bar-slot panel | `Axes.Place` (`AxesBuilderIndicatorVerbs`) | every indicator verb and `AddIndicator` |
| the locator a scale brings (the decades of a log axis, the symmetric-log locator, the dates of a date X axis) | `ScaleRule.Locator` (and `BringsLocator`, which asks the same without making one) | the tick source of every Cartesian axis |
| the name a kind of series is written under in the JSON, and the registry key its reader is made under | `ISeriesKind<T>.Kind` on the series class (`series.WireKind()` for the writer) | `ToSeriesDto` of each series, `SeriesRegistry.Register<T>` and `RegisterReader<T>` |
| how a series reads an optional setting from its record (`is { } x`, applied only when present), leaves a flag at rest out | `RestingValueExtensions`, `EnumParsingExtensions`, `ColorScaleExtensions` | every `ToSeriesDto` and `FromSeriesDto`, every DTO record |
| which text of a document names an enum member | `EnumWireNames<T>` with `IndexOfWireName` | every reader of an enum name: each `FromSeriesDto` and DTO |
| what a setting starts from when its maker is told nothing: the number, flag or enum member its property is born with | the `Resting` constant beside the property | the property, `ToSeriesDto`, `FromSeriesDto`, the verb of the axes, of both builders |
| a series made from its JSON object, or reported as an unknown kind or as unreadable data with its reader's reason | `AxesSerializationExtensions.CreateFromDtoOrReport` | `AxesSerializationExtensions.AddSeriesFromDto` (the primary series list) |
| the renderer an axes gets, a coordinate system nobody registered included | the table of `AxesRenderer` (`Create` falls back to its Cartesian entry) | `ChartRenderer`, `ConstrainedLayoutEngine`, `ChartLayout` |
| which lists are an axes' overlays, their change mark and the order a kept draw compares them in | `AxesOverlays` (`LatestChange`, `Items`) | `Axes.LatestChange`, `AxesDrawnDataExtensions.DataHolders` (`KeptAxesDraw`) |
| a threshold: a dashed line at the value, a span to the far edge on the breach side, an optional label at the value | `AxesOverlays.AddThreshold` | `Axes.AddThreshold`, `AxesBuilder.Threshold`, `FigureBuilder.Threshold` |
| a drawing tool entering and leaving an axes: the list each kind is held in, and nothing else taken off by a remove | `IDrawingTool.AddTo` / `RemoveFrom` on the three tools, entered through `AxesOverlays.Add` | `Axes.AddTrendline` / `AddLevel` / `AddFibonacci`, the three add events |
| an event applied to the subplot it names, or dropped when the figure has none (or its frame is fixed, for a view that moves) | `FigureInteractionEvent.OnTargetAxes` / `OnMovableAxes` (held by `OneOwnerSourceContractTests`) | every event of the library that changes a figure |
| the Fibonacci ratios and the price a ratio of a move stands at | `Models.Tools.FibonacciRetracement` (`Ratios`, `PriceAt`) | the retracement tool's levels, the Fibonacci indicator's `Levels` |
| which session holds a chart id, still or streaming | `FigureRegistry` (one map of `IRegisteredChart`) | the hub's routes, `UnregisterAsync` |
| what a publisher keeps for a chart, and when it is let go of | `ChartPublisher` (`ForgetAsync`, its gate taken again after a forget) | `FigureRegistry.UnregisterAsync`, `ChartHub` when the last viewer leaves |
| how a builder sets an axis: its title, limits, colour (and the spine it stands on), link, margin, scale and ticks | `AxisVerbs` (one factory per axis builder) and `TickVerbs` | `AxesBuilder` (X, Y, Z), `YAxisSlotBuilder`, `XAxisSlotBuilder` |
| a band a verb or an indicator fills between two curves | `AreaSeries.Band` (outlined at the line's resting width), `AreaSeries.Channel` | `Ridgeline`, `StreamGraph`, `Marimekko`, `KaplanMeier`, `Horizon`, `Stairs` |
| series drawn as one unit wearing one colour: the next group number, drawn once, and every member stamped | `AxesSeatExtensions.InColorGroup` | `KaplanMeier`, `Raincloud`, `ConnectedScatter`, `Horizon`, `Kagi` |
| the subplot a key names for a shared scale, joined once every subplot stands | `Figure.ShareByKeys`, `Figure.ShareByPlace` | `FigureBuilder.Build`, the document reader (`FigureDto`) |
| the coordinate system a series added at any seat puts its axes in | `AxesSeatExtensions.TakeTheSystemOf` (reads `ChartSeries.DrawnIn`) | `YAxisSlot.AddSeries` (so `Axes.AddSeries`), `Axes.AddXSecondarySeries` |
| a default the layers of a verb, its twin over the other axis and the value it makes all take | a `Resting` or `Default` constant of the owner | every declaration (`VerbLayerParityContractTests`) |
| what a drawing surface keeps of what a draw tells it: the looks, the math texts, the opacity | `ThemedSurface`, composed by each surface through the internal `ISurfaceRenderContext` | `SvgRenderContext`, `SkiaRenderContext`, `MauiGraphicsRenderContext` |
| whether a font is bold or italic (a slant leans when italic or oblique, for every surface and for math) | `Font.IsBold`, `Font.IsItalic`, `FontSlant.IsSlanted` | the three surfaces convert the answer and state none of the rules |
| whether a polyline is a line (from two points), and that a polygon is drawn from the points it is given | `PathSinkExtensions.FormsALine` | `DrawLines` of the three surfaces |
| the fill, the hatch and the outline of a shape on a canvas, in that order, each when it shows | `CanvasShapeExtensions.PaintIn` | the Skia and MAUI surfaces paint the layers they are asked for |

## Where a verb lives

`AxesBuilder` declares only the builder (its constructor and `AddSeries`) and `FigureBuilder` only the figure (its canvas, its
subplots, its builds); every verb is an extension member in the class of its concern (the map above), and every published call is
pinned (`BuilderSurfaceContractTests`). A verb of a new series type is an `extension(AxesBuilder builder)` block beside the axes verb
it forwards to, and `FigureBuilderDefaultAxesVerbs` takes no new forwarder. A verb that composes a chart from series that already
exist (no series type, no `SeriesRegistry` key, no serializer path: the chart travels through JSON as its parts) is an extension
method on `AxesBuilder`. 38 verbs live there today, and each one:

- sits in namespace `MatPlotLibNet`, in folder `Builders/`, in one static class per family, the family decided by the shape of the
  data the caller holds:
  - `CategoricalAxesExtensions`: data keyed by names (categories, a `BarGroup` per group). Verbs: `Lollipop`, `Dumbbell`, `Pareto`, `SlopeGraph`, `DotPlot`, `ParallelCoordinates`, `Marimekko`.
  - `DistributionAxesExtensions`: a chart estimated from a sample (a `NamedSample` per group, a `SurvivalGroup` per group of survival data, a `QQReference` for the distribution a sample is compared with). Verbs: `Ridgeline`, `QQPlot`, `ProbabilityPlot`, `KaplanMeier`, `RiskTable`, `Raincloud`, `StemAndLeaf`.
  - `XYAxesExtensions`: x and y arrays, values over x positions, segments between two limits at each position, values in the bins between edges, the edges of a triangulated mesh. Verbs: `BumpChart`, `ControlChart`, `ShewhartChart`, `StreamGraph`, `Horizon`, `ConnectedScatter`, `VLines`, `HLines`, `Stairs`, `Triplot`.
  - `GridAxesExtensions`: cells of a grid. Verbs: `CalendarHeatmap`, `Spy`, `MatShow`, `CalendarEvents`.
  - `PriceAxesExtensions`: a series of prices in time order, drawn without its time axis (the closes as one `double[]`; the indicators take an `OhlcvBars`, these charts need nothing but the closes). Verbs: `Renko`, `Kagi`.
  - `SignalAxesExtensions`: a signal, samples taken at a steady rate and handed over in the order they were taken as a `double[]`, drawn as what it is made of (a spectrum). Its order is its meaning, so it is neither x and y arrays nor a sample. Verbs: `Psd`, `Csd`, `Cohere`, `MagnitudeSpectrum`, `PhaseSpectrum`, `AngleSpectrum`, `Acorr`, `Xcorr`.
- takes at most 4 parameters besides `this`, the optional inputs of its computation before `configure`, which comes last and may be
  left out (`Action<TSeries>` run once per series it draws, or `Action<XxxParts>` run once);
- returns the builder, so verbs chain with every other call.

The types the verbs introduce:

- Parts: `DumbbellParts`, `ParetoParts`, `QQPlotParts`, `RiskTableParts`, `ControlChartParts`, `RaincloudParts`, `HorizonParts`, `CalendarEventsParts`, `RenkoParts`, `ConnectedScatterParts`, `KagiParts`, `StemAndLeafParts`, `StairsParts`, `CorrelationParts`. Each is a readonly record struct only the library makes, whose properties are the series on the axes.
- Named data: `BarGroup`, `NamedSample`, `SurvivalGroup`, `ControlLimits`, `ControlChartData`, `QQReference`, `SpectralOptions`, `SpectrumOptions`, `CorrelationOptions`. Each is a public readonly record struct: a name and its values, the limits of a control chart, the distribution a sample is compared with, or how a spectrum or a correlation is worked out.
- The spectral options hold `SpectralWindow`, `SpectralDetrend` and the enums `SpectralScaling` and `SpectralSides`;
  `MagnitudeSpectrum` takes `MagnitudeScale`.

What holds for every verb:

- One drawn unit is one series, or several series sharing one colour group (`Axes.InColorGroup`, added back to back), which take one
  colour slot and one legend entry; each series keeps its own JSON entry and click target (`SlotIndex`).
- A verb sets no axis limit unless the chart cannot be read without it (`Pareto`, `BumpChart`, `Spy`); a limit set afterwards wins.
- A missing value is left out or left as a gap, never drawn as zero; a verb refuses what it cannot draw with an `ArgumentException`
  naming the parameter, before the first series is added, so a refused call leaves the axes as it was.
- A composed chart is its parts in JSON and is not computed again when read back (`ComposedChartContractTests`); it draws exactly
  what its chain of public calls draws (each shape of `ComposedChartShapes` keeps it as its `ByHand`).
- What a verb owes its callback is one contract at every layer (`VerbContractTests` over `VerbCatalogue`).
- A verb that computes hands the work to an owner named after what it computes and keeps only its checks, the series it adds and
  the callback.

The container verbs `WithAddRightYAxis`, `WithAddLeftYAxis` and `WithAddBottomXAxis` are in no family: they stack one more axis on
their side in `AxesBuilderLinkedAxesVerbs` and hand a `YAxisSlotBuilder` (an `XAxisSlotBuilder` below the plot) to a required
`configure`. `AddPatch` is no verb: one method of `AxesBuilderOverlayVerbs` takes every kind of patch. `GroupedBar`, `DensityScatter`
and `Threshold` draw what one call draws, so they stand with the series and overlay verbs.

A new public method a caller writes on a builder, declared or added by an extension, takes at most four parameters besides the
builder, gathering its inputs in one value; the published methods over that count are exceptions `InstanceMethodBudgetContractTests`
names, and it holds public constructors and static factories to the same budget.

The verbs of `MatPlotLibNet.Geo` (`GeoAxesExtensions`: `WithProjection`, `Land`, `Ocean`, `Borders`, `Coastlines`, `Choropleth`,
`Points`, `Bubbles`, `HexBins`, `H3Cells`, `H3Bins`, `Flows`, `Cartogram`, `ContiguousCartogram`, `Dorling`) live in that package
with the same shape, held by the Geo suite's GeoVerbShapeTests; the layers that draw a map are one series kind, `GeoPolygonSeries`.

An extension is written as a block, never as `static R M(this T value, …)` (`ExtensionShapeContractTests`). A published static that
never took `this` stays a plain static (`HueGrouper.GroupBy`, `NearestPointFinder.Find`, `MonotonicViewportSlicer.Slice`,
`PriceSources.Resolve`, `CompositeOperation.Blend`, `DashPatterns.GetPattern`, `HatchTile.MarksOf`,
`DataFrameColumnReader.ToDoubleArray`): package validation reads its replacement by a block as a removal.

## The guards

- The wire is pinned by bytes: `ChartSerializerGoldenCorpusTests` (every series type at rest), `GivenSeriesCorpusTests` and
  `GivenAxesCorpusTests` (every setting given), `ThemeCorpusTests` (every theme) and `RestingValueCorpusTests` (what each setting
  starts from), all read through `BytePins`, the one place `MPL_REGEN_GOLDEN=1` writes them.
- `OneOwnerSourceContractTests` holds a row per rule with one owner (the area files of `Tst/MatPlotLibNet/Packaging/OwnedRules/`):
  the pattern that spells the rule and the only files that may; a row whose owner no longer spells it is refused too, and every row
  has a probe. `ResponsibilityBudgetContractTests` holds every type to 20 methods of its own, an exception named with its reason.
- `CodeCloneContractTests` holds every file of `Src` and `Tst` to no block of eight lines written in two files, as written or by
  shape; `MirroredAxisBodiesContractTests` refuses X/Y twins; `NoObsoleteMemberContractTests` keeps `[Obsolete]` out of `Src`.
- `DeletedNameTests` keeps a deleted name out of the code, the tests and the documents (the wiki suite reads the pages);
  `TestClassContractTests` gives a type one test class, `<Type>Tests`.
- The XML documentation of a public member names nothing the packages keep internal but a constant, the one place a value is
  written (the wiki suite's PublicDocumentationTests, over the documentation file the build writes beside each package).
- The tests share their readers and doubles (`SvgMarkupExtensions`, `RecordingRenderContext`, `DrawnSeriesFixtures`, `IndicatorCases`,
  `ThemeCases`, the fixtures of `Tst/MatPlotLibNet/TestFixtures`); a `TestCodeRules` or `TestReadRules` row refuses a test that writes
  one again, and a double with no logic of its own has no test file.

## Key design patterns

| Pattern | Where | Why |
|---------|-------|-----|
| One owner per rule | the table in "One owner per rule" | a rule two layers would otherwise each work out lives in one class that every reader asks |
| Fluent builder | FigureBuilder, AxesBuilder, ThemeBuilder, YAxisSlotBuilder, SecondaryXAxisBuilder | matplotlib-style method chaining |
| Shapes the caller makes | `Patch` (abstract, private protected constructor) → `FilledPatch` → sealed `Rectangle`, `Polygon`, `Arrow`, `Circle`, `Ellipse`, `Wedge` | a patch knows its outline in data units (`Outline()`, the path matplotlib builds), its drawn style |
| Visitor | ISeriesVisitor + ChartSeries.Accept() | extensible per-series rendering without switch |
| A sealed shape per enum value | `MarkerShape`, `ArrowHead`, `ConnectionPath`, `CalloutShape`, `GraphLayoutAlgorithm`, `DendrogramFrame`, `GeoGeometryKind` (Geo) | the public enum stays the caller's word |
| Strategy | IRenderContext (SVG, MAUI, Skia), AxesRenderer (Cartesian, Polar, 3D, Ternary) | multiple output targets and coordinate systems from same model |
| Template method | FigureTransform base class, AxesRenderer base class | shared renderer, format/coordinate-specific overrides |
| Fluent result | TransformResult record | polymorphic ToStream/ToFile/ToBytes from any transform |
| Self-serialization | ISeriesSerializable.ToSeriesDto() + per-series static FromSeriesDto(Axes, SeriesDto) on all 91 series | each series knows how to both serialize and deserialize itself; no central switch on either side |
| One value, not N fields | StreamingSeries → RingBuffer&lt;StreamingPoint&gt | a value that must stay consistent is stored as one object |
| Self-tabulation | ISeries.ToDataTable() overridden per series | this follows the same principle as self-serialization: the type that owns the data also owns its column names |
| One shaped text, three readers | Skia: TextShaper → ShapedText, kept once per (face, size, text) in `ShapedTextStore` and read by SkiaFontMetrics (width), SkiaGlyphPathProvider | the renderer draws text at the same width the layout measured for it |
| Ambient context | RcParams + AsyncLocal + StyleContext | global config with scoped overrides |
| Registry | `NamedTable<TKey, TValue>` behind InterpolationRegistry, ColorMapRegistry, StyleSheetRegistry, SeriesRegistry | one thread-safe name -> value table (ordinal unless given a comparer, last writer wins, Keys a snapshot) |
| Generic base classes | XYSeries, PolarSeries, GridSeries3D, HierarchicalSeries | DRY shared properties and behaviour across series and renderer families |
| Interface segregation | IHasDataRange, IPolarSeries, I3DGridSeries, I3DPointSeries, IPriceSeries, IColormappable, INormalizable, ICategoryLabeled, IColorBarDataProvider | narrow contracts for cross-cutting concerns |
| DI interfaces | IFigureTransform, IChartRenderer, ISvgRenderer, IChartSerializer | testable, replaceable services |
| SRP extension methods | FigureExtensions (Save, Transform, ToSvg, RegisterTransform) | FigureExtensions holds the output methods, so FigureBuilder only builds the figure |
| Process-wide slot | ChartSerializerDefault, ChartRendererDefault, SvgRendererDefault, FontMetricsDefault, GlyphPathProviderDefault | non-DI usage for console apps and the native controls |
| Admitted statics | an `extension(T value)` block | a static is one of these four or it is a member of the thing it is about: no `*Helper` / `*Util` class |
| Record types | Font, GridStyle, Legend, TickConfig, Color, Point, Rect, TransformResult | immutable value objects |
| One drawing sequence | `ChartRenderer.Draw` onto a `FrameTarget` (`DirectFrameTarget`; `KeepingFrameTarget<TLayer>` → `SvgFrameTarget`, `SkiaFrameTarget`), `FigureFrame` | every target is drawn by the same steps and the frame is laid out once |
| One copy per document | SvgFrameTarget + per-subplot SvgRenderContext + SvgDocumentParts | the subplots render in turn, each into its own context, and the document is copied once into its string |
| Every change reports itself | ChangeClock, IChangeTracked, StampedList, DataSnapshot | every setter of a drawn type stores through Change (a stamp only when the value differs) |
| Draw only what changed | `KeptAxesDraw<TLayer>` (markup for SVG, an `SKPicture` for the native controls), SvgFigureDocument, ChartPublisher + ChartSvgParts | a subplot nothing changed in is handed out (SVG) or replayed (native) as it was drawn, by ONE rule for both |
| Thread safety | volatile fields, NamedTable (a ConcurrentDictionary) for the six registries | safe concurrent access |
| Name table | `EnumWireNames<T>`, matched by `IndexOfWireName`, read by `WireEnum<T>()`, `WireEnumJsonConverter` and the MCP vocabulary | a document names an enum member by its name in any case and by nothing else |
| Default interface method | IRenderContext.DrawRichText; ISeries.ToDataTable() | a new member reaches every implementor without breaking any of them |
| Internal interface beside a public one | `IMarkupRenderContext`, `IFrameChartRenderer`, `ISvgDocumentRenderer`, `IHasLinePoints`, `ISeriesRendererHost`, AspNetCore's `IKeepingChartPublisher` | what the library's own types add to a public interface never sits on it |
| Command table | MathCommand (one sealed leaf per `\command` family, found by name) read by MathTextCursor | a command is looked up once and its family reads its own arguments and builds its box |
| Two-pass layout | ConstrainedLayoutEngine | measure text extents first, then compute margins |
| One obstacle map, one score, one selector | `MarkObstacles` (`Builder`, `Badness`, `FirstLeastCovered`), `AxesRenderer.LegendObstacles` / `CollectMarks`, `SeriesRenderer<T>.AddMarks` | the legend at `Best` and the annotations that avoid overlap look at the same marks as drawn |
| Compose existing series | `CategoricalAxesExtensions`, `DistributionAxesExtensions`, `XYAxesExtensions`, `GridAxesExtensions`, `PriceAxesExtensions`, `SignalAxesExtensions` | a chart that is a drawing of several series adds no series type, no registry key and no serializer path |
| A reflected contract | `SeriesDrawnPropertiesRoundTripTests` over `DrawnSeriesFixtures`, `AxesDrawnPropertiesRoundTripTests` over `DrawnAxesFixtures` | every settable property of every series type and every part of an axes |
| Closed polymorphic DTO family | `TickLocatorDto`, `TickFormatterDto`, `NormalizerDto`, `ColorMapDto`, `AxisTransformDto` with `IJsonDescribed<T>` | a built-in describes itself and a derived record restores it through a virtual |
| A series states what it is | `ChartSeries.DrawsWithoutAxes`, `KeepsAxesBackground`, `LegendSwatch`, `LegendPartsUnder`, `ColorSlotSpan`, `Artist` (a `MatplotlibArtist`) | the axes, the legend, the colour bar and the polar scale ask the series |
| The spelling of a colour in a spec, and the bounds of a spec | `ChartSpecReader.NormaliseColours`, `RenderLimits.EnsureSize` / `EnsurePictureFits` / `CheckText` | the MCP reader writes every colour the way the serializer reads it and holds every size |
| Refused at the verb | `ArrayLengthExtensions`, the series verbs, `ChartSpecReader.RefuseWhatTheLibraryRefuses` | a series that cannot be drawn from its data is refused when it is built, in one wording |
| Package data | `SeriesDto.PackageData`, `SeriesRegistry.Register`, `GeoMapData` | a package carries its own series through the shared DTO without core knowing its shape |
| A value at rest is not written | `RestingValueExtensions.OrNullAtRest` | JSON from an earlier release reads at the defaults and a figure that never touches a setting writes the bytes it always wrote |
| A value left alone is the theme's | a nullable value behind the public one: the width and dash of a kind whose line takes both as one `GivenStroke` | the one mechanism for "the caller gave no value, so the theme decides": a value that was given |
| Edge bands: the drawer measures | `AxesRenderer.MeasureEdgeBands` → `EdgeBands` | layout and renderer share one rule for what sits past the four spines |
| One container for the Y axes | `YAxisContainer` (`Left`, `Right`, `TitleGap`, `IdentityOrder`), `YAxisStack` (`Add`, `FirstOrAdd`, `OutwardOffset`, `SlotAtOutward`), `YAxisSlot` | the primary Y axis, the first right axis and every axis stacked past them are the SAME kind of thing, held in one place |
| The wire is outward-only | `YAxisStack.OutwardOffset` / `SlotAtOutward`, `SecondaryAxisRange`, `GetSecondaryDataRanges`, the `data-sec-*` attributes, `YAxisSlotsPastSlot0Dto` | the interaction wire and the 1.23.0 JSON fields number only the axes past the primary and must keep doing so |
| A replaced member is removed | `NoObsoleteMemberContractTests`; the published names in `DeletedName` | a published member that a better shape replaces goes in the release that brings the replacement |
| Capability + template method | `IHasLabelPlacement` on the series | the series says where its name goes |
| Named record types | IndexRange, Normalized3DPoint, AdxResult, ConfidenceBand, ColorStop, StreamingPoint, MinMaxRange, MatShape, XYCurve, BarRange, GaugeBand, DataPoint | replace anonymous/named tuples for discoverability and structural equality |
| Extension methods on value types | `string.EscapeForXml()`, `double[].Percentile()`, `Color.Modulate` / `Shade`, `RingBuffer<T>.Min()` / `Max()` (INumber-constrained) | discoverable dot-access APIs instead of `*Helper` static classes |

## Process-wide state

Nearly everything the library holds belongs to an object: a figure, an axes, a session, a publisher. A few places are
reachable and writable from anywhere in the process. They are public, they were published that way, and they are kept to two
shapes, each one separated from every other so that replacing one cannot move another:

- **A slot** is one replaceable value behind one `volatile` field, read and written as `Current` (or `Browser`): the one thing
  it exists to provide, and nothing else hung off it. A slot refuses `null` unless `null` is its default.
- **A registry** is a name -> value table: a public face (`Register`, `Get`, `Names`) over one `NamedTable<TKey, TValue>`
  (thread safe, ordinal unless the registry asks for another comparer, the last writer wins, `Keys` a snapshot). The table is
  the process-wide place; the type itself keeps no state.

Any other class that holds state anyone can write is a global class, and CONTRIBUTING.md § "Class design" bans it.
`ProcessWideStateContractTests` scans every package for the public static places a caller can write (a settable static
property, a static event, a `Register…` or `Use…` method) and goes red until this section names the class that owns it, so a
new one is a decision the maintainer takes and writes down here, never an accident. Removing one is a break of the public API,
so none is removed.

| Place | Kind | Holds | Written by |
|---|---|---|---|
| `ChartSerializerDefault.Current` | slot | the serializer behind `ToJson` / `FromJson` when no DI container supplies one | the application |
| `ChartRendererDefault.Current` | slot | the renderer every native control draws with, because none has a DI container to reach from its paint callback | the application; read by the WPF, Avalonia, Uno and MAUI controls |
| `SvgRendererDefault.Current` | slot | the SVG writer of `ToSvg` / `Save` | the application |
| `FontMetricsDefault.Current` | slot | the text measurement every render context lays out with | the application; `MatPlotLibNet.Skia` replaces it with Skia's metrics when its assembly loads |
| `GlyphPathProviderDefault.Current` | slot | the glyph-to-path converter (`null`: text stays `<text>`) | `MatPlotLibNet.Skia` when its assembly loads |
| `FigureExtensions.RegisterTransform` | registry | file extension -> exporter, seeded with a refusal for `.png`, `.pdf` and `.gif`; any other extension writes SVG | the application; `MatPlotLibNet.Skia` replaces the refusals |
| `AxesRenderer.RegisterRenderer` | registry | coordinate system -> the factory of its axes renderer (Cartesian, polar, ternary, 3-D are seeded) | the application, to replace the renderer of a coordinate system |
| `InterpolationRegistry`, `ColorMapRegistry`, `StyleSheetRegistry` | registries | name -> interpolation engine, colour map, style sheet; each ignores case | the application, by `Register` |
| `SeriesRegistry` | registry | the JSON discriminator -> the series' own `FromSeriesDto` | the library for its own series; a package for its own (`GeoPolygonSeries.RegisterReader`, when the Geo assembly loads) |
| `Plt.Style.Use` | the global look | writes `RcParams.Default`, which `FigureBuilder.Build` lays over the theme of a figure that names none | the application |
| `ChartDiagnostics.Emitted` | event | every failure the library skips instead of throwing | the application, which unsubscribes when it is done |
| `InteractiveExtensions.Browser` | slot (Interactive) | the launcher `ShowAsync` opens a browser with, replaced for tests or for a custom one | the application |
| `ChartServer.Instance` | lazy singleton (Interactive) | the embedded server `ShowAsync` publishes to, built on first use and kept for the life of the process | the library |
| `SkiaFonts.Register` | registry (Skia) | font file -> face, one per family, weight and slant, replaced whole | the application |
| `ShapedTextStore.Shared` | cache (Skia, internal) | every text the package shaped, by the face it resolved to, the size and the text, up to 4,096 and then started over | filled by the first caller |
| `GeoAxesExtensions.WithProjection` | registry (Geo) | axes builder -> the projection it was given, in a `ConditionalWeakTable` keyed by the builder | the application, by `WithProjection`; read by every layer verb |
| `NaturalEarth110m` | cache (Geo) | the coastlines, the countries and the boundary lines, read from the embedded resource on first use | filled by the first caller |

A test that swaps a slot, or writes a name the library ships, joins a `*GlobalState` collection with
`DisableParallelization = true` and puts the old value back (a `*Default.Current` slot through its `Holding`); a test that
writes a name of its own needs none.

## Left out on purpose

Things a reader may expect that the library does not do, with the reason:

- **The library's own themes open an empty or a flat range by the library's rule, not by matplotlib's locators.** An axis with no data is 0 to 1 and one value is opened by half a unit (`Range1D.Normalized`) before the margin, under the library's own themes.
- **The library's own themes do not range an axis by its spans and trend lines.** A span there is clipped to the range the data give, a span wholly beyond it draws nothing, and a threshold's zone relies on that (`SpanRegion`, `Axes.AddThreshold`); a trend line is drawn over the range.
- **A row of a broken-bar chart with no segment keeps its place along Y under every theme.** A row is a slot the caller gave (one label a row, the same extent as the rows of a Gantt chart, `BarHeightExtensions.AcrossRows`), as a category keeps its slot; matplotlib's `broken_barh` draws one row a call and has no row to leave out.
- **A limit set on one side of an axis whose data lie beyond it is not turned into an inverted axis.** matplotlib keeps the other side at whatever the view held before the call (`set_ylim(bottom=2)` over data from 0 to 1 draws from 2 down to 1.05, and from 2 down to 1 when no data has been plotted yet), a number that depends on the order of the calls.

- **An outside legend stands past a colour bar on its edge, the axes' bar and the figure's alike, not the bar past the legend.** The outside positions follow matplotlib's outside legend (`fig.legend(loc='outside right center')`), which its constrained layout stands past every colour bar, the figure's included (legend from 378.06 against the bar to 371.62 on a 600 x 400 figure).
- **The filled arrow heads keep their own formulas.** The open head of two barbs is one value (`BarbsArrowHead`), but matplotlib's streamplot head (`-|>` with its shrink), the quiver polygon and annotation `fancy` and `wedge` are four formulas of their own, not copies of one; and a 3-D quiver's head is the same head with the two barbs in the other order (`ArrowHead.OpenHeadMirrored`), kept so no picture's markup moves.
- **The icicle and the stat tile lay a hatch as it was given, not by the theme's hatch rule.** matplotlib's rule (a hatch takes the colour of the edge of its patch) is the one every patch-like series is styled by (`ResolvedPatchEdge.StyleOf`), a treemap tile included.
- **A figure's stages and the depth queue still set an opacity themselves.** `ChartRenderer` puts the surface at full opacity at the start of each stage, the background first (a guarantee for drawing the library does not own, a renderer someone registers or a series of their own, not the end of a group's opacity; `OpacityStageTests` goes red without either the subplot or the overlay reset).
- **A few numbers that look like a resting value are another rule.** The rain of a raincloud chart is drawn at 6 points squared (a scatter's size is an area, 36 at rest), and the rc parameter `figure.dpi` is matplotlib's own 100, which nothing reads, where a figure's layout unit is a pixel at 96.
- **The Geo package keeps its own `ToRadians` and `ToDegrees`.** The core's (`AngleExtensions`) multiply by pi over 180 in two operations, as the sites of the core did; the Geo package's public block multiplies by a precomputed factor, the one-multiply form its projections' numbers are pinned to (and numpy's `deg2rad`).
- **Ten projections give no inverse.** Only Plate Carrée, Mercator and Sinusoidal turn a point of the plane back into a place; the other ten answer `null` (`IGeoProjection.Inverse` says so).
- **A map's line widths are no theme look.** `GeoAxesExtensions` holds the widths of the coastlines, the borders and the region outlines as constants the verbs' defaults read: cartopy names no width of its own for a feature (it draws one at matplotlib's `patch.linewidth`, 1.0 under both styles), so no theme would hold another value.
- **`RollingMean` does not fold `RollingMeanStep` value by value.** It sums the first window in order and then adds the value that comes in less the value that goes out, which is the step's running sum to the last bit (a test holds the two bit for bit over every period and length it tries), but it reads the values it is given instead of keeping a ring of them: the four indicators that kept their own loop ran 6 to 39 % slower through the step.
- **The spread of a short run is not worked out by SIMD.** `MomentsExtensions` sums a run of fewer than 32 values in order: the in-order sums are the ones the indicators computed with before, and a window of 20 bars is already cheap that way.
- **A range of no width is the middle of the colour map under every normaliser but matplotlib's `LogNorm`, which maps it to 0.** That is matplotlib's rule for `vmin == vmax`, read from its source and kept under the themes that colour as matplotlib does (`LogNorm`); the library's own normalisers answer 0.5 (`RangeFractionExtensions`).
- **The hue verbs (`JointPlotFigure`, `FacetedFigure`, the typed-sequence figures) apply Tab10 as an explicit palette when none is named.** A group has to wear one colour on every panel and in every legend, and a scatter in a colour group takes slot 0 for every group under the matplotlib classic theme (matplotlib's scatter takes no colour of the cycle there), so only an explicit colour tells the groups apart; `HueGrouper.GroupBy` documents the default.
- **The classic style's autohinter is not reproduced.** Under the matplotlib themes a text is measured as Agg sets its glyphs at whole pixels (`TextLook.MeasuresLikeAgg`): every glyph's advance and ink rounded at the font's size rounded to whole pixels, an overshoot on its zone.
- **The core's own font metrics measure a line of text under every theme.** `DefaultFontMetrics` is a table of character widths with no glyphs, so it has no ink to round; with the Skia package installed the matplotlib themes measure by the glyphs of the bundled DejaVu Sans (`IGlyphInkMetrics`).

- **The MACD has no step of its own beside `EmaStep`.** The line is two subtractions (a SIMD one in the batch indicator) and the signal an `EmaStep`, which the batch and the streaming form already share; a step that held the two subtractions would run the SIMD pass bar by bar.
- **The batch stochastic does not fold `StochasticStep` over its bars.** The step run bar by bar costs three times what the extremes of every window in one pass, the raw %K of every bar and the means cost; the two compose the same three parts (`RollingExtremeStep`, `StochasticStep.RawK`, `RollingMeanStep`) and the streaming equality theory holds them equal over every prefix.
- **The ADX's +DI and -DI seed one bar apart from TA-Lib's.** Wilder sums the first period movements, TA-Lib period - 1 and smooths with the next; the gap is 0.6 % at the first value and 1e-9 by bar 299.
- **The RSI, ATR, OBV, Bollinger bands and Keltner channels follow Wilder, TA-Lib and the definition where pandas-ta differs.** On the 60 synthetic bars of the fidelity suite: pandas-ta's adjusted smoothing gives an RSI from bar 1 and an ATR from bar 13 where Wilder's seeded mean (TA-Lib's) starts at bar 14, and stands up to 25 RSI points and 0.058 of the ATR (1.7 %) from it.
- **A ring across a globe's horizon keeps an edge only where its two ends stand on different sides.** An edge with both ends on the far side that passes the near side between them is not shown: the shipped data has none, and an edge is followed in longitude and latitude, the shorter way round, as cartopy follows it, so such an edge is a long one.
- **The three area sums of a map stay three.** A ring's area and centre (`GeoAxesExtensions.Measure`), the flow cartogram's lattice areas and the marching squares of a contour each sum a shoelace over their own data shape (a ring of points with a centroid, the vertex arrays of a flow, a lattice from a relative origin); one function over all three would change the rounding of the contour's lines.
- **`ToSeriesDto` of the sixteen oldest series writes the label though the shared fields write it again.** `ToSeriesDto` is public, and a caller who asks it directly gets the series' label in the answer; the serializer's shared fields write the same value.

- **The theme, the spacing and the figure's colour bar are values `FigureBuilder` holds, not steps.** A later call edits what the earlier ones left, and the style is read only when no theme was given; as steps they would run again at every build.
- **A CSV ends its records with CRLF on every system.** RFC 4180 defines that line end, so it does not follow the platform's.
- **An SVG coordinate is written to two decimals.** A hundredth of a pixel is below what a screen or print shows, and the extra digits cost most of the writing time: a burst of figures took 26 % less processor time (5,495 to 4,057 ms) and seven stacked panels draw in 457 µs instead of 1.62 ms ([BENCHMARKS.md](../../BENCHMARKS.md#a-live-dashboard-drawn-again-and-again)).
- **A pixel is never computed with a fused multiply-add.** The browser script has no fused operation, and every path must give the same bits; the exact form costs about 50 ns at 1,000 points (343 against 294 ns) and nothing at 10,000.
- **The SVG writer keeps no memo of widths without the Skia package.** The default metrics read a table per character and do not shape: a label of the kinds an axis writes is measured in 5.6 ns (14 labels in 78.8 ns), and a kept width found by text and size takes 4.0 ns (55.4 ns for the 14).
- **A Y slot's band is measured twice, by the layout and by the draw.** The layout does not know the plot height yet and measures beside the densest ticks it can draw; the draw measures beside its real, coarser ticks.
- **`AxesRenderer` keeps its protected members, its factory table and its hooks.** They are surface of a public base class.
- **A renderer makes its colour-slot table when it first needs it and never again.** Every renderer the library makes is made for one use (a draw, a measure, a hit-test) and reads its axes as they stand then; a renderer kept by a caller across a change to the axes' series is made again, as `AxesRenderer.Create` does for every use.
- **`DirectLabelMetrics` makes the renderer the factory would draw with to ask whether it draws end labels.** It asks only when some series wants an end label, and the answer is the registered renderer's (`AxesRenderer.RegisterRenderer`), so it cannot be read from the axes without a second place that chooses the renderer.
- **A path segment a user derives is written through its `ToSvgPathData()` string.** The bytes are the same as for the library's own segments.
- **Radar rings stand at fifths of the axis under the library's own themes.** Under the matplotlib themes they follow the radial locator as matplotlib's polar rings do.
- **The library's own themes place the ticks of each kind of axis as they always have.** A 2-D axis by `AutoLocator`'s nearest step, a 3-D axis by ten MaxN intervals whatever its width (matplotlib's bar3d scene), a colour bar and the rings of a polar axes as a short ruler of two to nine ticks; the kind is one sealed leaf of the tick request (`TickAxisKind`), asked, never compared.
- **Each subplot is painted through a closure of its own.** One reusable painter saves about a kilobyte on a 3 x 3 grid (163,208 against 164,288 bytes) and no measurable time (30.0 against 30.1 µs).
- **What cannot pair in a regression, residual or other tolerant XY model is skipped and reported through `ChartDiagnostics`.** It is not refused at the verb, because the library reports unusable input in one place.
- **A Best legend scores its places without a spatial index.** A pick over a live-wall panel takes about 7 µs; a box per block of vertices would save about 3 % of the panel's draw for a second array per path, and a branchless count measured twice as slow.
- **A change is stamped from one process-wide counter (about 3 ns per change).** Per-thread blocks, per-draw marks or a plain increment could each let a changed figure be drawn stale.
- **A subplot's data range is worked out once per draw and not kept between draws.** The change stamps see a setter, not a value changed in place in a series' data, so a range kept past the draw would be drawn stale after such an edit.
- **A kept estimate (a density, a relative rotation) is made again on any change of its series, and on any value changed in place in its data.** The change mark sees a setter and the data copy sees an array edited in place; neither sees both.
- **Only a series that keeps an estimate keeps the copy of its data.** For any other series a kept copy would add eight bytes per series and a second pass over a long line edited at its end.
- **The MCP reader's strict pass reads number lists as System.Text.Json does.** A refusal then names the item to fix, which a converter cannot.
- **A draw does not install the locator a scale brings.** A figure drawn once stays the figure the caller built: same JSON, same next draw.
- **A ridgeline estimates its groups in turn.** In one run of 3,000 builds of eight groups of 300 values, estimating the groups at once took the wall time from 1.57 to 0.6 ms a build and raised the processor time of the process from 4.76 s to between 6.9 and 7.4 s (+46 to +55 %), and the processor time is what a host pays.
- **`AxisTransform.Power` is defined on 0 and above (above 0 for a negative exponent).** A value-to-value inverse cannot know which side of 0 a range lies on. A link made with `Of` is code and is not written to JSON. A series on a linked axis is refused by the builders.
- **A `SkiaFontMetrics` or `SkiaGlyphPathProvider` an application builds keeps its own shapings.** It is the application's object; sharing the package's store would tie its outlines to the installed ones.
- **A patch has no table beside the chart.** The accessible table lists data, and a patch is a drawing.
- **Members only a test reads stay where a test has no other window on what the library does.** The count of events a chart session dropped or lost and the last fault, the question whether a publisher or a server still holds a chart, whether a Blazor client has its connection id, the recordings a Skia painter keeps, the size of the shaping cache, the marks a draw collected (`MarkObstacles` boxes, paths and points), the length of a generated Unicode table.
- **Under the library's own themes:** the grid stands under patches; the minor grid stands where minor ticks would; a log or logit axis draws its minor marks only when they are switched on; minor ticks split each major step in five; a 3-D wall carries major lines only; the colour-bar extend is a 10 % rectangle; series text sizes are pixels.
- **The bilinear and the Hanning engine each make their own target grid.** Their `Resample(double[,], ...)` is the same five lines (make the grid, hand the core the two grids as spans, return it).

## Third-party notices

A package that carries someone else's files hands the recipient their licence with them. Two packages embed such files in
their assembly: `MatPlotLibNet.Skia` the four DejaVu Sans faces and DejaVu Sans Display (Bitstream Vera and Arev licence, kept as
`Fonts/LICENSE_DEJAVU`) and seven Computer Modern faces of the BaKoMa collection as matplotlib ships them (the BaKoMa licence, which
lets them be copied and distributed for any purpose with its notice, the author statement and the CTAN location, kept as
`Fonts/LICENSE_BAKOMA`), `MatPlotLibNet.Interactive` the browser client of SignalR (`signalr.min.js`, MIT). The `MatPlotLibNet.Mcp`
tool package carries its whole dependency closure: assemblies (MIT, Apache-2.0, BSD-3-Clause), the native SkiaSharp and HarfBuzz
libraries, the fonts and the map data. Each of the three packs a `THIRD-PARTY-NOTICES.md` beside its `LICENSE`. The Natural Earth
data of `MatPlotLibNet.Geo` is public domain and needs none. Two contract tests hold it:
`ThirdPartyNoticesContractTests` of the core suite (every `EmbeddedResource` of every package is the library's own or named with
its origin, the notices are packed and carry the licence text) and the one of the MCP suite (the notices name every package the
restore put in the tool, and everything below the marker line is the licence and notice files those packages ship, copied in
whole; `MPL_REGEN_NOTICES=1` rewrites it after a version moves).

## MatPlotLibNet.Skia package

This package renders PNG and PDF through SkiaSharp. It also provides the text pipeline that every backend shares once
the package is loaded: the module initializer installs `SkiaFontMetrics` on `FontMetricsDefault` and
`SkiaGlyphPathProvider` on `GlyphPathProviderDefault`, so an SVG rendered in the same process carries the same
glyph outlines that the PNG paints.

```
MatPlotLibNet.Skia/
  FigureSkiaExtensions.cs — [ModuleInitializer] Initialize()
  RasterRenderingExtensions.cs (RasterSize, nested in RasterRenderingExtensions) — internal readonly record struct
  RasterRenderingExtensions.cs — internal: the figure drawn onto a bitmap at a dpi — the one place a raster export
  PngTransform.cs (WithTextChunks) — internal: adds the Title and Author of SaveOptions to a finished PNG as iTXt chunks straight after its header
  SkiaFonts.cs — the font door. Register(path | Stream) → RegisteredFont(Family, Weight, Slant)
  TextShaper.cs — internal static: Shape(text, SKFont, SKShaper) → ShapedText
  ShapedText.cs — readonly record struct: Glyphs, Positions (visual order), Width
  ResolvedTypeface.cs — readonly record struct: Typeface + Shaper, never disposed by callers
  RegisteredFont.cs — public readonly record struct: what Register returned
  ShapedTextStore.cs — internal: Of(text, font | resolved face, size) → ShapedRun, kept by (resolved face and shaper, size, text)
  SkiaPathSink.cs — internal readonly record struct over an SKPath, Core's IPathSink: a move, a line
  ShapedRun.cs (a Skia path as SVG) — internal: AppendSvgPathData(sb, SKPath, dx, dy) writes a Skia path as SVG path data
  SkiaRenderContext.cs (the conversions) — internal: ToSkRect(rect)
  ShapedRun.cs — internal: one text shaped in one face at one size: Width, Ink
  SkiaFontMetrics.cs — IFontMetrics: Measure = a line (TextExtent.OfLine) of the kept shaping's Width
  FaceInk.cs — internal: how the glyphs of one face stand when matplotlib's Agg backend sets them
  SkiaGlyphPathProvider.cs — IGlyphPathProvider and Core's internal IShapedGlyphPathProvider: the kept shaping's Outline
  SkiaRenderContext.cs — IRenderContext over SKCanvas
  SkiaRenderContext.AggPixelGrid.cs — internal: the Agg arithmetic over a canvas: OnPixelGrid
  SkiaFrameTarget.cs — internal KeepingFrameTarget<SKPicture>: a subplot drawn into a recording under the canvas' TotalMatrix
  SkiaChartPainter.cs — internal: the painter one native control holds, in its MplChartSurface
  Fonts/DejaVuSans*.ttf — embedded, 4 faces and DejaVu Sans Display; Fonts/LICENSE_DEJAVU their licence
  Fonts/cm*.ttf — embedded, the 7 Computer Modern faces the math font sets read (not cmss10: no command sets \mathsf)
  THIRD-PARTY-NOTICES.md — packed beside LICENSE: the DejaVu and the BaKoMa licence texts, every embedded face named
```

Packaging: `SkiaSharp.HarfBuzz 3.*` follows SkiaSharp's version line and pins `HarfBuzzSharp 8.3.1.5`. The native
`libHarfBuzzSharp` comes from `HarfBuzzSharp.NativeAssets.{Linux,Win32,macOS}` at exactly that version; those packages
have no 3.x line and no `.NoDependencies` Linux variant. They are referenced directly by the two projects that have no
host application above them: the Skia test project and the MCP tool.

## MatPlotLibNet.Mcp package

This package is a Model Context Protocol server. It ships as a .NET tool (`PackAsTool`, `PackageType=McpServer`,
net10.0) that an MCP host starts over stdio. Only one file uses the MCP SDK; every other file is a plain class that a
test can drive without a protocol host.

```
MatPlotLibNet.Mcp/
  Program.cs — host + stdio transport + WithTools<ChartTools>(); no decisions, excluded from coverage
  ChartTools.cs — the only [McpServerTool] file: five tools, and the one error boundary
  ChartSpec.cs — readonly record struct ChartSpec(string Json) — a spec cannot be swapped with a path
  ChartSpecReader.cs — parse -> normalise -> strict DTO pass -> semantic pass -> ChartSerializer.FromJson
  AxesSeatShape.cs — where a subplot keeps its axes and series, read off the wire records
  ChartSchemaDescription.cs — describe_chart_schema: the fields of every figure record and the kinds of every described family
  ISeatNodes.cs — the one seat walk (ISeatNodes), over the JSON nodes (normalise) and the DTO records (validate)
  ChartSpecReader.SpecVocabulary.cs — the accepted values of every enum-valued spec field, per node kind
  ChartTypeCatalog.cs — SeriesRegistry.Discriminators minus the types whose reader ignores the document
  ChartRendering.cs — spec -> Figure -> bytes; one owner, two sinks (inline PNG, or a file)
  OutputPathResolver.cs — the single directory save_chart may write under
  ChartSummarizer.cs — FigureSummary / SeriesSummary via the series' own ComputeDataRange
  ChartTabulation.cs — chart_data_table: the figure's own ToDataTables(), in both forms
  ChartTables.cs — those tables as the wire vocabulary: header/kind/axis per column, rows of numbers, dates written out in full
  RenderLimits.cs — the canvas, text and table-row ceilings (the canvas ceiling IS the token ceiling; so is the row ceiling)
  FontDirectory.cs — the server's font door: Expand(MATPLOTLIBNET_FONTS) → files to register + problems to log
  ToolRefusalException.cs — a refusal the boundary turns into an McpException the model can act on
  ChartSpecReader.Colours.cs (NormaliseColours, AsColour) — the spelling of every colour of a spec: a CSS4 name becomes its hex
  Extensions/WireContractExtensions.cs — extension(Type): a figure record as the serializer reads it
  .mcp/server.json — the registry manifest; its version is pinned to the project's by a test
  THIRD-PARTY-NOTICES.md — packed beside LICENSE: the licence of every package, native library, font and data set the tool carries
```

### Why the package has its own reader

`ChartSerializer.FromJson` is a round-trip reader for the library's own writer, so it is lenient: an unknown series
type is dropped with a diagnostic, an unknown property is skipped by `System.Text.Json`, a misspelled enum name in a
text field is left out with a diagnostic (`WireEnum`), and an absent `width` overwrites the figure default with zero. That lenience gives wire
compatibility for the library's own clients. When a model writes the document, each of those cases turns a typo into a
blank picture and reports it as a success. So the package validates the document first, against the same DTO records
the serializer reads, and refuses it with the field name and the JSON path.

A subplot keeps its axes and its series at many seats: the primary pair, the top and right axes, and every Y axis slot of
either side (the frozen lists and their overflow lists). Every check of the reader (the chart-type catalog, the text limit,
the enum spelling, the order of a described value) must reach every seat, so none is written as a loop over a field name:
`AxesSeatShape` finds the seats once from the wire records, by member type (an axis record, a list of series, a list of
Y axis slots), and both the normalising pass (over the JSON nodes) and the validating pass (over the DTO records) walk it.
A seat added to the wire format is covered on the next build, and `describe_chart_schema` names the seats from the same
shape. A refusal names the seat's path (`$.subPlots[0].leftYAxes[1].series[0].type`). An inset is a subplot in turn.

Whether a series fits its own data (arrays that pair, a grid the size of its axes, a count of at least one) is the library's
rule, so the reader holds none: it builds the series through the registry on a scratch axes (`RefuseWhatTheLibraryRefuses`) and,
when the library refuses it, names the place from its own seat walk with the library's reason (`The 'surface' series at
$.subPlots[0].series[1] was refused: Array lengths must match. ...`). A rule the library gains is a rule here, at every seat.

A chart the library composes from several series travels as those series: none of the 38 chart verbs is a chart type of
this server, in its catalog, its served list or its vocabulary, and a spec that asks for one is refused like any unknown
type (ComposedVerbVocabularyTests holds that). A document the library writes for a composed chart is accepted whole, a
missing value as the string "NaN" (ComposedChartSpecTests runs every composed shape through the reader).

### Two process-wide behaviours this package has to handle

The package references `MatPlotLibNet.Skia` for PNG and PDF. That package's `[ModuleInitializer]` installs
`GlyphPathProviderDefault.Current` and `FontMetricsDefault.Current` the first time the runtime prepares a method that
names a Skia type. The constructor of `ChartRendering` forces that module constructor to run, so the first chart a process renders
is drawn the same way as every later one. The SDK forwards only `McpException.Message` to the model and replaces any
other exception with a generic line, so every refusal leaves the server through that one type.

## MatPlotLibNet.DataFrame package

A thin extension-method bridge. It passes `Microsoft.Data.Analysis.DataFrame` column names
into the core charting, indicator, and regression APIs. It adds no logic of its own; all data processing
happens inside the core library.

```
MatPlotLibNet.DataFrame/
  DataFrameColumnReader.cs — static: ToDoubleArray / ToStringArray — type-safe column materialisation
  DataFrameColumnExtensions.cs — Values (public): one named column as numbers; StringCol / HueCol / AnyCol (internal)
  DataFrameFigureExtensions.cs — Line / Scatter / Hist — the named columns handed to the core's one hue split
  DataFrameIndicatorExtensions.cs — Hlc / Hlcv / Ohlc / Cv / Hl / Hlv: the bars of each shape an indicator reads
  DataFrameNumericsExtensions.cs — PolyFit / PolyEval / ConfidenceBand — delegates to LeastSquares
```

### Column resolution

Every extension method reads a column through `DataFrameColumnExtensions` (`Values`, and the internal `StringCol` and `AnyCol`),
which throws `ArgumentException` with the column name in the message when a column is not found.
All column access goes through `DataFrameColumnReader.ToDoubleArray` / `ToStringArray`.

### Indicators

The package states nothing per indicator. An indicator is made from its own class, which says its parameters, their defaults
and what it computes; the package reads its input from named columns: `df.Values(column)` for an indicator over one column
(`new Sma(df.Values("close"), 20)`) and one call per shape of `OhlcvBars` (`df.Hlc`, `Hlcv`, `Ohlc`, `Cv`, `Hl`, `Hlv`) for an
indicator over bars (`new Atr(df.Hlc("high", "low", "close"))`). `IndicatorSurfaceContractTests` of the package holds its public
indicator surface to those six shapes and the package description to their names.

### Regression bridge

`DataFrameNumericsExtensions` wraps the `LeastSquares` static methods:
`PolyFit(xCol, yCol, degree)` returns the coefficients; `PolyEval(xCol, coeffs)` returns the fitted Y;
`ConfidenceBand(xCol, yCol, coeffs, evalX, level)` returns `ConfidenceBand(Upper[], Lower[])`.

## MatPlotLibNet.Geo package

Map projections, Natural Earth 110m data and the layers of a map axes. Core exposes its internals to this package
(`InternalsVisibleTo` in core's `Properties/AssemblyAttributes.cs`), so the map series overrides the same internal capabilities
as a series of core (`ProvidesColorBar`, the data range's sticky edges) and reads `SeriesDto.PackageData`.

```
MatPlotLibNet.Geo/
  GeoAxesExtensions.cs — the verbs of a map axes (listed in "Where a verb lives")
  GeoAxesExtensions.H3Cells.cs — internal: the cells of Uber's H3 index through the managed port pocketken.H3
  GeoAxesExtensions.ProjectedRegions.cs — internal: a region's area and centre, resizing (Cartogram, Dorling) and the flow cartogram
  GeoPackage.cs — internal: the [ModuleInitializer] that registers the reader (GeoPolygonSeries.RegisterReader)
  GeoFlow.cs — public readonly record struct (From, To, Value): a quantity that moves between two places
  Series/GeoPolygonSeries.cs — the map series: Features, Projection, Color, StrokeColor, StrokeWidth, Values
  Series/GeoPolygonRenderer.cs — internal: draws a map through the library's series pass
  Series/GeoPixelMapping.cs, GeoMapData.cs — internal: a ring as pixels; the map data a series carries in its JSON
  GeoJson/ — GeoFeature, GeoPolygon and GeoPosition, GeoJsonReader / GeoJsonWriter, GeoGeometryKind (one sealed kind per type)
      and GeoClipping (SplitAtDateline, the one cut of a ring at the antimeridian)
  Projections/ — IGeoProjection and the 13 projections (one file each, PlateCarree to TransverseMercator)
    GeoBounds.cs, GeoBoundsExtensions.cs — the bounds of a map, as cartopy states them
    ProjectionParameterSet.cs, IProjectionParameters.cs — internal: the PROJ-named numbers a projection is made with
    CentredFrame.cs, CentredPlace.cs — internal: the centre of an azimuthal projection, and a place seen from it
    ProjectedPointExtensions.cs, GeoProjectionExtensions.cs — internal: a place off the domain; Make(name, parameters)
    IProjectionHorizon.cs, RingPieces.cs — internal: the horizon of a globe and the cut of a ring at it, as cartopy cuts
    IProjectionOutline.cs, ProjectionOutlineExtensions.cs — internal: the outline of a map (cartopy's boundary)
  Extensions/AngleExtensions.cs — the package's own ToRadians and ToDegrees ("Left out on purpose")
  Data/NaturalEarth110m.cs — Coastlines(), Countries() and BoundaryLines(), read once from the embedded resource
```

A map travels through JSON whole: `ToSeriesDto` writes the shared fields (colour, edge colour, line width, values, colour map,
normaliser) and `PackageData`, and the reader registered by the package rebuilds it. A figure that holds a map and is read by a
core that does not know the series drops it with a diagnostic, as for any unknown kind. The Geo test project's
GeoDrawnPropertiesRoundTripTests holds every settable property of the map series to the round trip, as the contract tests of core
do for core's series.
