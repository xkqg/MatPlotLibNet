// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Fluent builder for configuring an <see cref="Models.Axes"/> instance within a subplot.</summary>
public sealed class AxesBuilder
{
    private readonly Axes _axes = new();

    /// <summary>Sets the axes title.</summary>
    public AxesBuilder WithTitle(string title) { _axes.Title = title; return this; }

    /// <summary>Sets the X-axis label.</summary>
    public AxesBuilder SetXLabel(string label) { _axes.XAxis.Label = label; return this; }

    /// <summary>Sets the Y-axis label.</summary>
    public AxesBuilder SetYLabel(string label) { _axes.YAxis.Label = label; return this; }

    /// <summary>Sets the X-axis data range limits.</summary>
    public AxesBuilder SetXLim(double min, double max) { _axes.XAxis.Min = min; _axes.XAxis.Max = max; return this; }

    /// <summary>Sets the Y-axis data range limits.</summary>
    public AxesBuilder SetYLim(double min, double max) { _axes.YAxis.Min = min; _axes.YAxis.Max = max; return this; }

    /// <summary>Sets the X-axis scale type (e.g., linear or logarithmic).</summary>
    public AxesBuilder SetXScale(AxisScale scale) { _axes.XAxis.Scale = scale; return this; }

    /// <summary>Sets the Y-axis scale type (e.g., linear or logarithmic).</summary>
    public AxesBuilder SetYScale(AxisScale scale) { _axes.YAxis.Scale = scale; return this; }

    /// <summary>Adds a buy or sell signal marker at the specified data coordinates.</summary>
    public AxesBuilder AddSignal(double x, double y, SignalDirection direction, Action<SignalMarker>? configure = null)
    {
        var marker = _axes.AddSignal(x, y, direction);
        configure?.Invoke(marker);
        return this;
    }

    /// <summary>Applies a technical indicator to this axes, adding computed series or decorations.</summary>
    /// <remarks>Overlay indicators (SMA, EMA, Bollinger Bands) add series to the same axes.
    /// Panel indicators (RSI, MACD) should be placed in a separate subplot by the caller.</remarks>
    public AxesBuilder AddIndicator(IIndicator indicator)
    {
        indicator.Apply(_axes);
        return this;
    }

    /// <summary>Configures the spine (border line) display for this axes.</summary>
    public AxesBuilder WithSpines(Func<SpinesConfig, SpinesConfig> configure) { _axes.Spines = configure(_axes.Spines); return this; }

    /// <summary>Hides the top spine.</summary>
    public AxesBuilder HideTopSpine() { _axes.Spines = _axes.Spines with { Top = _axes.Spines.Top with { Visible = false } }; return this; }

    /// <summary>Hides the right spine.</summary>
    public AxesBuilder HideRightSpine() { _axes.Spines = _axes.Spines with { Right = _axes.Spines.Right with { Visible = false } }; return this; }

    /// <summary>Shares the X axis with the axes identified by the given key.</summary>
    public AxesBuilder ShareX(string key) { _shareXKey = key; return this; }
    private string? _shareXKey;

    /// <summary>Shares the Y axis with the axes identified by the given key.</summary>
    public AxesBuilder ShareY(string key) { _shareYKey = key; return this; }
    private string? _shareYKey;

    /// <summary>Gets the pending share-X key, if any. Used by FigureBuilder to resolve sharing at build time.</summary>
    internal string? ShareXKey => _shareXKey;

    /// <summary>Gets the pending share-Y key, if any. Used by FigureBuilder to resolve sharing at build time.</summary>
    internal string? ShareYKey => _shareYKey;

    /// <summary>Enables or disables native SVG tooltips for data elements.</summary>
    public AxesBuilder WithTooltips(bool enabled = true) { _axes.EnableTooltips = enabled; return this; }

    /// <summary>Configures a secondary Y-axis and adds series to it.</summary>
    public AxesBuilder WithSecondaryYAxis(Action<SecondaryAxisBuilder> configure)
    {
        _axes.TwinX();
        var builder = new SecondaryAxisBuilder(_axes);
        configure(builder);
        return this;
    }

    /// <summary>Adds a text annotation at the specified data coordinates.</summary>
    public AxesBuilder Annotate(string text, double x, double y, Action<Annotation>? configure = null)
    {
        var annotation = _axes.Annotate(text, x, y);
        configure?.Invoke(annotation);
        return this;
    }

    /// <summary>Adds a horizontal reference line at the specified Y value.</summary>
    public AxesBuilder AxHLine(double y, Action<ReferenceLine>? configure = null)
    {
        var line = _axes.AxHLine(y);
        configure?.Invoke(line);
        return this;
    }

