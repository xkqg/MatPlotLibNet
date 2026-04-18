// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Fluent builder for constructing a <see cref="Models.Figure"/> with a chainable API.</summary>
/// <example>
/// Two-panel financial dashboard with interactive zoom/pan and tooltips:
/// <code>
/// string svg = Plt.Create()
///     .WithSize(900, 500)
///     .WithZoomPan()
///     .WithRichTooltips()
///     .AddSubPlot(2, 1, 1, ax =>
///     {
///         ax.Candlestick(open, high, low, close)
///           .BollingerBands(20, 2.0)
///           .SetYLabel("Price");
///     })
///     .AddSubPlot(2, 1, 2, ax =>
///         ax.Bar(labels, volume).SetYLabel("Volume"))
///     .WithTitle("OHLC Dashboard")
///     .ToSvg();
/// </code>
/// 3-D surface with perspective camera and interactive rotation:
/// <code>
/// string svg = Plt.Create()
///     .Surface(x, y, z)
///     .WithCamera(elevation: 35, azimuth: -55, distance: 8)
///     .WithLighting(0.5, 0.5, 1.0)
///     .With3DRotation()
///     .ToSvg();
/// </code>
/// </example>
public sealed class FigureBuilder
{
    private string? _title;
    private string? _altText;
    private string? _description;
    private double _width = 800;
    private double _height = 600;
    private double _dpi = 96;
    private Color? _background;
    private Theme _theme = Theme.MatplotlibV2;
    private Axes? _defaultAxes;
    private readonly List<SubPlotSpec> _subPlots = [];
    private GridSpec? _gridSpec;

    /// <summary>Sets the figure title.</summary>
    public FigureBuilder WithTitle(string title) { _title = title; return this; }

    /// <summary>Sets the short alternative text rendered as the SVG <c>&lt;title&gt;</c> element.</summary>
    public FigureBuilder WithAltText(string altText) { _altText = altText; return this; }

    /// <summary>Sets the longer description rendered as the SVG <c>&lt;desc&gt;</c> element.</summary>
    public FigureBuilder WithDescription(string description) { _description = description; return this; }

    /// <summary>Sets the figure dimensions in pixels.</summary>
    public FigureBuilder WithSize(double width, double height) { _width = width; _height = height; return this; }

    /// <summary>Sets the figure DPI (dots per inch).</summary>
    public FigureBuilder WithDpi(double dpi) { _dpi = dpi; return this; }

    /// <summary>Sets the theme used for styling the figure.</summary>
    public FigureBuilder WithTheme(Theme theme) { _theme = theme; return this; }

    /// <summary>Sets the figure background color.</summary>
    public FigureBuilder WithBackground(Color color) { _background = color; return this; }

    /// <summary>Adds a line series to the default axes.</summary>
    public FigureBuilder Plot(double[] x, double[] y, Action<LineSeries>? configure = null) =>
        AddSeries(ax => ax.Plot(x, y), configure);

    /// <summary>Adds a scatter series to the default axes.</summary>
    public FigureBuilder Scatter(double[] x, double[] y, Action<ScatterSeries>? configure = null) =>
        AddSeries(ax => ax.Scatter(x, y), configure);

    /// <summary>Adds a <see cref="SignalXYSeries"/> with monotonically ascending X values to the default axes.</summary>
    public FigureBuilder SignalXY(double[] x, double[] y, Action<SignalXYSeries>? configure = null) =>
        AddSeries(ax => ax.SignalXY(x, y), configure);

    /// <summary>Adds a <see cref="SignalSeries"/> with uniform sample rate to the default axes.</summary>
    public FigureBuilder Signal(double[] y, double sampleRate = 1.0, double xStart = 0.0, Action<SignalSeries>? configure = null) =>
        AddSeries(ax => ax.Signal(y, sampleRate, xStart), configure);

    /// <summary>Adds a line series with DateTime X values to the default axes; automatically activates the date X-axis.</summary>
    public FigureBuilder Plot(DateTime[] dates, double[] y, Action<LineSeries>? configure = null)
    {
        double[] x = dates.Select(d => d.ToOADate()).ToArray();
        ApplyDateXAxis(EnsureDefaultAxes());
        return AddSeries(ax => ax.Plot(x, y), configure);
    }

