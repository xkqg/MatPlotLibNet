// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a contour plot series displaying iso-lines or filled regions for 3D surface data. It backs a colour bar:
/// one line across the bar at each level, in the colour of its line, as matplotlib's bar of a contour; a contour that fills its
/// bands has the bar of a filled contour.</summary>
public sealed class ContourSeries : GridContourSeries, ILabelable, ISeriesKind<ContourSeries>, IGivenTextSize, ILabelFormatted
{
    /// <summary>The format a level label is written in when none is given: four significant digits.</summary>
    internal const string DefaultLabelFormat = "G4";

    /// <inheritdoc />
    string ILabelFormatted.DefaultLabelFormat => DefaultLabelFormat;

    /// <inheritdoc />
    /// <remarks>matplotlib's <c>contour</c> asks neither cycle. A line contour's set stands at 2 and a filled one's at 1, as <c>contour</c> and <c>contourf</c> give their <c>QuadContourSet</c>.</remarks>
    internal override MatplotlibArtist Artist => Filled ? MatplotlibArtist.Mesh : MatplotlibArtist.LineCollection;

    /// <summary>When <see langword="true"/> the bands between neighbouring levels are filled with the colour map as
    /// well; the iso-lines are drawn either way. Default <see langword="false"/>.</summary>
    public bool Filled { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public bool ShowLabels { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public string? LabelFormat { get; set => Change(ref field, value); }

    /// <summary>Font size of the level labels, in pixels (the unit of <see cref="Font.Size"/>). Default <see cref="RestingLabelFontSize"/>. Given no size, the labels
    /// are the size of the theme's text under the matplotlib themes, as matplotlib's <c>clabel</c> takes <c>font.size</c> (<c>TextLook.SeriesTextAtFontSize</c>), and <see cref="RestingLabelFontSize"/> under every other theme; a size that is given, the default included, is drawn as given.</summary>
    public double LabelFontSize { get => GivenLabelFontSize ?? RestingLabelFontSize; set => GivenLabelFontSize = value; }

    /// <summary>The label size a new series holds under a theme that names none.</summary>
    private const double RestingLabelFontSize = 10;

    /// <summary>The label size that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenLabelFontSize { get; private set => Change(ref field, value); }

    double? IGivenTextSize.GivenTextSize => GivenLabelFontSize;

    double IGivenTextSize.RestingTextSize => RestingLabelFontSize;

    /// <summary>Initializes a new instance of <see cref="ContourSeries"/> with the specified grid data.</summary>
    /// <param name="xData">The X-axis grid coordinates.</param>
    /// <param name="yData">The Y-axis grid coordinates.</param>
    /// <param name="zData">The 2D matrix of Z values at each grid point.</param>
    public ContourSeries(double[] xData, double[] yData, double[,] zData) : base(xData, yData, zData)
    {
    }

    /// <summary>The values the colours of the lines run over: the first level to the last, whether the series laid them or was given
    /// them (matplotlib's <c>_process_colors</c>: <c>norm.autoscale_None(levels)</c>), so a line on the edge of a band takes a colour
    /// between those of the bands and a level the grid never reaches still bounds the span. One level has no span, and matplotlib's
    /// norm then returns 0 (<c>vmin == vmax</c>), the map's first colour, which a span of 1 gives; no level at all leaves the values.</summary>
    /// <param name="levels">The levels, ascending.</param>
    /// <param name="values">The range of the values that can be drawn.</param>
    /// <returns>The span.</returns>
    internal static MinMaxRange LineColourSpan(double[] levels, Range1D values) =>
        levels.Length == 0 ? new(values.Lo, values.Hi) : levels[^1] > levels[0] ? new(levels[0], levels[^1]) : new(levels[0], levels[0] + 1);

    /// <summary>The colour of the line at <paramref name="level"/> under <paramref name="theme"/>: the colour the map gives the level
    /// over <paramref name="span"/> when the series names a map, or, naming none, under the matplotlib themes, which colour a contour
    /// through their own map as matplotlib does; the theme's neutral grey under the library's own themes. The one rule the lines on
    /// the plot and the lines of the colour bar take their colour by.</summary>
    /// <param name="level">The level of the line.</param>
    /// <param name="theme">The theme of the figure.</param>
    /// <param name="span">The values the colours run over (<see cref="LineColourSpan"/>).</param>
    /// <returns>The colour.</returns>
    internal Color LineColorOf(double level, Theme theme, MinMaxRange span) =>
        ColorMap is not null || theme.Looks.Patch.LikeCollections
            ? this.LinearMappingUnder(theme).Over(span).ColorOf(level)
            : theme.Neutral;

    /// <summary>The width the lines are drawn at under <paramref name="theme"/>: a collection of lines (<see cref="LineWidthRule.LineCollection"/>),
    /// the theme's width where it names one: the one rule the lines and the colour bar's lines of the kind are drawn by.</summary>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <returns>The width, in pixels.</returns>
    internal double LineWidthUnder(Theme theme) => theme.Looks.Lines.WidthOf(null, LineWidthRule.LineCollection);

    /// <summary>A line at every level, the ones the grid never reaches included (matplotlib's <c>add_lines</c> is given
    /// <c>cs.levels</c>), in the colour and width of its line on the plot. A contour that fills its bands (<see cref="Filled"/>) has the
    /// filled bar of a <see cref="ContourfSeries"/>: none.</summary>
    /// <param name="theme">The theme of the figure.</param>
    /// <returns>The lines, or <see langword="null"/> when the bands are filled.</returns>
    private protected override ColorBarLines? BarLinesUnder(Theme theme)
    {
        if (Filled) return null;
        double[] levels = LevelsUnder(theme) ?? [];
        var colors = new Color[levels.Length];
        if (levels.Length > 0)
        {
            // The span of levels that are there never reads the values.
            var span = LineColourSpan(levels, new Range1D(levels[0], levels[^1]));
            for (int i = 0; i < levels.Length; i++) colors[i] = LineColorOf(levels[i], theme, span);
        }

        return new ColorBarLines(levels, colors, LineWidthUnder(theme));
    }

    static string ISeriesKind<ContourSeries>.Kind => "contour";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => GridOnTheWire() with
    {
        Type = this.WireKind(),
        Filled = Filled.OrNullAtRest(false),
        ShowLabels = ShowLabels.OrNullAtRest(false),
        LabelFormat = LabelFormat,
        FontSize = GivenLabelFontSize,
        ColorScale = this.ColorScaleOnTheWire(),
    };

    /// <summary>The reader of a <see cref="ContourSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static ContourSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Contour(dto.XData ?? [], dto.YData ?? [], dto.HeatmapData.From2DList());
        s.RestoreGrid(dto);
        if (dto.Filled is { } filled) s.Filled = filled;
        if (dto.ShowLabels is { } showLabels) s.ShowLabels = showLabels;
        s.LabelFormat = dto.LabelFormat;
        if (dto.FontSize is { } fontSize) s.LabelFontSize = fontSize;
        s.RestoreColorScale(dto);
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
