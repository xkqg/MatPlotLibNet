# MatPlotLibNet Core -- Architecture (v1.24.0)

## Package dependency graph

```
MatPlotLibNet (Core)                      net10.0 + net8.0
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
    +-- @matplotlibnet/angular (npm)      @microsoft/signalr + Angular
    |
    +-- @matplotlibnet/react (npm)        @microsoft/signalr + React 19
    |
    +-- @matplotlibnet/vue (npm)          @microsoft/signalr + Vue 3
```

## Repository-wide build files

```
Directory.Build.props             imported BEFORE every project: authorship, the repository URL, deterministic
                                  builds, Source Link, EnableWindowsTargeting for non-Windows hosts, and the
                                  package icon all fifteen packages carry
icon.png                          the 128x128 package icon, at the repository root so the props file above can
                                  pack it into every package. Drawn BY the library — sample 43 of the console
                                  sample generator renders it — so the mark cannot drift from what the library
                                  actually produces. ReleaseContractTests checks the format, the size and the
                                  1 MB ceiling nuget.org enforces at push time
Directory.Build.targets           imported AFTER every project, so the version it declared is already known.
                                  Holds the one release-notes sentence all fifteen packages carry, with the
                                  version and the changelog anchor built from that version. Fifteen hand-kept
                                  copies would drift the first time one was forgotten
```

## Core library structure