    /// <summary>Adds a scatter series with DateTime X values to the default axes; automatically activates the date X-axis.</summary>
    public FigureBuilder Scatter(DateTime[] dates, double[] y, Action<ScatterSeries>? configure = null)
    {
        double[] x = dates.Select(d => d.ToOADate()).ToArray();
        ApplyDateXAxis(EnsureDefaultAxes());
        return AddSeries(ax => ax.Scatter(x, y), configure);
    }

    private static void ApplyDateXAxis(Axes axes)
    {
        if (axes.XAxis.Scale == AxisScale.Date) return; // already set
        var locator = new Rendering.TickLocators.AutoDateLocator();
        axes.XAxis.Scale = AxisScale.Date;
        axes.XAxis.TickLocator = locator;
        axes.XAxis.TickFormatter = new Rendering.TickFormatters.AutoDateFormatter(locator);
    }

    /// <summary>Adds a bar series to the default axes.</summary>
    public FigureBuilder Bar(string[] categories, double[] values, Action<BarSeries>? configure = null) =>
        AddSeries(ax => ax.Bar(categories, values), configure);

    /// <summary>Adds a histogram series to the default axes.</summary>
    public FigureBuilder Hist(double[] data, int bins = 10, Action<HistogramSeries>? configure = null) =>
        AddSeries(ax => ax.Hist(data, bins), configure);

    /// <summary>Adds an ECDF series to the default axes.</summary>
    public FigureBuilder Ecdf(double[] data, Action<EcdfSeries>? configure = null) =>
        AddSeries(ax => ax.Ecdf(data), configure);

    /// <summary>Adds a stacked area (stackplot) series to the default axes.</summary>
    public FigureBuilder StackPlot(double[] x, double[][] ySets, Action<StackedAreaSeries>? configure = null) =>
        AddSeries(ax => ax.StackPlot(x, ySets), configure);

    /// <summary>Adds a pie series to the default axes.</summary>
    public FigureBuilder Pie(double[] sizes, string[]? labels = null, Action<PieSeries>? configure = null) =>
        AddSeries(ax => ax.Pie(sizes, labels), configure);

    /// <summary>Adds an image series to the default axes.</summary>
    public FigureBuilder Image(double[,] data, Action<ImageSeries>? configure = null) =>
        AddSeries(ax => ax.Image(data), configure);

    /// <summary>Adds a 2D histogram (density) series to the default axes.</summary>
    public FigureBuilder Histogram2D(double[] x, double[] y, int bins = 20, Action<Histogram2DSeries>? configure = null) =>
        AddSeries(ax => ax.Histogram2D(x, y, bins), configure);

    /// <summary>Enables interactive zoom and pan via JavaScript in SVG output.</summary>
    public FigureBuilder WithZoomPan(bool enabled = true) { _enableZoomPan = enabled; return this; }
    private bool _enableZoomPan;

    /// <summary>Enables click-to-toggle legend entries in SVG output.</summary>
    public FigureBuilder WithLegendToggle(bool enabled = true) { _enableLegendToggle = enabled; return this; }
    private bool _enableLegendToggle;

    /// <summary>Enables styled HTML tooltip overlays (replacing native browser tooltips) in SVG output.</summary>
    public FigureBuilder WithRichTooltips(bool enabled = true) { _enableRichTooltips = enabled; return this; }
    private bool _enableRichTooltips;

    /// <summary>Enables series highlight-on-hover (dims sibling series) in SVG output.</summary>
    public FigureBuilder WithHighlight(bool enabled = true) { _enableHighlight = enabled; return this; }
    private bool _enableHighlight;

    /// <summary>Enables Shift+drag rectangular data selection in SVG output.</summary>
    public FigureBuilder WithSelection(bool enabled = true) { _enableSelection = enabled; return this; }
    private bool _enableSelection;

    /// <summary>Controls whether the emitted SVG's root element carries an inline
    /// <c>style="max-width:100%;height:auto"</c> declaration so the chart resizes
    /// fluidly with its container. Default is <see langword="true"/>; set to
    /// <see langword="false"/> for byte-identical pre-v1.7.2 output (e.g. pixel-diff
    /// fixtures that pin the SVG string).</summary>
    public FigureBuilder WithResponsiveSvg(bool enabled = true) { _responsiveSvg = enabled; return this; }
    private bool _responsiveSvg = true;