    /// <summary>Adds a vertical reference line at the specified X value.</summary>
    public AxesBuilder AxVLine(double x, Action<ReferenceLine>? configure = null)
    {
        var line = _axes.AxVLine(x);
        configure?.Invoke(line);
        return this;
    }

    /// <summary>Adds a horizontal shaded span between the specified Y values.</summary>
    public AxesBuilder AxHSpan(double yMin, double yMax, Action<SpanRegion>? configure = null)
    {
        var span = _axes.AxHSpan(yMin, yMax);
        configure?.Invoke(span);
        return this;
    }

    /// <summary>Adds a vertical shaded span between the specified X values.</summary>
    public AxesBuilder AxVSpan(double xMin, double xMax, Action<SpanRegion>? configure = null)
    {
        var span = _axes.AxVSpan(xMin, xMax);
        configure?.Invoke(span);
        return this;
    }

    /// <summary>Sets the X-axis to date scale with intelligent tick placement and automatic format selection.</summary>
    /// <remarks>
    /// Installs an <see cref="Rendering.TickLocators.AutoDateLocator"/> and a paired
    /// <see cref="Rendering.TickFormatters.AutoDateFormatter"/> that together choose granularity
    /// (years, months, weeks, days, hours, minutes, or seconds) from the visible range.
    /// </remarks>
    public AxesBuilder SetXDateAxis()
    {
        var locator = new Rendering.TickLocators.AutoDateLocator();
        _axes.XAxis.Scale = AxisScale.Date;
        _axes.XAxis.TickLocator = locator;
        _axes.XAxis.TickFormatter = new Rendering.TickFormatters.AutoDateFormatter(locator);
        return this;
    }

    /// <summary>Adds a line series with DateTime X values; automatically activates the date X-axis.</summary>
    /// <param name="dates">X values as <see cref="DateTime"/> instances (converted to OLE Automation dates).</param>
    /// <param name="y">Y values corresponding to each date.</param>
    /// <param name="configure">Optional delegate to further configure the series.</param>
    public AxesBuilder Plot(DateTime[] dates, double[] y, Action<Models.Series.LineSeries>? configure = null)
        => SetXDateAxis().Plot(dates.Select(d => d.ToOADate()).ToArray(), y, configure);

    /// <summary>Adds a scatter series with DateTime X values; automatically activates the date X-axis.</summary>
    /// <param name="dates">X values as <see cref="DateTime"/> instances (converted to OLE Automation dates).</param>
    /// <param name="y">Y values corresponding to each date.</param>
    /// <param name="configure">Optional delegate to further configure the series.</param>
    public AxesBuilder Scatter(DateTime[] dates, double[] y, Action<Models.Series.ScatterSeries>? configure = null)
        => SetXDateAxis().Scatter(dates.Select(d => d.ToOADate()).ToArray(), y, configure);

    /// <summary>Sets the X-axis to date scale with the specified format.</summary>
    public AxesBuilder SetXDateFormat(string format = "yyyy-MM-dd")
    {
        _axes.XAxis.Scale = AxisScale.Date;
        _axes.XAxis.TickFormatter = new Rendering.TickFormatters.DateTickFormatter { DateFormat = format };
        return this;
    }

    /// <summary>Sets the Y-axis to date scale with the specified format.</summary>
    public AxesBuilder SetYDateFormat(string format = "yyyy-MM-dd")
    {
        _axes.YAxis.Scale = AxisScale.Date;
        _axes.YAxis.TickFormatter = new Rendering.TickFormatters.DateTickFormatter { DateFormat = format };
        return this;
    }

    /// <summary>Sets a custom tick formatter on the X-axis.</summary>
    public AxesBuilder SetXTickFormatter(Rendering.TickFormatters.ITickFormatter formatter)
    {
        _axes.XAxis.TickFormatter = formatter;
        return this;
    }

    /// <summary>Sets a custom tick formatter on the Y-axis.</summary>
    public AxesBuilder SetYTickFormatter(Rendering.TickFormatters.ITickFormatter formatter)
    {
        _axes.YAxis.TickFormatter = formatter;
        return this;
    }

    /// <summary>Sets a custom tick locator on the X-axis, overriding the default nice-number algorithm.</summary>
    /// <param name="locator">The <see cref="Rendering.TickLocators.ITickLocator"/> implementation to use.</param>
    /// <remarks>Also overrides any <see cref="Models.TickConfig.Spacing"/> set on the axis. Tick formatting via the axis <c>TickFormatter</c> is still applied after the locator produces its positions.</remarks>
    public AxesBuilder SetXTickLocator(Rendering.TickLocators.ITickLocator locator)
    {
        _axes.XAxis.TickLocator = locator;
        return this;
    }