```
MatPlotLibNet/
  Plt.cs                              entry point: Plt.Create() / Plt.Figure()
  ChartServices.cs                    five separated static DI defaults (ChartSerializerDefault, ChartRendererDefault,
                                      SvgRendererDefault, FontMetricsDefault, GlyphPathProviderDefault), each a single
                                      replaceable Current slot
  DisplayMode.cs                      enum: Inline, Expandable, Popup
  IChartSubscriptionClient.cs         shared SignalR client contract
  SaveOptions.cs                      sealed record: Dpi, PrettifySvg, SvgDecimalPrecision, Title, Author — Figure.Save(path, options); each
                                      property says which format reads it, and a format that cannot use one leaves it alone
  SaveOptionsExtensions.cs            internal: the rules that read SaveOptions in one place (DpiFor: the options' resolution unless it is
                                      the default, else the figure's own; TitleOrNull, AuthorOrNull), so every export asks the same question

  FigureTemplates.cs                  pre-built layouts: FinancialDashboard(), ScientificPaper(), SparklineDashboard(), JointPlot(), PairPlot(), FacetGrid(), Clustermap()

  Plt.Style                           nested static class: Use(), Context() for rcParams configuration

  Builders/
    FigureBuilder.cs                  fluent API: Plt.Create().WithTitle().Plot().Build() (build only)
    AxesBuilder.cs                    subplot config: WithTitle(), SetXLabel(), Plot(), Scatter(), Threshold() (ref-line+breach-span+label), WithLegendValues() (last-Y in legend), etc.
                                      The Y axes are the container's (Models/YAxisContainer.cs): WithAddRightYAxis(configure) and
                                      WithAddLeftYAxis(configure) stack ONE more Y axis on their side per call, no cap, nothing to
                                      count; WithYAxisTitleGap(pixels) sets the container's gap. The two verbs share one body
                                      (AddYAxis) and take a required callback; SetYLabel/SetYLim/SetYAxisColor act on the primary
                                      axis, left slot 0. The older verbs WithSecondaryYAxis (the first right axis, created when there
                                      is none, never stacking) and WithOutwardYAxis (one more axis on the side it is given) are
                                      [Obsolete] wrappers over the container
    YAxisSlotBuilder.cs               ONE builder for any Y axis, either side: SetYLabel/SetYLim/SetYAxisColor/Plot/Scatter/StreamingPlot/
                                      WithDirectLabel, over one YAxisSlot. The callback of AxesBuilder.WithAddRightYAxis/WithAddLeftYAxis
                                      receives it; the builder's own Y verbs are it over the primary axis (left slot 0), so a setting
                                      is made the same way on every Y axis. SetYAxisColor colours the slot's ticks, tick labels and
                                      title, and the spine of its edge for slot 0 of a side only
    CategoryTickExtensions.cs         internal: SetXCategoryTicks(names) / SetYCategoryTicks(names, topDown) — categories written as
                                      an axis's ticks: a FixedLocator on each slot centre (CategorySlot) and the CategoryFormatter
                                      that reads the name there, made as one pair (names.AsCategoryTicks() → CategoryTicks) so
                                      positions and names cannot disagree; both built-ins, so the ticks survive JSON. Read by
                                      Clustermap, the composed verbs, and CartesianAxesRenderer, which names the categories its
                                      series stand on with the same pair, through that axis' tick path (upright bars, a count
                                      plot and a candlestick's dates along X; bars that lie down along Y). Also names only
                                      some slots (the months over a calendar's week columns) and names at positions of their
                                      own (the centres of a Marimekko chart's columns)
    CategoricalAxesExtensions.cs      the 17 chart verbs, four static classes in namespace MatPlotLibNet (see "Where a verb
    DistributionAxesExtensions.cs     lives"): extension methods on AxesBuilder that compose a chart from series that already
    XYAxesExtensions.cs               exist. Categorical: Lollipop, Dumbbell, Pareto, SlopeGraph, DotPlot, ParallelCoordinates,
    GridAxesExtensions.cs             Marimekko. Distribution: Ridgeline, QQPlot, KaplanMeier. XY: BumpChart, ControlChart,
                                      StreamGraph. Grid: CalendarHeatmap, Spy
    DumbbellParts.cs                  what a verb's callback receives when it draws several series types: a readonly record
    ParetoParts.cs                    struct with an internal constructor and get-only properties holding the very series on
    QQPlotParts.cs                    the axes. Dumbbell (Connector, From, To), Pareto (Bars, Cumulative), QQPlot (Points,
    ControlChartParts.cs              Reference), ControlChart (Values, Violations, Center, Lower, Upper)
    ControlLimits.cs                  readonly record struct (Center, Lower, Upper): the limits ControlChart draws as given
                                      instead of estimating them
    (AxesBuilder.cs)                  SecondaryAxisBuilder and OutwardYAxisBuilder, the two builders 1.23.0 published for a second Y
                                      axis, are [Obsolete] and hold only a YAxisSlotBuilder: every verb forwards to it, so the old
                                      and the new builder cannot differ. Kept so code written against 1.23.0 still compiles
    ThemeBuilder.cs                   custom themes: Theme.CreateFrom().WithFont().WithName().WithAlarmPalette().Build()
                                      (v1.14: Build() CLONES the base theme — it used to rebuild from 8 of 15
                                      properties and silently reset the rest)
    OpsDashboardBuilder.cs            Plt.OpsDashboard(): tiles + timelines + topology panels + trend panel
                                      on ONE pinned time window; the CALLER supplies the clock
                                      (WithWindow(end, span)) — the library never reads DateTime.Now. Row
                                      ratios are filled by ONE running cursor, so no row can inherit another's
                                      height; a panel appended after the trend would otherwise have taken the
                                      trend's ratio and left it at zero, drawing nothing and reporting nothing

  Extensions/
    FigureExtensions.cs               Save(), Transform(), ToSvg(), ToJson(), RegisterTransform(),
                                      AccessibleName() (alt text -> title -> tile labels; the SVG <title> and
                                      the data-table caption read ONE rule, not two)
                                      (SRP: all output responsibility moved here from FigureBuilder)
    FigureDataTableExtensions.cs      ToDataTables() on Figure and on Axes: the figure's data as tables, for the
                                      reader who cannot see the picture. Series sharing an x VALUE merge into
                                      one table; the rest each get their own; neighbouring subplots that are the
                                      same single row (a KPI tile row) merge into one table with a row each;
                                      every positional column is resolved against the axis it is a position on.
                                      KeyedByName: a column of positions the axis names (a named tick on every
                                      one, AxesTickSourceExtensions.NamesAt) is read as those names and is the
                                      table's key, under the axis title or "category": the x column, or a y
                                      column the Y axis names, moved in front (a dot plot). A series whose rows
                                      its columns cannot place (ITableNamedByAxes) tables itself with AxisNames
    ArrayLengthExtensions.cs          internal: ThrowIfMisaligned — the one check, in one wording ("Array lengths must match. Got 3 and 2."; for a
                                      grid "... The values have R rows and C columns; the axes give Y y and X x values."), that a verb makes of the
                                      arrays of a series BEFORE it constructs it. A series is refused at its verb, and a figure read from JSON builds
                                      each series through its verb, so a document whose arrays do not pair is refused when it is read (an
                                      ArgumentException) and never at the first draw. The MCP reader asks the library and holds no copy of the rule.
                                      RegressionSeries and ResidualSeries keep their tolerance: what does not pair is skipped and reported
                                      through ChartDiagnostics
    MissingValueExtensions.cs         internal: NaN is the one missing value, and only a finite value can be drawn. IsDrawable,
                                      DrawableRange (a series' data range, over what can be placed on its axis), DrawableRuns
                                      and DrawableIndexRuns (a line breaks at every point that cannot be drawn),
                                      DrawableValues (the observations a box, a violin, a density or a fit is made from),
                                      IsDrawableCell (a contour masks a cell with a missing corner). A colormap, a renderer and
                                      a data range ask here and none tests double.IsNaN on its own, so no render context is
                                      handed a coordinate it cannot draw. A base the scale cannot place (0 on a log axis) is
                                      drawn at the edge where the axis begins (DataTransform.DataToPixelClippedY), and a span
                                      is cut at the plot edge

  Models/
    Figure.cs                         top-level container (Title, Width, Height, Dpi — sizes a PNG or a GIF frame: a unit of the layout is dpi / 96 pixels, Theme, EnableZoomPan, EnableLegendToggle, EnableRichTooltips, EnableHighlight, EnableSelection, HasInteractivity)
    Axes.cs                           subplot: series, annotations, ref lines, spans, the Y axis container (YAxes), the top X axis,
                                      insets, ShareX/ShareY, EnableInteractiveAttributes.
                                      YAxis is YAxes.Left[0].Axis and Series is YAxes.Left[0].Series: the primary Y axis is the first
                                      axis of the left side, held in no list of its own.
                                      AllSeries = every Y axis' series in YAxisContainer.IdentityOrder (the primary's, every right
                                      slot's, every left slot's past the primary) + the top X axis' — the one answer to "what does
                                      this subplot show", and the order SeriesSlotExtensions.SlotIndex counts through. The order is
                                      an identity contract (data-series-index, a legend toggle's target, the default colours), so it
                                      never follows the order slots were added in.
                                      The 1.23.0 shapes are [Obsolete] accessors over the container, kept so old code compiles:
                                      SecondaryYAxis/SecondarySeries (right slot 0), RightYAxes/LeftYAxes (live views of the stacks,
                                      indexed past the primary), TwinX() (FirstOrAdd on the right stack), AddOutwardYAxis(side) (Add
                                      on that side's stack, no cap) and AddSecondarySeries (a series on right slot 0)
    Axis.cs                           label, min/max, scale, ticks, Inverted (drawn high to low: Range1D.OrientedBy, the rule a
                                      limit set high to low follows; a state like matplotlib's set_inverted, never a second turn)
    YAxisContainer.cs                 every Y axis of an Axes: Left and Right, one YAxisStack per side, and TitleGap (4 by default,
                                      matplotlib's axes.labelpad; set with AxesBuilder.WithYAxisTitleGap; negative, NaN or infinite is
                                      refused). TitleGap is ONE value for the whole subplot: the blank gap between a Y title's ink and
                                      the ink of the widest tick label beside it, on both sides and for every slot, and the gap
                                      between one slot and the one stacked past it. IdentityOrder is the numbering order above
    YAxisStack.cs                     the Y axes of ONE side, in stack order, 0 nearest the plot. Add() stacks one more (no cap),
                                      FirstOrAdd() returns slot 0 and creates it only when there is none (it never stacks).
                                      The left stack is born holding the primary Y axis (OutwardOffset 1), the right stack empty
                                      (0). SlotAtOutward(i) is the ONE conversion between the numbering of the container and the
                                      outward numbering the interaction wire and the 1.23.0 JSON fields use (see Interaction/ below)
    YAxisSlot.cs                      sealed class (Axis, Side, Series, AddSeries): one Y axis and the series plotted against it.
                                      A class, held by the stack, the builders and the renderer alike, so a setting made through one
                                      reaches the others. Stack rank is the slot's position in its stack, nothing stores it twice
    SecondaryAxisSlot.cs              [Obsolete] readonly record struct: a value view of a YAxisSlot, what the obsolete RightYAxes and
                                      LeftYAxes lists hand out (SecondaryAxisSlotView is their live list). Two views of one slot are equal
    AxisSide.cs                       enum: Left, Right (ordinals pinned, EnumOrdinalContractTests) — which side a Y axis stands on
    AxisColorExtensions.cs            Axis.ApplyAxisColor(color): colours an axis' tick marks, tick labels and title; Axes.ApplyAxisColor(axis,
                                      SpineEdge?, color): the ONE rule every colour verb calls (SetYAxisColor/SetXAxisColor on AxesBuilder,
                                      SetYAxisColor on YAxisSlotBuilder, SetXAxisColor on the top-X builder) — it also colours the spine
                                      on the axis' edge (slot 0 of a side, the bottom X axis, the top X axis), or none for a Y axis
                                      stacked past slot 0 of its side, which draws no spine. The colour of every axis lives on its own
                                      Axis (tick marks, tick labels, title), so it reaches the JSON through the axis' own fields
    Annotation.cs                     text annotation: Text, X, Y, ArrowTarget, Font (null = the theme's data text font), Alignment,
                                        VerticalAlignment (TextVerticalAlignment: Baseline default, Center, Top, Bottom), OffsetX/OffsetY
                                        (screen pixels, y down, applied before the text is placed), AvoidOverlap (opt in to AnnotationLayout),
                                        Rotation, ArrowStyle, BackgroundColor
    ValueObjects/OpsCondition.cs      what an operations reading is in, on TWO axes that must never be one:
                                        enum OpsSeverity {Normal, Warning, Critical} — the only one that
                                          compares; a roll-up takes the worst
                                        enum OpsVisibility {Observed, Unknown, Shelved} — whether the reading
                                          can be believed and whether someone silenced it; never ranked
                                        readonly record struct OpsCondition(Severity, Visibility) — Resting,
                                          Unknown, Shelve(), RollUp(children) -> OpsRollUp
                                        readonly record struct OpsRollUp(Condition, Observed, Unknown, Shelved)
                                          — worst SEEN child decides; unseen and muted are counted, not ranked
                                        readonly record struct OpsMark(Accent, Hatch) — the form a condition
                                          takes, resolved by Styling/OpsConditionExtensions against AlarmPalette
    ArrowStyle.cs                     enum: None, Simple, FancyArrow
    ReferenceLine.cs                  horizontal/vertical reference line (AxHLine, AxVLine)
    SpanRegion.cs                     shaded horizontal/vertical region (AxHSpan, AxVSpan)
    GridSpec.cs                       unequal subplot layout: Rows, Cols, HeightRatios, WidthRatios
    InsetBounds.cs                    inset position: X, Y, Width, Height (axes-fraction coordinates);
                                      canonical form for Axes.AddInset / AxesBuilder.AddInset — the
                                      double-argument overloads forward here (v1.13.0)
    SpinesConfig.cs                   per-spine visibility/position: Top, Bottom, Left, Right
    ChartDataTable.cs                 the accessible alternative to the picture: Caption, Columns, Rows, and
                                      ToHtml() / ToMarkdown() / ToCsv() (HTML and markdown cap at
                                      DefaultMaxRows = 1000 with a "... N more rows" trailer; CSV never caps)
    AxisNames.cs                      internal readonly record struct (Axes, OnPrimaryX, OnPrimaryY): the names an
                                      axes shows along the axes ONE series stands on (Along(axis, positions));
                                      a series on a secondary axis is named along neither
    ChartDataColumn.cs                readonly record struct (Header, DataColumnKind, DataAxis) — the kind
                                      decides how a cell PRINTS; the axis says WHOSE coordinate it is, so the
                                      composition can turn a position on a date axis into a date
    DataCell.cs                       readonly record struct: Empty | Number | Text. A jagged row is a real
                                      shape (an OHLC row has no volume) and an empty cell says so without a
                                      sentinel number

    Series/                           83 series types across 14 categories (Streaming is one of them)
      ISeries.cs                      interface: Label, Visible, ZOrder, Accept(), ToDataTable() (default
                                      interface method returning null — a series with no tabular form, like a
                                      quiver key, says so by not overriding it)
      ISeriesSerializable.cs          interface: each series serializes itself (eliminates SeriesToDto switch)
      IHasDataRange.cs                interface: series that expose their own data bounds
      IPolarSeries.cs                 interface: polar coordinate series
      I3DGridSeries.cs                interface: 3D grid-based series (Surface, Wireframe)
      I3DPointSeries.cs               interface: 3D point-based series (Scatter3D, Stem3D, Bar3D)
      IPriceSeries.cs                 interface: financial OHLC series (Candlestick, OhlcBar)
      IColormappable.cs               interface: ColorMap { get; set; } — polymorphic colormap assignment
      INormalizable.cs                interface: Normalizer { get; set; } — polymorphic normalizer assignment
      ICategoryLabeled.cs             interface: CategoryLabels for polymorphic tick-label rendering, on the axis that carries the
                                      categories (X, or Y for bars that lie down). Bars, a count plot, a candlestick and a
                                      waterfall implement it, and name their categories on the slot centres (CategorySlot)
      ITableNamedByAxes.cs            internal interface: ToDataTable(AxisNames) — a series whose rows stand on the axes in a way its
                                      columns cannot say as positions tables itself with the names the axes shows there
                                      (HeatmapSeries: its rows and columns; AreaSeries: a fill down to a named level, a ridge)
      IHasBarOrientation.cs           internal interface: BarOrientation Orientation (BarSeries, CountSeries) — which axis their
                                      categories stand along (BarOrientationExtensions)
      IColorBarDataProvider.cs        interface: GetColorBarRange() + ColorMap for colorbar auto-detection; a provider that is a plain fill answers
                                      ChartSeries.ProvidesColorBar = false and the bar is backed by the series that has values
      IStackable.cs                   interface: Values a stacked render sums (on two running sums, see StackableExtensions) + StackBaseline, the foot a user sets (a stacked render keeps its own stacking as render state, never in this field)
      IHasColor.cs                    interface: Color? Color — polymorphic color access (~20 series)
      IHasAlpha.cs                    interface: double Alpha — polymorphic alpha access (~7 series)
      IHasEdgeColor.cs                interface: Color? EdgeColor — polymorphic edge-color access (~3 series)
      ILabelable.cs                   interface: bool ShowLabels; string? LabelFormat — series with data-point labels
      IHasLabelPlacement.cs           interface: SeriesLabelPlacement LabelPlacement { get; set; } + internal LinePoints() — the
                                      series that can show their name at the line end (LineSeries, StreamingLineSeries). The
                                      series only SAYS where its name goes; the renderer of the axes decides whether it can
      SeriesLabelPlacement.cs         enum: Legend = 0 (default), LineEnd = 1 — ordinals are contract, members only appended
      LinePoints.cs                   internal readonly record struct: one consistent snapshot of a line's x and y values, so an
                                      end label is placed at the end of the line that is drawn
      ChartSeries.cs                  abstract base: implements ISeries + ISeriesSerializable; declares the
                                      virtual ToDataTable() 63 types override — 8 family bases plus 55 leaves (a class cannot
                                      override a default interface member — the base must own the virtual).
                                      A series also SAYS what it is, through internal virtual capabilities, so the renderers, the legend and the
                                      layout ask it and hold no list of types: DrawsOwnLabel, FixesYRange, DrawsWithoutAxes (its marks stand where
                                      it puts them whatever the limits are — radar, pie, donut, funnel, gauge, progress bar, bullet graph, stat
                                      tile, sparkline, Sankey, treemap/sunburst/dendrogram, table, tree grid, clustermap, pair grid — so the axes
                                      draws no grid, ticks or frame when every series on it says so; AxisLessCapabilityContractTests measures each
                                      answer against the limits), LegendSwatch and LegendParts (what the legend draws for it, an entry for each
                                      part; LegendSwatchCapabilityTests), ColorSlotSpan (how many slots of the colour cycle it holds),
                                      ProvidesColorBar and RadialExtent (how far out it reaches on a polar axes). A package that has
                                      InternalsVisibleTo (Geo) overrides them like a series of core
      LegendSwatch.cs                 internal: enum LegendSwatchKind (Patch, Line, LineMarker, SolidLine, Marker, ErrorBar, Stripes) + the readonly
                                      record struct LegendSwatch(Kind, LineWidth, LineStyle, RestingLineWidth, Marker, MarkerSize, States): how one
                                      series is drawn in its legend entry. The series states its own values, the slot's cycler decides the colour
                                      and AxesRenderer.DrawLegendSwatch draws the shape
      LegendPart.cs                   internal readonly record struct (Label, Swatch, ColorSlotOffset): one entry of a legend. ChartSeries.LegendParts
                                      is one under the series' label unless a series names its own parts (a stack plot's layers)
      RadialExtent.cs                 internal readonly record struct (Max, Sticky): how far out a series reaches on a polar axes — the input of
                                      RadialScale; Sticky = exactly (a polar heatmap's cell edge), not with the 5 % margin
      XYSeries.cs                     generic base: XData, YData, MaxDisplayPoints (Line, Scatter, Step, Area, ErrorBar, Bubble, Sparkline, Stem, Ecdf)
      OhlcSeries.cs                   financial base: Open, High, Low, Close, DateLabels, UpColor, DownColor, PriceData (Candlestick, OhlcBar)
      DatasetSeries.cs                distribution base: Datasets[][], default ComputeDataRange (Stripplot, Swarmplot, Pointplot, Box, Violin;
                                      Box and Violin override it to range over where they are drawn, see DatasetPositionExtensions)
      PolarSeries.cs                  generic base: RData, ThetaData (PolarLine, PolarScatter, PolarBar); states its RadialExtent (the largest radius
                                      that can be placed) and never scales itself — the scale is the axes' (RadialScale)
      GridSeries3D.cs                 generic base: XData, YData, ZData[,] (Surface, Wireframe)
      HierarchicalSeries.cs           abstract base: Root, ColorMap, ShowLabels, IColormappable (Treemap, Sunburst, Dendrogram)
      LineSeries.cs                   XYSeries: Color, LineStyle, LineWidth, Marker
      ScatterSeries.cs                XYSeries: Color, MarkerSize, Sizes[], Colors[], Alpha (the opacity of the marker fill), IColormappable
      BarSeries.cs                    Categories, Values, Color, Orientation, BarWidth, StackBaseline, ShowLabels, LabelFormat, ICategoryLabeled, IStackable;
                                      internal SlotOf(i): where bar i stands in the current render (renderer and data range read it)
      CategorySlot.cs                 internal readonly record struct (Left, Centre, Right): the one rule for where a category sits —
                                      category k owns [k, k + 1], centred CentreOffset (0.5) past k; Bar(width, offset, align) is the
                                      bar standing in it, Around(x, width) a bar on a numeric axis. Read by the bar and count
                                      renderers and data ranges, the category labels, the candlestick body, the indicator
                                      shortcuts and the category ticks (Categorical/)
      BarOrientationExtensions.cs     internal: the one rule for which axis bars stand their categories along — CategoriesAlongY()
                                      (Horizontal lies down), OnAxes(range, orientation) puts a range measured upright (categories
                                      as X, values as Y) on the axes (X and Y swapped, sticky edges with them, for bars that lie
                                      down), NamesCategoriesAlongY(series), Transposed — a range with X and Y swapped, sticky
                                      edges with them — and ValueScale(context), the scale of the axis the values run along, which
                                      the bar and count ranges fold through. Read by BarSeries, CountSeries and (a box that lies
                                      down) BoxSeries ranges and by CartesianAxesRenderer (which axis names the categories) (Categorical/)
      StackableExtensions.cs          internal: StackedFeet(layers) / StackedEnds(layers) — the one rule for stacking bars, on TWO running sums:
                                      in each column a layer stands on the sum of the layers before it that point the same way, so the layers
                                      at or above zero grow up from zero and the layers below zero grow down from it (a diverging stack, a Likert
                                      chart); a zero goes with the positives, a value that is not finite adds to neither sum; the columns are the
                                      first layer's. An overload over plain columns (IReadOnlyList&lt;double[]&gt;) serves a stack the series model
                                      does not hold: the polar bars (RadialScale). CartesianAxesRenderer stands each layer on its feet
                                      (StackedBottoms) and the stacked bar range takes in every foot and end, a falling layer's too (Categorical/)
      BarGroup.cs                     public readonly record struct (Label, Values): a name and one value per place — the named-values
                                      type of GroupedBar and of seven verbs (Dumbbell, SlopeGraph, DotPlot, ParallelCoordinates,
                                      Marimekko, BumpChart, StreamGraph); its summary names them and a test keeps that list equal
                                      to the public API (Categorical/)
      BarGroupExtensions.cs           internal: ThrowIfMisaligned(count, paramName, places) — the one check every verb that takes a
                                      BarGroup makes: one value per place (category, dimension, column or x position), refused in
                                      the same words everywhere, and a group without values refused by name (Categorical/)
      CategoryValuesExtensions.cs     internal: the same check for a plain double[] (Lollipop, Pareto, ControlChart, CalendarGrid) (Categorical/)
      HistogramSeries.cs              Data, Bins, Color, Alpha, Density, Cumulative, Weights, ComputeBins(); internal Heights(bins), the
                                      heights the bars are drawn at, read by the renderer and by the data range
      PieSeries.cs                    Sizes, Labels, Colors[] (only what the user names; a wedge without one wears the theme's colour at
                                      its position, a missing slice counts as 0 in the total), StartAngle
      HeatmapSeries.cs                Data[,], ColorMap, Normalizer, ShowLabels, LabelFormat, MaskMode, CellValueColor, IColormappable, INormalizable, ILabelable, IColorBarDataProvider,
                                      ITableNamedByAxes; RowSlot/ColumnSlot: the one owner of where a cell stands (row 0 on top), read by
                                      its renderer and by its table, which writes a row or column the axis names as that name
      HeatmapMaskMode.cs              enum: None, UpperTriangle, LowerTriangle, UpperTriangleStrict, LowerTriangleStrict + HeatmapMaskModeExtensions.Hides(row,col)
      ImageSeries.cs                  Data[,], ColorMap, Normalizer, VMin, VMax, IColormappable, INormalizable, IColorBarDataProvider (Grid/)
      Histogram2DSeries.cs            X[], Y[], BinsX, BinsY (each at least 1), ColorMap, Normalizer, IColormappable, INormalizable, IColorBarDataProvider (Grid/)
      BoxSeries.cs                    DatasetSeries: Color, MedianColor, ShowOutliers, ShowMeans, Whis, Positions, Widths, Vert; overrides
                                      ComputeDataRange to cover each box at its position plus and minus half its width, turned onto
                                      the axes when the box lies down; internal HalfWidth
      ViolinSeries.cs                 DatasetSeries: Color, Alpha, ShowMeans, ShowMedians, ShowExtrema, Positions, Widths, Side;
                                      overrides ComputeDataRange to cover what is drawn on each side of a position (a half violin
                                      makes no room on the side it does not draw); internal ExtremaHalfWidth, StatsHalfWidth,
                                      BodyLeft, BodyRight, ReachAround
      DatasetPositionExtensions.cs    internal: PositionOf(index) — where a dataset stands (its Positions entry, otherwise its
                                      index) — and CoverAlongPositions — the range that follows, so the picture and the axes cannot
                                      disagree about where a box or a violin is; a series that sets no Positions keeps its usual
                                      room around 0 to n-1 (Distribution/)
      ViolinBodyExtensions.cs         internal: Outline(sorted, widths) → ViolinSlice(Y, HalfWidth)[] — the one place the width of a
                                      violin's body at each height is decided, read by the renderer and by the range (Distribution/)
      NamedSample.cs                  public readonly record struct (Label, Values): a sample of any length with a name, the group
                                      type of Ridgeline (Distribution/)
      NamedSampleExtensions.cs        internal: Observations / SortedObservations — the observations of a sample that can be drawn
                                      (NaN and infinities left out) and one wording for refusing a sample with too few (Distribution/)
      SurvivalGroup.cs                public readonly record struct (Label, Durations, Observed): one group of survival data, the
                                      group type of KaplanMeier (Distribution/)
      SurvivalGroupExtensions.cs      internal: ProductLimit() — the product-limit estimate of a group, and the check its data is
                                      held to (a missing, negative or infinite duration is refused); returns SurvivalCurve (Times,
                                      Survival) (Distribution/)
      ContourSeries.cs                XData, YData, ZData[,], Levels, LevelValues, Filled, ShowLabels, LabelFormat, LabelFontSize, ColorMap, IColormappable, IHasDataRange; Filled paints through ContourBandFillExtensions, as ContourfSeries does
      ContourfSeries.cs               XData, YData, ZData[,], Levels, LevelValues, Alpha, ShowLines, LineWidth, ColorMap, Normalizer, IColormappable, INormalizable, IColorBarDataProvider (Grid/)
                                      LevelValues are the levels drawn when named (ascending, each once); Levels is the count otherwise
                                      (Rendering/Algorithms/ContourLevels.cs is the one place that says which)
      CalendarGrid.cs                 internal: a span of days laid out as a calendar and the one owner of where a day stands — 7 rows by
                                      one column per week, the week start the caller's (never the culture's), a day's cell the sum of its
                                      values, a day without a value NaN (blank); RowOf, ColumnOf, WeekdayNames, MonthColumns. A year is
                                      53 columns wide, 54 when it has 366 days and starts on the last day of a week (Grid/)
      StemSeries.cs                   XYSeries: MarkerColor, StemColor, BaselineColor
      AreaSeries.cs                   XYSeries: YData2 (fill between), Alpha, FillColor; ITableNamedByAxes: a fill down to one level
                                      the Y axis names (a ridge) tables x and its height above that level under its label or the name
      StepSeries.cs                   XYSeries: StepPosition (Pre/Mid/Post)
      ErrorBarSeries.cs               XYSeries: YErrorLow/High, XErrorLow/High, CapSize
      CandlestickSeries.cs            OhlcSeries: BodyWidth, ICategoryLabeled
      QuiverSeries.cs                 XData, YData, UData, VData, Scale, ArrowHeadSize
      RadarSeries.cs                  Categories, Values, FillColor, Alpha, MaxValue
      DonutSeries.cs                  Sizes, InnerRadius, CenterText (Circular/)
      BubbleSeries.cs                 XYSeries: Sizes, Alpha (XY/)
      OhlcBarSeries.cs                OhlcSeries: TickWidth (Financial/)
      WaterfallSeries.cs              Categories, Values, IncreaseColor, DecreaseColor, TotalColor, TotalIndices, BarWidth, ICategoryLabeled;
                                      internal Bars() → WaterfallBar(Foot, Top, IsTotal, Rises)[]: where each bar stands and ends, the
                                      one place the data range and the renderer both read (a total bar stands on zero in TotalColor,
                                      and the steps after it start from its level); internal SlotOf(i) (Categorical/)
      FunnelSeries.cs                 Labels, Values, Colors (Categorical/)
      GanttSeries.cs                  Tasks, Starts, Ends, BarHeight (Categorical/)
      GaugeSeries.cs                  Value, Min, Max, Ranges (GaugeBand[]?), NeedleColor (Circular/)
      ProgressBarSeries.cs            Value, FillColor, TrackColor (Categorical/)
      StatTileSeries.cs               Value, AccentColor, Format, Target, Caption, Trend, TrendColor, Hatch, HatchColor
                                       — single-value KPI tile, no axes; full tile anatomy since v1.14.0 (Categorical/)
      BulletGraphSeries.cs            Value, Target, Bands (IReadOnlyList&lt;GaugeBand&gt;?), BarColor, TargetColor,
                                       Orientation — Few's bullet graph (measure + target tick + qualitative bands
                                       in one hue); new v1.14.0, reuses GaugeBand rather than a second band model (Categorical/)
      StateTimelineSeries.cs          IReadOnlyList&lt;StateSegment&gt; Segments — single-row discrete-state timeline along X; its legend entry is a
                                      Stripes swatch, one stripe for each distinct state fill in first-seen order (StateSegmentExtensions.DistinctFills,
                                      the fill the renderer draws a state with: its colour and its hatch) (Categorical/)
      SparklineSeries.cs              XYSeries: Values, LineWidth (XY/)
      EcdfSeries.cs                   XYSeries: sorted empirical CDF (XY/)
      StackedAreaSeries.cs            X[], YSets[][], Labels (a name for each layer), Baseline, Alpha, Hatch, HatchColor, IHasAlpha; LayerTops delegates to
                                      StackedBaselineExtensions.TopEdgesFor, so the series and StreamGraph read the stack by one rule. It holds one
                                      colour slot for each layer (ColorSlotSpan), so the series drawn after it wears the slot past them, and lists
                                      each named layer in the legend in its own colour (LegendParts). It is not IStackable (XY/)
      StreamplotSeries.cs             XData[], YData[], UData[,], VData[,], Density, ArrowSize (Field/)
      PolarLineSeries.cs              PolarSeries: Color, LineStyle (Polar/)
      PolarScatterSeries.cs           PolarSeries: Color, MarkerSize (Polar/)
      PolarBarSeries.cs               PolarSeries: Color, Alpha, BarWidth; the bar series of an axes whose BarMode is Stacked stand on one another,
                                      wedge by wedge (the feet come from RadialScale, never written into the series) (Polar/)
      SurfaceSeries.cs                GridSeries3D: ColorMap, ShowWireframe, IColormappable, I3DGridSeries
      WireframeSeries.cs              GridSeries3D: WireColor, I3DGridSeries
      Scatter3DSeries.cs              I3DPointSeries: XData, YData, ZData, MarkerSize
      TreemapSeries.cs                HierarchicalSeries: Padding (Hierarchical/)
      SunburstSeries.cs               HierarchicalSeries: InnerRadius (Hierarchical/)
      DendrogramSeries.cs             HierarchicalSeries: Orientation, CutHeight, CutLineColor, ColorByCluster (Hierarchical/)
      DendrogramOrientation.cs        enum: Top, Bottom, Left, Right — leaf-axis placement
      TreeNodeExtensions.cs           TreeNode.Walk() — DFS pre-order enumerable; replaces hand-rolled visit-all recursions
      ClustermapSeries.cs             ChartSeries + IColorBarDataProvider + IColormappable + INormalizable + ILabelable: Data[,], RowTree?, ColumnTree?, RowDendrogramWidth, ColumnDendrogramHeight; ResolveLeafOrder() (Grid/)
      PairGridSeries.cs               ChartSeries: Variables[][], Labels?, HueGroups?, HueLabels?, DiagonalKind, OffDiagonalKind, Triangular, DiagonalBins, MarkerSize, CellSpacing (Grid/)
      PairGridDiagonalKind.cs         enum: Histogram, Kde, None — univariate kind for diagonal cells
      PairGridOffDiagonalKind.cs      enum: Scatter, None, Hexbin — bivariate kind for off-diagonal cells
      PairGridTriangle.cs             enum: Both, LowerOnly, UpperOnly — N×N triangle selector
      Graph/NetworkGraphSeries.cs     ChartSeries + IColormappable: Nodes, Edges, Layout, ColorMap, ShowNodeLabels, ShowEdgeWeights, EdgeThicknessScale, NodeRadiusScale, LayoutSeed, LayoutIterations, ConvergenceThreshold
      XY/RelativeRotationSeries.cs   ChartSeries + IColormappable: AssetCloses[][], BenchmarkCloses[], AssetLabels[], Formula, ShortPeriod, LongPeriod, MomentumLookback, TailLength, ShowQuadrantGrid, ColorMap; ComputeRsData() (XY/)
      RrgFormula.cs                  enum: DualEma=0 (JdK canonical), ZScore=1 (mean-reversion), LogReturn=2
      Graph/GraphNode.cs              readonly record struct: Id, X, Y, ColorScalar, SizeScalar, Label
      Graph/GraphEdge.cs              readonly record struct: From, To, Weight, IsDirected
      Graph/GraphLayout.cs            enum: Manual=0, ForceDirected=1, Circular=2, Hierarchical=3
      SankeySeries.cs                 Nodes, Links (Flow/)
      KdeSeries.cs                    Data[], Bandwidth?, Fill, Alpha, LineWidth, Color, LineStyle; the curve is evaluated from three bandwidths below
                                      the smallest value to three above the largest, and its data range is that extent; a bandwidth that is not a
                                      positive finite number falls back to Silverman's (Distribution/)
      RegressionSeries.cs             XData[], YData[], Degree, ShowConfidence, ConfidenceLevel, BandColor, BandAlpha (XY/)
      HexbinSeries.cs                 X[], Y[], GridSize, MinCount, IColormappable, INormalizable, IColorBarDataProvider (Grid/)
      RugplotSeries.cs                Vec Data, Height, Color, Alpha, LineWidth (Distribution/)
      StripplotSeries.cs              DatasetSeries: Jitter, MarkerSize, Color, Alpha (Distribution/)
      SwarmplotSeries.cs              DatasetSeries: MarkerSize, Color, Alpha — beeswarm layout (Distribution/)
      EventplotSeries.cs              double[][] Positions, LineWidth, Colors[], LineLength (Categorical/)
      BrokenBarSeries.cs              BarRange[][] Ranges, Labels, BarHeight, Color (Categorical/)
      CountSeries.cs                  string[] Values, Color, Orientation, BarWidth — auto group-count (Categorical/)
      PointplotSeries.cs              DatasetSeries: Categories, Color, MarkerSize, CapSize, ConfidenceLevel (Categorical/)
      PcolormeshSeries.cs             Vec X (M+1), Vec Y (N+1), double[,] C (N×M), IColormappable, INormalizable, IColorBarDataProvider (Grid/)
      SpectrogramSeries.cs            Vec Signal, SampleRate, WindowSize=256, Overlap=128, IColormappable, INormalizable, IColorBarDataProvider (Grid/)
      ResidualSeries.cs               Vec XData, Vec YData, Degree=1, MarkerSize, Color, ShowZeroLine (XY/)
      TableSeries.cs                  string[][] CellData, ColumnHeaders, RowHeaders, CellHeight, CellPadding, colors, FontSize (Special/)
      TricontourSeries.cs             Vec X, Y, Z (unstructured), Levels=10, IColormappable, INormalizable (Grid/)
      TripcolorSeries.cs              Vec X, Y, Z, int[]? Triangles (auto-Delaunay), IColormappable, INormalizable, IColorBarDataProvider (Grid/)
      QuiverKeySeries.cs              double X, Y (axes-fraction), U (data units), Label, FontSize — reference arrow annotation (Field/)
      BarbsSeries.cs                  Vec X, Y, Speed, Direction, BarbLength, Color — meteorological wind barbs (Field/)
      Stem3DSeries.cs                 Vec X, Y, Z, Color, MarkerSize, I3DPointSeries — vertical stems (ThreeD/)
      Bar3DSeries.cs                  Vec X, Y, Z (heights), BarWidth, Color, I3DPointSeries — 3D bar prisms (ThreeD/)

  Rendering/
    Lighting/                         per-face 3D lighting model
      ILightSource.cs                 interface: ComputeIntensity(nx, ny, nz) → [0,1]
      DirectionalLight.cs             sealed record: Dx/Dy/Dz direction + Ambient/Diffuse (Lambertian)
      Vec3.cs                         readonly record struct Vec3(X, Y, Z) + static FaceNormal(v0, v1, v2) cross-product face normal
      ColorExtensions.cs              Color.Modulate(intensity), Color.Shade(normal, lightDir) — extensions on Styling.Color
    Svg/
      Svg3DRotationScript.cs          embedded JS for mouse/keyboard interactive 3D rotation

  Indicators/                           stacked base classes: IIndicator → Indicator → Indicator<T> → CandleIndicator<T> / PriceIndicator<T>
    IIndicator.cs                       interface: Apply(Axes)
    Indicator.cs                        abstract base: Color, Label, LineWidth, LineStyle, Offset, MakeX(), PlotSignal(), PlotBands()
    Indicator<TResult>.cs               generic: typed Compute() with IIndicatorResult constraint
    CandleIndicator<TResult>.cs         OHLCV base: Open, High, Low, Close, Volume, BarCount, ComputeTrueRange(), ComputeTypicalPrice(), ComputeDonchianMid()
    PriceIndicator<TResult>.cs          single-price base: Prices, PriceSource constructor overload
    IIndicatorResult.cs                 marker interface for all result types
    SignalResult.cs                     single-line result (SMA, EMA, RSI, ATR, etc.) with implicit double[] conversion
    BandsResult.cs                      band result (Bollinger Bands, Keltner Channels): Middle, Upper, Lower
    MacdResult.cs                       MACD result: MacdLine, SignalLine, Histogram
    AdxResult.cs                        sealed record: Adx[], PlusDi[], MinusDi[] — returned by Adx.ComputeFull()
    StochasticResult.cs                 Stochastic result: K, D
    IchimokuResult.cs                   Ichimoku result: TenkanSen, KijunSen, SenkouSpanA, SenkouSpanB, ChikouSpan
    PriceSource.cs                      enum (Close/Open/HL2/HLC3/OHLC4) + PriceSources resolver
    Sma.cs, Ema.cs, Roc.cs             PriceIndicator: moving averages / rate-of-change (overlay)
    BollingerBands.cs                   PriceIndicator: upper/lower bands via PlotBands() (overlay)
    Vwap.cs                             PriceIndicator: volume-weighted average price (overlay)
    FibonacciRetracement.cs             Horizontal levels at key ratios (overlay)
    Rsi.cs                              PriceIndicator: Relative Strength Index 0-100 (panel)
    Macd.cs                             PriceIndicator: MACD + signal + histogram (panel)
    Stochastic.cs                       CandleIndicator: %K + %D oscillator (panel)
    VolumeIndicator.cs                  Volume bars (panel)
    Atr.cs                              CandleIndicator: Average True Range via ComputeTrueRange() (panel)
    Adx.cs                              CandleIndicator: Average Directional Index via ComputeTrueRange() (panel)
    KeltnerChannels.cs                  CandleIndicator: EMA + ATR bands via PlotBands() (overlay)
    Ichimoku.cs                         CandleIndicator: Ichimoku Cloud via ComputeDonchianMid() (overlay)
    BuySellSignal.cs                    Buy/sell triangle markers
    EquityCurve.cs                      Cumulative P&L line (panel)
    ProfitLoss.cs                       Per-trade colored bars (panel)
    DrawDown.cs                         % drawdown from peak (panel)
    /* v1.8.0 additions (24) — volatility, momentum, cycle, microstructure, entropy, change-point, dispersion: */
    GarmanKlass.cs, YangZhang.cs, TurbulenceIndex.cs       CandleIndicator / multivariate: vol estimators
    AroonOscillator.cs, RelativeVigorIndex.cs + RviResult  CandleIndicator: momentum / oscillators
    SqueezeMomentum.cs + SqueezeResult                     CandleIndicator: TTM Squeeze (histogram + flags)
    LaguerreRsi.cs, KaufmanEfficiencyRatio.cs              PriceIndicator: smoother RSI / KAMA efficiency
    MamaFama.cs + MamaFamaResult                           PriceIndicator: Ehlers MAMA/FAMA (uses Ehlers/)
    AdaptiveStochastic.cs                                  CandleIndicator: Ehlers adaptive-lookback stochastic
    FractionalDifferentiation.cs, RoofingFilter.cs         PriceIndicator: Lopez de Prado / Ehlers band-pass
    CyberCycle.cs, EhlersSineWave.cs + SineWaveResult      PriceIndicator: cycle / phase
    AmihudIlliquidity.cs, CorwinSchultz.cs, RollSpread.cs, Vpin.cs  CandleIndicator: microstructure
    ForceIndex.cs                                          CandleIndicator: Elder's force index
    Bocpd.cs, Cusum.cs + CusumResult                       PriceIndicator: change-point detection
    PermutationEntropy.cs, WaveletEntropy.cs, WaveletEnergyRatio.cs  PriceIndicator: entropy (Wavelet/HaarDwt helper)
    DispersionIndex.cs                                     MultivariateIndicator: cross-sectional stddev
    /* v1.9.0 additions (12) — volume / money-flow, trend / transform, advanced + cross-asset: */
    KlingerVolumeOscillator.cs + KlingerResult             CandleIndicator: Klinger 1977 volume oscillator
    TwiggsMoneyFlow.cs, EaseOfMovement.cs, VwapZScore.cs   CandleIndicator: volume / money-flow
    Supertrend.cs + SupertrendResult                       CandleIndicator: Seban 2008 ATR trailing-stop; reuses Atr
    CgOscillator.cs                                        PriceIndicator: Ehlers 2002 Center-of-Gravity
    InverseFisherTransform.cs                              Indicator<T>: tanh meta-indicator (any bounded series)
    YangZhangVolRatio.cs                                   CandleIndicator: YZ short/long regime ratio; reuses YangZhang
    EhlersITrend.cs                                        PriceIndicator: adaptive MA; reuses Ehlers/HilbertDiscriminator
    Decycler.cs                                            PriceIndicator: price − HP; reuses Ehlers/HighPassFilter
    EhlersSuperSmoother.cs                                 Indicator<T>: public wrapper over Ehlers/SuperSmoother
    TransferEntropy.cs                                     Indicator<T>: Schreiber 2000 directional info flow (scalar, histogram-based)
    Ehlers/                                                internal DSP helpers (v1.8.0): HilbertDiscriminator + HilbertResult, SuperSmoother, HighPassFilter
    Wavelet/                                               internal DSP helpers (v1.8.0): HaarDwt + DwtResult

  Transforms/                         polymorphic export: IFigureTransform -> FigureTransform -> concrete
    IFigureTransform.cs               interface: Transform(Figure, Stream) and Transform(Figure, Stream, SaveOptions); the second is a
                                      default interface method that reads no option and calls the first, so a transform written before
                                      the save settings existed keeps working and writes the same file
    FigureTransform.cs                abstract base: holds ChartRenderer, shared by all transforms; the options overload is virtual
    SvgTransform.cs                   FigureTransform + ISvgRenderer: parallel SVG rendering; applies the SVG settings of SaveOptions
                                      (SvgDocumentExtensions)
    TransformResult.cs                fluent record: ToStream(), ToFile(), ToBytes()

  Animation/
    IAnimation<TState>.cs             interface: typed animation state pipeline
    AnimationController<TState>.cs    controller: runs animation loop with ConfigureAwait(false)
    AnimationBuilder.cs               legacy: frame-based animation (FrameCount, Interval, Loop)
    LegacyAnimationAdapter.cs         adapter: bridges AnimationBuilder to IAnimation<TState>

  Rendering/
    IChartRenderer.cs                 interface: Render(Figure, IRenderContext)
    ChartRenderer.cs                  figure-level orchestrator: background, constrained layout, subplot layout, dispatch to AxesRenderer.
                                        SubPlotRects is the one rule for the plot areas; the drawing and the layout both ask it.
                                        The figure's colour bar reads the same provider and the same ticks as an axes' (ColorBarRendering/)
    AxesRenderer.cs                   abstract base for coordinate-system-specific rendering (ConcurrentDictionary registry). The legend
                                        swatch of a line, a step or a scatter draws its marker through MarkerRenderer, the one owner of
                                        marker shapes, so a swatch shows the marker its series has
    MarkObstacles.cs                  the marks a chart drew on one axes, in pixels as drawn: boxes (a bar, a text's box), paths
                                        (a line's vertices) and points (a scatter's centres). ONE obstacle map, ONE score and ONE selector for
                                        legend Best and for the annotations that avoid overlap: Badness is matplotlib's four integer terms
                                        (path vertices strictly inside the box + point centres strictly inside + overlapping boxes, touching
                                        is not overlapping + 1 per path with a segment that touches it, the separating-axis test of
                                        _path.h), FirstLeastCovered takes the first minimum and stops at 0. Builder collects, Including adds
                                        a box (an annotation's text). Internal
    MarkCollectingContext.cs          an IRenderContext that measures through the one it wraps, draws nothing and records what a
                                        renderer draws as marks: the way the renderers of polar, 3-D and the small-mark families (AddMarksByDrawing)
                                        report their marks without a second drawing code
    TextBox.cs                        the one rule for the rectangle a text occupies (Anchor, Size, FontSize, Alignment, Vertical,
                                        Rotation): ascent = the font size, descent = the height less it, a turned text rotated
                                        counter-clockwise about its drawn baseline point; CentreDrop = 0.35 is the one constant for centring
                                        on a point. The annotations' text boxes and the tick label reach (AxisTickExtensions.Reach) read it,
                                        so what is measured is what is drawn
    TextVerticalAlignment.cs          enum {Baseline = 0, Center = 1, Top = 2, Bottom = 3}: public, ordinals are contract
    ThemedFontProvider.cs             the theme's fonts: TickFont, DefaultFont and DataTextFont(theme), the family and size of the
                                        default font in the foreground text colour — the font of the annotations, the radar's, the
                                        funnel's and the network graph's labels and the bar's value labels; the pie's percentages
                                        take its family and size, in white
    CartesianAxesRenderer.cs          Cartesian (X,Y): grid, ticks, spans, series, annotations, signals
                                        internal helpers: RenderGridLines(Orientation,…), DrawBreakSegments(Orientation,BreakStyle,…)
                                        Its ticks come from AxesTickSourceExtensions.PrimaryTickSource, the rule the data table shares.
                                        PrimaryXDraw / XLabelRotation: the one decision whether the X labels turn, taken at the width the
                                        plot is drawn at; the layout asks the same code through MeasureEdgeBands
                                        Every Y axis slot is PLACED once per render, here: PlacePrimary (left slot 0: the axes' own range
                                        and transform) and PlaceSlot (a slot past it: its own range, the transform through its own scale
                                        and symlog threshold, its ticks by its own locator or its scale's) make a PlacedYSlot; SlotBand is
                                        its band by the one band rule; PlaceYSlots adds the identity and the outward offset of each slot
                                        (each side stacked once, the container's gap between each pair). The drawing, the layout's
                                        reserve (ConstrainedLayoutEngine asks PlaceSlot and SlotBand) and the interaction views
                                        (ComputeSecondaryViewTransforms) all read these, so none works a slot's range, transform or band
                                        out a second way
    CartesianInsetAxesRenderer.cs     inset axes (Axes.InsetBounds; chosen by the Cartesian factory entry): draws like
                                        Cartesian but answers DrawsDirectLabels = false — an inset has no right margin, so
                                        a directly-labelled line keeps its legend entry instead of a label over the parent
    SeriesSlot.cs                     the owner of a series' colour AND its stroke: SeriesAxisPair (Primary / SecondaryY = every right
                                        slot / SecondaryLeftY = every left slot past the primary / SecondaryX; append-only, in the
                                        order AllSeries concatenates), YSlotSeat + Axes.YSlotSeats() (every Y slot in identity order
                                        with the list that numbers its series), SeriesSlot (the cycle colour, the theme's cycler entry for the slot and the colours the theme
                                        gives a violin; ColorOf(series) = own colour, else the theme's violin colour for a violin, else
                                        the slot colour; StatsColorOf for the lines over a violin; LineStyleOf, LineWidthOf,
                                        LineMarkerOf and ScatterMarkerOf = the series' own value when it names one, else the cycler's;
                                        StrokeOf builds the whole stroke in one call) and Axes.SlotIndex / Theme.SeriesSlot /
                                        Theme.ThemedSlot. Axes.ColorSlots() numbers the colour slot of every series: a series takes
                                        ChartSeries.ColorSlotSpan slots from its first (a stacked area's layers, a pie's slices, a funnel's stages)
                                        and the series after it the slot past them, a ColorGroup run takes the largest span of its members;
                                        SeriesRenderContext.FirstColorSlot hands a renderer its first slot. A value counts as named when it differs from what a new series holds
                                        (RestingLineStyle, RestingLineWidth, RestingScatterMarker), the rule the JSON uses for a
                                        value at rest, so a figure draws the same picture before and after a round trip. Primary
                                        passes, the part of every Y slot, the line renderers and the legend swatch all ask here, so
                                        a line and its swatch always wear the same colour, dash, width and marker
    SeriesGroupExtensions.cs          the one rule for the group a series is drawn in (IRenderContext.BeginSeriesGroup):
                                        data-series-index = slot + aria-label when interactive, aria-label otherwise.
                                        Used by the primary passes and the part of every Y slot, so the legend toggle,
                                        the hover highlight and SvgDiffEngine reach a line on any axis too
    AxisTickExtensions.cs             the one tick drawing: IRenderContext.RenderAxisTicks/DrawTickMark + TickDrawContext + TickSide
                                        (Bottom/Left/Right/Top); honours TickConfig (length, width, colour, direction, label
                                        size/colour, pad, visibility). Used by the primary axes, their mirrored side and the part of
                                        every Y slot, for numbers and category names alike. TickLabelBand is the band the labels take
                                        from the edge. ONE rule places an axis title past them on all four edges, TitleOffset, fed by
                                        a TitleClearance (the band, the blank gap, the title's size, the FacingInk): the title's
                                        placing line stands the band, the gap and the part of its own ink that faces the labels
                                        (FacingInk.Facing times the size) past the edge, so that ink edge stands exactly the gap away
                                        whichever way the title faces. Top/BottomAxisLabelOffset call it with XLabelGap (6) and
                                        BottomAxisTitleExtent is the room the X title takes below the labels (the layout reserves it);
                                        the Y titles call it through YAxisTitleExtent, with the container's gap
                                        LabelExtent is how far a label reaches past the mark and its pad, turned or upright, so a
                                        label turned by a set rotation or by the 30 degrees of crowded labels is measured turned
    FacingInk.cs                      internal readonly record struct (Facing, Outward): how far an axis title's ink reaches each way
                                        from its placing line, as fractions of its font size. The font metrics measure a box, not an
                                        ascent and a descent, so the X titles are placed by an ascent of 0.8 em (Ascent) and a descent
                                        of 0.2 em (Descent). A Y title is turned (rotate(-90)), its up direction points left: its ink
                                        spans [x - ascent, x + descent], and that reach decides what it takes away from the room beside
                                        it (Outward). Where it stands is decided by the ink a reader sees facing the labels, which is
                                        less: the baseline on the left (TurnedSink, 0) and the body of the text on the right (TurnedRise,
                                        0.6 em, not the full ascent of a bracket). With those, the 4 px gap leaves the right title where
                                        1.23.0 stood it and brings the left one 8 px closer; a Y title placed at one fixed offset stood
                                        12 px from its labels on the left and 4 px on the right. FacingInk.Of (TickSide) says which
    TitleClearance.cs                 internal readonly record struct (TickLabelBand, Gap, LabelSize, Ink; WithoutTickLabels): the
                                        input of TitleOffset. WithoutTickLabels is the fixed place of a title when no tick label is
                                        drawn on its edge (hidden ticks, axis-less series): 45 px for a Y title, 35 for the X title
    YAxisTitleExtent.cs               YAxisTitleInput (tick label band, side, gap, theme title font) and YAxisTitleExtent (TitleOffset,
                                        Band): the ONE band rule of a Y slot, for every slot including the primary, in
                                        YAxisTitleExtensions (Axis.YTitleExtent, IRenderContext.DrawYAxisTitle, Axis.TitleFont). The
                                        title stands the container's gap past the widest tick label's ink, its style's pad (TextStyle.Pad)
                                        on top of the gap; the band reaches the title's OUTWARD ink (its ascent on the left, its
                                        descent on the right), so on both sides it is the tick label band, the gap, the pad and the
                                        title's size. The layout reserves by it, the renderer stacks the slots by it and each slot
                                        places its title by it, so what is reserved is what is drawn.
                                        The 4 px is the library's geometry contract: exactly equal on both sides and between stacked
                                        slots, measured with the library's own font metrics and pinned by tests. A browser draws the
                                        same SVG with the ink metrics of its own font (the library measures a digit at 0.6 em where a
                                        font draws about 0.556), so what a reader sees is font-dependent: measured in Edge on a chart of
                                        two axes at 13, 13.89 and 18 pt, the left gap is 4 to 5 px and the right 3 to 4 px (1.23.0: 12
                                        left, 4 right at 13 pt); between stacked slots 3 to 4 px on the right and 1 to 5 on the left,
                                        where a descender dips into the gap. The Y titles are calibrated to what is seen, not to the
                                        ascent of a bracket, which is why the right title did not move
    PlacedYSlot.cs                    internal readonly record struct: one Y slot as ONE render draws it: the slot, its Range, its
                                        Transform (through the slot's own scale), its Ticks and TickSource, where its series stand in
                                        the identity order (Pair, FirstIndexInPair), its Band and its Offset past the plot's edge;
                                        TickDraw(theme) is how its labels are drawn and measured. Placed once per render
                                        (CartesianAxesRenderer) and read by the part, the end labels and the interaction views
    YSideTerms.cs                     internal readonly record struct: everything that differs between a Y axis on the left and one on
                                        the right, derived once from AxisSide (Left, Right, Of(side)): the tick side, the outward
                                        sign, the edge fraction, the label alignment, the spine, the series list that numbers the side's
                                        outward slots. The slot part never asks which side it is on, so a left slot is the mirror
                                        image of a right slot; the mirrored primary Y ticks use the Right terms too
    AxisScaleExtensions.cs            the one rule that maps a data value into the space an axis is drawn in and back (log10,
                                        symlog, identity): DataTransform, the range the renderer draws, the server-interaction
                                        view and a pan all go through it
    AxesTickSourceExtensions.cs       TickSource (Locator, Formatter, NamesCategories) + PrimaryTickSource: the one rule for X and Y
                                        that decides where an axis' ticks stand and whether they label numbers or names (the series
                                        stand categories along it, or its formatter is a CategoryFormatter, whoever set it); ScaleLocator,
                                        ScaleFormatter, CategoryNames. NamesAt: the names the axis shows at given positions, all of them
                                        on named ticks or none. The axes renderer draws and measures by it, the data table reads a
                                        position by it, so the table and the picture cannot name one place differently
    AxesRangeExtensions.cs            the automatic range of an axes: SnapshotContributions evaluates every series' ComputeDataRange
                                        once, then adds one contribution for each line drawn across the plot at a data value
                                        (LineContributions: a reference line, a level, a Fibonacci level), as matplotlib's axhline
                                        and axvline update the data limits, so a line beyond the data is drawn inside the plot
    Algorithms/ContourLevels.cs       which levels a contour draws: the ones the user names (ascending, each once, only those that
                                        can be drawn), else the count spread evenly (Interior for lines, Spanning for bands); read by
                                        the line, filled and triangle contours
    ValueObjects/LineSegmentExtensions.cs  the one place a straight line is cut to the area it is drawn in (ClippedTo, the
                                        Liang-Barsky cut; HorizontalAcross / VerticalAcross for a line across the plot); an end inside
                                        the area comes back bit for bit
    CartesianParts/                   one responsibility per class, walked by CartesianAxesRenderer.Render in a fixed order:
      CartesianSpansPart.cs             shaded spans behind the series, cut at the plot edge (an edge beyond the range runs to the
                                        plot edge, a span wholly beyond it draws nothing, a threshold's zone with it)
      CartesianReferenceLinesPart.cs    full-width reference lines; a line beyond a limit the caller set is not drawn
                                        (LineSegmentExtensions.HorizontalAcross / VerticalAcross)
      CartesianToolsPart.cs             trendlines, levels, Fibonacci overlays; a trendline is cut to the plot area
                                        (LineSegmentExtensions.ClippedTo)
      CartesianAnnotationsPart.cs       text, arrows, callouts: the text in the theme's data text font on its point's baseline, centre,
                                        top or bottom, moved by OffsetX/OffsetY. Placed() is every text's TextBox before anything is drawn
                                        (the legend's input, AddTextBoxes), Laid() the box an AvoidOverlap annotation ends at, with a
                                        leader line when it moved more than 6 px (none when it has an arrow)
      AnnotationLayout.cs               where the annotations that set AvoidOverlap stand: each tries its anchor, then eight directions
                                        (up, down, right, left, the four diagonals) at one to four text heights, inside the plot, and takes
                                        the first place MarkObstacles.FirstLeastCovered scores lowest against the marks, the legend's box,
                                        the annotations that stay put and the ones already placed. Annotations do not go through
                                        LabelLayoutEngine.Place: that is continuous repulsion with a clamp, this is a fixed candidate set
      CartesianSignalsPart.cs           buy/sell markers
      CartesianYAxisSlotPart.cs         ONE part for EVERY Y axis of the container, the primary (left slot 0) included:
                                        a slot's own series (own group per series), major and minor ticks
                                        (AxisTickExtensions) and title (YAxisTitleExtensions.DrawYAxisTitle, math as rich
                                        text), from its PlacedYSlot (range, transform, ticks, identity, offset — placed once
                                        per render by CartesianAxesRenderer) and YSideTerms (edge sign, tick side, label
                                        anchor, spine; derived once from AxisSide, no side branch in the part). A slot past
                                        the primary WITH series is drawn whole (Render); the primary goes through the same
                                        part in the axes' drawing order (ticks with the X ticks, title with the X title).
                                        What one axis can do every axis does: a log or symlog scale, a tick locator or spacing,
                                        minor ticks, turned tick labels, a title in math markup. It replaces two parts that
                                        differed only in their side (a right part and a left part) and the primary's own inline copy
      CartesianPartContext.cs           the axes, plot area, context and theme every part of one render draws with
      CartesianSecondaryXAxisPart.cs    the top axis likewise; MeasureBand is the band the layout reserves for it
      CartesianDirectLabelsPart.cs      the end labels: placed ONCE per axes (the lines of every Y axis together, spread by
                                        Interval1DExtensions.SpreadApart), each at the height its own slot's transform puts the end of
                                        its line at (PlacedYSlot), in one column right of the plot past the right axes. Drawn by each
                                        series pass inside that series' own group so hiding or highlighting the series takes its
                                        label along. Not a step of the sequence: the collaborator every series pass calls
    PolarAxesRenderer.cs              Polar (r,theta): angle labels and the circular grid; the rings, their labels (at 22.5 degrees) and the extent
                                        come from RadialScale, which every polar series reads too (SeriesRenderContext.RadialScale)
    RadialScale.cs                    internal readonly record struct (Extent, Rings, Labels, Feet): the ONE radial scale of a polar axes. The extent
                                        is the limit the axes was given, exactly, or the farthest radius any series states (RadialExtent) with a 5 %
                                        margin; the rings are a MaxNLocator sized to the radius in pixels, in one number format (or the axis'
                                        formatter); Feet = where each polar bar series starts when BarMode is Stacked (StackableExtensions, a radius
                                        that is not drawn adds nothing). The grid and every series read it, so a ring labelled 10 stands where a
                                        radius of 10 is drawn; nothing in it is written into a series or its JSON
    ThreeDAxesRenderer.cs             3D (X,Y,Z): projection, bounding box wireframe, depth sorting.
                                        Panes / the 9 drawn cube edges / wall grids / tick rows / axis titles all
                                        read Projection3D.Faces (v1.14.1) instead of literal planes, so the axis
                                        frame follows the camera at any azimuth (was: correct only for the
                                        default quadrant elev>=0, azim in [-90,0]).
                                        v1.14.2: ONE tick rule (ComputeAxisTicks: locator -> spacing -> MaxN) shared by
                                        grid + tick rows + title clearance, and the axis-title pad is MEASURED
                                        (TitlePad: widest drawn label, projected onto the outward perpendicular)
                                        instead of the old 42/60 px constants that sat inside the label band
    IRenderContext.cs                  drawing primitives: DrawLine/DrawRect/etc. take StrokeStyle(Color,Thickness,Style)
                                        / ShapeStyle(Fill,Stroke,StrokeThickness) records (v1.13.0, was loose params —
                                        centralizes the Thickness<=0 visibility guard across every backend);
                                        DrawText, DrawText(…,rotation), DrawRichText (default method)
    ISeriesVisitor.cs                 visitor pattern: Visit() for each of the 83 series types
    DataTransform.cs                  data space <-> pixel space; TransformBatch uses AVX SIMD interleave (zero intermediate alloc)
    RenderArea.cs                     plot bounds + context container
    Primitives.cs                     record structs: Point, Size, Rect, DataRange, PathSegment; internal Rect.FromCorners(a, b) is the
                                      one way a mark placed by two data corners becomes a rectangle (smaller coordinates, absolute
                                      distances), so it keeps a positive size on an axis that runs backwards

    TickLocators/                       ITickLocator strategy for axis tick placement
      ITickLocator.cs                   interface: double[] Locate(double min, double max)
      AutoLocator.cs                    nice-number algorithm (extracted from AxesRenderer)
      MaxNLocator.cs                    nice numbers capped to at most N ticks; ForLength(lengthPx, tickFontPx, labelSpan) chooses N from how many
                                        labels fit along a length (VerticalLabelSpan 2, HorizontalLabelSpan 3 label heights for one tick, clamped to
                                        FewestTicks 2 .. MostTicks 9): the colour bar and the polar rings use it. A bar is a short ruler, so its tick
                                        density differs from an axis's (AutoLocator) on purpose
      MultipleLocator.cs                ticks at multiples of a fixed base
      FixedLocator.cs                   exactly the provided positions within range
      LogLocator.cs                     powers of 10 within range
      AutoDateLocator.cs                OA date range → DateInterval → aligned tick positions (new v0.8.1)
      DateInterval.cs                   enum: Years, Months, Weeks, Days, Hours, Minutes, Seconds (new v0.8.1)

    TickFormatters/
      ITickFormatter.cs                 interface: string Format(double value)
      NumericTickFormatter.cs           G5 with scientific fallback
      DateTickFormatter.cs              OLE dates with configurable format string
      AutoDateFormatter.cs              reads ChosenInterval from AutoDateLocator, selects matching format (new v0.8.1)
      LogTickFormatter.cs               powers of 10
      EngFormatter.cs                   SI prefix engineering notation (k, M, G, m, µ, n)
      PercentFormatter.cs               value/max*100 + "%" suffix
      CategoryFormatter.cs              a tick from k to just below k + 1 → label k; reversed: index 0 reads the last label, for
                                        categories drawn from the top down (a heatmap draws its row 0 at the TOP) (new v1.11.1).
                                        Made with positions of its own, a tick at positions[k] reads labels[k] and a tick anywhere
                                        else reads nothing: names under columns of different widths (a Marimekko chart)
      DateTimeTickFormatter.cs          FromArray (index-based) + FromEpochMs (Unix ms) factories (new v1.11.1)

    Layout/                             margin computation (new v0.8.1)
      ConstrainedLayoutEngine.cs        Compute(Figure, IRenderContext) → SubPlotSpacing; measures text extents, clamps margins.
                                        Owns the right, top, measured-left and measured-bottom edge rules for BOTH layout paths
                                        (Compute for tight/constrained, ReserveEdgeBands for the default path): MeasureRightBand,
                                        SecondaryAxisBand(s) (the right stack), LeftAxisBands (the left stack past the primary),
                                        TitleBand, LeftNeed (the whole left stack, below),
                                        BottomNeed (the measured X tick band and X title + pad; the default path widens the
                                        theme's margin to it, the tight path never goes below its old floor); measured bands
                                        lift the margin, gutter and row-gap caps. LeftNeed is the whole left stack of the Y axis
                                        container: the primary's tick label band and its title by the one band rule
                                        (YAxisTitleExtensions), every left slot past it, the container's TitleGap between each pair
                                        (LeftStackBand), then PadLeft; the default path reserves it only when the stack draws
                                        something, so a figure that asks for no margin and draws nothing left of the spine (an icon
                                        that fills its square, a row of tiles with no axes) keeps the margin and the gutter it asked
                                        for. SecondaryAxisBands/LeftAxisBands give the band of EVERY slot on their side, each
                                        measured by the renderer that draws it (PlaceSlot + SlotBand: the slot's own locator, scale
                                        and ticks, not a recomputation), summed by AxisStackingExtensions — a slot with no series
                                        still reserves its band. There is no second copy of the title rule: the constant of 12 px the
                                        layout used and the one-off term the renderer added for the primary's band are gone.
                                        Three steps in both paths: the sides first (ComputeSides in Compute, ReserveSideBands in
                                        ReserveEdgeBands: the Y tick labels, the secondary axes and the end labels, which do not
                                        depend on the plot's width); then SettleSides, because the room the X tick labels need BESIDE
                                        their plot does depend on it: they turn at the width the plot is drawn at, and a turned
                                        label stands out sideways (EdgeBands.Overhang). WidenForOverhang widens the left margin, the
                                        right margin or the gutter by the overhang and the pad; the widening narrows the plot, so it
                                        is measured again at the width that remains, in at most 4 passes (MaxSettlePasses), stopping
                                        when two measures agree to 0.01 px (SettledWithin); nothing standing out changes nothing.
                                        Then the top and bottom, measured at the width each plot is drawn at
                                        (ChartRenderer.SubPlotRects), because crowded X labels turn at that width
      AxisStackingExtensions.cs         StackedOffsets(gap)/TotalStackedBand(gap) on IReadOnlyList<double>: the ONE rule both the
                                        layout engine (how much to reserve) and the render (where each slot's outward offset starts)
                                        read, so the two can never disagree. The first slot stands at the plot's edge, each slot
                                        after it one gap past the far side of the one before it; bands are summed once, one gap
                                        between each pair, none before the first or past the last. The gap is the container's TitleGap
      LabelLayoutEngine.cs              Place(candidates, bounds): continuous repulsion of text boxes that stay inside the bounds; the
                                        value labels of a bar, a pie and a sankey are placed by it (SeriesRenderer.PlaceLabels). The end
                                        labels keep their own layout, Interval1DExtensions.SpreadApart, on purpose: they are a one-
                                        dimensional, order-preserving column OUTSIDE the plot, x is fixed and only y is spread, and
                                        Place's clamp to the plot bounds would pull them back inside. Their centring constant is
                                        TextBox.CentreDrop, the one owner it shares with the annotations
      LayoutMetrics.cs                  internal record: LeftNeeded, BottomNeeded, TopNeeded, RightNeeded, RightBand, TopBand,
                                        LeftBand, BottomBand (per subplot)
      EdgeBands.cs                      readonly record struct (Top, Right, Left, Bottom, BottomTitle, Overhang; Overhang =
                                        SideOverhang(Left, Right): how far the first and the last X tick label stand out sideways
                                        past the plot at the rotation they are drawn at, from AxisTickExtensions.LabelReach(Away,
                                        Before, After) over the label's TextBox): what an axes draws past
                                        its four spines besides the title and the end labels (mirrored tick labels, secondary X
                                        axis, the category names on the Y axis, every X axis' tick labels and the X title below
                                        them), MEASURED by the renderer that draws it (AxesRenderer.MeasureEdgeBands) — the drawer
                                        is the measurer; Left is the far side of the primary Y axis' tick labels, numbers or category
                                        names, as drawn (a polar or 3-D axes measures none, and the tight layout estimates its
                                        labels); a polar or 3-D axes measures nothing below (it draws its X title elsewhere)
      RightBand.cs                      readonly record struct (SecondaryAxis, MirroredYTicks, EndLabels): the right edge in drawing
                                        order; Axis = the TOTAL stacked width of every right slot (AxisStackingExtensions),
                                        Width = axis side + end labels + outer padding. The layout reserves Width and
                                        CartesianDirectLabelsPart starts its column at Axis
      DirectLabelMetrics.cs             the owner of what the end labels say, where each line ends (last finite point inside the
                                        visible X range), their font (the legend's) and the gap; layout reserves with it and the
                                        drawing draws with it. DirectLabel record struct = one label, carrying its Slot (which Y
                                        axis' own transform puts the end of its line) — Collect() walks every Y slot of the container
                                        in identity order (Axes.YSlotSeats), the primary's lines and those of every slot on either
                                        side, so a line on a left-hand axis keeps its name at its end like any other
      Interval1D.cs                     readonly record struct (Start, Size): a span on one axis
      Interval1DExtensions.cs           SpreadApart: order-preserving collision sweep over spans; Sankey node padding and the end
                                        labels call the same one
      LegendMeasurer.cs                 the legend box and font, measured from the same LegendEntries list the drawing reads

    LegendRendering/                    the legend
      LegendLayoutCalculator.cs         the legend's geometry from the Legend config, its labels and the plot area (font, columns,
                                        box, place): drawing and hit-testing read one copy. PositionOf(legend) is the one rule for
                                        the place: the one named, or, for a legend left at Best, Theme.DefaultLegendPosition (the classic
                                        theme: UpperRight; every other theme: Best); the renderer's decision to collect the marks reads it too
      LegendPositionStrategy.cs         one strategy per place (UpperRight ... Center, the four Outside*) + BestStrategy: the first of
                                        ten places, in matplotlib's order, that MarkObstacles.FirstLeastCovered scores lowest; with no
                                        marks it is the upper right. Best works over the marks of Cartesian, polar and 3-D axes
                                        (AxesRenderer.LegendObstacles); a map (Geo) hands over none
      LegendEntry.cs                    readonly record struct (Series, OriginalIndex, ColorSlotIndex, Label, Swatch) +
                                        Axes.LegendEntries(directLabelsDrawn):
                                        the ONE enumerator of which series are named in a legend, in which order, under which
                                        index (the series slot), in which colour slot, with which text and swatch: every Y slot's series
                                        in identity order, then the top X axis' (a labelled series on the top axis has an entry), an entry
                                        for each of a series' LegendParts. The parts of one series share its index, so a click hides or shows
                                        the whole series and SvgLegendToggleScript dims every entry of it together. RenderLegend,
                                        ComputeLegendBounds (the hit-test) and LegendMeasurer all read it, so drawing, click and
                                        margin cannot disagree. A series that draws its own label (ChartSeries.DrawsOwnLabel: the
                                        stat tile) never has an entry

    TextMeasurement/                    text width estimation (new v0.8.1)
      CharacterWidthTable.cs            internal static: per-char width factors for Helvetica/Arial at 1em
      IFontMetrics.cs                   interface: Measure(text, font) — DefaultFontMetrics here, SkiaFontMetrics in Skia
      IGlyphPathProvider.cs             interface: GetPathData(text, font) — text as SVG <path>; null → <text> fallback

    Text/                               the Unicode Bidirectional Algorithm, UAX #9 (v1.17.0)
      BidiClass.cs                      enum: the 23 Bidi_Class values + BidiDirection (LeftToRight/RightToLeft)
      BidiClassTable.cs / .g.cs         internal static: GetClass(codePoint) by binary search over 1 267 generated
                                        ranges (UCD 17.0.0, tools/unicode/generate_bidi_tables.py)
      BidiBrackets.cs / .g.cs           internal static: the 128 paired brackets for rule N0, canonical equivalents
      BidiAlgorithm.cs                  internal static: Resolve(text, direction?) → BidiParagraph; rules P2–P3,
                                        X1–X10, W1–W7, N0–N2, I1–I2, L1, L2 — proven against BidiTest.txt +
                                        BidiCharacterTest.txt (861 948 cases) in the test suite
      BidiParagraph.cs                  readonly record struct: Levels per UTF-16 unit (Removed for X9), ParagraphLevel,
                                        VisualOrder(), RenderLevels(). Internal, visible to MatPlotLibNet.Skia

    MathText/                           mini-LaTeX → SVG tspan rendering (new v0.8.1)
      MathTextParser.cs                 state machine: $…$ delimiters, \cmd → Unicode, ^{} / _ → TextSpan
      RichText.cs                       RichText(IReadOnlyList<TextSpan>); TextSpan(Text, Kind, FontSizeScale); TextSpanKind enum
      GreekLetters.cs                   48-entry dictionary: \alpha…\Omega → Unicode code points
      MathSymbols.cs                    40+ entries: \pm, \times, \leq, \infty, \degree, … → Unicode

    Interpolation/                      image interpolation engines
      IInterpolationEngine.cs           interface: Resample(double[,], int, int) strategy
      NearestInterpolation.cs           singleton: identity / pixel duplication (default)
      BilinearInterpolation.cs          singleton: 2x2 neighborhood, linear weights
      BicubicInterpolation.cs           singleton: 4x4 Catmull-Rom kernel with output clamping
      InterpolationRegistry.cs          thread-safe ConcurrentDictionary, mirrors ColorMapRegistry pattern

    Downsampling/                       performance helpers for large datasets
      IDownsampler.cs                   interface: Downsample(x, y, targetPoints)
      LttbDownsampler.cs                Largest-Triangle-Three-Buckets O(n) algorithm
      ViewportCuller.cs                 static: filter to [xMin,xMax] + one padding point each side

    SeriesRenderers/                    generic SeriesRenderer<T> per series type
      SeriesRenderContext.cs            record: Transform + Ctx + Color + Area + options
      SeriesRenderer.cs                 abstract base: ResolveColor(), ApplyAlpha(), ApplyDownsampling(), ApplyMonotonicDownsampling(), ResolveColormapping() + ColormapContext record struct + generic SeriesRenderer<T>
                                        Collect mode: SeriesRenderer<T>.AddMarks (internal virtual, no-op = a family that is not scanned)
                                        reports the marks a series draws, from the pixel geometry its Render uses; AddPolyline/AddSegment
                                        are for the hot families, AddMarksByDrawing runs Render against a MarkCollectingContext for the
                                        families with few marks. PlaceLabels/DrawPlacedLabel(s) are the shared bar, pie and sankey label placing
      PixelSegment.cs                   readonly record struct: one stroked segment as drawn, for AddSegment
      Categorical/BarPixels.cs          PixelBar/PixelBars: the bars of a series as drawn (stacked, grouped, either orientation), with
                                        VerticalEnd/HorizontalEnd — the end a value label stands beyond; the bar renderer and its AddMarks share them.
                                        A value label stands beyond the end in the direction the bar points in the data
                                        (DataTransform.ValueRunsBackwards), 4 px from it, on an axis that runs either way
      DrawStyleInterpolation.cs         internal static: Apply(x, y, style) — shared step-mode interpolation; eliminates duplication between LineSeriesRenderer and AreaSeriesRenderer
      XY/                               Line (LTTB), Scatter (viewport cull), Step (LTTB), Area (LTTB), ErrorBar, Bubble, Sparkline, Ecdf, StackedArea, Regression (LeastSquares polynomial + confidence band), Residual (LeastSquares residuals + optional zero line)
                                        A line, a step, an area (its outline too) and a stacked area break at every point that cannot be
                                        drawn (MissingValueExtensions.DrawableRuns), in the renderer, so PNG, PDF and the native controls draw
                                        the same gap as the SVG
      Categorical/                      Bar (ShowLabels), Histogram, Waterfall, Funnel, Gantt, ProgressBar, StatTile (single-value KPI, full tile anatomy since v1.14.0), BulletGraph (Few's measure+target+bands strip, v1.14.0), Eventplot, BrokenBar, Count (group-count), Pointplot (mean + CI)
      Circular/                         CircularRenderer<TSeries> (abstract base: BuildWedgePath, PlaceOuterLabels); Pie, Radar, Donut, Gauge
      Grid/                             Heatmap (its cells stand at their data coordinates, so a limit or a zoom moves them with the axes;
                                        RenderInArea fills the plot rectangle for a heatmap that is a panel of a larger chart: the clustermap's
                                        matrix, a filled contour's backdrop), Contour, Contourf (both fill the bands between their levels through ContourBandFillExtensions.FillContourBands: the lowest band over the grid, the bands above it painted over, so the fill stands where the grid stands and follows the axes' range and the limits you set), Image, Histogram2D, Hexbin (HexGrid flat-top bins), Pcolormesh (quadrilateral cells), Spectrogram (STFT via Fft helper), Tricontour (Delaunay + marching triangles), Tripcolor (Delaunay fill), Clustermap (composite: heatmap + row/column dendrograms with sub-pixel-suppressed margin panels), PairGrid (composite: N×N matrix of histograms/KDE on diagonal + scatters/hexbin off-diagonal, optional hue grouping)
                                        PairGridLayout.cs — pure geometry: ComputeCellRects(plotBounds, n, cellSpacing) → Rect[n,n]; MinPanelPx sub-pixel gate
                                        PairGridOffDiagonalPainters.cs — IPairGridOffDiagonalPainter strategy + ScatterOffDiagonalPainter / HexbinOffDiagonalPainter / Registry; PairGridGeometry (MapPoint, ComputeAxisSpan); PairGridHue (IsValid, BuildCache, GetColor)
      Graph/                            NetworkGraph (composite: nodes + edges in 2D, directed-edge arrowheads, optional weight labels; layout delegated to Numerics/NetworkGraphLayouts.cs)
      Distribution/                     Box, Violin, Kde (density math delegated to Numerics/GaussianKde.cs — Silverman bandwidth), Rugplot (tick marks), Stripplot (jittered points), Swarmplot (BeeswarmLayout circle-packing)
      Financial/                        Candlestick, OhlcBar
      Polar/                            PolarLine, PolarScatter, PolarBar (stacked on the RadialScale feet), PolarHeatmap; PolarSeriesContextExtensions.PolarTransformFor(series):
                                        the transform out to the axes' radial scale (the extent the series makes alone when no axes hands one over)
      Field/                            Quiver, Stem, Streamplot, QuiverKey (reference arrow), Barbs (meteorological wind barbs)
      Hierarchical/                     Treemap, Sunburst, Dendrogram (shared HierarchicalSeries base; tree-of-U-shape segments with optional cut-height clustering)
                                        HierarchicalLayout.cs — nested Dendrogram/Treemap/Sunburst/Clustermap constant classes (LabelOffsetPx, HeaderHeightPx, OuterRingInsetPx, MinPanelPx, …)
      Flow/                             Sankey
      Special/                          Table (measured column widths, header+data rows, borders)
      ThreeD/                           Stem3D (vertical stems via Projection3D), Bar3D (prism faces, painter's depth sort)

    ColorBarRendering/                  the colour bar
      ColorBarRenderer.cs               abstract base (range, colour map, extend wedges) of the orientation renderers (Vertical/HorizontalColorBarRenderer,
                                        chosen by ColorBarRendererFactory); ScaleAlong(length, vertical) asks ColorBarScale for the ticks of the bar as
                                        long as it is drawn. The obsolete TickCount (6, fifths of the range) stays for binary compatibility; nothing reads it
      ColorBarScale.cs                  internal readonly record struct (Min, Max, Ticks, Labels): the ONE place a colour bar decides which values it names —
                                        a MaxNLocator sized to the bar's length (MaxNLocator.ForLength), at round numbers, in one number format; FractionOf(value)
                                        is where a tick stands along the gradient. WidestLabel is the widest label any tick count can produce, which the layout
                                        reserves before it knows the length. ColorBarRangeExtensions.RangeOf(provider) is the one range rule: the provider's
                                        range when it has a width, otherwise the bar's own Min and Max, 0 to 1 when that has none
      ColorBarProviderExtensions.cs     internal: ColorBarProvider() — the one rule for the series a bar reads: the first IColorBarDataProvider that
                                        ProvidesColorBar. AxesRenderer.RenderColorBar, ChartRenderer (the figure's bar) and ConstrainedLayoutEngine call it,
                                        so the bar of an axes, the bar of a figure and the room the layout reserves cannot read different series

    Svg/
      ISvgRenderer.cs                 interface: Render(Figure) -> string (backward compat)
      SvgRenderContext.cs             IRenderContext impl: StringBuilder-based SVG emission; BeginDataGroup(cssClass, idx, ariaLabel?), BeginLegendItemGroup(idx, ariaLabel?), BeginAccessibleGroup(cssClass, ariaLabel); uses string.EscapeForXml() extension
      SvgXml.cs                       internal static: EscapeForXml(this string) — DRY XML escaping extension used by SvgRenderContext and SvgTransform (v1.8.0: renamed from SvgXmlHelper)
      SvgDocumentExtensions.cs        internal: WithSaveOptions(svg, options) — the settings of SaveOptions an SVG can use, applied to the
                                      finished document text so they hold for whichever ISvgRenderer wrote it: PrettifySvg indents one
                                      element to a line, SvgDecimalPrecision rounds positions and sizes (never stroke widths, opacities or
                                      font sizes), Title and Author become Dublin Core metadata. Options that ask for none return the very string
      SvgSeriesRenderer.cs            thin visitor dispatcher to SeriesRenderer<T> instances; in collect mode (Marks) the same dispatch calls
                                      AddMarks instead of Render (Dispatch), so there is one place that knows which renderer a series has
      SvgInteractivityScript.cs       embedded JS: zoom/pan via viewBox; tabindex + aria-roledescription; keyboard +/-/arrows/Home (new v0.8.8)
      SvgLegendToggleScript.cs        embedded JS: click/Enter/Space on data-legend-index; role=button, aria-pressed, tabindex (new v0.8.8; toggle on click only, never pointerdown); every entry of one index dims and undims together
      SvgLegendDragScript.cs          embedded JS: press-and-hold legend item to translate <g class="legend"> via transform; capture-phase stopPropagation coordinates with pan/zoom + toggle; viewBox clamp; client-only translation
      SvgCustomTooltipScript.cs       embedded JS: styled floating div tooltip; role=tooltip, aria-live=polite, focus/blur listeners (new v0.8.8)
      SvgHighlightScript.cs           embedded JS: mouseenter/focus dims siblings; tabindex, blur restores (new v0.8.8)
      SvgSelectionScript.cs           embedded JS: Shift+drag selection rect; Escape cancels; aria-label on rect (new v0.8.8)

  Models/Series/XY/
    IndexRange.cs                     readonly record struct(StartInclusive, EndExclusive) + Count/IsEmpty — returned by IMonotonicXY.IndexRangeFor

  Rendering/
    Normalized3DPoint.cs              readonly record struct(Nx, Ny, Nz) — returned by Projection3D.Normalize()
    ValueObjects/CubeFaceSelection.cs which of each axis' two parallel cube faces points AWAY from the camera
                                        (new v1.14.1 — port of matplotlib axis3d._get_coord_info +
                                        _get_axis_line_edge_points, incl. its edge-on tie handling):
                                        enum CubeAxis {X,Y,Z} · enum CubeSide {Min,Max}
                                        readonly record struct CubePlane(CubeAxis Axis, CubeSide Back) — Front,
                                          Coordinate(Box3D, CubeSide) resolves a side against the CALLER's box
                                        readonly record struct AxisEdge3D(Vec3 From, Vec3 To) — a tick-bearing edge
                                        readonly record struct CubeFaceSelection(CubePlane X, Y, Z) — Planes,
                                          this[CubeAxis], AxisEdge(CubeAxis, Box3D), FarCorner(Box3D)
                                        Computed once per projection and read as Projection3D.Faces; the selection
                                        carries SIDES, never coordinates, because a projection may be built over a
                                        different box than the renderer draws with
    ValueObjects/PinnedAxes.cs        internal [Flags] enum + PinnedAxesExtensions.ToWire()/OtherAxes() — which
                                        vertex components sit on a selected face, emitted as data-v3d-pinned so
                                        Svg3DRotationScript can mirror them when it re-selects mid-drag

  Interaction/                        the managed interaction layer — in the CORE package, so a native control
                                      only has to translate its own pointer and key events into three neutral
                                      records; no platform type reaches this folder
    IInteractionController.cs         what a host talks to: five Handle* methods, three read-only state
                                      properties, UpdateLayout, and InvalidateRequested
    InteractionController.cs          the one implementation. It builds the fifteen modifiers in a fixed order
                                      and gives the first one whose Handles* returns true the event. Two
                                      factories: CreateLocal applies events to the figure in-process,
                                      Create takes any sink (a SignalR publisher, say). BOTH are wrapped in the
                                      constructor, so DataPointClicked — the point the reader clicked — reaches
                                      the application in either mode
    Modifiers/IInteractionModifier.cs one gesture, one class: Handles*(args) claims, On*(args) acts. Fifteen of
                                      them — pan, zoom, reset, legend toggle, brush/span/rectangle select,
                                      3-D rotate, crosshair, hover, data cursor, three drawing tools and their
                                      selection. Order is the priority; it is written out where they are built.
                                      DataCursorModifier is handed the PanModifier: a press on a span (a timeline
                                      segment) waits for the release, so a click selects and a drag still pans
    ChartLayout.cs / IChartLayout.cs  what a modifier measures against: plot rectangles, HitTestAxes(x, y),
                                      PixelToData, GetDataRange. Rebuilt after every render. The range and scale
                                      are the renderer's own: ChartLayout.View asks the axes renderer for the
                                      transform Render draws with (ComputeViewTransform: autoscale, margins,
                                      limits, log/symlog), kept until a gesture changes the limits or scales.
                                      GetSecondaryDataRanges(axesIndex) is the per-axis counterpart for every
                                      Y axis stacked PAST the primary, in the OUTWARD numbering (see below) — a DEFAULT
                                      INTERFACE MEMBER (empty for a host that states only a plot area and a primary
                                      range), so every pre-existing IChartLayout implementer kept compiling unchanged;
                                      ChartLayout's own override is NOT cached like View (a figure with several
                                      Y axes being actively dragged recomputes every move — a deliberate, named cost
                                      trade-off, not an oversight). The views are the slots placed by the renderer
                                      (CartesianAxesRenderer.ComputeSecondaryViewTransforms: PlaceSlot, the one
                                      range-and-transform rule the drawing uses), read through YAxisStack.SlotAtOutward
    ChartLayoutViewExtensions.cs      ViewOf(layout, axes): that transform for every modifier; a host's own
                                      IChartLayout is read linearly over the range it states. SecondaryViewsOf
                                      mirrors it for every slot past the primary — scale-aware for ChartLayout, a plain
                                      linear DataTransform built from GetSecondaryDataRanges for any other host —
                                      so a modifier converts a pixel position through a SPECIFIC axis'
                                      own scale (PixelToData already inverts log/symlog correctly; no scale math
                                      is re-derived by hand for a slot)
    NearestPointFinder.cs             which data point is under the cursor, across every visible XY series in
                                      one axes, with the pixel distance it was found at; a StateTimelineSeries
                                      segment is hit by containing the cursor (last drawn wins) and reports its
                                      index in Segments plus a Detail sentence the tooltip shows
    FigureInteractionEvent.cs         the abstract event with ApplyTo(figure). Fifteen leaves: Pan, Zoom,
                                      AxisRange, Reset, LegendToggle, BrushSelect, SpanSelect, RectangleZoom,
                                      Rotate3D, Hover, DataCursor, the three add-drawing events and the remove.
                                      An event comes from a client over the wire, so the subplot it names may be gone (the
                                      server's figure changed after the client drew) or never have been (a crafted index): every
                                      event the library ships DROPS itself then — it changes nothing and does not throw, because
                                      one stale event must not end the loop that applies the events behind it. TryApplyTo (internal
                                      virtual) says which happened; TargetAxes (protected) still throws, as it always did, for the
                                      events outside the library, and TryTargetAxes (protected) answers false instead of throwing,
                                      with the same bounds rule; XLinkedAxes is empty for a missing subplot. A session that drains
                                      events counts the dropped ones and the ones that throw (see "Real-time SignalR updates")
    FigureNotificationEvent.cs        the events that ask a question rather than change anything: ApplyTo is a
                                      sealed no-op, so a host cannot accidentally mutate a figure with one
    SignalREventSink.cs               the sink that publishes events to a hub instead of applying them
    InteractionToolbar.cs             which drawing tool is active; ToolbarState + ToolbarButton
    ViewHistoryManager.cs             back/forward over axis limits
    PanEvent.cs / AxisRangeEvent.cs   apply to the target axes AND every axes linked to it by ShareX; an axes made
                                      of series that fix their own Y range (timelines) ignores the vertical part.
                                      PanEvent.InView carries the range on screen so a pan can pin an auto-scaled axes.
                                      Both carry SecondaryRanges (IReadOnlyList<SecondaryAxisRange>?, init-only —
                                      the SAME additive idiom InView already set) so a gesture moves every Y axis
                                      stacked past the primary too, each by its own range; a
                                      slot whose own series fix their Y range is skipped the same way the primary
                                      is, independently (AxisRangeEventExtensions.ApplySecondaryRanges, an
                                      extension(Axes) block and the ONE place either event family writes a change onto a
                                      slot; a change for a slot the axes does not have is skipped). The modifiers compute
                                      each slot's new range before sending: Zoom/RectangleZoom re-read the SAME
                                      pixel rectangle through that slot's own transform (DataTransform.PixelToData
                                      already inverts log/symlog); Pan scales the SAME fraction-of-plot delta by
                                      that slot's own span (PanEvent.Shifted: a DataSpan in, a DataSpan out, so a modifier
                                      can apply it without an Axis object); Reset replays the
                                      snapshot taken at construction, same as it already did for the primary.
                                      THE WIRE IS OUTWARD-ONLY, and stays so. SecondaryAxisRange(Side, SlotIndex, Range),
                                      GetSecondaryDataRanges and the data-sec-right-N / data-sec-left-N attributes
                                      number only the slots PAST the primary: (Left, 0) is the first left axis past the
                                      primary Y axis, (Right, 0) is the first right axis, and the primary is never
                                      addressable through it. The container's own index counts the primary as left
                                      slot 0, so the left side shifts by one between the two; YAxisStack.OutwardOffset
                                      (left 1, right 0) and SlotAtOutward(i) are the ONLY place that converts, read by
                                      ApplySecondaryRanges, by the renderer's views and by the SVG writer (SvgRenderContext's
                                      data-sec attributes). The numbering is a contract with every browser tab that holds an
                                      older drawing, which sends these indices back after an upgrade, so it did not change when
                                      the container arrived; the names keep "secondary" for the same reason
                                      (they mean "an axis past the primary"). SvgSignalRInteractionScript reads
                                      data-sec-*-count and the loop index as the slot, and needs no knowledge of the container
    DataSpan.cs                       internal readonly record struct (Min, Max): one axis' range as a pair of data values, the
                                      range a pan starts from and the range it moves to (PanEvent.Shifted takes and returns one,
                                      with the scale and threshold, so the arithmetic stays within four parameters)
    PinnedAnnotation.cs               a clicked point: series label, data x/y, pixel x/y, axes index, and the
                                      point's own position in the caller's arrays — what DataCursorEvent
                                      carries and what DataPointClicked hands the application

  Data/
    RingBuffer.cs                     public sealed RingBuffer<T> — fixed-capacity circular SEQUENCE, oldest
                                      first; one writer / many readers (ReaderWriterLockSlim), never allocates
                                      on Append. The append IS the eviction: nothing is paid per item to keep
                                      the window bounded. A SEQUENCE, not a set — "is this key in the window"
                                      is a different shape and does not belong here
    RingBufferExtensions.cs           Min()/Max() over INumber<T> folded under one read lock without
                                      allocating, + MinOrNaN()/MaxOrNaN() for the double ring an axis reads.
                                      Arithmetic over the VALUES lives here, never inside the ring
    DoubleRingBuffer.cs               RingBuffer<double> under the name it has carried since v1.7 — a thin
                                      forwarder, so the wrap arithmetic exists once

  Numerics/
    PointDensity.cs                   how crowded each point's neighbourhood is, counted on a grid rather than
                                      estimated with a kernel: a 2-D kernel estimate walks every other point
                                      for every point, and this chart is only worth drawing where that cost is
                                      unpayable. Read by AxesBuilder.DensityScatter
    LeastSquares.cs                   public static: PolyFit (normal equations), PolyEval (Horner), ConfidenceBand (t-distribution leverage; z for 120+ degrees of freedom from StandardNormal)
    StandardNormal.cs                 internal static: Quantile(p) — the one inverse of the normal CDF (Wichura AS 241, ~1e-16); saturates to ±∞ outside (0, 1), NaN → NaN
    SortOrderExtensions.cs            internal: DescendingOrder() — the permutation from the largest value to the smallest, stable (ties keep
                                      the order given), NaN last; a verb reorders its parallel arrays by it (Pareto, DotPlot)
    StackedBaselineExtensions.cs      internal: ComputeFor(baseline, ySets, n) — the first line of a stack under Zero, Symmetric, Wiggle or
                                      WeightedWiggle (the last two are matplotlib's stackplot baselines of those names, checked against
                                      its source) — and TopEdgesFor — each layer's top edge; read by the stacked-area series and StreamGraph
    ConfidenceBand.cs                 sealed record(Upper[], Lower[]) — returned by LeastSquares.ConfidenceBand()
    Vec.cs                            readonly record struct: Data[], Length, Min/Max/Mean/Std/Sum/Percentile(p)/Quantile(q), element-wise ops, implicit double[] conversions
    Fft.cs                            public static: Forward (Cooley-Tukey radix-2, Hann window, zero-pad to power-of-2), Stft (sliding window FFT → StftResult)
                                        StftResult: double[,] Magnitudes, double[] Frequencies, double[] Times
    Delaunay.cs                       public static: Triangulate(x, y) → TriMesh — Bowyer-Watson algorithm, CCW super-triangle, epsilon jitter for collinear points
                                        TriMesh: int[] Triangles (flat, every 3 = 1 triangle), double[] X, double[] Y
    HierarchicalClustering.cs         public static: Cluster(double[,] distanceMatrix) → Dendrogram — Ward's method (Lance-Williams), O(n³)
                                        Dendrogram: DendrogramNode[] Merges, int[] LeafOrder
                                        DendrogramNode: Left, Right, Distance, Size
    HexGrid.cs*                       internal static: ComputeHexBins → Dictionary<AxialHex, int>, HexagonVertices → Point[], HexCenter → Point
                                      (* file lives in Rendering/SeriesRenderers/Grid/ but uses MatPlotLibNet.Numerics namespace)
    AxialHex.cs*                      internal readonly record struct AxialHex(Q, R) — axial coord of a flat-top hex cell
    MinMaxRange.cs                    public readonly record struct MinMaxRange(Min, Max) + MinMaxRangeExtensions.ScanColorBarRange(this double[,]) — inclusive numeric interval
    MatShape.cs                       public readonly record struct MatShape(Rows, Cols) — 2-D dimensions
    XYCurve.cs                        public readonly record struct XYCurve(X, Y) — paired sample arrays (spline / KDE results)
    GaussianKde.cs                    internal static: SilvermanBandwidth(sorted) (a sample without spread takes the size of its value as the
                                      spread, as R's bw.nrd0 does), JointBandwidth(groups) (the mean of the groups' own bandwidths, ggridges'
                                      rule), Evaluate(sorted, bandwidth, numPoints) → XYCurve, and its two halves Grid and DensityAt, so several
                                      groups can be estimated on one shared grid (moved from Rendering/SeriesRenderers/Distribution/ — K3 namespace-cycle kill)
    NetworkGraphLayouts.cs            internal static: pure-function layouts ApplyManual/ApplyCircular/ApplyHierarchical/ApplyForceDirected + Apply(kind, seed, iterations, threshold) dispatch (moved from Rendering/SeriesRenderers/Graph/ — K3 namespace-cycle kill)

    SeriesRenderers/Distribution/
    BeeswarmLayout.cs                 internal static: Compute(sortedValues, markerRadius, categoryCenter, pixelScale) — greedy circle-packing, cap 1000 pts, jitter fallback
    SortedArrayExtensions.cs          internal extensions on double[]: Percentile(p), BisectLeft(v), BisectRight(v) — used by Box / Violin / ECDF (v1.8.0: renamed from MathHelpers)

  Serialization/
    IChartSerializer.cs               interface: ToJson(Figure), FromJson(string)
    ChartSerializer.cs                System.Text.Json round-trip — ToSeriesDto() serializes; per-series static
                                      FromSeriesDto(Axes, SeriesDto) factory deserializes (Information Expert;
                                      v1.13.0: no central switch on either side; 1454 -> 888 lines). The fields
                                      every series shares (label, placement, z-order, visibility, point budget)
                                      have ONE writer and ONE reader here; a line's and a scatter's own fields
                                      have one reader each (RestoreFrom), whichever axis the series sits on. A
                                      setting at its default is left out (RestingValueExtensions.OrNullAtRest),
                                      so old JSON reads at the defaults and an untouched figure writes the same
                                      bytes. SeriesDrawnPropertiesRoundTripTests reflects every settable property
                                      of the composed series types and turns red on the one a JSON drops; a
                                      Theory over the composed chart shapes turns red on a shape built from a
                                      series or overlay type that neither reflected contract covers.
                                      What an axes draws besides its series (each axis with its ticks, locator,
                                      formatter and label style; title style; legend; colour bar, the figure's
                                      too; grid; spines; every overlay) is written by AxesToDto and read back by
                                      ONE RestoreAxes, for a subplot and an inset alike. A grid changed over the
                                      theme's grid is written as the grid it draws under the figure's theme
                                      (Axes.GridUnder, the rule the renderer draws by), marked gridOverridesTheme so
                                      a grid switched off stays off under a theme that draws one.
                                      The figure's own settings (theme, spacing, interaction flags and tokens,
                                      responsive SVG, chart id) are written by FigureToDto and read back by
                                      DtoToFigure, each left out at a new figure's value; the theme is left out
                                      while it is the default instance. AxesDrawnPropertiesRoundTripTests reflects
                                      those parts the same way (parts "Figure" and "Theme", with an exemption list
                                      that gives a reason per entry). A number that is not finite is written as the
                                      string "NaN", "Infinity" or "-Infinity" and read back as itself (one number rule
                                      for the writer and the MCP reader, ChartSerializer.ReadOptions); a colour with
                                      transparency is written #RRGGBBAA and an opaque one #RRGGBB as always
                                      (ColorJsonConverter). ComposedChartRoundTripTests holds every composed chart to a
                                      write, a read back that draws the same picture, and a missing value that is never
                                      written into the picture.
                                      PACKAGE DATA: SeriesDto.PackageData (a JsonElement?) carries what a package writes of its OWN series. Core writes
                                      it for no series and reads it for none: the series of the package reads it in its own FromSeriesDto (the maps of
                                      MatPlotLibNet.Geo). It is left out when absent. A 1.23.0 core ignores the field and drops the series it belongs to
                                      with the diagnostic for a kind it does not know; an older MCP server refuses a spec that carries it. A package
                                      registers the reader of its series with SeriesRegistry.Register (Geo: [ModuleInitializer], and the public
                                      GeoPolygonSeries.RegisterReader() for a process that reads the JSON before it has used any type of the package)
                                      THE Y AXES on the wire (SlotsPastSlot0ToDto writes, RestoreSlotsPastSlot0 reads, one path
                                      per side). The 1.23.0 fields are frozen and mean what 1.23.0 made them mean: yAxis and series
                                      are the primary (left slot 0), secondaryYAxis and secondarySeries right slot 0, and the
                                      entries of extraRightYAxes and leftYAxes the slots past slot 0 of their side, in OUTWARD
                                      order (entry i is slot i + 1; the primary is never an entry). A 1.23.0 reader holds one entry
                                      per side and MERGES every further entry onto it, so the writer puts at most one slot past
                                      slot 0 in each frozen list (YAxisSlotsPastSlot0Dto.FrozenCap) and every further slot, in the
                                      same order, in a field a 1.23.0 reader skips: rightYAxesOverflow and leftYAxesOverflow, left
                                      out when empty. Every slot has exactly one carrier, there is no version marker, and the reader
                                      takes the frozen list and then the overflow list. So a figure within the old caps (two
                                      right axes, one left past the primary) writes the bytes 1.23.0 wrote, and a JSON 1.23.0
                                      wrote reads into the container with every slot in its place and writes back byte for byte
                                      (Compat/v1.23.0-four-axes.json, written by the published 1.23.0, is pinned outside the
                                      regenerated golden files). A 1.23.0 reader that meets a newer file keeps the first axes and
                                      DROPS the overflow: it draws exactly the capped figure, and nothing is merged onto a wrong
                                      axis. yAxisTitleGap (the container's TitleGap) is written only when it is not 4, and a
                                      negative, NaN or infinite value is refused on read. The colour of an axis travels on its
                                      own axis fields; spines.left/right.color keep meaning the plot's left and right edge, so
                                      slot 0 of a side. AxesDrawnPropertiesRoundTripTests reads the container through
                                      DrawnAxesFixtures, which has a row for every seat of the wire: left slots 0 to 2 and
                                      right slots 0 to 2 (the third of each side travels in an overflow field) and the
                                      container with its gap, so a Y axis setting a JSON drops turns a Theory red
    ThemeDto.cs                       a theme as data: every property written where it differs from the plain
                                      Theme.Default and read back over it, so a ThemeBuilder theme survives too
                                      (PropCyclerDto: the cycler's four lists as built; AlarmPaletteDto). The name
                                      is a label, never a preset lookup.
    SubPlotSpacingDto.cs,             the figure's spacing (and a theme's default spacing) and the interaction
    InteractionThemeDto.cs              tokens: each field left out at a new value's
    IJsonDescribed.cs                 internal interface IJsonDescribed<TDescription>: Describe(). The built-in tick
                                      locators, tick formatters and normalisers, and the colour maps built in code,
                                      implement it explicitly; a type written outside the library does not, and the JSON
                                      leaves it out. Describe may return null (the reverse of a map that cannot be described).
    TickLocatorDto.cs                 the described families: one base record per family, closed by
    TickFormatterDto.cs                 [JsonPolymorphic] + [JsonDerivedType] ("type" is the discriminator);
    NormalizerDto.cs                    each derived record restores its value (virtual Restore). An unknown kind
                                        reads as the base record, which restores nothing and emits a
                                        ChartDiagnostic. No registry, no switch: a new built-in adds its record,
                                        one attribute line and its Describe.
    NormalizableExtensions.cs         the one rule by which a series that maps values onto a colour map (every
                                      INormalizable: 18 types) writes its normaliser (DescribeNormalizer: a built-in
                                      describes itself, anything else is left out) and takes it back
                                      (RestoreNormalizer); a series' ToSeriesDto/FromSeriesDto call it, none holds
                                      its own copy of the cast.
    ColorMapDto.cs                    a colour map as data, closed like the tick locators: linear (colours, their positions,
                                      under / over / bad colours), listed, reversed (holds the map it reverses) and named (a map
                                      the library ships, by its registered name)
    ColorMapDescriptionExtensions.cs  the one rule by which every series drawn through a colour map, the colour bars and a pair
                                      grid's off-diagonal map write their map (Describe: a shipped map by its name, as it always
                                      went; a map built in code as what it is made of; a map of the user's own type by a registered
                                      name only) and take it back (RestoreColorMap; a description wins over a name beside it).
                                      ColorMapRegistry.IsShipped says which maps every reader knows by name
    RestingValueExtensions.cs         OrNullAtRest(resting): a value is written only while it differs from what a new object holds,
                                      so old JSON reads at the defaults and an untouched figure writes the bytes it always wrote
    HierarchicalSeriesExtensions.cs   the one rule by which treemap, sunburst and dendrogram write the settings they
                                      share (colour map, its limits, the normaliser, whether nodes are labelled:
                                      DescribeSharedSettings) and take them back (RestoreSharedSettings); each
                                      series adds only the settings of its own type.
    LegendDto.cs, ColorBarDto.cs,     the record parts of an axes: each owns its From(model) (null at rest) and its
    GridDto.cs, TickConfigDto.cs,       To...() back; a field at its resting value is left out
    TextStyleDto.cs
    SeriesRegistry.cs                 ConcurrentDictionary<string, Func<Axes, SeriesDto, ISeries?>> mapping each
                                      discriminator to the series' own FromSeriesDto factory; a table
                                      only, no series-specific logic (v1.13.0: cyclomatic complexity 173 -> 1).
                                      ResetForTests is an [Obsolete] public shim; real reset is internal
                                      ResetForTestsInternal (InternalsVisibleTo)

  Diagnostics/
    ChartDiagnostics.cs                new v1.13.0: process-wide diagnostic event bus — static event Emitted +
                                      Emit(ChartDiagnostic); ChartDiagnostic readonly record struct(Source, Message,
                                      Exception?) defined in the same file. Library-wide observability seam for
                                      degraded-but-non-throwing paths (regression fit failure, unknown deserialize
                                      discriminator, ChartSubscriptionClient teardown races) — emitting never
                                      changes default behavior

  Styling/
    RcParamKeys.cs                    static constants for all supported rcParams keys
    RcParams.cs                       global config registry: typed Dictionary + AsyncLocal scoping
    StyleSheet.cs                     named bundle of RcParams overrides + Theme bridge
    StyleContext.cs                    IDisposable scoped override: push on construct, pop on Dispose
    StyleSheetRegistry.cs             thread-safe ConcurrentDictionary: name -> StyleSheet
    BlendMode.cs                      enum (Normal, Multiply, Screen, Overlay) + CompositeOperation utility
    Color.cs                          readonly record struct (R, G, B, A) + named colors + hex
                                      Color constants: Tab10Blue, Tab10Orange, Tab10Green, GridGray,
                                      EdgeGray, Amber, FibonacciOrange (replace magic hex strings)
    ColorExtensions.cs                Color.Luminance() (Rec. 709 Y'), Color.ContrastingTextColor() — auto black/white for readable cell labels
    Font.cs                           sealed record (Family, Size, Weight, Slant, Color)
    Theme.cs                          30 built-in themes (incl. OpsNight/OpsPanel/OpsWarm/OpsContrast operator backgrounds); DefaultLegendPosition (matplotlib's legend.loc: UpperRight for the classic theme, Best for every other; carried by ThemeDto, omitted at rest) + AlarmPalette (Resting/Warning/Critical/Unknown — colour is reserved for what needs attention; the resting state wears none)
    AlarmPalette.cs                   readonly record struct: the colours a theme reserves for abnormal states (+ ColorBlindSafe Okabe-Ito, HighContrast WCAG AAA) + GridStyle sealed record + PropCycler? property (new v0.8.8)
    LineStyle.cs                      enum: Solid, Dashed, Dotted, DashDot, None
    MarkerStyle.cs                    enum: None, Circle, Square, Triangle, Diamond, etc.
    DashPatterns.cs                   canonical dash ratios shared by SVG + MAUI + Skia renderers
    PropCycler.cs                     LCM-based multi-property cycler; CycledProperties record struct (new v0.8.1). Its colours, line
                                      styles, markers and line widths are all drawn: SeriesSlot hands the entry for a series' slot out
    PropCyclerBuilder.cs              fluent builder: WithColors(), WithLineStyles(), WithMarkerStyles(), WithLineWidths() (new v0.8.1)

    ColorMaps/
      IColorMap.cs                    interface: GetColor(double normalized) -> Color
      LerpColorMap.cs                 internal sealed: linear interpolation between Color[] hex stops
      ReversedColorMap.cs             public decorator: inverts normalized input, appends _r suffix
      ColorMapRegistry.cs             thread-safe ConcurrentDictionary, case-insensitive, auto-registers _r variants
      INormalizer.cs                  interface + 8 implementations: LinearNormalizer (singleton), LogNormalizer,
                                        TwoSlopeNormalizer(center), BoundaryNormalizer(double[]), SymLogNormalizer,
                                        PowerNormNormalizer, CenteredNormNormalizer, NoNormNormalizer; each writes itself to JSON
                                        (IJsonDescribed<NormalizerDto>), and a missing value never reaches one
      ColorMaps.cs                    9 perceptually-uniform + rainbow: Viridis, Plasma, Inferno, Magma,
                                        Coolwarm, Blues, Reds, Turbo (prefer over Jet), Jet (legacy)
      SequentialColorMaps.cs          21 sequential: Cividis, Greens, Oranges, Purples, Greys, YlOrBr,
                                        YlOrRd, OrRd, PuBu, YlGn, BuGn, Hot, Copper, Bone, BuPu, GnBu,
                                        PuRd, RdPu, YlGnBu, PuBuGn, Cubehelix
      DivergingColorMaps.cs           9 diverging: RdBu, RdYlGn, RdYlBu, BrBG, PiYG, Spectral, PuOr,
                                        Seismic, Bwr
      CyclicColorMaps.cs              3 cyclic (start≈end): Twilight, TwilightShifted, Hsv
      QualitativeColorMaps.cs         14 qualitative: Tab10, Tab20, Set1, Set2, Set3, Pastel1, Pastel2,
                                        Dark2, Accent, Paired, OkabeIto (color-blind safe),
                                        Petroff6, Petroff8, Petroff10 (accessible sequences, Petroff 2021)
      AdditionalColorMaps.cs          13 more: Gray, Spring, Summer, Autumn, Winter, Cool, AfmHot, PRGn,
                                        RdGy, Rainbow, Ocean, Terrain, CMRmap
      PerceptualColorMaps2.cs         5 Seaborn perceptually-uniform: Rocket, Mako, Crest, Flare, Icefire
                                      Total: 74 base colormaps × 2 (+ _r reversed) = 148 registered names
      ColormapExtensions.cs           IColormappable.GetColorMapOrDefault(fallback) + int.ColormapFraction(count, singletonT)
                                        — replaces 17 inline `?? Viridis|Tab10` sites and 3 `index/(count-1)` sites
```