    /// <summary>Convenience that enables ALL browser-side interactions in one call:
    /// <see cref="WithZoomPan"/>, <see cref="WithRichTooltips"/>, <see cref="WithLegendToggle"/>,
    /// <see cref="WithHighlight"/>, <see cref="WithSelection"/>, plus the chart-type-specific
    /// <see cref="With3DRotation"/> (drag to rotate 3D scenes), <see cref="WithTreemapDrilldown"/>
    /// (click a tile to zoom), and <see cref="WithSankeyHover"/> (highlight upstream/downstream
    /// flows). Each chart-type-specific script is inert when its element is absent, so a 2D line
    /// chart with this convenience pays no runtime cost for the 3D / treemap / sankey scripts.
    /// The resulting SVG works standalone in any browser — no .NET runtime needed on the client.
    /// Adds ~5 KB of inline JS in total.</summary>
    public FigureBuilder WithBrowserInteraction(bool enabled = true)
    {
        _enableZoomPan = enabled;
        _enableRichTooltips = enabled;
        _enableLegendToggle = enabled;
        _enableHighlight = enabled;
        _enableSelection = enabled;
        _enable3DRotation = enabled;
        _enableTreemapDrilldown = enabled;
        _enableSankeyHover = enabled;
        return this;
    }

    private string? _chartId;
    private bool _serverInteraction;

    /// <summary>Opts the figure into bidirectional SignalR interaction under the supplied
    /// <paramref name="chartId"/>. Interaction events (zoom, pan, reset, legend-toggle) selected
    /// in <paramref name="configure"/> are routed through the ChartHub to the server's
    /// <c>FigureRegistry</c>, which mutates the registered figure and re-publishes the updated
    /// SVG. When this method is called, the SVG output embeds the SignalR dispatcher script
    /// instead of the local client-side IIFE scripts.</summary>
    public FigureBuilder WithServerInteraction(string chartId, Action<ServerInteractionBuilder> configure)
    {
        _chartId = chartId;
        _serverInteraction = true;
        var builder = new ServerInteractionBuilder();
        configure(builder);
        if (builder.Zoom || builder.Pan || builder.Reset)
            _enableZoomPan = true;
        if (builder.LegendToggle)
            _enableLegendToggle = true;
        if (builder.BrushSelect)
            _enableSelection = true;
        if (builder.Hover)
            _enableRichTooltips = true;
        return this;
    }

    private bool _enable3DRotation;

    /// <summary>Enables tight layout, which computes minimal margins automatically.</summary>
    public FigureBuilder TightLayout() { _spacing = _spacing with { TightLayout = true }; return this; }

    /// <summary>Enables constrained layout, which adjusts margins to fit tick labels and axis labels.</summary>
    public FigureBuilder ConstrainedLayout() { _spacing = _spacing with { ConstrainedLayout = true }; return this; }

    /// <summary>Configures custom subplot spacing (margins and gaps).</summary>
    public FigureBuilder WithSubPlotSpacing(Func<SubPlotSpacing, SubPlotSpacing>? configure = null) { if (configure is not null) _spacing = configure(_spacing); return this; }
    private SubPlotSpacing _spacing = new();

    /// <summary>Adds an error bar series to the default axes.</summary>
    public FigureBuilder ErrorBar(double[] x, double[] y, double[] yErrorLow, double[] yErrorHigh, Action<ErrorBarSeries>? configure = null) =>
        AddSeries(ax => ax.ErrorBar(x, y, yErrorLow, yErrorHigh), configure);

    /// <summary>Adds a step-function series to the default axes.</summary>
    public FigureBuilder Step(double[] x, double[] y, Action<StepSeries>? configure = null) =>
        AddSeries(ax => ax.Step(x, y), configure);

    /// <summary>Adds a filled area (fill-between) series to the default axes.</summary>
    public FigureBuilder FillBetween(double[] x, double[] y, double[]? y2 = null, Action<AreaSeries>? configure = null) =>
        AddSeries(ax => ax.FillBetween(x, y, y2), configure);

    /// <summary>Adds a filled contour series to the default axes.</summary>
    public FigureBuilder Contourf(double[] x, double[] y, double[,] z, Action<ContourfSeries>? configure = null) =>
        AddSeries(ax => ax.Contourf(x, y, z), configure);

