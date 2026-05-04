# MatPlotLibNet Core -- Architecture (v1.10.0)

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
    +-- MatPlotLibNet.Notebooks           Microsoft.DotNet.Interactive (Polyglot Notebooks)
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

  FigureTemplates.cs                  pre-built layouts: FinancialDashboard(), ScientificPaper(), SparklineDashboard(), JointPlot(), PairPlot(), FacetGrid(), Clustermap()

  Plt.Style                           nested static class: Use(), Context() for rcParams configuration

  Builders/
    FigureBuilder.cs                  fluent API: Plt.Create().WithTitle().Plot().Build() (build only)
    AxesBuilder.cs                    subplot config: WithTitle(), SetXLabel(), Plot(), Scatter(), etc.
    SecondaryAxisBuilder.cs           secondary Y-axis: SetYLabel(), Plot(), Scatter()
    ThemeBuilder.cs                   custom themes: Theme.CreateFrom().WithFont().Build()

  Extensions/
    FigureExtensions.cs               Save(), Transform(), ToSvg(), ToJson(), RegisterTransform()
                                      (SRP: all output responsibility moved here from FigureBuilder)

  Models/
    Figure.cs                         top-level container (Title, Width, Height, Theme, EnableZoomPan, EnableLegendToggle, EnableRichTooltips, EnableHighlight, EnableSelection, HasInteractivity)
    Axes.cs                           subplot: series, annotations, ref lines, spans, secondary axis, insets, ShareX/ShareY, EnableInteractiveAttributes
    Axis.cs                           label, min/max, scale, ticks
    Annotation.cs                     text annotation: Text, X, Y, ArrowTarget, Alignment, Rotation, ArrowStyle, BackgroundColor
    ArrowStyle.cs                     enum: None, Simple, FancyArrow
    ReferenceLine.cs                  horizontal/vertical reference line (AxHLine, AxVLine)
    SpanRegion.cs                     shaded horizontal/vertical region (AxHSpan, AxVSpan)
    GridSpec.cs                       unequal subplot layout: Rows, Cols, HeightRatios, WidthRatios
    InsetBounds.cs                    inset position: X, Y, Width, Height (axes-fraction coordinates)
    SpinesConfig.cs                   per-spine visibility/position: Top, Bottom, Left, Right

    Series/                           75 series types across 15 families
      ISeries.cs                      interface: Label, Visible, ZOrder, Accept()
      ISeriesSerializable.cs          interface: each series serializes itself (eliminates SeriesToDto switch)
      IHasDataRange.cs                interface: series that expose their own data bounds
      IPolarSeries.cs                 interface: polar coordinate series
      I3DGridSeries.cs                interface: 3D grid-based series (Surface, Wireframe)
      I3DPointSeries.cs               interface: 3D point-based series (Scatter3D, Stem3D, Bar3D)
      IPriceSeries.cs                 interface: financial OHLC series (Candlestick, OhlcBar)
      IColormappable.cs               interface: ColorMap { get; set; } — polymorphic colormap assignment
      INormalizable.cs                interface: Normalizer { get; set; } — polymorphic normalizer assignment
      ICategoryLabeled.cs             interface: CategoryLabels for polymorphic tick-label rendering
      IColorBarDataProvider.cs        interface: GetColorBarRange() + ColorMap for colorbar auto-detection
      IStackable.cs                   interface: StackBaseline for bar stacking offset computation
      IHasColor.cs                    interface: Color? Color — polymorphic color access (~20 series)
      IHasAlpha.cs                    interface: double Alpha — polymorphic alpha access (~7 series)
      IHasEdgeColor.cs                interface: Color? EdgeColor — polymorphic edge-color access (~3 series)
      ILabelable.cs                   interface: bool ShowLabels; string? LabelFormat — series with data-point labels
      ChartSeries.cs                  abstract base: implements ISeries + ISeriesSerializable
      XYSeries.cs                     generic base: XData, YData, MaxDisplayPoints (Line, Scatter, Step, Area, ErrorBar, Bubble, Sparkline, Stem, Ecdf)
      OhlcSeries.cs                   financial base: Open, High, Low, Close, DateLabels, UpColor, DownColor, PriceData (Candlestick, OhlcBar)
      DatasetSeries.cs                distribution base: Datasets[][], default ComputeDataRange (Stripplot, Swarmplot, Pointplot, Box, Violin)
      PolarSeries.cs                  generic base: RData, ThetaData (PolarLine, PolarScatter, PolarBar)
      GridSeries3D.cs                 generic base: XData, YData, ZData[,] (Surface, Wireframe)
      HierarchicalSeries.cs           abstract base: Root, ColorMap, ShowLabels, IColormappable (Treemap, Sunburst, Dendrogram)
      LineSeries.cs                   XYSeries: Color, LineStyle, LineWidth, Marker
      ScatterSeries.cs                XYSeries: Color, MarkerSize, Sizes[], Colors[], IColormappable
      BarSeries.cs                    Categories, Values, Color, Orientation, BarWidth, StackBaseline, ShowLabels, LabelFormat, ICategoryLabeled, IStackable
      HistogramSeries.cs              Data, Bins, Color, Alpha, ComputeBins()
      PieSeries.cs                    Sizes, Labels, Colors[], StartAngle
      HeatmapSeries.cs                Data[,], ColorMap, Normalizer, ShowLabels, LabelFormat, MaskMode, CellValueColor, IColormappable, INormalizable, ILabelable, IColorBarDataProvider
      HeatmapMaskMode.cs              enum: None, UpperTriangle, LowerTriangle, UpperTriangleStrict, LowerTriangleStrict + HeatmapMaskModeExtensions.Hides(row,col)
      ImageSeries.cs                  Data[,], ColorMap, Normalizer, VMin, VMax, IColormappable, INormalizable, IColorBarDataProvider (Grid/)
      Histogram2DSeries.cs            X[], Y[], BinsX, BinsY, ColorMap, Normalizer, IColormappable, INormalizable, IColorBarDataProvider (Grid/)
      BoxSeries.cs                    DatasetSeries: Color, MedianColor, ShowOutliers (overrides ComputeDataRange)
      ViolinSeries.cs                 DatasetSeries: Color, Alpha (overrides ComputeDataRange)
      ContourSeries.cs                XData, YData, ZData[,], Levels, Filled, ShowLabels, LabelFormat, LabelFontSize, ColorMap, IColormappable, IHasDataRange
      ContourfSeries.cs               XData, YData, ZData[,], Levels, Alpha, ShowLines, LineWidth, ColorMap, Normalizer, IColormappable, INormalizable, IColorBarDataProvider (Grid/)
      StemSeries.cs                   XYSeries: MarkerColor, StemColor, BaselineColor
      AreaSeries.cs                   XYSeries: YData2 (fill between), Alpha, FillColor
      StepSeries.cs                   XYSeries: StepPosition (Pre/Mid/Post)
      ErrorBarSeries.cs               XYSeries: YErrorLow/High, XErrorLow/High, CapSize
      CandlestickSeries.cs            OhlcSeries: BodyWidth, ICategoryLabeled
      QuiverSeries.cs                 XData, YData, UData, VData, Scale, ArrowHeadSize
      RadarSeries.cs                  Categories, Values, FillColor, Alpha, MaxValue
      DonutSeries.cs                  Sizes, InnerRadius, CenterText (Circular/)
      BubbleSeries.cs                 XYSeries: Sizes, Alpha (XY/)
      OhlcBarSeries.cs                OhlcSeries: TickWidth (Financial/)
      WaterfallSeries.cs              Categories, Values, IncreaseColor, DecreaseColor (Categorical/)
      FunnelSeries.cs                 Labels, Values, Colors (Categorical/)
      GanttSeries.cs                  Tasks, Starts, Ends, BarHeight (Categorical/)
      GaugeSeries.cs                  Value, Min, Max, Ranges (GaugeBand[]?), NeedleColor (Circular/)
      ProgressBarSeries.cs            Value, FillColor, TrackColor (Categorical/)
      SparklineSeries.cs              XYSeries: Values, LineWidth (XY/)
      EcdfSeries.cs                   XYSeries: sorted empirical CDF (XY/)
      StackedAreaSeries.cs            X[], YSets[][], StackLabels, FillColors, IStackable (XY/)
      StreamplotSeries.cs             XData[], YData[], UData[,], VData[,], Density, ArrowSize (Field/)
      PolarLineSeries.cs              PolarSeries: Color, LineStyle (Polar/)
      PolarScatterSeries.cs           PolarSeries: Color, MarkerSize (Polar/)
      PolarBarSeries.cs               PolarSeries: Color, BarWidth (Polar/)
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
      Graph/GraphNode.cs              readonly record struct: Id, X, Y, ColorScalar, SizeScalar, Label
      Graph/GraphEdge.cs              readonly record struct: From, To, Weight, IsDirected
      Graph/GraphLayout.cs            enum: Manual=0, ForceDirected=1, Circular=2, Hierarchical=3
      SankeySeries.cs                 Nodes, Links (Flow/)
      KdeSeries.cs                    Data[], Bandwidth?, Fill, Alpha, LineWidth, Color, LineStyle (Distribution/)
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
    Lighting/                         per-face 3D lighting model (Phase G, v0.9.0)
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
    Sma.cs, Ema.cs                      PriceIndicator: moving averages (overlay)
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
    IFigureTransform.cs               interface: Transform(Figure, Stream)
    FigureTransform.cs                abstract base: holds ChartRenderer, shared by all transforms
    SvgTransform.cs                   FigureTransform + ISvgRenderer: parallel SVG rendering
    TransformResult.cs                fluent record: ToStream(), ToFile(), ToBytes()

  Animation/
    IAnimation<TState>.cs             interface: typed animation state pipeline
    AnimationController<TState>.cs    controller: runs animation loop with ConfigureAwait(false)
    AnimationBuilder.cs               legacy: frame-based animation (FrameCount, Interval, Loop)
    LegacyAnimationAdapter.cs         adapter: bridges AnimationBuilder to IAnimation<TState>

  Rendering/
    IChartRenderer.cs                 interface: Render(Figure, IRenderContext)
    ChartRenderer.cs                  figure-level orchestrator: background, constrained layout, subplot layout, dispatch to AxesRenderer
    AxesRenderer.cs                   abstract base for coordinate-system-specific rendering (ConcurrentDictionary registry)
    CartesianAxesRenderer.cs          Cartesian (X,Y): grid, ticks, spans, series, annotations, signals
                                        internal helpers (Phase L): RenderGridLines(Orientation,…), RenderAxisTicks(…)+TickDrawContext, DrawBreakSegments(Orientation,BreakStyle,…)
    PolarAxesRenderer.cs              Polar (r,theta): circular grid, radial lines, angle labels
    ThreeDAxesRenderer.cs             3D (X,Y,Z): projection, bounding box wireframe, depth sorting
    IRenderContext.cs                  drawing primitives: DrawLine, DrawRect, DrawText, DrawText(…,rotation), DrawRichText (default method)
    ISeriesVisitor.cs                 visitor pattern: Visit() for each of the 75 series types
    DataTransform.cs                  data space <-> pixel space; TransformBatch uses AVX SIMD interleave (zero intermediate alloc)
    RenderArea.cs                     plot bounds + context container
    Primitives.cs                     record structs: Point, Size, Rect, DataRange, PathSegment

    TickLocators/                       ITickLocator strategy for axis tick placement
      ITickLocator.cs                   interface: double[] Locate(double min, double max)
      AutoLocator.cs                    nice-number algorithm (extracted from AxesRenderer)
      MaxNLocator.cs                    nice numbers capped to at most N ticks
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

    Layout/                             margin computation (new v0.8.1)
      ConstrainedLayoutEngine.cs        Compute(Figure, IRenderContext) → SubPlotSpacing; measures text extents, clamps margins
      LayoutMetrics.cs                  internal record: LeftNeeded, BottomNeeded, TopNeeded, RightNeeded (per subplot)

    TextMeasurement/                    text width estimation (new v0.8.1)
      CharacterWidthTable.cs            internal static: per-char width factors for Helvetica/Arial at 1em

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
      DrawStyleInterpolation.cs         internal static: Apply(x, y, style) — shared step-mode interpolation; eliminates duplication between LineSeriesRenderer and AreaSeriesRenderer
      XY/                               Line (LTTB), Scatter (viewport cull), Step (LTTB), Area (LTTB), ErrorBar, Bubble, Sparkline, Ecdf, StackedArea, Regression (LeastSquares polynomial + confidence band), Residual (LeastSquares residuals + optional zero line)
      Categorical/                      Bar (ShowLabels), Histogram, Waterfall, Funnel, Gantt, ProgressBar, Eventplot, BrokenBar, Count (group-count), Pointplot (mean + CI)
      Circular/                         CircularRenderer<TSeries> (abstract base: BuildWedgePath, PlaceOuterLabels); Pie, Radar, Donut, Gauge
      Grid/                             Heatmap, Contour, Contourf, Image, Histogram2D, Hexbin (HexGrid flat-top bins), Pcolormesh (quadrilateral cells), Spectrogram (STFT via Fft helper), Tricontour (Delaunay + marching triangles), Tripcolor (Delaunay fill), Clustermap (composite: heatmap + row/column dendrograms with sub-pixel-suppressed margin panels), PairGrid (composite: N×N matrix of histograms/KDE on diagonal + scatters/hexbin off-diagonal, optional hue grouping)
                                        PairGridLayout.cs — pure geometry: ComputeCellRects(plotBounds, n, cellSpacing) → Rect[n,n]; MinPanelPx sub-pixel gate
                                        PairGridOffDiagonalPainters.cs — IPairGridOffDiagonalPainter strategy + ScatterOffDiagonalPainter / HexbinOffDiagonalPainter / Registry; PairGridGeometry (MapPoint, ComputeAxisSpan); PairGridHue (IsValid, BuildCache, GetColor)
      Graph/                            NetworkGraph (composite: nodes + edges in 2D, directed-edge arrowheads, optional weight labels)
                                        NetworkGraphLayouts.cs — pure-function layouts: ApplyManual (pass-through), ApplyCircular (unit-circle evenly-spaced), ApplyHierarchical (BFS top-down with cycle handling), ApplyForceDirected (Fruchterman–Reingold spring-embedder with seeded RNG via NpRandom + temperature cooling + optional convergence-threshold early-stop); Apply(kind, seed, iterations, threshold) enum dispatch
      Distribution/                     Box, Violin, Kde (GaussianKde Silverman bandwidth), Rugplot (tick marks), Stripplot (jittered points), Swarmplot (BeeswarmLayout circle-packing)
      Financial/                        Candlestick, OhlcBar
      Polar/                            PolarTransformRenderer<TSeries> (abstract base: PrepareTransform → rMax + PolarTransform); PolarLine, PolarScatter, PolarBar
      Field/                            Quiver, Stem, Streamplot, QuiverKey (reference arrow), Barbs (meteorological wind barbs)
      Hierarchical/                     Treemap, Sunburst, Dendrogram (shared HierarchicalSeries base; tree-of-U-shape segments with optional cut-height clustering)
                                        HierarchicalLayout.cs — nested Dendrogram/Treemap/Sunburst/Clustermap constant classes (LabelOffsetPx, HeaderHeightPx, OuterRingInsetPx, MinPanelPx, …)
      Flow/                             Sankey
      Special/                          Table (measured column widths, header+data rows, borders)
      ThreeD/                           Stem3D (vertical stems via Projection3D), Bar3D (prism faces, painter's depth sort)

    Svg/
      ISvgRenderer.cs                 interface: Render(Figure) -> string (backward compat)
      SvgRenderContext.cs             IRenderContext impl: StringBuilder-based SVG emission; BeginDataGroup(cssClass, idx, ariaLabel?), BeginLegendItemGroup(idx, ariaLabel?), BeginAccessibleGroup(cssClass, ariaLabel); uses string.EscapeForXml() extension
      SvgXml.cs                       internal static: EscapeForXml(this string) — DRY XML escaping extension used by SvgRenderContext and SvgTransform (v1.8.0: renamed from SvgXmlHelper)
      SvgSeriesRenderer.cs            thin visitor dispatcher to SeriesRenderer<T> instances
      SvgInteractivityScript.cs       embedded JS: zoom/pan via viewBox; tabindex + aria-roledescription; keyboard +/-/arrows/Home (new v0.8.8)
      SvgLegendToggleScript.cs        embedded JS: click/Enter/Space on data-legend-index; role=button, aria-pressed, tabindex (new v0.8.8; v1.7.2 Phase S — toggle on click only, never pointerdown)
      SvgLegendDragScript.cs          embedded JS: press-and-hold legend item to translate <g class="legend"> via transform; capture-phase stopPropagation coordinates with pan/zoom + toggle; viewBox clamp; client-only translation (new v1.7.2 Phase S)
      SvgCustomTooltipScript.cs       embedded JS: styled floating div tooltip; role=tooltip, aria-live=polite, focus/blur listeners (new v0.8.8)
      SvgHighlightScript.cs           embedded JS: mouseenter/focus dims siblings; tabindex, blur restores (new v0.8.8)
      SvgSelectionScript.cs           embedded JS: Shift+drag selection rect; Escape cancels; aria-label on rect (new v0.8.8)

  Models/Series/XY/
    IndexRange.cs                     readonly record struct(StartInclusive, EndExclusive) + Count/IsEmpty — returned by IMonotonicXY.IndexRangeFor

  Rendering/
    Normalized3DPoint.cs              readonly record struct(Nx, Ny, Nz) — returned by Projection3D.Normalize()

  Numerics/
    LeastSquares.cs                   public static: PolyFit (normal equations), PolyEval (Horner), ConfidenceBand (t-distribution leverage)
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

    SeriesRenderers/Distribution/
    BeeswarmLayout.cs                 internal static: Compute(sortedValues, markerRadius, categoryCenter, pixelScale) — greedy circle-packing, cap 1000 pts, jitter fallback
    SortedArrayExtensions.cs          internal extensions on double[]: Percentile(p), BisectLeft(v), BisectRight(v) — used by Box / Violin / ECDF (v1.8.0: renamed from MathHelpers)
    GaussianKde.cs                    internal static: SilvermanBandwidth(sorted), Evaluate(sorted, bandwidth, numPoints) → XYCurve

  Serialization/
    IChartSerializer.cs               interface: ToJson(Figure), FromJson(string)
    ChartSerializer.cs                System.Text.Json round-trip — delegates to ISeriesSerializable per series
    SeriesRegistry.cs                 ConcurrentDictionary-based type registry for deserialization
                                      (series register themselves; no central switch statement)

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
    Theme.cs                          8 built-in themes (+ ColorBlindSafe Okabe-Ito, HighContrast WCAG AAA) + GridStyle sealed record + PropCycler? property (new v0.8.8)
    LineStyle.cs                      enum: Solid, Dashed, Dotted, DashDot, None
    MarkerStyle.cs                    enum: None, Circle, Square, Triangle, Diamond, etc.
    DashPatterns.cs                   canonical dash ratios shared by SVG + MAUI + Skia renderers
    PropCycler.cs                     LCM-based multi-property cycler; CycledProperties record struct (new v0.8.1)
    PropCyclerBuilder.cs              fluent builder: WithColors(), WithLineStyles(), WithMarkerStyles(), WithLineWidths() (new v0.8.1)

    ColorMaps/
      IColorMap.cs                    interface: GetColor(double normalized) -> Color
      LerpColorMap.cs                 internal sealed: linear interpolation between Color[] hex stops
      ReversedColorMap.cs             public decorator: inverts normalized input, appends _r suffix
      ColorMapRegistry.cs             thread-safe ConcurrentDictionary, case-insensitive, auto-registers _r variants
      INormalizer.cs                  interface + 4 implementations: LinearNormalizer (singleton),
                                        LogNormalizer, TwoSlopeNormalizer(center), BoundaryNormalizer(double[])
      ColorMaps.cs                    9 perceptually-uniform + rainbow: Viridis, Plasma, Inferno, Magma,
                                        Coolwarm, Blues, Reds, Turbo (prefer over Jet), Jet (legacy)
      SequentialColorMaps.cs          21 sequential: Cividis, Greens, Oranges, Purples, Greys, YlOrBr,
                                        YlOrRd, OrRd, PuBu, YlGn, BuGn, Hot, Copper, Bone, BuPu, GnBu,
                                        PuRd, RdPu, YlGnBu, PuBuGn, Cubehelix
      DivergingColorMaps.cs           9 diverging: RdBu, RdYlGn, RdYlBu, BrBG, PiYG, Spectral, PuOr,
                                        Seismic, Bwr
      CyclicColorMaps.cs              3 cyclic (start≈end): Twilight, TwilightShifted, Hsv
      QualitativeColorMaps.cs         11 qualitative: Tab10, Tab20, Set1, Set2, Set3, Pastel1, Pastel2,
                                        Dark2, Accent, Paired, OkabeIto (new v0.8.8 — color-blind safe)
                                      Total: 53 base colormaps × 2 (+ _r reversed) = 106 registered names
      ColormapExtensions.cs           IColormappable.GetColorMapOrDefault(fallback) + int.ColormapFraction(count, singletonT)
                                        — replaces 17 inline `?? Viridis|Tab10` sites and 3 `index/(count-1)` sites
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
string                 complete <svg>...</svg> document
                       optional scripts appended at end of body:
                         SvgInteractivityScript  (EnableZoomPan)
                         SvgLegendToggleScript   (EnableLegendToggle)
                         SvgLegendDragScript     (EnableLegendToggle — co-emitted; Phase S)
                         SvgCustomTooltipScript  (EnableRichTooltips)
                         SvgHighlightScript      (EnableHighlight)
                         SvgSelectionScript      (EnableSelection)
                       data-series-index / data-legend-index attributes emitted
                         when Figure.HasInteractivity via Axes.EnableInteractiveAttributes
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
ChartSerializer        Figure -> each series via ISeriesSerializable -> JsonSerializer.Serialize()
    |
string                 JSON with camelCase, null-ignoring, Color as hex

ChartServices.Serializer.FromJson(json)
    |
ChartSerializer        JsonSerializer.Deserialize() -> SeriesRegistry resolves type -> Figure
    |
SeriesRegistry         ConcurrentDictionary<string, Func<...>> maps type discriminator to factory
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
| Strategy | IRenderContext (SVG, MAUI, Skia), AxesRenderer (Cartesian, Polar, 3D) | multiple output targets and coordinate systems from same model |
| Template method | FigureTransform base class, AxesRenderer base class | shared renderer, format/coordinate-specific overrides |
| Fluent result | TransformResult record | polymorphic ToStream/ToFile/ToBytes from any transform |
| Self-serialization | ISeriesSerializable on all 60 series | each series knows how to serialize itself (no central switch) |
| Ambient context | RcParams + AsyncLocal + StyleContext | thread-safe global config with scoped overrides |
| Registry | SeriesRegistry (ConcurrentDictionary) | thread-safe deserialization type lookup |
| Generic base classes | XYSeries, PolarSeries, GridSeries3D, HierarchicalSeries; CircularRenderer<T>, PolarTransformRenderer<T>, OhlcStreamingIndicatorTests<T> (Phase L) | DRY shared properties and behaviour across series and renderer families |
| Interface segregation | IHasDataRange, IPolarSeries, I3DGridSeries, I3DPointSeries, IPriceSeries, IColormappable, INormalizable, ICategoryLabeled, IColorBarDataProvider, IStackable, IHasColor, IHasAlpha, IHasEdgeColor, ILabelable | narrow contracts for cross-cutting concerns |
| DI interfaces | IFigureTransform, IChartRenderer, ISvgRenderer, IChartSerializer | testable, replaceable services |
| SRP extension methods | FigureExtensions (Save, Transform, ToSvg, RegisterTransform) | builder only builds; output is separate |
| Static defaults | ChartServices | non-DI usage for console apps |
| Record types | Font, GridStyle, Legend, TickConfig, Color, Point, Rect, TransformResult | immutable value objects |
| Parallel rendering | SvgTransform + per-subplot SvgRenderContext | multi-core subplot rendering |
| Thread safety | volatile fields, ConcurrentDictionary for GlobalTransforms, AxesRenderer registry, SeriesRegistry | safe concurrent access |
| Adapter | LegacyAnimationAdapter | bridges AnimationBuilder to IAnimation\<TState\> |
| Delegate extraction | SvgTransform.BuildSvgDocument, ChartSerializer.ApplyEnum | DRY via higher-order functions |
| Default interface method | IRenderContext.DrawRichText | all backends get plain-text fallback; SVG overrides with tspan emission |
| State machine | MathTextParser | single-pass text classification into Normal/Superscript/Subscript spans |
| Two-pass layout | ConstrainedLayoutEngine | measure text extents first, then compute margins |
| Named record types | IndexRange, Normalized3DPoint, AdxResult, ConfidenceBand, ColorStop, StreamingPoint, MinMaxRange, MatShape, XYCurve, BarRange, GaugeBand, DataPoint, LineSegment, Size, Vec3 | replace anonymous/named tuples in public API for discoverability and structural equality (v1.8.0 completed the sweep — no anonymous tuples remain) |
| Extension methods on value types | `string.EscapeForXml()`, `double[].Percentile()` / `BisectLeft()` / `BisectRight()`, `Color.Modulate()` / `Shade()` | replaces `*Helper` static classes with discoverable dot-access APIs (v1.8.0: SvgXmlHelper → SvgXml, MathHelpers → SortedArrayExtensions, LightingHelper → Vec3 + ColorExtensions) |

---

## MatPlotLibNet.DataFrame package

Thin extension-method bridge that funnels `Microsoft.Data.Analysis.DataFrame` column names
into the core charting, indicator, and regression APIs. Zero new logic — all data processing
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
All column access funnels through `DataFrameColumnReader.ToDoubleArray` / `ToStringArray`.

### Indicator bridge

`DataFrameIndicatorExtensions` exposes 51 indicator bridge methods covering all core indicators:

| Group | Methods |
|---|---|
| Price | `Sma`, `Ema`, `Rsi`, `BollingerBands`, `Obv`, `Macd`, `DrawDown`, `CgOscillator`, `Bocpd`, `CyberCycle`, `Decycler`, `EhlersITrend`, `EhlersSineWave` (→ `SineWaveResult`), `EhlersSuperSmoother`, `FractionalDifferentiation`, `KaufmanEfficiencyRatio`, `LaguerreRsi`, `MamaFama` (→ `MamaFamaResult`), `PermutationEntropy`, `RollSpread`, `RoofingFilter`, `WaveletEnergyRatio`, `WaveletEntropy`, `Cusum` (→ `CusumResult`) |
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

`DataFrameNumericsExtensions` wraps `LeastSquares` static methods:
`PolyFit(xCol, yCol, degree)` → coefficients; `PolyEval(xCol, coeffs)` → fitted Y;
`ConfidenceBand(xCol, yCol, coeffs, evalX, level)` → `ConfidenceBand(Upper[], Lower[])`.