## Where a verb lives

A method of `AxesBuilder` itself adds one series or sets one property of the axes. A verb that composes a chart from
series that already exist (it adds no series type, no `SeriesRegistry` key and no serializer path, so the chart travels
through JSON as its parts) is an extension method on `AxesBuilder`. 17 verbs live there today, and each one:

- sits in namespace `MatPlotLibNet`, so it needs no `using` beyond the one the builder already takes;
- sits in folder `Builders/`, in one static class per family, the family decided by the shape of the data the caller holds:
  - `CategoricalAxesExtensions`: data keyed by names (categories, a `BarGroup` per group). Verbs: `Lollipop`, `Dumbbell`, `Pareto`, `SlopeGraph`, `DotPlot`, `ParallelCoordinates`, `Marimekko`.
  - `DistributionAxesExtensions`: a chart estimated from a sample (a `NamedSample` per group, a `SurvivalGroup` per group of survival data). Verbs: `Ridgeline`, `QQPlot`, `KaplanMeier`, `Raincloud`.
  - `XYAxesExtensions`: x and y arrays, values over x positions. Verbs: `BumpChart`, `ControlChart`, `StreamGraph`, `Horizon`.
  - `GridAxesExtensions`: cells of a grid. Verbs: `CalendarHeatmap`, `Spy`.