    /// <summary>Adds a kernel density estimation (KDE) series to the default axes.</summary>
    public FigureBuilder Kde(double[] data, Action<KdeSeries>? configure = null) =>
        AddSeries(ax => ax.Kde(data), configure);

    /// <summary>Adds a treemap series to the default axes.</summary>
    public FigureBuilder Treemap(TreeNode root, Action<TreemapSeries>? configure = null) =>
        AddSeries(ax => ax.Treemap(root), configure);

    /// <summary>Adds a sunburst series to the default axes.</summary>
    public FigureBuilder Sunburst(TreeNode root, Action<SunburstSeries>? configure = null) =>
        AddSeries(ax => ax.Sunburst(root), configure);

    /// <summary>Adds a Sankey diagram series to the default axes.</summary>
    public FigureBuilder Sankey(SankeyNode[] nodes, SankeyLink[] links, Action<SankeySeries>? configure = null) =>
        AddSeries(ax => ax.Sankey(nodes, links), configure);

    /// <summary>Adds a polar line series to the default axes.</summary>
    public FigureBuilder PolarPlot(double[] r, double[] theta, Action<PolarLineSeries>? configure = null) =>
        AddSeries(ax => ax.PolarPlot(r, theta), configure);

    /// <summary>Adds a polar scatter series to the default axes.</summary>
    public FigureBuilder PolarScatter(double[] r, double[] theta, Action<PolarScatterSeries>? configure = null) =>
        AddSeries(ax => ax.PolarScatter(r, theta), configure);

    /// <summary>Adds a polar bar series to the default axes.</summary>
    public FigureBuilder PolarBar(double[] r, double[] theta, Action<PolarBarSeries>? configure = null) =>
        AddSeries(ax => ax.PolarBar(r, theta), configure);

    /// <summary>Adds a 3D surface series to the default axes.</summary>
    public FigureBuilder Surface(double[] x, double[] y, double[,] z, Action<SurfaceSeries>? configure = null) =>
        AddSeries(ax => ax.Surface(x, y, z), configure);

    /// <summary>Adds a 3D wireframe series to the default axes.</summary>
    public FigureBuilder Wireframe(double[] x, double[] y, double[,] z, Action<WireframeSeries>? configure = null) =>
        AddSeries(ax => ax.Wireframe(x, y, z), configure);

    /// <summary>Adds a 3D scatter series to the default axes.</summary>
    public FigureBuilder Scatter3D(double[] x, double[] y, double[] z, Action<Scatter3DSeries>? configure = null) =>
        AddSeries(ax => ax.Scatter3D(x, y, z), configure);

    /// <summary>Adds a 3D stem series to the default axes.</summary>
    public FigureBuilder Stem3D(double[] x, double[] y, double[] z, Action<Stem3DSeries>? configure = null) =>
        AddSeries(ax => ax.Stem3D(x, y, z), configure);

    /// <summary>Adds a 3D bar series to the default axes.</summary>
    public FigureBuilder Bar3D(double[] x, double[] y, double[] z, Action<Bar3DSeries>? configure = null) =>
        AddSeries(ax => ax.Bar3D(x, y, z), configure);

    /// <summary>Sets camera elevation, azimuth, and optional perspective distance on the default axes.</summary>
    /// <param name="elevation">Camera elevation above the XY plane in degrees.</param>
    /// <param name="azimuth">Camera azimuth rotation around the Z axis in degrees.</param>
    /// <param name="distance">Perspective camera distance. Null = orthographic. Minimum clamped to 2.0.</param>
    public FigureBuilder WithCamera(double elevation = 30, double azimuth = -60, double? distance = null)
    {
        var axes = EnsureDefaultAxes();
        axes.Elevation = elevation;
        axes.Azimuth = azimuth;
        axes.CameraDistance = distance;
        return this;
    }

    /// <summary>Enables interactive 3D rotation via mouse drag and keyboard arrow keys in the SVG output.</summary>
    public FigureBuilder With3DRotation(bool enabled = true)
    {
        _enable3DRotation = enabled;
        return this;
    }

