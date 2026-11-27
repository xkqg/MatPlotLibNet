// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Fluent builder for constructing a <see cref="Models.Figure"/> with a chainable API. The class is the figure itself: its
/// canvas and document (size, resolution, theme, background, the texts that describe it), its subplots and its builds. Every other
/// verb is an extension member in the class of its concern, in the namespace of the builder, so a call reads the same as a method of
/// the class: the layout (<see cref="FigureBuilderLayoutVerbs"/>), what is drawn outside the subplots
/// (<see cref="FigureBuilderDecorationVerbs"/>), what a viewer can do in a browser (<see cref="FigureBuilderInteractionVerbs"/>), the
/// export (<see cref="FigureBuilderExportVerbs"/>), and the verbs that draw on the default axes (<c>Plt.Create().Plot(x, y)</c>,
/// <c>AxHLine</c>, <c>WithCamera</c>, ...: <see cref="FigureBuilderDefaultAxesVerbs"/>), each the <see cref="AxesBuilder"/> verb of
/// the same name, taken on the default axes at every <see cref="Build"/>: the two builders draw a series the same way because there
/// is one way written.</summary>
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
    /// <summary>The theme asked for; <see langword="null"/> lets <see cref="Build"/> dress the figure in the one the style
    /// parameters describe (<c>RcParams.ThemeOver</c>), which is read only when no theme was asked for.</summary>
    private Theme? _theme;
    private readonly List<Action<AxesBuilder>> _defaultAxesSteps = [];
    private readonly List<SubPlotSpec> _subPlots = [];
    private readonly List<Action<Figure>> _figureSteps = [];

    /// <summary>The spacing asked for; kept here because the layout verbs edit the spacing the calls before them left
    /// (<see cref="FigureBuilderLayoutVerbs"/>).</summary>
    internal SubPlotSpacing? Spacing { get; set; }

    /// <summary>The figure's colour bar asked for; kept here because a second call edits the bar the first one made
    /// (<see cref="FigureBuilderDecorationVerbs"/>).</summary>
    internal ColorBar? FigureColorBar { get; set; }

    /// <summary>Sets the short alternative text rendered as the SVG <c>&lt;title&gt;</c> element.</summary>
    public FigureBuilder WithAltText(string altText) => OnFigure(f => f.AltText = altText);

    /// <summary>Sets the longer description rendered as the SVG <c>&lt;desc&gt;</c> element.</summary>
    public FigureBuilder WithDescription(string description) => OnFigure(f => f.Description = description);

    /// <summary>Sets the figure dimensions in pixels.</summary>
    public FigureBuilder WithSize(double width, double height) => OnFigure(f => { f.Width = width; f.Height = height; });

    /// <summary>Sizes the figure canvas to fit a treemap of <paramref name="root"/>'s leaves at the treemap's resting label font
    /// (<see cref="TreemapSeries.RestingFontSize"/>) without overflow.
    /// Most useful for static SVG output where the user can't pan/zoom — the canvas grows
    /// with leaf count and average label length so labels fit cleanly. With
    /// <see cref="FigureBuilderInteractionVerbs.extension(FigureBuilder).WithBrowserInteraction(bool)"/> enabled this is optional (the user can
    /// pan/zoom to read overflowing labels) but still gives a more-readable initial view.
    /// Floors at 800×600. 16:9 aspect ratio.</summary>
    public FigureBuilder WithAutoSize(TreeNode root)
    {
        // The size at which every leaf has room for its label at the resting font, from the leaf count and the average length of
        // the labels of all the nodes, worked out in one walk of the tree: a character is about 7 px wide, a leaf gets 8 px of
        // padding each side and 22 px of height, the parent header strips add half again, the shape is 16:9.
        int leaves = 0, characters = 0, nodes = 0;
        Visit(root);
        double averageChars = (double)characters / nodes;
        double leafArea = (averageChars * 7.0 + 16.0) * 22.0;
        double canvasArea = leaves * leafArea * 1.5;
        const double aspect = 16.0 / 9.0;
        int width = (int)Math.Sqrt(canvasArea * aspect);
        int height = (int)(width / aspect);
        return WithSize(Math.Max(AutoSizeMinWidth, width), Math.Max(AutoSizeMinHeight, height));

        void Visit(TreeNode node)
        {
            nodes++;
            characters += node.Label?.Length ?? 0;
            if (node.Children.Count == 0)
            {
                leaves++;
                return;
            }

            foreach (var child in node.Children)
            {
                Visit(child);
            }
        }
    }

    /// <summary>The narrowest canvas <see cref="WithAutoSize"/> gives a treemap.</summary>
    private const int AutoSizeMinWidth = 800;

    /// <summary>The shortest canvas <see cref="WithAutoSize"/> gives a treemap.</summary>
    private const int AutoSizeMinHeight = 600;

    /// <summary>Sets the resolution of the figure in dots per inch (<see cref="Figure.Dpi"/>). A raster
    /// export, a PNG or the frames of an animated GIF, is drawn with <c>dpi / 96</c> pixels for each unit of
    /// <see cref="WithSize"/>: <c>WithSize(500, 350).WithDpi(192)</c> saves as a 1000 by 700 PNG with the same layout.
    /// An SVG, a PDF page and the JSON have no pixels, so for them it changes nothing; the JSON keeps it with the
    /// figure. It is refused when a PNG is exported if it is not a positive, finite number.</summary>
    public FigureBuilder WithDpi(double dpi) => OnFigure(f => f.Dpi = dpi);

    /// <summary>Sets the theme used for styling the figure.</summary>
    public FigureBuilder WithTheme(Theme theme) { _theme = theme; return this; }

    /// <summary>Sets the figure background color.</summary>
    public FigureBuilder WithBackground(Color color) => OnFigure(f => f.BackgroundColor = color);

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
    /// <param name="key">Optional string key for referencing this axes in sharing (<c>ShareX</c>/<c>ShareY</c>).</param>
    public FigureBuilder AddSubPlot(GridPosition position, Action<AxesBuilder> configure, string? key = null)
    {
        _subPlots.Add(new SubPlotSpec(position, configure) { Key = key });
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
        // The figure's own defaults stand unless a call asked for something else: the builder restates none of them.
        var figure = new Figure { Theme = _theme ?? RcParams.Current.ThemeOver(Theme.MatplotlibV2) };
        if (Spacing is not null) figure.Spacing = Spacing;
        if (FigureColorBar is not null) figure.FigureColorBar = FigureColorBar;

        // Only add the default axes when it carries actual series OR when no subplots were
        // defined. FigureBuilder-level helpers (WithCamera, WithLighting, …) touch the default
        // axes as a convenience side-effect, leaving an empty Cartesian axes in the model that
        // would otherwise be rendered as ghost 2-D ticks on top of 3-D plots.
        if (_defaultAxesSteps.Count > 0)
        {
            var defaultAxes = new Axes();
            var defaultAxesBuilder = new AxesBuilder(defaultAxes);
            foreach (var step in _defaultAxesSteps) step(defaultAxesBuilder);
            if (defaultAxes.Series.Count > 0 || _subPlots.Count == 0)
                figure.AddAxes(defaultAxes);
        }

        foreach (var spec in _subPlots)
        {
            var builder = new AxesBuilder();
            spec.Configure(builder);

            Axes axes;
            if (spec.Position is { } pos)
                axes = builder.Build(pos);
            else
                axes = builder.Build(spec.Rows, spec.Cols, spec.Index);

            if (spec.Key is not null) axes.Key = spec.Key;
            figure.AddAxes(axes);
        }

        // A subplot that asked to share a scale is joined to the subplot of its key once every subplot stands.
        figure.ShareByKeys();

        // The figure-level steps, in the order they were asked for, once the subplots stand (an inset is placed in one). Each
        // build takes them again, so it gets texts, images and insets of its own: two figures built from one builder share
        // nothing either can change.
        foreach (var step in _figureSteps) step(figure);

        return figure;
    }

    /// <summary>Records what a figure-level call asks of the figure: a setting, a text, an image, an inset. The steps are taken
    /// on each new figure at every <see cref="Build"/>, in the order of the calls, once the subplots are added, so the figure
    /// keeps its own defaults for whatever no call asked for, and every build gets texts, images and insets of its own.</summary>
    /// <param name="step">What the call does to the figure.</param>
    /// <returns>This builder, for chaining.</returns>
    internal FigureBuilder OnFigure(Action<Figure> step)
    {
        _figureSteps.Add(step);
        return this;
    }

    /// <summary>Records a step that a figure-level verb takes on the default axes: the subplot verb of the same name, on the
    /// <see cref="AxesBuilder"/> of the default axes, so what each verb does is written once, in the subplot's builder. The
    /// steps are taken again on a new axes at every <see cref="Build"/>, as a subplot's configuration is, so two figures built
    /// from one builder never share an axes (nor a series on it) that one of them could change.</summary>
    /// <param name="step">The subplot verb to take.</param>
    /// <returns>This builder, for chaining.</returns>
    internal FigureBuilder OnDefaultAxes(Action<AxesBuilder> step)
    {
        _defaultAxesSteps.Add(step);
        return this;
    }

    /// <summary>Describes a subplot position and configuration within a figure grid.</summary>
    private readonly record struct SubPlotSpec
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
}