- takes at most 4 parameters besides `this`, the optional inputs of its computation (a bandwidth, a week start, a
  threshold) before `configure`, which comes last and may be left out. A verb that draws one series type takes
  `Action<TSeries>` and runs it once for each series it draws; one that draws several types takes `Action<XxxParts>` and
  runs it once;
- returns the builder, so verbs chain with every other call.

The types the verbs introduce:

- Parts: `DumbbellParts`, `ParetoParts`, `QQPlotParts`, `ControlChartParts`, `RaincloudParts`, `HorizonParts`. Each is a non-positional readonly record struct with an internal constructor and get-only properties, so only the library makes one, and each property is the very series (or overlay) on the axes, so a change made through it is the chart's.
- Named data: `BarGroup`, `NamedSample`, `SurvivalGroup`, `ControlLimits`. Each is a public readonly record struct: a name and its values, or the limits of a chart.
- A BarGroup (a name and one value per place) is the named-values type of eight verbs: GroupedBar, Dumbbell, SlopeGraph, DotPlot, ParallelCoordinates, Marimekko, BumpChart and StreamGraph. What the name stands for is each verb's to say: a legend entry, the class of a row, a segment of a column.

What holds for every verb:

- One drawn unit is one series, OR several series sharing one colour group. A series takes its own JSON entry and its own
  click target always (`SlotIndex`, positional, read by the legend's hit-test and the SVG's `data-series-index`, never
  affected by colour grouping, so a click on one member of a group never crosses to another); it takes its own colour
  slot and, when it carries a label, its own legend entry UNLESS it shares an internal `ChartSeries.ColorGroup` value
  with the series next to it in `Axes.AllSeries` — a verb that draws a curve alongside a band and censor marks that must
  read as the curve's own (`KaplanMeier`), or a violin, a box and raw points that must read as one group's three views of
  itself (`Raincloud`), asks the axes for a fresh group (`AxesBuilder.NextColorGroup()`, derived from the highest group
  already held, never stored) and sets it on each member it adds back to back. `SeriesSlotExtensions.ColorSlots` folds a
  contiguous run of one group into ONE rendered colour slot; a group that is not contiguous is not supported — every verb
  that groups series adds them one after another, and nothing in the library splits a group apart afterward. The
  decoration members of a group carry no label of their own, so only the series the group is named for gets a legend
  entry (`AxesLegendEntryExtensions.HasLegendEntry` already decides that from the label alone, unchanged).