    /// <summary>Enables click-to-drill-down on treemap rectangles in the SVG output.
    /// Click a rect to zoom into its subtree, Escape to zoom out, with smooth transitions.
    /// Requires a <c>TreemapSeries</c> on the figure.</summary>
    public FigureBuilder WithTreemapDrilldown(bool enabled = true)
    {
        _enableTreemapDrilldown = enabled;
        return this;
    }
    private bool _enableTreemapDrilldown;

    /// <summary>Enables Sankey hover emphasis in the SVG output: hovering a node dims every
    /// link not reachable upstream or downstream from it (ECharts <c>focus: adjacency</c>).
    /// Requires a <c>SankeySeries</c> on the figure.</summary>
    public FigureBuilder WithSankeyHover(bool enabled = true)
    {
        _enableSankeyHover = enabled;
        return this;
    }
    private bool _enableSankeyHover;

    /// <summary>Configures opacity / transition tokens for the embedded browser-side
    /// interaction scripts. Defaults match v1.7.1 hard-coded values, so calling this is
    /// only necessary if you want different fade opacities, transition durations, or
    /// tooltip offsets. Phase 7 of the v1.7.2 plan.</summary>
    public FigureBuilder WithInteractionTheme(Models.InteractionTheme theme)
    {
        _interactionTheme = theme;
        return this;
    }
    private Models.InteractionTheme _interactionTheme = Models.InteractionTheme.Default;

    /// <summary>Attaches a directional light source for per-face shading on 3D surfaces and bars on the default axes.</summary>
    /// <param name="dx">X component of the light direction.</param>
    /// <param name="dy">Y component of the light direction.</param>
    /// <param name="dz">Z component of the light direction.</param>
    /// <param name="ambient">Ambient light intensity [0, 1]. Default 0.3.</param>
    /// <param name="diffuse">Diffuse light intensity [0, 1]. Default 0.7.</param>
    public FigureBuilder WithLighting(double dx, double dy, double dz,
        double ambient = 0.3, double diffuse = 0.7)
    {
        EnsureDefaultAxes().LightSource = new Rendering.Lighting.DirectionalLight(dx, dy, dz, ambient, diffuse);
        return this;
    }

    // --- v0.8.0 shortcuts ---

    /// <summary>Adds a rug plot series to the default axes.</summary>
    public FigureBuilder Rugplot(double[] data, Action<RugplotSeries>? configure = null) =>
        AddSeries(ax => ax.Rugplot(data), configure);

    /// <summary>Adds a strip plot series to the default axes.</summary>
    public FigureBuilder Stripplot(double[][] datasets, Action<StripplotSeries>? configure = null) =>
        AddSeries(ax => ax.Stripplot(datasets), configure);

    /// <summary>Adds an event plot series to the default axes.</summary>
    public FigureBuilder Eventplot(double[][] positions, Action<EventplotSeries>? configure = null) =>
        AddSeries(ax => ax.Eventplot(positions), configure);

    /// <summary>Adds a broken bar series to the default axes.</summary>
    public FigureBuilder BrokenBarH((double Start, double Width)[][] ranges, Action<BrokenBarSeries>? configure = null) =>
        AddSeries(ax => ax.BrokenBarH(ranges), configure);

    /// <summary>Adds a count plot series to the default axes.</summary>
    public FigureBuilder Countplot(string[] values, Action<CountSeries>? configure = null) =>
        AddSeries(ax => ax.Countplot(values), configure);

    /// <summary>Adds a pseudocolor mesh series to the default axes.</summary>
    public FigureBuilder Pcolormesh(double[] x, double[] y, double[,] c, Action<PcolormeshSeries>? configure = null) =>
        AddSeries(ax => ax.Pcolormesh(x, y, c), configure);

    /// <summary>Adds a residual plot series to the default axes.</summary>
    public FigureBuilder Residplot(double[] x, double[] y, Action<ResidualSeries>? configure = null) =>
        AddSeries(ax => ax.Residplot(x, y), configure);

    /// <summary>Adds a point plot series to the default axes.</summary>
    public FigureBuilder Pointplot(double[][] datasets, Action<PointplotSeries>? configure = null) =>
        AddSeries(ax => ax.Pointplot(datasets), configure);