    /// <summary>Sets a custom tick locator on the Y-axis, overriding the default nice-number algorithm.</summary>
    /// <param name="locator">The <see cref="Rendering.TickLocators.ITickLocator"/> implementation to use.</param>
    /// <remarks>Also overrides any <see cref="Models.TickConfig.Spacing"/> set on the axis. Tick formatting via the axis <c>TickFormatter</c> is still applied after the locator produces its positions.</remarks>
    public AxesBuilder SetYTickLocator(Rendering.TickLocators.ITickLocator locator)
    {
        _axes.YAxis.TickLocator = locator;
        return this;
    }

    /// <summary>Enables or disables minor tick marks on both axes. Minor ticks subdivide each major interval into 5 sub-intervals.</summary>
    /// <param name="visible">When <c>true</c> (default), minor ticks are shown; when <c>false</c>, they are hidden.</param>
    public AxesBuilder WithMinorTicks(bool visible = true)
    {
        _axes.XAxis.MinorTicks = _axes.XAxis.MinorTicks with { Visible = visible };
        _axes.YAxis.MinorTicks = _axes.YAxis.MinorTicks with { Visible = visible };
        return this;
    }

    /// <summary>
    /// Enables bar value labels on the last <see cref="BarSeries"/> added to this axes.
    /// </summary>
    /// <param name="format">Optional .NET format string (e.g. "F1"). When null, uses "G4".</param>
    /// <remarks>Only the last <see cref="BarSeries"/> in the series list is affected. Call this method once per <see cref="BarSeries"/> immediately after adding it.</remarks>
    public AxesBuilder WithBarLabels(string? format = null)
    {
        if (_axes.Series.LastOrDefault(s => s is BarSeries) is BarSeries bar)
        {
            bar.ShowLabels = true;
            bar.LabelFormat = format;
        }
        return this;
    }

    /// <summary>
    /// Enables LTTB downsampling on the last XY series (Line, Area, Scatter, Step) added to this axes.
    /// Viewport culling is applied first; if the result still exceeds <paramref name="maxPoints"/>, LTTB reduces it.
    /// </summary>
    /// <param name="maxPoints">Maximum number of points to display. Default is 2000.</param>
    /// <remarks>Only the last XY series is affected; call after each series you want to downsample. The two-stage pipeline (viewport cull → LTTB) is opt-in — series without <c>MaxDisplayPoints</c> set are rendered at full resolution.</remarks>
    public AxesBuilder WithDownsampling(int maxPoints = 2000)
    {
        if (_axes.Series.LastOrDefault(s => s is Models.Series.XYSeries) is Models.Series.XYSeries xy)
            xy.MaxDisplayPoints = maxPoints;
        return this;
    }

    /// <summary>Sets the bar mode (grouped or stacked) for multiple bar series.</summary>
    public AxesBuilder SetBarMode(BarMode mode) { _axes.BarMode = mode; return this; }

    /// <summary>Sets the colormap on the last heatmap/contour/surface series by name (resolved from <see cref="Styling.ColorMaps.ColorMapRegistry"/>).</summary>
    public AxesBuilder WithColorMap(string name)
    {
        var map = Styling.ColorMaps.ColorMapRegistry.Get(name);
        if (map is not null) return WithColorMap(map);
        return this;
    }

    /// <summary>Sets the colormap on the last colormappable series (heatmap, image, histogram2d, contour, surface, scatter, hierarchical).</summary>
    public AxesBuilder WithColorMap(Styling.ColorMaps.IColorMap colorMap)
    {
        if (_axes.Series.LastOrDefault() is Models.Series.IColormappable c) c.ColorMap = colorMap;
        return this;
    }

    /// <summary>Sets the normalizer on the last normalizable series (heatmap, image, histogram2d).</summary>
    public AxesBuilder WithNormalizer(Styling.ColorMaps.INormalizer normalizer)
    {
        if (_axes.Series.LastOrDefault() is Models.Series.INormalizable n) n.Normalizer = normalizer;
        return this;
    }

    /// <summary>Enables a color bar alongside the plot area. Auto-detects colormap and range from heatmap/contour series.</summary>
    public AxesBuilder WithColorBar(Func<ColorBar, ColorBar>? configure = null)
    {
        var cb = new ColorBar { Visible = true };
        if (configure is not null) cb = configure(cb);
        _axes.ColorBar = cb;
        return this;
    }

    /// <summary>Configures the legend display for this axes.</summary>
    public AxesBuilder WithLegend(LegendPosition position = LegendPosition.Best, bool visible = true)
    {
        _axes.Legend = new Legend { Visible = visible, Position = position };
        return this;
    }