- A verb sets no axis limit unless the chart cannot be read without it: `Pareto` puts its two Y axes on one scale,
  `BumpChart` puts rank 1 on top, `Spy` leaves half a cell past the grid. A limit the caller sets afterwards wins.
- A missing value (NaN) is left out of a sample or left as a gap in a line, and never drawn as a zero or bridged; the one
  value a verb refuses when it is missing is a survival duration, because a subject without a time cannot be counted at
  risk. A verb refuses what it cannot draw with an
  `ArgumentException` that names the parameter (an `ArgumentNullException` for an argument that is missing), before the
  first series is added, so a refused call leaves the axes as it was.
- A composed chart is its parts in JSON. Read back (`FromJson`, the MCP server, the transports) it draws the same picture
  and is not computed again: a reloaded Pareto chart does not sort or accumulate. None of the verbs is a chart type of the
  MCP server; a spec names the parts.
- The verb draws exactly what the same chain of public calls draws: the tests compare the SVG of each verb with the SVG
  of that chain (`ComposedChartShapes` keeps most of the chains).

The two container verbs, `WithAddRightYAxis` and `WithAddLeftYAxis`, are in no family: they add a Y axis to the
container, not a composed chart, so they are instance methods of `AxesBuilder`. Each stacks one more axis on its side per call
and hands a `YAxisSlotBuilder` to a required `configure` callback that comes last; they return the builder and take no more
than four parameters. `VerbPlacementContractTests` finds them through that callback type, so a container verb added later is
held to the same shape without an edit.