    /// <summary>Adds a swarm plot series to the default axes.</summary>
    public FigureBuilder Swarmplot(double[][] datasets, Action<SwarmplotSeries>? configure = null) =>
        AddSeries(ax => ax.Swarmplot(datasets), configure);

    /// <summary>Adds a spectrogram series to the default axes.</summary>
    public FigureBuilder Spectrogram(double[] signal, int sampleRate = 1, Action<SpectrogramSeries>? configure = null) =>
        AddSeries(ax => ax.Spectrogram(signal, sampleRate), configure);

    /// <summary>Adds a table series to the default axes.</summary>
    public FigureBuilder Table(string[][] cellData, Action<TableSeries>? configure = null) =>
        AddSeries(ax => ax.Table(cellData), configure);

    /// <summary>Adds a contour series on a triangular mesh to the default axes.</summary>
    public FigureBuilder Tricontour(double[] x, double[] y, double[] z, Action<TricontourSeries>? configure = null) =>
        AddSeries(ax => ax.Tricontour(x, y, z), configure);

    /// <summary>Adds a pseudocolor series on a triangular mesh to the default axes.</summary>
    public FigureBuilder Tripcolor(double[] x, double[] y, double[] z, Action<TripcolorSeries>? configure = null) =>
        AddSeries(ax => ax.Tripcolor(x, y, z), configure);

    /// <summary>Adds a quiver key series to the default axes.</summary>
    public FigureBuilder QuiverKey(double x, double y, double u, string label, Action<QuiverKeySeries>? configure = null) =>
        AddSeries(ax => ax.QuiverKey(x, y, u, label), configure);

    /// <summary>Adds a wind barb series to the default axes.</summary>
    public FigureBuilder Barbs(double[] x, double[] y, double[] speed, double[] direction, Action<BarbsSeries>? configure = null) =>
        AddSeries(ax => ax.Barbs(x, y, speed, direction), configure);

    /// <summary>Sets the grid specification for advanced subplot layouts with ratios and spanning.</summary>
    /// <param name="rows">Number of rows in the grid.</param>
    /// <param name="cols">Number of columns in the grid.</param>
    /// <param name="heightRatios">Relative row heights (length must equal <paramref name="rows"/>); <see langword="null"/> gives equal heights.</param>
    /// <param name="widthRatios">Relative column widths (length must equal <paramref name="cols"/>); <see langword="null"/> gives equal widths.</param>
    public FigureBuilder WithGridSpec(int rows, int cols, double[]? heightRatios = null, double[]? widthRatios = null)
    {
        _gridSpec = new GridSpec { Rows = rows, Cols = cols, HeightRatios = heightRatios, WidthRatios = widthRatios };
        return this;
    }

    /// <summary>Adds a figure-level color bar rendered outside all subplots.</summary>
    /// <param name="configure">Optional function to customise the color bar (label, orientation, shrink, etc.).</param>
    public FigureBuilder WithColorBar(Func<ColorBar, ColorBar>? configure = null)
    {
        var cb = _figureColorBar ?? new ColorBar { Visible = true };
        if (configure is not null) cb = configure(cb);
        _figureColorBar = cb;
        return this;
    }

    private ColorBar? _figureColorBar;

    /// <summary>Adds a subplot at the specified grid position, configured via an <see cref="AxesBuilder"/>.</summary>
    /// <param name="rows">Number of rows in the subplot grid.</param>
    /// <param name="cols">Number of columns in the subplot grid.</param>
    /// <param name="index">One-based index of this subplot within the grid.</param>
    /// <param name="configure">Action to configure the subplot axes.</param>
    /// <param name="key">Optional string key for referencing this axes in sharing.</param>
    public FigureBuilder AddSubPlot(int rows, int cols, int index, Action<AxesBuilder> configure, string? key = null)
    {
        _subPlots.Add(new SubPlotSpec(rows, cols, index, configure) { Key = key });
        return this;
    }

    /// <summary>Adds a subplot at the specified <see cref="GridPosition"/> within the current grid spec.</summary>
    /// <param name="position">The cell position (and optional span) within the grid.</param>
    /// <param name="configure">Action to configure the subplot axes.</param>
    public FigureBuilder AddSubPlot(GridPosition position, Action<AxesBuilder> configure)
    {
        _subPlots.Add(new SubPlotSpec(position, configure));
        return this;
    }