    /// <summary>Toggles grid line visibility on the axes.</summary>
    public AxesBuilder ShowGrid(bool visible = true) { _axes.Grid = _axes.Grid with { Visible = visible }; return this; }

    /// <summary>Adds a line series to the axes.</summary>
    public AxesBuilder Plot(double[] x, double[] y, Action<LineSeries>? configure = null)
        => AddSeries(ax => ax.Plot(x, y), configure);

    /// <summary>Adds a scatter series to the axes.</summary>
    public AxesBuilder Scatter(double[] x, double[] y, Action<ScatterSeries>? configure = null)
        => AddSeries(ax => ax.Scatter(x, y), configure);

    /// <summary>Adds a bar series to the axes.</summary>
    public AxesBuilder Bar(string[] categories, double[] values, Action<BarSeries>? configure = null)
        => AddSeries(ax => ax.Bar(categories, values), configure);

    /// <summary>Adds a histogram series to the axes.</summary>
    public AxesBuilder Hist(double[] data, int bins = 10, Action<HistogramSeries>? configure = null)
        => AddSeries(ax => ax.Hist(data, bins), configure);

    /// <summary>Adds an ECDF series to the axes.</summary>
    public AxesBuilder Ecdf(double[] data, Action<EcdfSeries>? configure = null)
        => AddSeries(ax => ax.Ecdf(data), configure);

    /// <summary>Adds a stacked area (stackplot) series to the axes.</summary>
    public AxesBuilder StackPlot(double[] x, double[][] ySets, Action<StackedAreaSeries>? configure = null)
        => AddSeries(ax => ax.StackPlot(x, ySets), configure);

    /// <summary>Adds a pie series to the axes.</summary>
    public AxesBuilder Pie(double[] sizes, string[]? labels = null, Action<PieSeries>? configure = null)
        => AddSeries(ax => ax.Pie(sizes, labels), configure);

    /// <summary>Adds a heatmap series to the axes.</summary>
    public AxesBuilder Heatmap(double[,] data, Action<HeatmapSeries>? configure = null)
        => AddSeries(ax => ax.Heatmap(data), configure);

    /// <summary>Adds an image series to the axes.</summary>
    public AxesBuilder Image(double[,] data, Action<ImageSeries>? configure = null)
        => AddSeries(ax => ax.Image(data), configure);

    /// <summary>Adds a 2D histogram (density) series to the axes.</summary>
    public AxesBuilder Histogram2D(double[] x, double[] y, int bins = 20, Action<Histogram2DSeries>? configure = null)
        => AddSeries(ax => ax.Histogram2D(x, y, bins), configure);

    /// <summary>Adds a box plot series to the axes.</summary>
    public AxesBuilder BoxPlot(double[][] datasets, Action<BoxSeries>? configure = null)
        => AddSeries(ax => ax.BoxPlot(datasets), configure);

    /// <summary>Adds a violin series to the axes.</summary>
    public AxesBuilder Violin(double[][] datasets, Action<ViolinSeries>? configure = null)
        => AddSeries(ax => ax.Violin(datasets), configure);

    /// <summary>Adds a hexagonal binning series to the axes.</summary>
    public AxesBuilder Hexbin(double[] x, double[] y, Action<HexbinSeries>? configure = null)
        => AddSeries(ax => ax.Hexbin(x, y), configure);

    /// <summary>Adds a polynomial regression series to the axes.</summary>
    public AxesBuilder Regression(double[] x, double[] y, Action<RegressionSeries>? configure = null)
        => AddSeries(ax => ax.Regression(x, y), configure);

    /// <summary>Adds a kernel density estimation (KDE) series to the axes.</summary>
    public AxesBuilder Kde(double[] data, Action<KdeSeries>? configure = null)
        => AddSeries(ax => ax.Kde(data), configure);

    /// <summary>Adds a contour series to the axes.</summary>
    public AxesBuilder Contour(double[] x, double[] y, double[,] z, Action<ContourSeries>? configure = null)
        => AddSeries(ax => ax.Contour(x, y, z), configure);

    /// <summary>Adds a filled contour series to the axes.</summary>
    public AxesBuilder Contourf(double[] x, double[] y, double[,] z, Action<ContourfSeries>? configure = null)
        => AddSeries(ax => ax.Contourf(x, y, z), configure);

    /// <summary>Adds a stem series to the axes.</summary>
    public AxesBuilder Stem(double[] x, double[] y, Action<StemSeries>? configure = null)
        => AddSeries(ax => ax.Stem(x, y), configure);