The verbs of `MatPlotLibNet.Geo` (`GeoAxesExtensions`: `WithProjection`, `Land`, `Ocean`, `Borders`, `Coastlines`, `Choropleth`,
`Points`, `Bubbles`, `HexBins`, `Flows`, `Cartogram`, `Dorling`) live in that package, in namespace `MatPlotLibNet.Geo`, because the
package ships its own series and its own data. They have the same shape (extensions of `AxesBuilder` that return it, at most four
parameters besides it, `configure` last and optional) and are held to it by a test of the Geo test project (GeoVerbShapeTests),
which reads the class by reflection, as `VerbPlacementContractTests` does for the families above. A map layer is refused before anything is added, so a
refused call leaves the axes as it was; `Points`, `Bubbles`, `Dorling`, `HexBins` and `Flows` draw existing series (`ScatterSeries`,
`HexbinSeries`, `LineSeries`) and travel as those series, and the layers that draw a map (`Land`, `Ocean`, `Borders`, `Coastlines`,
`Choropleth`, `Cartogram`) are one series kind, `GeoPolygonSeries`.

A family's class is added together with its first verb. `GroupedBar`, `DensityScatter` and `Threshold` stay instance
methods of `AxesBuilder`, as they shipped: they predate the rule, and moving a shipped method into an extension class
keeps the source compiling but breaks every caller compiled against the old method, a binary break that package
validation rejects. CONTRIBUTING.md says how a new verb is added and tested.