    /// <summary>Builds the figure and wraps it in a <see cref="StreamingFigure"/> for live data.
    /// Use this when the figure contains streaming series that receive data via <c>AppendPoint</c>.</summary>
    /// <param name="renderInterval">Minimum time between renders. Default 33ms (~30fps).</param>
    /// <returns>A <see cref="StreamingFigure"/> managing render throttling and axis scaling.</returns>
    public StreamingFigure BuildStreaming(TimeSpan? renderInterval = null)
    {
        var sf = new StreamingFigure(Build());
        if (renderInterval is not null)
            sf.MinRenderInterval = renderInterval.Value;
        return sf;
    }

    /// <summary>Builds and returns the configured <see cref="Models.Figure"/> instance.</summary>
    /// <returns>The fully constructed figure.</returns>
    public Figure Build()
    {
        var figure = new Figure
        {
            Title = _title,
            AltText = _altText,
            Description = _description,
            Width = _width,
            Height = _height,
            Dpi = _dpi,
            BackgroundColor = _background,
            Theme = _theme,
            ChartId = _chartId,
            ServerInteraction = _serverInteraction,
            EnableZoomPan = _enableZoomPan,
            EnableLegendToggle = _enableLegendToggle,
            EnableRichTooltips = _enableRichTooltips,
            EnableHighlight = _enableHighlight,
            EnableSelection = _enableSelection,
            Enable3DRotation = _enable3DRotation,
            EnableTreemapDrilldown = _enableTreemapDrilldown,
            EnableSankeyHover = _enableSankeyHover,
            InteractionTheme = _interactionTheme,
            ResponsiveSvg = _responsiveSvg,
            Spacing = _spacing,
            GridSpec = _gridSpec,
            FigureColorBar = _figureColorBar
        };

        // Only add the default axes when it carries actual series OR when no subplots were
        // defined. FigureBuilder-level helpers (WithCamera, WithLighting, …) call
        // EnsureDefaultAxes() as a convenience side-effect, leaving an empty Cartesian axes
        // in the model that would otherwise be rendered as ghost 2-D ticks on top of 3-D plots.
        if (_defaultAxes is not null &&
            (_defaultAxes.Series.Count > 0 || _subPlots.Count == 0))
            figure.AddAxes(_defaultAxes);

        var keyedAxes = new Dictionary<string, Axes>();
        var deferredSharing = new List<(Axes axes, string? shareXKey, string? shareYKey)>();

        foreach (var spec in _subPlots)
        {
            var builder = new AxesBuilder();
            spec.Configure(builder);

            Axes axes;
            if (spec.Position is { } pos)
                axes = builder.Build(pos);
            else
                axes = builder.Build(spec.Rows, spec.Cols, spec.Index);

            if (spec.Key is not null)
            {
                axes.Key = spec.Key;
                keyedAxes[spec.Key] = axes;
            }

            if (builder.ShareXKey is not null || builder.ShareYKey is not null)
                deferredSharing.Add((axes, builder.ShareXKey, builder.ShareYKey));

            figure.AddAxes(axes);
        }

        // Resolve sharing references by key
        foreach (var (axes, shareXKey, shareYKey) in deferredSharing)
        {
            if (shareXKey is not null && keyedAxes.TryGetValue(shareXKey, out var xTarget))
                axes.ShareXWith = xTarget;
            if (shareYKey is not null && keyedAxes.TryGetValue(shareYKey, out var yTarget))
                axes.ShareYWith = yTarget;
        }

        // Apply pending insets
        foreach (var (subplotIndex, x, y, w, h, configure) in _pendingInsets)
        {
            if (subplotIndex >= 0 && subplotIndex < figure.SubPlots.Count)
            {
                var inset = figure.SubPlots[subplotIndex].AddInset(x, y, w, h);
                configure?.Invoke(inset);
            }
        }

        return figure;
    }

    // --- Annotation / reference line / span convenience methods on default axes ---

    /// <summary>Adds a text annotation at the specified data coordinates on the default axes.</summary>
    public FigureBuilder Annotate(string text, double x, double y, Action<Annotation>? configure = null)
    {
        var ann = EnsureDefaultAxes().Annotate(text, x, y);
        configure?.Invoke(ann);
        return this;
    }