    /// <summary>Adds a gauge (speedometer) chart to the axes.</summary>
    public AxesBuilder Gauge(double value, Action<GaugeSeries>? configure = null)
        => AddSeries(ax => ax.Gauge(value), configure);

    /// <summary>Adds a progress bar to the axes.</summary>
    public AxesBuilder ProgressBar(double value, Action<ProgressBarSeries>? configure = null)
        => AddSeries(ax => ax.ProgressBar(value), configure);

    /// <summary>Adds a sparkline to the axes.</summary>
    public AxesBuilder Sparkline(double[] values, Action<SparklineSeries>? configure = null)
        => AddSeries(ax => ax.Sparkline(values), configure);

    /// <summary>Adds a donut chart series to the axes.</summary>
    public AxesBuilder Donut(double[] sizes, string[]? labels = null, Action<DonutSeries>? configure = null)
        => AddSeries(ax => ax.Donut(sizes, labels), configure);

    /// <summary>Adds a bubble chart series to the axes.</summary>
    public AxesBuilder Bubble(double[] x, double[] y, double[] sizes, Action<BubbleSeries>? configure = null)
        => AddSeries(ax => ax.Bubble(x, y, sizes), configure);

    /// <summary>Adds a traditional OHLC bar chart series to the axes.</summary>
    public AxesBuilder OhlcBar(double[] open, double[] high, double[] low, double[] close, string[]? dateLabels = null, Action<OhlcBarSeries>? configure = null)
        => AddSeries(ax => ax.OhlcBar(open, high, low, close, dateLabels), configure);

    /// <summary>Adds a waterfall chart series to the axes.</summary>
    public AxesBuilder Waterfall(string[] categories, double[] values, Action<WaterfallSeries>? configure = null)
        => AddSeries(ax => ax.Waterfall(categories, values), configure);

    /// <summary>Adds a funnel chart series to the axes.</summary>
    public AxesBuilder Funnel(string[] labels, double[] values, Action<FunnelSeries>? configure = null)
        => AddSeries(ax => ax.Funnel(labels, values), configure);

    /// <summary>Adds a Gantt chart series to the axes.</summary>
    public AxesBuilder Gantt(string[] tasks, double[] starts, double[] ends, Action<GanttSeries>? configure = null)
        => AddSeries(ax => ax.Gantt(tasks, starts, ends), configure);

    /// <summary>Adds a treemap series to the axes.</summary>
    public AxesBuilder Treemap(TreeNode root, Action<TreemapSeries>? configure = null)
        => AddSeries(ax => ax.Treemap(root), configure);

    /// <summary>Adds a sunburst series to the axes.</summary>
    public AxesBuilder Sunburst(TreeNode root, Action<SunburstSeries>? configure = null)
        => AddSeries(ax => ax.Sunburst(root), configure);

    /// <summary>Adds a Sankey diagram series to the axes.</summary>
    public AxesBuilder Sankey(SankeyNode[] nodes, SankeyLink[] links, Action<SankeySeries>? configure = null)
        => AddSeries(ax => ax.Sankey(nodes, links), configure);

    /// <summary>Adds a polar line series to the axes.</summary>
    public AxesBuilder PolarPlot(double[] r, double[] theta, Action<PolarLineSeries>? configure = null)
        => AddSeries(ax => ax.PolarPlot(r, theta), configure);

    /// <summary>Adds a polar scatter series to the axes.</summary>
    public AxesBuilder PolarScatter(double[] r, double[] theta, Action<PolarScatterSeries>? configure = null)
        => AddSeries(ax => ax.PolarScatter(r, theta), configure);

    /// <summary>Adds a polar bar series to the axes.</summary>
    public AxesBuilder PolarBar(double[] r, double[] theta, Action<PolarBarSeries>? configure = null)
        => AddSeries(ax => ax.PolarBar(r, theta), configure);

    /// <summary>Adds a 3D surface series to the axes.</summary>
    public AxesBuilder Surface(double[] x, double[] y, double[,] z, Action<SurfaceSeries>? configure = null)
        => AddSeries(ax => ax.Surface(x, y, z), configure);

    /// <summary>Adds a 3D wireframe series to the axes.</summary>
    public AxesBuilder Wireframe(double[] x, double[] y, double[,] z, Action<WireframeSeries>? configure = null)
        => AddSeries(ax => ax.Wireframe(x, y, z), configure);

    /// <summary>Adds a 3D scatter series to the axes.</summary>
    public AxesBuilder Scatter3D(double[] x, double[] y, double[] z, Action<Scatter3DSeries>? configure = null)
        => AddSeries(ax => ax.Scatter3D(x, y, z), configure);

