// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Fluent builder for constructing a <see cref="Models.Figure"/> with a chainable API.</summary>
public sealed class FigureBuilder
{
    private string? _title;
    private double _width = 800;
    private double _height = 600;
    private double _dpi = 96;
    private Color? _background;
    private Theme _theme = Theme.Default;
    private Axes? _defaultAxes;
    private readonly List<SubPlotSpec> _subPlots = [];
    private GridSpec? _gridSpec;

    /// <summary>Sets the figure title.</summary>
    public FigureBuilder WithTitle(string title) { _title = title; return this; }

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

    /// <summary>Enables tight layout, which computes minimal margins automatically.</summary>
    public FigureBuilder TightLayout() { _spacing = _spacing with { TightLayout = true }; return this; }

    /// <summary>Enables constrained layout, which adjusts margins to fit tick labels and axis labels.</summary>
    public FigureBuilder ConstrainedLayout() { _spacing = _spacing with { ConstrainedLayout = true }; return this; }

    /// <summary>Configures custom subplot spacing (margins and gaps).</summary>
    public FigureBuilder WithSubPlotSpacing(Func<SubPlotSpacing, SubPlotSpacing> configure) { _spacing = configure(_spacing); return this; }
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
    public FigureBuilder WithGridSpec(int rows, int cols, double[]? heightRatios = null, double[]? widthRatios = null)
    {
        _gridSpec = new GridSpec { Rows = rows, Cols = cols, HeightRatios = heightRatios, WidthRatios = widthRatios };
        return this;
    }

    /// <summary>Adds a figure-level color bar rendered outside all subplots.</summary>
    /// <param name="configure">Optional function to customise the color bar (label, orientation, shrink, etc.).</param>
    public FigureBuilder WithColorBar(Func<ColorBar, ColorBar>? configure = null)
    {
        var cb = new ColorBar { Visible = true };
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

    /// <summary>Builds and returns the configured <see cref="Models.Figure"/> instance.</summary>
    /// <returns>The fully constructed figure.</returns>
    public Figure Build()
    {
        var figure = new Figure
        {
            Title = _title,
            Width = _width,
            Height = _height,
            Dpi = _dpi,
            BackgroundColor = _background,
            Theme = _theme,
            EnableZoomPan = _enableZoomPan,
            EnableLegendToggle = _enableLegendToggle,
            EnableRichTooltips = _enableRichTooltips,
            EnableHighlight = _enableHighlight,
            EnableSelection = _enableSelection,
            Spacing = _spacing,
            GridSpec = _gridSpec,
            FigureColorBar = _figureColorBar
        };

        if (_defaultAxes is not null)
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

        return figure;
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