The verbs share internal owners instead of each holding a copy of a rule:

| Rule | Owner | Read by |
|---|---|---|
| where a category and a bar on it sit | `CategorySlot` | the bar and count renderers, the category ticks, every verb that puts a mark on a category |
| categories written as an axis's ticks, on slot centres, on some slots, or at positions of their own | `CategoryTickExtensions` | every verb that names categories, the clustermap, the axes renderer |
| one value per category, in one wording | `BarGroupExtensions`, `CategoryValuesExtensions` (`ThrowIfMisaligned`) | the verbs that take a group or an array |
| the stable largest-first order | `SortOrderExtensions.DescendingOrder` | `Pareto`, `DotPlot` |
| which observations of a sample can be drawn, and how a refusal is worded | `NamedSampleExtensions` | `Ridgeline`, `QQPlot`, `ControlChart` |
| the product-limit estimate of survival data | `SurvivalGroupExtensions.ProductLimit` | `KaplanMeier` |
| where a day stands in a calendar | `CalendarGrid` | `CalendarHeatmap` |
| the name an axis shows at a position | `AxesTickSourceExtensions` (`PrimaryTickSource`, `NamesAt`) | the axes renderer, the data table, which keys a verb's table by its categories |
| the top edge of each stacked layer | `StackedBaselineExtensions.TopEdgesFor` | the stacked-area series and `StreamGraph` |
| what a bar stands on in a stack, on two sums (above zero, below zero) | `StackableExtensions` (`StackedFeet`, `StackedEnds`) | the stacked bar renderer and range, the polar bars (`RadialScale`) |
| which arrays of a series fit, in one wording | `ArrayLengthExtensions` (`ThrowIfMisaligned`) | every verb that builds a series, `FromJson` through them, the MCP reader (it asks the library) |
| which values a colour bar names, and where | `ColorBarScale` (`For`, `WidestLabel`, `FractionOf`), `MaxNLocator.ForLength` | the vertical and horizontal bar, the figure's bar, the layout that reserves its room |
| which series a colour bar reads | `ColorBarProviderExtensions.ColorBarProvider` | the axes' bar, the figure's bar, the layout |
| the radial scale of a polar axes: extent, rings, stacked feet | `RadialScale` | the polar grid and every polar series |
| the fill of a contour: the bands between its levels, over the grid | `ContourBandFillExtensions.FillContourBands` | `ContourSeries` with `Filled` and `ContourfSeries` |
| the one projection of a map axes | `GeoAxesExtensions` (recorded by `WithProjection`, read by every layer) | `Land`, `Ocean`, `Borders`, `Coastlines`, `Choropleth`, `Points`, `Bubbles`, `HexBins`, `Flows`, `Cartogram`, `Dorling` |
| one bandwidth for several groups, a shared grid, a density at given points | `GaussianKde` | `Ridgeline`, the density curve |
| the inverse of the normal CDF | `StandardNormal.Quantile` | `QQPlot`, the regression band |
| what NaN and infinity mean to a chart | `MissingValueExtensions` | every renderer and every data range |
| the rectangle between two data corners | `Rect.FromCorners` | every renderer that draws a bar, box, cell or segment from two corners |

## Data flow

### Static SVG rendering

```
Plt.Create()           user builds figure via fluent API
    |
FigureBuilder.Build()  produces Figure model
    |
figure.ToSvg()         calls SvgRendererDefault.Current.Render(figure)
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
    |                  places every Y axis slot of the container once (range, transform, ticks, band, offset)
    |                  and draws each with the one slot part, on either side, through its own scale
    |                  renders annotations (text + arrows)
    |
SvgSeriesRenderer      visitor dispatches to type-specific rendering
    |                  transforms data points to pixels via DataTransform
    |                  emits SVG elements to SvgRenderContext
    |                  wraps data elements in <title> for tooltips when enabled
    |
SvgRenderContext       accumulates SVG markup in StringBuilder
    |
string                 complete <svg>...</svg> document
                       optional scripts appended at end of body:
                         SvgInteractivityScript  (EnableZoomPan)
                         SvgLegendToggleScript   (EnableLegendToggle)
                         SvgLegendDragScript     (EnableLegendToggle — co-emitted)
                         SvgCustomTooltipScript  (EnableRichTooltips)
                         SvgHighlightScript      (EnableHighlight)
                         SvgSelectionScript      (EnableSelection)
                       data-series-index / data-legend-index attributes emitted
                         when Figure.HasInteractivity via Axes.EnableInteractiveAttributes
```

### Polymorphic figure transforms

```
IFigureTransform           common interface: Transform(Figure, Stream), and Transform(Figure, Stream, SaveOptions)
    |                      for the transforms that read a save setting
FigureTransform            abstract base: holds ChartRenderer
    |
    +-- SvgTransform       writes UTF-8 SVG text (also implements ISvgRenderer); indentation, decimals, title, author
    +-- PngTransform       writes PNG bytes via SkiaSharp (MatPlotLibNet.Skia) at the dpi of the options or the figure; title, author
    +-- PdfTransform       writes PDF bytes via SkiaSharp (MatPlotLibNet.Skia); title, author
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
figure.ToJson()        calls ChartSerializerDefault.Current.ToJson(figure)
    |
ChartSerializer        Figure -> each series via ISeriesSerializable -> JsonSerializer.Serialize()
    |                  the Y axis container -> yAxis (primary), secondaryYAxis (right slot 0), the slots past slot 0 of
    |                  each side in extraRightYAxes / leftYAxes (the old caps) and rightYAxesOverflow / leftYAxesOverflow
    |                  (every one past them), yAxisTitleGap only when it is not 4
    |
string                 JSON with camelCase, null-ignoring, Color as hex, a number that is not finite as the string "NaN",
                       "Infinity" or "-Infinity"; a value at its default is left out

ChartSerializerDefault.Current.FromJson(json)
    |
ChartSerializer        JsonSerializer.Deserialize() -> SeriesRegistry resolves type -> Figure
    |                  each frozen list, then its overflow list, stacks its slots on the container in outward order
    |
SeriesRegistry         ConcurrentDictionary<string, Func<...>> maps type discriminator to factory
```

The Y axes are the one part of the wire that is shaped by the reader of an older release: see the Y axes paragraph of
`ChartSerializer.cs` above for what 1.23.0 reads of a newer file.

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

Client gesture (server-interactive chart): the SVG's script sends a zoom, pan, reset or legend event over the hub
    |
ChartHub               writes the event to the chart's channel and returns
    |
ChartSession           ONE reader task per chart drains the channel in order and applies each event to the figure
                       (TryApplyTo), publishing once per drained batch. One event costs the chart only itself: an event
                       that names a subplot the figure lacks is dropped and counted (DroppedEventCount), one that throws
                       (an application event, a handler) is counted (FaultedEventCount, LastFault) and the reader goes
                       on with the next. Only cancellation and a closed channel end the reader