    /// <summary>Adds a 3D stem series to the axes.</summary>
    public AxesBuilder Stem3D(double[] x, double[] y, double[] z, Action<Stem3DSeries>? configure = null)
        => AddSeries(ax => ax.Stem3D(x, y, z), configure);

    /// <summary>Adds a 3D bar series to the axes.</summary>
    public AxesBuilder Bar3D(double[] x, double[] y, double[] z, Action<Bar3DSeries>? configure = null)
        => AddSeries(ax => ax.Bar3D(x, y, z), configure);

    /// <summary>Sets the 3D projection angles for ThreeD coordinate system axes.</summary>
    public AxesBuilder WithProjection(double elevation = 30, double azimuth = -60)
    {
        _axes.Projection = new Rendering.Projection3D(elevation, azimuth,
            default, 0, 1, 0, 1, 0, 1); // Placeholder bounds; actual bounds computed at render time
        return this;
    }

    /// <summary>Adds a radar (spider) chart series to the axes.</summary>
    public AxesBuilder Radar(string[] categories, double[] values, Action<RadarSeries>? configure = null)
        => AddSeries(ax => ax.Radar(categories, values), configure);

    /// <summary>Adds a quiver (vector field) series to the axes.</summary>
    public AxesBuilder Quiver(double[] x, double[] y, double[] u, double[] v, Action<QuiverSeries>? configure = null)
        => AddSeries(ax => ax.Quiver(x, y, u, v), configure);

    /// <summary>Adds a streamplot (vector field streamlines) series to the axes.</summary>
    public AxesBuilder Streamplot(double[] x, double[] y, double[,] u, double[,] v, Action<StreamplotSeries>? configure = null)
        => AddSeries(ax => ax.Streamplot(x, y, u, v), configure);

    /// <summary>Adds a candlestick (OHLC) series to the axes.</summary>
    public AxesBuilder Candlestick(double[] open, double[] high, double[] low, double[] close, string[]? dateLabels = null, Action<CandlestickSeries>? configure = null)
        => AddSeries(ax => ax.Candlestick(open, high, low, close, dateLabels), configure);

    /// <summary>Adds an error bar series to the axes.</summary>
    public AxesBuilder ErrorBar(double[] x, double[] y, double[] yErrorLow, double[] yErrorHigh, Action<ErrorBarSeries>? configure = null)
        => AddSeries(ax => ax.ErrorBar(x, y, yErrorLow, yErrorHigh), configure);

    /// <summary>Adds a step-function series to the axes.</summary>
    public AxesBuilder Step(double[] x, double[] y, Action<StepSeries>? configure = null)
        => AddSeries(ax => ax.Step(x, y), configure);

    /// <summary>Adds a filled area (fill-between) series to the axes.</summary>
    public AxesBuilder FillBetween(double[] x, double[] y, double[]? y2 = null, Action<AreaSeries>? configure = null)
        => AddSeries(ax => ax.FillBetween(x, y, y2), configure);

    // --- v0.8.0 builder methods ---

    /// <summary>Adds a rug plot series to the axes.</summary>
    public AxesBuilder Rugplot(double[] data, Action<RugplotSeries>? configure = null)
        => AddSeries(ax => ax.Rugplot(data), configure);

    /// <summary>Adds a strip plot series to the axes.</summary>
    public AxesBuilder Stripplot(double[][] datasets, Action<StripplotSeries>? configure = null)
        => AddSeries(ax => ax.Stripplot(datasets), configure);

    /// <summary>Adds an event plot series to the axes.</summary>
    public AxesBuilder Eventplot(double[][] positions, Action<EventplotSeries>? configure = null)
        => AddSeries(ax => ax.Eventplot(positions), configure);

    /// <summary>Adds a broken bar series to the axes.</summary>
    public AxesBuilder BrokenBarH((double Start, double Width)[][] ranges, Action<BrokenBarSeries>? configure = null)
        => AddSeries(ax => ax.BrokenBarH(ranges), configure);

    /// <summary>Adds a count plot series to the axes.</summary>
    public AxesBuilder Countplot(string[] values, Action<CountSeries>? configure = null)
        => AddSeries(ax => ax.Countplot(values), configure);

    /// <summary>Adds a pseudocolor mesh series to the axes.</summary>
    public AxesBuilder Pcolormesh(double[] x, double[] y, double[,] c, Action<PcolormeshSeries>? configure = null)
        => AddSeries(ax => ax.Pcolormesh(x, y, c), configure);

    /// <summary>Adds a residual plot series to the axes.</summary>
    public AxesBuilder Residplot(double[] x, double[] y, Action<ResidualSeries>? configure = null)
        => AddSeries(ax => ax.Residplot(x, y), configure);

