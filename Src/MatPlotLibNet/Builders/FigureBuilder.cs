// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Transforms;

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

    /// <summary>Adds a bar series to the default axes.</summary>
    public FigureBuilder Bar(string[] categories, double[] values, Action<BarSeries>? configure = null) =>
        AddSeries(ax => ax.Bar(categories, values), configure);

    /// <summary>Adds a histogram series to the default axes.</summary>
    public FigureBuilder Hist(double[] data, int bins = 10, Action<HistogramSeries>? configure = null) =>
        AddSeries(ax => ax.Hist(data, bins), configure);

    /// <summary>Adds a pie series to the default axes.</summary>
    public FigureBuilder Pie(double[] sizes, string[]? labels = null, Action<PieSeries>? configure = null) =>
        AddSeries(ax => ax.Pie(sizes, labels), configure);

    /// <summary>Enables interactive zoom and pan via JavaScript in SVG output.</summary>
    public FigureBuilder WithZoomPan(bool enabled = true) { _enableZoomPan = enabled; return this; }
    private bool _enableZoomPan;

    /// <summary>Enables tight layout, which computes minimal margins automatically.</summary>
    public FigureBuilder TightLayout() { _spacing = _spacing with { TightLayout = true }; return this; }

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

    /// <summary>Adds a subplot at the specified grid position, configured via an <see cref="AxesBuilder"/>.</summary>
    /// <param name="rows">Number of rows in the subplot grid.</param>
    /// <param name="cols">Number of columns in the subplot grid.</param>
    /// <param name="index">One-based index of this subplot within the grid.</param>
    /// <param name="configure">Action to configure the subplot axes.</param>
    public FigureBuilder AddSubPlot(int rows, int cols, int index, Action<AxesBuilder> configure)
    {
        _subPlots.Add(new SubPlotSpec(rows, cols, index, configure));
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
            Spacing = _spacing
        };

        if (_defaultAxes is not null)
            figure.AddAxes(_defaultAxes);

        foreach (var spec in _subPlots)
        {
            var builder = new AxesBuilder();
            spec.Configure(builder);
            var axes = builder.Build(spec.Rows, spec.Cols, spec.Index);
            figure.AddAxes(axes);
        }

        return figure;
    }

    // --- Output methods (auto-build, no explicit Build() needed) ---

    /// <summary>Builds the figure and renders it as an SVG string.</summary>
    public string ToSvg() => Build().ToSvg();

    /// <summary>Builds the figure and serializes it to JSON.</summary>
    public string ToJson(bool indented = false) => Build().ToJson(indented);

    /// <summary>Builds the figure and saves it as an SVG file.</summary>
    public void SaveSvg(string path) => Build().SaveSvg(path);

    /// <summary>Builds the figure and binds it to a transform for fluent output (ToFile, ToStream, ToBytes).</summary>
    public TransformResult Transform(IFigureTransform transform) => Build().Transform(transform);

    /// <summary>Builds the figure and saves it to a file. Format is auto-detected from extension (.svg, .png, .pdf, .json).</summary>
    /// <remarks>PNG and PDF require the MatPlotLibNet.Skia package. Register custom transforms via <see cref="RegisterTransform"/>.</remarks>
    public void Save(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext))
        {
            SaveSvg(path + ".svg");
            return;
        }
        if (ext == ".json") { File.WriteAllText(path, ToJson()); return; }
        if (_transforms.TryGetValue(ext, out var transform))
        {
            Build().Transform(transform).ToFile(path);
            return;
        }
        SaveSvg(path);
    }

    /// <summary>Registers a transform for a file extension (e.g., ".png" for PngTransform).</summary>
    public FigureBuilder RegisterTransform(string extension, IFigureTransform transform)
    {
        _transforms[extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}"] = transform;
        return this;
    }

    private readonly Dictionary<string, IFigureTransform> _transforms = new(GlobalTransforms);

    private static readonly Dictionary<string, IFigureTransform> GlobalTransforms = new()
    {
        [".svg"] = new SvgTransform()
    };

    /// <summary>Registers a transform globally for all future builders (e.g., call once at startup for PNG/PDF).</summary>
    public static void RegisterGlobalTransform(string extension, IFigureTransform transform)
    {
        GlobalTransforms[extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}"] = transform;
    }

    private FigureBuilder AddSeries<T>(Func<Axes, T> factory, Action<T>? configure) where T : ISeries
    {
        var series = factory(EnsureDefaultAxes());
        configure?.Invoke(series);
        return this;
    }

    private Axes EnsureDefaultAxes() => _defaultAxes ??= new Axes();
}

/// <summary>Describes a subplot position and configuration within a figure grid.</summary>
/// <param name="Rows">Number of rows in the subplot grid.</param>
/// <param name="Cols">Number of columns in the subplot grid.</param>
/// <param name="Index">One-based index of this subplot within the grid.</param>
/// <param name="Configure">Action to configure the subplot axes.</param>
internal readonly record struct SubPlotSpec(int Rows, int Cols, int Index, Action<AxesBuilder> Configure);