    /// <summary>Adds a text annotation with an arrow pointing to the given target coordinates on the default axes.</summary>
    public FigureBuilder Annotate(string text, double x, double y, double arrowX, double arrowY,
        Action<Annotation>? configure = null)
    {
        var ann = EnsureDefaultAxes().Annotate(text, x, y);
        ann.ArrowTargetX = arrowX;
        ann.ArrowTargetY = arrowY;
        configure?.Invoke(ann);
        return this;
    }

    /// <summary>Adds a horizontal reference line at the specified Y value on the default axes.</summary>
    public FigureBuilder AxHLine(double y, Action<ReferenceLine>? configure = null)
    {
        var line = EnsureDefaultAxes().AxHLine(y);
        configure?.Invoke(line);
        return this;
    }

    /// <summary>Adds a vertical reference line at the specified X value on the default axes.</summary>
    public FigureBuilder AxVLine(double x, Action<ReferenceLine>? configure = null)
    {
        var line = EnsureDefaultAxes().AxVLine(x);
        configure?.Invoke(line);
        return this;
    }

    /// <summary>Adds a horizontal shaded span between the specified Y values on the default axes.</summary>
    public FigureBuilder AxHSpan(double yMin, double yMax, Action<SpanRegion>? configure = null)
    {
        var span = EnsureDefaultAxes().AxHSpan(yMin, yMax);
        configure?.Invoke(span);
        return this;
    }

    /// <summary>Adds a vertical shaded span between the specified X values on the default axes.</summary>
    public FigureBuilder AxVSpan(double xMin, double xMax, Action<SpanRegion>? configure = null)
    {
        var span = EnsureDefaultAxes().AxVSpan(xMin, xMax);
        configure?.Invoke(span);
        return this;
    }

    // --- Output convenience methods (delegate to Figure extensions via Build()) ---

    /// <summary>Builds the figure and renders it as an SVG string.</summary>
    public string ToSvg() => Build().ToSvg();

    /// <summary>Builds the figure and serializes it to JSON.</summary>
    public string ToJson(bool indented = false) => Build().ToJson(indented);

    /// <summary>Builds the figure and saves it to a file. Format auto-detected from extension.</summary>
    public void Save(string path) => Build().Save(path);

    private FigureBuilder AddSeries<T>(Func<Axes, T> factory, Action<T>? configure) where T : ISeries
    {
        var series = factory(EnsureDefaultAxes());
        configure?.Invoke(series);
        return this;
    }

    /// <summary>Adds an inset axes to the subplot at the given zero-based index.
    /// The bounds are fractional positions within the parent subplot (0–1).</summary>
    /// <param name="subplotIndex">Zero-based index of the parent subplot.</param>
    /// <param name="x">Horizontal position as a fraction of parent width.</param>
    /// <param name="y">Vertical position as a fraction of parent height.</param>
    /// <param name="width">Width as a fraction of parent width.</param>
    /// <param name="height">Height as a fraction of parent height.</param>
    /// <param name="configure">Optional action to configure the inset axes.</param>
    public FigureBuilder AddInset(int subplotIndex, double x, double y, double width, double height,
        Action<Axes>? configure = null)
    {
        _pendingInsets.Add((subplotIndex, x, y, width, height, configure));
        return this;
    }

    private readonly List<(int SubplotIndex, double X, double Y, double Width, double Height, Action<Axes>? Configure)>
        _pendingInsets = [];

    private Axes EnsureDefaultAxes() => _defaultAxes ??= new Axes();
}

/// <summary>Describes a subplot position and configuration within a figure grid.</summary>
internal readonly record struct SubPlotSpec
{
    public int Rows { get; init; }

    public int Cols { get; init; }

    public int Index { get; init; }

    public GridPosition? Position { get; init; }

    public string? Key { get; init; }

    public Action<AxesBuilder> Configure { get; init; }

    /// <summary>Creates a legacy grid-index spec.</summary>
    public SubPlotSpec(int rows, int cols, int index, Action<AxesBuilder> configure)
        : this() { Rows = rows; Cols = cols; Index = index; Configure = configure; }

    /// <summary>Creates a GridPosition-based spec.</summary>
    public SubPlotSpec(GridPosition position, Action<AxesBuilder> configure)
        : this() { Position = position; Configure = configure; }
}