    /// <summary>Adds a point plot series to the axes.</summary>
    public AxesBuilder Pointplot(double[][] datasets, Action<PointplotSeries>? configure = null)
        => AddSeries(ax => ax.Pointplot(datasets), configure);

    /// <summary>Adds a swarm plot series to the axes.</summary>
    public AxesBuilder Swarmplot(double[][] datasets, Action<SwarmplotSeries>? configure = null)
        => AddSeries(ax => ax.Swarmplot(datasets), configure);

    /// <summary>Adds a spectrogram series to the axes.</summary>
    public AxesBuilder Spectrogram(double[] signal, int sampleRate = 1, Action<SpectrogramSeries>? configure = null)
        => AddSeries(ax => ax.Spectrogram(signal, sampleRate), configure);

    /// <summary>Adds a table series to the axes.</summary>
    public AxesBuilder Table(string[][] cellData, Action<TableSeries>? configure = null)
        => AddSeries(ax => ax.Table(cellData), configure);

    /// <summary>Adds a contour series on an unstructured triangular mesh to the axes.</summary>
    public AxesBuilder Tricontour(double[] x, double[] y, double[] z, Action<TricontourSeries>? configure = null)
        => AddSeries(ax => ax.Tricontour(x, y, z), configure);

    /// <summary>Adds a pseudocolor series on a triangular mesh to the axes.</summary>
    public AxesBuilder Tripcolor(double[] x, double[] y, double[] z, Action<TripcolorSeries>? configure = null)
        => AddSeries(ax => ax.Tripcolor(x, y, z), configure);

    /// <summary>Adds a quiver key series to the axes.</summary>
    public AxesBuilder QuiverKey(double x, double y, double u, string label, Action<QuiverKeySeries>? configure = null)
        => AddSeries(ax => ax.QuiverKey(x, y, u, label), configure);

    /// <summary>Adds a wind barb series to the axes.</summary>
    public AxesBuilder Barbs(double[] x, double[] y, double[] speed, double[] direction, Action<BarbsSeries>? configure = null)
        => AddSeries(ax => ax.Barbs(x, y, speed, direction), configure);

    private AxesBuilder AddSeries<T>(Func<Axes, T> factory, Action<T>? configure) where T : ISeries
    {
        var series = factory(_axes);
        configure?.Invoke(series);
        return this;
    }

    // --- Intuitive indicator shortcuts (auto-resolve price data from axes) ---

    /// <summary>Adds a Simple Moving Average overlay. Auto-extracts price data from the last series on the axes.</summary>
    public AxesBuilder Sma(int period, Action<Indicators.Sma>? configure = null)
    {
        var indicator = new Indicators.Sma(GetPriceData(), period);
        configure?.Invoke(indicator);
        indicator.Apply(_axes);
        return this;
    }

    /// <summary>Adds an Exponential Moving Average overlay.</summary>
    public AxesBuilder Ema(int period, Action<Indicators.Ema>? configure = null)
    {
        var indicator = new Indicators.Ema(GetPriceData(), period);
        configure?.Invoke(indicator);
        indicator.Apply(_axes);
        return this;
    }

    /// <summary>Adds Bollinger Bands overlay.</summary>
    public AxesBuilder BollingerBands(int period = 20, double stdDev = 2.0, Action<Indicators.BollingerBands>? configure = null)
    {
        var indicator = new Indicators.BollingerBands(GetPriceData(), period, stdDev);
        configure?.Invoke(indicator);
        indicator.Apply(_axes);
        return this;
    }

    /// <summary>Adds an RSI panel indicator.</summary>
    public AxesBuilder Rsi(double[] prices, int period = 14, Action<Indicators.Rsi>? configure = null)
    {
        var indicator = new Indicators.Rsi(prices, period);
        configure?.Invoke(indicator);
        indicator.Apply(_axes);
        return this;
    }

    /// <summary>Adds a Williams %R panel indicator.</summary>
    public AxesBuilder WilliamsR(double[] high, double[] low, double[] close, int period = 14, Action<Indicators.WilliamsR>? configure = null)
    {
        var indicator = new Indicators.WilliamsR(high, low, close, period);
        configure?.Invoke(indicator);
        indicator.Apply(_axes);
        return this;
    }

    /// <summary>Adds an On-Balance Volume panel indicator.</summary>
    public AxesBuilder Obv(double[] close, double[] volume, Action<Indicators.Obv>? configure = null)
    {
        var indicator = new Indicators.Obv(close, volume);
        configure?.Invoke(indicator);
        indicator.Apply(_axes);
        return this;
    }