```

## Left out on purpose

Things a reader may expect that the library does not do, with the reason:

- **Radar rings stay at fifths of the axis.** They carry no value labels, and every radar is normalised to its own documented `MaxValue`.
- **`AutoLocator` keeps its nearest-step rule** (matplotlib takes the smallest step at or above). Changing it moves every axis picture. The tick count of a colour bar and of polar rings is chosen by length on purpose (`MaxNLocator.ForLength`); an axis is not.
- **A colour bar does not follow the normaliser.** Its gradient and its ticks are linear in the value, whatever normaliser the series has. This is a limit of colour mapping, stated on the roadmap, not of tick placement.
- **A polar axes has no offset origin** (matplotlib's `rmin`): `PolarTransform` maps radius 0 to the centre.
- **A colour bar's tick count cannot be set.**
- **A filled `ContourSeries` has no colour bar of its own;** `ContourfSeries` is the contour that backs one.
- **The legends of a pie, a donut and a funnel are unchanged:** one entry for the series. Only their colour slots span their parts.
- **`RegressionSeries`, `ResidualSeries` and the tolerant XY models are not refused at the verb.** What does not pair is skipped and reported through `ChartDiagnostics`, by documented, tested design.
- **No H3 cells.** They need the H3 index library, which the package does not ship; `HexBins` covers the need.
- **No contiguous (diffusion) cartogram.** The contiguous methods are iterative field algorithms; `Cartogram` resizes each country about its centre and `Dorling` puts a bubble on it.
- **The MCP server does not know the Geo package.** A map is not an accepted chart type of a spec; the catalog and the schema tests pin it.
- **Bubble and scatter size formulas stay as they are,** and the map bubbles use the scatter's rule. Changing the bubble series would move its pictures.
- **A colour bar's extend wedge is 10 % of its length** (matplotlib's is 5 %) **and the polar angle grid is every 30 degrees** (matplotlib's is every 45). Neither is part of any request; changing them moves pictures.

## Key design patterns

| Pattern | Where | Why |
|---------|-------|-----|
| Fluent builder | FigureBuilder, AxesBuilder, ThemeBuilder, YAxisSlotBuilder | matplotlib-style method chaining |
| Visitor | ISeriesVisitor + ChartSeries.Accept() | extensible per-series rendering without switch |
| Strategy | IRenderContext (SVG, MAUI, Skia), AxesRenderer (Cartesian, Polar, 3D) | multiple output targets and coordinate systems from same model |
| Template method | FigureTransform base class, AxesRenderer base class | shared renderer, format/coordinate-specific overrides |
| Fluent result | TransformResult record | polymorphic ToStream/ToFile/ToBytes from any transform |
| Self-serialization | ISeriesSerializable.ToSeriesDto() + per-series static FromSeriesDto(Axes, SeriesDto) on all 83 series | each series knows how to both serialize and deserialize itself (v1.13.0: no central switch on either side) |
| One value, not N fields | StreamingSeries → RingBuffer&lt;StreamingPoint&gt;; StreamingCandlestickSeries → RingBuffer&lt;OhlcBar&gt;; StreamingSignalSeries → RingBuffer&lt;SignalSample&gt; (the sample carries its own ordinal) | a value that must stay consistent is stored as one object, so a reader running beside the writer never sees fields from different samples. Locking a separate buffer per field keeps each buffer safe, but not the invariant across the buffers. A measured run showed that invariant failing |
| Self-tabulation | ISeries.ToDataTable() overridden per series; FigureDataTableExtensions groups the tables, writes their captions and reads a position the axes names as that name (the axes owns the names, through the rule its ticks are drawn by) | this follows the same principle as self-serialization: the type that owns the data also owns its column names, so a new series type needs no change in any central file |
| One shaped text, three readers | Skia: TextShaper → ShapedText, read by SkiaFontMetrics (width), SkiaGlyphPathProvider (SVG outlines) and SkiaRenderContext (pixels) | the renderer draws text at the same width the layout measured for it; bidi levels come from Core's BidiAlgorithm and scripts from HarfBuzz, so Arabic letters join and Hebrew runs reverse the same way in every output |
| Ambient context | RcParams + AsyncLocal + StyleContext | thread-safe global config with scoped overrides |
| Registry | SeriesRegistry (ConcurrentDictionary<string, Func<Axes, SeriesDto, ISeries?>>) | thread-safe lookup from a discriminator to the series' own FromSeriesDto factory; thin table only |
| Generic base classes | XYSeries, PolarSeries, GridSeries3D, HierarchicalSeries; CircularRenderer<T>, OhlcStreamingIndicatorTests<T> | DRY shared properties and behaviour across series and renderer families |
| Interface segregation | IHasDataRange, IPolarSeries, I3DGridSeries, I3DPointSeries, IPriceSeries, IColormappable, INormalizable, ICategoryLabeled, IColorBarDataProvider, IStackable, IHasColor, IHasAlpha, IHasEdgeColor, ILabelable, IHasBarOrientation (internal) | narrow contracts for cross-cutting concerns |
| DI interfaces | IFigureTransform, IChartRenderer, ISvgRenderer, IChartSerializer | testable, replaceable services |
| SRP extension methods | FigureExtensions (Save, Transform, ToSvg, RegisterTransform) | FigureExtensions holds the output methods, so FigureBuilder only builds the figure |
| Static defaults | ChartSerializerDefault, ChartRendererDefault, SvgRendererDefault, FontMetricsDefault, GlyphPathProviderDefault | non-DI usage for console apps, each a single separated slot |
| Record types | Font, GridStyle, Legend, TickConfig, Color, Point, Rect, TransformResult | immutable value objects |
| Parallel rendering | SvgTransform + per-subplot SvgRenderContext | multi-core subplot rendering |
| Thread safety | volatile fields, ConcurrentDictionary for GlobalTransforms, AxesRenderer registry, SeriesRegistry | safe concurrent access |
| Adapter | LegacyAnimationAdapter | bridges AnimationBuilder to IAnimation\<TState\> |
| Delegate extraction | SvgTransform.BuildSvgDocument, ChartSerializer.ApplyEnum | DRY via higher-order functions |
| Default interface method | IRenderContext.DrawRichText; ISeries.ToDataTable() | a new member reaches every implementor without breaking any of them; the default supplies the fallback answer (plain text; "no tabular form") |
| State machine | MathTextParser | single-pass text classification into Normal/Superscript/Subscript spans |
| Two-pass layout | ConstrainedLayoutEngine | measure text extents first, then compute margins |
| One obstacle map, one score, one selector | `MarkObstacles` (`Builder`, `Badness`, `FirstLeastCovered`), `AxesRenderer.LegendObstacles` / `CollectMarks`, `SeriesRenderer<T>.AddMarks` | the legend at `Best` and the annotations that avoid overlap look at the same marks as drawn, score a candidate by the same matplotlib terms and take the first least covered one, so the two cannot disagree about what covers what; a renderer reports its marks from the pixel geometry it draws with, never from a copy of it. The order is acyclic: the series' marks, then the legend (marks and the annotations' boxes at their anchors), then the annotations that opted in (marks, the chosen legend box, the annotations that stay, the ones placed already); the legend is still drawn after them |
| One enumerator for the legend | `Axes.LegendEntries(directLabelsDrawn)` → `LegendEntry`, one per `LegendPart` | the drawing, the click hit-test and the margin reservation read one list, so the number of rows, their order, their index, their colour slot, their swatch and their text cannot differ between them; "a series is named in the legend" is decided in one place, and a series with named parts (a stack plot's layers) says them itself |
| One rule for where a category sits | `CategorySlot` (`Of`, `Bar`, `Around`), `BarSeries.SlotOf`, `CategoryTickExtensions` | the bar and count renderers, their data ranges, the category labels, a candlestick body, the indicator shortcuts and the category ticks read one rule, so a label, a mark and the bar it belongs to cannot drift apart: an edge-aligned bar chart's range and a count plot's labels had drifted from their bars before they shared it |
| One owner of a series' colour and stroke | `SeriesSlotExtensions` (`Axes.SlotIndex`, `Theme.SeriesSlot`), `SeriesSlot` (`ColorOf`, `LineStyleOf`, `LineWidthOf`, `LineMarkerOf`, `ScatterMarkerOf`, `StrokeOf`) | the primary passes, the part of every Y slot, the line renderers and the legend swatch ask the same owner, so a line on any axis and its swatch cannot wear different colours, dashes, widths or markers under a cycler theme — `SlotIndex` sums every right slot's series before counting a left slot's or the top axis', so two slots on different sides never collide on the same identity; a pie, a donut, a funnel and a stacked area take their colours from the theme through it too, one slot for each part (`ChartSeries.ColorSlotSpan`), and the series after them the slot past them |
| Compose existing series | `CategoricalAxesExtensions`, `DistributionAxesExtensions`, `XYAxesExtensions`, `GridAxesExtensions`, the `XxxParts` record structs | a chart that is a drawing of several series adds no series type, no registry key and no serializer path: one drawn unit is one series (a colour slot, a legend entry, a JSON entry), the chart travels as its parts, and the verb draws what the same chain of public calls draws |
| One missing value | `MissingValueExtensions` | a colormap, a renderer and a data range ask one class what NaN and infinity mean: a line breaks at the gap, a mark that cannot be placed is skipped before any render context sees it, and the automatic range leaves it out. When each layer tested `double.IsNaN` for itself, charts wrote NaN into the SVG |
| A reflected contract | `SeriesDrawnPropertiesRoundTripTests` over `DrawnSeriesFixtures`, `AxesDrawnPropertiesRoundTripTests` over `DrawnAxesFixtures` | every settable property of every series type and every part of an axes is found by reflection, set to a value that is not its default, and compared after a JSON round trip, so a property added without its DTO field turns a Theory red and names itself; a property no JSON can carry is named in a list with the reason, and a series type that is neither under the contract nor named fails a test |
| Closed polymorphic DTO family | `TickLocatorDto`, `TickFormatterDto`, `NormalizerDto`, `ColorMapDto` with `IJsonDescribed<T>` | a built-in describes itself and a derived record restores it through a virtual, so JSON carries the kind and what it was made with; an unknown kind reads as the base record and emits a diagnostic; no registry and no switch |
| A series states what it is | `ChartSeries.DrawsWithoutAxes`, `LegendSwatch`, `LegendParts`, `ColorSlotSpan`, `ProvidesColorBar`, `RadialExtent`, `DrawsOwnLabel`, `FixesYRange` (internal virtual) | the axes, the legend, the colour bar and the polar scale ask the series; none of them holds a list of series types, so a new kind of series (or one in a package) needs no edit in any of them. `AxisLessCapabilityContractTests` checks each `DrawsWithoutAxes` answer against a measurement (the same marks under any limits, or marks that move with them) |
| One scale per concern | `ColorBarScale`, `RadialScale`, `MaxNLocator.ForLength` | where a colour bar names its values and where polar rings stand are each decided once, from the length they are drawn along; the drawing, the layout's reserve and every series that draws on them read the same value, so a tick and a mark cannot disagree |
| Refused at the verb | `ArrayLengthExtensions`, the series verbs, `ChartSpecReader.RefuseWhatTheLibraryRefuses` | a series that cannot be drawn from its data is refused when it is built, in one wording, and a figure read from JSON builds through the same verbs; the MCP reader asks the library whether a series fits and names only the place it stands |
| Package data | `SeriesDto.PackageData`, `SeriesRegistry.Register`, `GeoPackageData` | a package carries its own series through the shared DTO without core knowing its shape; the package writes and reads one shape (`GeoPackageData`) and registers its reader when it loads |
| A value at rest is not written | `RestingValueExtensions.OrNullAtRest` | JSON from an earlier release reads at the defaults and a figure that never touches a setting writes the bytes it always wrote |
| Edge bands: the drawer measures | `AxesRenderer.MeasureEdgeBands` → `EdgeBands`; `ConstrainedLayoutEngine.MeasureRightBand` → `RightBand`; `ConstrainedLayoutEngine.LeftNeed` / `BottomNeed` / `LeftAxisBands`; `AxisStackingExtensions`; `DirectLabelMetrics` | layout and renderer share one rule for what sits past the four spines (mirrored ticks, every Y axis of the container on either side, end labels, the category names on the Y axis, the X tick labels and X title below the plot): the renderer that draws a band reports its size, and the layout engine reserves exactly that in both layout paths — every slot on either side, the primary included, follows the SAME rule, not a second one |
| One container for the Y axes | `YAxisContainer` (`Left`, `Right`, `TitleGap`, `IdentityOrder`), `YAxisStack` (`Add`, `FirstOrAdd`, `OutwardOffset`, `SlotAtOutward`), `YAxisSlot` | the primary Y axis, the first right axis and every axis stacked past them are the SAME kind of thing, held in one place, with no cap and no empty "ghost" axis: the builders, the renderer, the legend, the end labels, the serializer and the interaction layer all ask the container, so a seat for a fifth axis needs no new list and no new branch. The two old shapes (a primary held in no list, and two parallel lists of slots with a cap each) were three things a caller reached for depending on how many axes already stood on a side |
| One rule per concern for a Y axis | `YAxisTitleExtensions` (`YTitleExtent`, `DrawYAxisTitle`), `TitleClearance` + `FacingInk` (`TitleOffset`), `YSideTerms`, `PlacedYSlot`, `CartesianYAxisSlotPart` | where a Y title stands, how much room a slot takes, where its ticks are, which way it faces and how it is drawn are each decided once: the layout reserves by the rule the renderer places by, the left and the right are one part fed by side terms (no branch on the side), and the primary is left slot 0 of the same part. The gap between a title's ink and its tick labels is therefore equal on both sides for every slot, held by a mirror test |
| The wire shows outward-only | `YAxisStack.OutwardOffset` / `SlotAtOutward`, `SecondaryAxisRange`, `GetSecondaryDataRanges`, the `data-sec-*` attributes, `YAxisSlotsPastSlot0Dto` | the interaction wire and the 1.23.0 JSON fields number only the axes past the primary and must keep doing so (an open browser tab and a published reader depend on it); the container counts the primary as left slot 0, so one conversion owns the shift of one and everything else reads it. An axis past the old caps goes in a field a 1.23.0 reader skips, so an older reader loses the extra axes and never merges them onto a wrong one |
| A wrapper over the new shape | the `[Obsolete]` Y axis members (`WithSecondaryYAxis`, `WithOutwardYAxis`, `SecondaryAxisBuilder`, `OutwardYAxisBuilder`, `Axes.SecondaryYAxis`, `SecondarySeries`, `TwinX`, `AddSecondarySeries`, `AddOutwardYAxis`, `RightYAxes`, `LeftYAxes`, `SecondaryAxisSlot`, `PlotSecondary`, `ScatterSecondary`) | a published member that a better shape replaces is not removed (that would break every caller compiled against it) but kept as a wrapper that forwards to the new shape and says what replaces it in its `Obsolete` message; nothing inside the library calls it, and one test file (`ObsoleteYAxisMembersCompatTests`) pins what each still does, the first right axis never stacking among them |
| One rule for which axis carries the categories | `BarOrientationExtensions` (`CategoriesAlongY`, `OnAxes`, `NamesCategoriesAlongY`), `IHasBarOrientation` | a horizontal bar chart is an upright one with its axes swapped: the bar and count ranges are measured upright and put on the axes by one transposition, and the axes renderer names the categories on the axis that carries them through that axis' tick path (`AxesTickSourceExtensions.PrimaryTickSource`, one rule for X and Y, which the data table reads too), so hidden ticks, label size, colour, rotation, mirroring and the grid reach the names as they reach numbers |
| A reversed axis | `Range1D.Interval` / `OrientedLike` / `OrientedBy`, `TickLocatorExtensions.LocateInterval` | a limit set high to low and `Axis.Inverted` draw an axis reversed by one rule: the range runs high to low, the ticks are the ticks of its interval, each drawn where its value is; `Inverted` is a state (matplotlib's `set_inverted(True)`), so the two together reverse the axis once, and a zoom that writes the limits as drawn keeps it reversed |
| One extension opens every series group | `IRenderContext.BeginSeriesGroup` / `BeginSeriesClip` (`SeriesGroupExtensions`) | the primary passes and the part of every Y slot open the group the same way, so the legend toggle, hover highlight and `SvgDiffEngine` find a line on any axis exactly as a primary one; the end label is drawn inside that group |
| Capability + template method | `IHasLabelPlacement` on the series; `AxesRenderer.DrawsDirectLabels` (virtual, false) and `DrawInsideSeriesGroup` (virtual, no-op) overridden by the Cartesian renderer, `DrawsDirectLabels` re-overridden to false by `CartesianInsetAxesRenderer` | the series says where its name goes; the renderer of the axes says whether it can draw it. Only then does the series leave the legend, so polar, 3-D and inset axes keep the legend entry and the name is never lost, with no `is` check on the axes type |
| Named record types | IndexRange, Normalized3DPoint, AdxResult, ConfidenceBand, ColorStop, StreamingPoint, MinMaxRange, MatShape, XYCurve, BarRange, GaugeBand, DataPoint, LineSegment, Size, Vec3, CubePlane, CubeFaceSelection, AxisEdge3D, BarGroup, NamedSample, SurvivalGroup, ControlLimits, CategorySlot, WaterfallBar, SurvivalCurve, ViolinSlice, RasterSize | replace anonymous/named tuples in public API for discoverability and structural equality (v1.8.0 completed the sweep — no anonymous tuples remain) |
| Extension methods on value types | `string.EscapeForXml()`, `double[].Percentile()` / `BisectLeft()` / `BisectRight()`, `Color.Modulate()` / `Shade()`, `RingBuffer<T>.Min()` / `Max()` (INumber-constrained) | replaces `*Helper` static classes with discoverable dot-access APIs (v1.8.0: SvgXmlHelper → SvgXml, MathHelpers → SortedArrayExtensions, LightingHelper → Vec3 + ColorExtensions) |

---

## MatPlotLibNet.Skia package

This package renders PNG and PDF through SkiaSharp. It also provides the text pipeline that every backend shares once
the package is loaded: the module initializer installs `SkiaFontMetrics` on `FontMetricsDefault` and
`SkiaGlyphPathProvider` on `GlyphPathProviderDefault`, so an SVG rendered in the same process carries the same
glyph outlines that the PNG paints.

```
MatPlotLibNet.Skia/
  FigureSkiaExtensions.cs        [ModuleInitializer] Initialize(): registers .png/.pdf, loads the bundled fonts,
                                 installs the metrics + glyph provider; ToPng(), ToPdf()
  RasterSize.cs                  internal readonly record struct (Width, Height, ScaleX, ScaleY): the size in pixels a figure is drawn at
                                 for a dpi (a unit of the layout is dpi / 96 pixels, the count rounded to a whole number) and the scale
                                 that makes the drawing meet all four edges of the bitmap
  RasterRenderingExtensions.cs   internal: the figure drawn onto a bitmap at a dpi — the one place a raster export (a PNG, a frame of a
                                 GIF) decides how big the bitmap is and how the figure is scaled onto it
  PngTextChunkExtensions.cs      internal: adds the Title and Author of SaveOptions to a finished PNG as iTXt chunks straight after
                                 its header, with the CRC-32 the PNG standard puts after each chunk
  SkiaFonts.cs                   the font door. Register(path | Stream) → RegisteredFont(Family, Weight, Slant);
                                 internal Resolve(Font | family, weight, slant) → ResolvedTypeface (typeface +
                                 one shared SKShaper), memoised; the registry and the cache are replaced WHOLE on a
                                 registration (volatile fields, no lock); a family found nowhere is named once
                                 through ChartDiagnostics. Bundled: DejaVu Sans ×4 (Latin, Greek, Cyrillic, Arabic,
                                 Hebrew); user-over-user registration throws, user-over-bundled replaces + reports
  TextShaper.cs                  internal static: Shape(text, SKFont, SKShaper) → ShapedText. Core's BidiAlgorithm
                                 gives the levels, HarfBuzz's UnicodeFunctions the script; runs are shaped one by one
                                 (Buffer.Direction/Script set explicitly — SKShaper.Shape(string) is single-run) and
                                 laid out by rule L2 over runs
  ShapedText.cs                  readonly record struct: Glyphs, Positions (visual order), Width
  ResolvedTypeface.cs            readonly record struct: Typeface + Shaper, never disposed by callers
  RegisteredFont.cs              public readonly record struct: what Register returned
  SkiaFontMetrics.cs             IFontMetrics: Measure = ShapedText.Width
  SkiaGlyphPathProvider.cs       IGlyphPathProvider: glyph outlines at the shaped positions; MeasureAdvance = Width
  SkiaRenderContext.cs           IRenderContext over SKCanvas; DrawText/DrawRichText paint a positioned SKTextBlob
                                 built from ShapedText (each rich-text span shaped once, at its own size)
  Fonts/DejaVuSans*.ttf          embedded, 4 faces
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
  Program.cs                       host + stdio transport + WithTools<ChartTools>(); no decisions, excluded from coverage
  ChartTools.cs                    the only [McpServerTool] file: five tools, and the one error boundary
  ChartSpec.cs                     readonly record struct ChartSpec(string Json) — a spec cannot be swapped with a path
  ChartSpecReader.cs               parse -> normalise -> strict DTO pass -> semantic pass -> ChartSerializer.FromJson
  AxesSeatShape.cs                 where a subplot keeps its axes and series, read off the wire records (AxesDto,
                                   SecondaryAxisSlotDto): a new axis or series list on the wire is a seat on the next build
  AxesSeatWalk.cs                  the one seat walk, over the JSON nodes (normalise) and the DTO records (validate)
  SpecVocabulary.cs                the accepted values of every enum-valued spec field, per node kind
  ChartTypeCatalog.cs              SeriesRegistry.Discriminators minus the types whose reader ignores the document
  ChartRendering.cs                spec -> Figure -> bytes; one owner, two sinks (inline PNG, or a file)
  OutputPathResolver.cs            the single directory save_chart may write under
  ChartSummarizer.cs               FigureSummary / SeriesSummary via the series' own ComputeDataRange
  ChartTabulation.cs               chart_data_table: the figure's own ToDataTables(), in both forms — the one
                                   question a model cannot put to an image, answered in text and in values
  ChartTables.cs                   those tables as the wire vocabulary: header/kind/axis per column, rows of
                                   numbers, dates written out in full, and texts. The tool returns it as
                                   structured content BESIDE the markdown, never instead of it
  RenderLimits.cs                  the canvas, text and table-row ceilings (the canvas ceiling IS the token
                                   ceiling; so is the row ceiling). The canvas is the picture the spec draws, so a
                                   spec's dpi counts: width and height times dpi / 96 stay inside the ceiling
  FontDirectory.cs                 the server's font door: Expand(MATPLOTLIBNET_FONTS) → files to register +
                                   problems to log; a host starts this process, so a font file arrives by variable
  ToolRefusalException.cs          a refusal the boundary turns into an McpException the model can act on
  Extensions/StringDistanceExtensions.cs   edit distance, for "did you mean 'line'?"
  .mcp/server.json                 the registry manifest; its version is pinned to the project's by a test
```

### Why the package has its own reader

`ChartSerializer.FromJson` is a round-trip reader for the library's own writer, so it is lenient: an unknown series
type is dropped with a diagnostic, an unknown property is skipped by `System.Text.Json`, a misspelled enum value is
ignored by `Enum.TryParse`, and an absent `width` overwrites the figure default with zero. That lenience gives wire
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

A chart the library composes from several series travels as those series: none of the 17 chart verbs is a chart type of
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
  DataFrameColumnReader.cs           static: ToDoubleArray / ToStringArray — type-safe column materialisation
  DataFrameFigureExtensions.cs       Line / Scatter / Hist — delegates to EnumerableFigureExtensions
  DataFrameIndicatorExtensions.cs    indicator bridge methods — delegates to core indicator types
  DataFrameNumericsExtensions.cs     PolyFit / PolyEval / ConfidenceBand — delegates to LeastSquares
```

### Column resolution

Every extension method calls an internal `Col(df, name)` helper that throws
`ArgumentException` with the column name in the message when a column is not found.
All column access goes through `DataFrameColumnReader.ToDoubleArray` / `ToStringArray`.

### Indicator bridge

`DataFrameIndicatorExtensions` exposes 51 indicator bridge methods covering all core indicators:

| Group | Methods |
|---|---|
| Price | `Sma`, `Ema`, `Roc`, `Rsi`, `BollingerBands`, `Obv`, `Macd`, `DrawDown`, `CgOscillator`, `Bocpd`, `CyberCycle`, `Decycler`, `EhlersITrend`, `EhlersSineWave` (→ `SineWaveResult`), `EhlersSuperSmoother`, `FractionalDifferentiation`, `KaufmanEfficiencyRatio`, `LaguerreRsi`, `MamaFama` (→ `MamaFamaResult`), `PermutationEntropy`, `RollSpread`, `RoofingFilter`, `WaveletEnergyRatio`, `WaveletEntropy`, `Cusum` (→ `CusumResult`) |
| Close+Vol | `AmihudIlliquidity`, `ForceIndex`, `Vpin`, `VwapZScore` |
| High+Low | `AroonOscillator`, `CorwinSchultz` |
| Candle | `Adx` (scalar), `AdxFull` (→ `AdxResult`), `Atr`, `Cci`, `WilliamsR`, `Stochastic`, `ParabolicSar`, `KeltnerChannels`, `Vwap`, `AdaptiveStochastic`, `Ichimoku` (→ `IchimokuResult`), `Supertrend` (→ `SupertrendResult`), `SqueezeMomentum` (→ `SqueezeResult`) |
| H+L+V | `EaseOfMovement` |
| HLCV | `KlingerVolumeOscillator` (→ `KlingerResult`), `TwiggsMoneyFlow` |
| OHLC | `GarmanKlass`, `RelativeVigorIndex` (→ `RviResult`), `YangZhang`, `YangZhangVolRatio` |
| Two-col | `TransferEntropy` |

`Vwap` pre-computes the typical price `(H+L+C)/3` before delegating to the core `Vwap` indicator.
`ParabolicSar` returns `.Sar` from `ParabolicSarResult`.

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
  GeoAxesExtensions.cs           the verbs of a map axes. WithProjection records the axes' ONE projection (a ConditionalWeakTable keyed by
                                 the builder); every layer reads it, a layer given a projection of its own must give the same kind with the
                                 same numbers (IsSameProjectionAs) or is refused, and a layer asked for before the axes has one is refused.
                                 Land, Ocean, Borders, Coastlines, Choropleth and Cartogram add one GeoPolygonSeries; Points, Bubbles,
                                 Dorling, HexBins and Flows project the places and add Scatter, Hexbin and Line series. A layer is refused
                                 before anything is added
  GeoPackage.cs                  internal: the [ModuleInitializer] that registers the reader (GeoPolygonSeries.RegisterReader)
  GeoFlow.cs                     public readonly record struct (From, To, Value): a quantity that moves between two places
  Series/GeoPolygonSeries.cs     the map series: Features, Projection, Color, StrokeColor, StrokeWidth, Values (one for each feature), ColorMap,
                                 Normalizer, IsRawProjected (the features are already in the plane of the projection). It is a colour bar's data
                                 provider only with Values (ProvidesColorBar), draws a region with its holes as ONE path, has the projection's
                                 bounds as its sticky frame, and is written and read through ToSeriesDto / FromSeriesDto
  Series/GeoPackageData.cs       internal: the one writer and the one reader of what a map puts into SeriesDto.PackageData — the projection (name and
                                 PROJ-named parameters), the features (the id of a built-in set, or a GeoJSON FeatureCollection) and whether they are
                                 raw-projected; Read returns GeoMapData, or null for data this package cannot make a map of
  GeoJson/                       GeoFeature (Geometry, Properties; NumberOf(property) reads a number invariantly, NaN when it is none), GeoGeometry
                                 (Rings flat, Polygons with their holes), GeoPolygon + GeoPosition (public record structs; FromRings winds the
                                 exterior counter-clockwise and the holes clockwise), GeoJsonReader / GeoJsonWriter (the writer is the counterpart
                                 of the reader and keeps the holes), GeoClipping, ProjectedRegion (ProjectedBy, Measure, ScaledAbout: a region in
                                 the plane of a projection, its area and centre, and resized about its centre — the ground of Cartogram and Dorling)
  Projections/                   IGeoProjection and the 13 projections; ProjectionParameters.cs (internal IProjectionParameters: the PROJ-named
                                 numbers a projection was made with — lon_0, lat_0, lat_1, lat_2 — and Made(parameters), the projection of the
                                 same kind made again from numbers, which the parametric projection itself knows how to do; and
                                 GeoProjectionExtensions: ParametersOf, IsSameProjectionAs, Described, and Make(name, parameters), a lookup of
                                 the name in the table of the 13 kinds); GreatCircle (the places along the shortest path, for Flows)
  Data/NaturalEarth110m.cs       Coastlines() and Countries() from the embedded GeoJSON; SourceOf / OfSource give a built-in set the id a map's JSON
                                 carries instead of its features
```

A map travels through JSON whole: `ToSeriesDto` writes the shared fields (colour, edge colour, line width, values, colour map,
normaliser) and `PackageData`, and the reader registered by the package rebuilds it. A figure that holds a map and is read by a
core that does not know the series drops it with a diagnostic, as for any unknown kind. The Geo test project's
GeoDrawnPropertiesRoundTripTests holds every settable property of the map series to the round trip, as the contract tests of core
do for core's series.