    /// <summary>Adds a Commodity Channel Index panel indicator.</summary>
    public AxesBuilder Cci(double[] high, double[] low, double[] close, int period = 20, Action<Indicators.Cci>? configure = null)
    {
        var indicator = new Indicators.Cci(high, low, close, period);
        configure?.Invoke(indicator);
        indicator.Apply(_axes);
        return this;
    }

    /// <summary>Adds a Parabolic SAR overlay to the price axes.</summary>
    public AxesBuilder ParabolicSar(double[] high, double[] low, double step = 0.02, double max = 0.2, Action<Indicators.ParabolicSar>? configure = null)
    {
        var indicator = new Indicators.ParabolicSar(high, low, step, max);
        configure?.Invoke(indicator);
        indicator.Apply(_axes);
        return this;
    }

    /// <summary>Adds a buy signal marker at the given position.</summary>
    public AxesBuilder BuyAt(double x, double y, Action<SignalMarker>? configure = null)
        => AddSignal(x, y, SignalDirection.Buy, configure);

    /// <summary>Adds a sell signal marker at the given position.</summary>
    public AxesBuilder SellAt(double x, double y, Action<SignalMarker>? configure = null)
        => AddSignal(x, y, SignalDirection.Sell, configure);

    /// <summary>Extracts Y data from the last series on the axes for indicator computation.</summary>
    private double[] GetPriceData()
    {
        var last = _axes.Series.LastOrDefault();
        return last is IPriceSeries price
            ? price.PriceData
            : throw new InvalidOperationException("No price data found. Add a series with Y data before calling indicator shortcuts.");
    }

    internal Axes Build(int rows, int cols, int index)
    {
        _axes.GridRows = rows;
        _axes.GridCols = cols;
        _axes.GridIndex = index;
        return _axes;
    }

    internal Axes Build(GridPosition position)
    {
        _axes.GridPosition = position;
        return _axes;
    }

    /// <summary>Adds an inset axes at the specified fractional position within this axes.</summary>
    public AxesBuilder AddInset(double x, double y, double width, double height, Action<AxesBuilder> configure)
    {
        var insetBuilder = new AxesBuilder();
        configure(insetBuilder);
        var inset = _axes.AddInset(x, y, width, height);
        // Copy series and configuration from the builder's axes to the inset
        foreach (var series in insetBuilder._axes.Series)
            inset.AddSeries(series);
        inset.Title = insetBuilder._axes.Title;
        inset.XAxis.Label = insetBuilder._axes.XAxis.Label;
        inset.YAxis.Label = insetBuilder._axes.YAxis.Label;
        inset.XAxis.Min = insetBuilder._axes.XAxis.Min;
        inset.XAxis.Max = insetBuilder._axes.XAxis.Max;
        inset.YAxis.Min = insetBuilder._axes.YAxis.Min;
        inset.YAxis.Max = insetBuilder._axes.YAxis.Max;
        inset.Spines = insetBuilder._axes.Spines;
        inset.Grid = insetBuilder._axes.Grid;
        return this;
    }
}

/// <summary>Fluent builder for configuring a secondary Y-axis and adding series that scale against it.</summary>
/// <remarks>Obtained via <see cref="AxesBuilder.WithSecondaryYAxis"/>. The secondary axis renders ticks and
/// labels on the right side of the plot and uses an independent Y-axis data range.</remarks>
public sealed class SecondaryAxisBuilder
{
    private readonly Axes _axes;

    internal SecondaryAxisBuilder(Axes axes) => _axes = axes;

    /// <summary>Sets the label displayed alongside the right-side Y-axis.</summary>
    public SecondaryAxisBuilder SetYLabel(string label) { _axes.SecondaryYAxis!.Label = label; return this; }

    /// <summary>Sets explicit min/max limits for the secondary Y-axis data range.</summary>
    public SecondaryAxisBuilder SetYLim(double min, double max) { _axes.SecondaryYAxis!.Min = min; _axes.SecondaryYAxis!.Max = max; return this; }

    /// <summary>Adds a line series plotted against the secondary Y-axis.</summary>
    public SecondaryAxisBuilder Plot(double[] x, double[] y, Action<LineSeries>? configure = null)
    {
        var series = _axes.PlotSecondary(x, y);
        configure?.Invoke(series);
        return this;
    }

    /// <summary>Adds a scatter series plotted against the secondary Y-axis.</summary>
    public SecondaryAxisBuilder Scatter(double[] x, double[] y, Action<ScatterSeries>? configure = null)
    {
        var series = _axes.ScatterSecondary(x, y);
        configure?.Invoke(series);
        return this;
    }
}
