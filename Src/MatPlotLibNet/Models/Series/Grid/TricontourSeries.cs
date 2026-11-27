// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a contour series on an unstructured triangular mesh. It backs a colour bar: one line across the bar at each
/// level, in the colour of its line.</summary>
public sealed class TricontourSeries : ChartSeries, IColormappable, INormalizable, IColorBarDataProvider, IThemedColorBar, IAsksOfAxes, ISeriesKind<TricontourSeries>
{
    /// <inheritdoc />
    /// <remarks>matplotlib's <c>tricontour</c> asks neither cycle. 2: <c>tricontour</c> draws its lines as a contour set at 2.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.LineCollection;

    /// <inheritdoc cref="AxesAsk.TightUnder"/>
    AxesAsk IAsksOfAxes.AskUnder(Theme theme) => AxesAsk.TightUnder(theme);

    /// <summary>The X coordinate of each scattered point, paired by index with <see cref="Y"/> and
    /// <see cref="Z"/>.</summary>
    public Vec X { get; }

    /// <summary>The Y coordinate of each scattered point, paired by index with <see cref="X"/> and
    /// <see cref="Z"/>.</summary>
    public Vec Y { get; }

    /// <summary>The value at each scattered point, paired by index with <see cref="X"/> and <see cref="Y"/>; the
    /// iso-lines trace levels of it.</summary>
    public Vec Z { get; }

    private const int RestingLevels = GridContourSeries.RestingLevels;
    /// <summary>How many iso-lines to aim for, as matplotlib's <c>tricontour(..., levels=N)</c>: the levels are round numbers that span
    /// the values of <see cref="Z"/>, about this many apart from the one at or below the lowest value to the one at or above the
    /// highest (10 over values from 0 to 1 gives 0 to 1 in steps of 0.1), the same levels a contour of a grid takes. A level the
    /// surface never reaches crosses no triangle and adds no line. Under a <see cref="LogNormalizer"/> the levels are powers of ten, as
    /// matplotlib's <c>tricontour(..., norm=LogNorm())</c> lays them. A value below 1 counts as 1. Default <see cref="RestingLevels"/>.</summary>
    public int Levels { get; set => Change(ref field, value); } = RestingLevels;

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set => Change(ref field, value); }

    /// <summary>Initializes a new instance of <see cref="TricontourSeries"/>.</summary>
    public TricontourSeries(Vec x, Vec y, Vec z)
    {
        X = x; Y = y; Z = z;
    }

    /// <summary>The levels the lines are drawn at under <paramref name="theme"/>: matplotlib's round levels for <see cref="Levels"/>
    /// over the values that can be drawn (<c>ContourLevels.Auto</c>, which reads the theme's autolimit mode as matplotlib's locator
    /// does, and lays decades under a <see cref="LogNormalizer"/> over the values above 0); <see langword="null"/> when the values
    /// have no two 1e-12 or more apart.</summary>
    /// <param name="theme">The theme of the figure.</param>
    /// <returns>The levels, ascending.</returns>
    internal double[]? LevelsUnder(Theme theme)
    {
        var levelScale = this.NormalizerOrLinear().Rule.LevelScale;
        return ContourLevels.Under(theme, null, Z.Data.DrawableRange(levelScale), Levels, levelScale);
    }

    /// <summary>The width the lines are drawn at under <paramref name="theme"/>: a collection of lines (<see cref="LineWidthRule.OnePixelCollection"/>),
    /// the theme's width where it names one: the one rule the lines and the colour bar's lines of the kind are drawn by.</summary>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <returns>The width, in pixels.</returns>
    internal double LineWidthUnder(Theme theme) => theme.Looks.Lines.WidthOf(null, LineWidthRule.OnePixelCollection);

    /// <summary>The colour of the line at <paramref name="level"/>: the colour <paramref name="mapping"/> gives it over the first level to
    /// the last. The one rule the lines on the plot and the lines of the colour
    /// bar take their colour by.</summary>
    /// <param name="level">The level of the line.</param>
    /// <param name="mapping">The map and the normaliser the series draws with under the figure's theme (<c>ColorMappingUnder</c>).</param>
    /// <param name="levels">Every level, ascending.</param>
    /// <returns>The colour.</returns>
    internal static Color LineColorOf(double level, ColorMapping mapping, double[] levels) =>
        mapping.Over(new(levels[0], levels[^1])).ColorOf(level);

    /// <inheritdoc />
    /// <remarks>The first level to the last under a figure's default theme; the values when there are no two levels.</remarks>
    public MinMaxRange GetColorBarRange() => ((IThemedColorBar)this).ColorBarRangeUnder(Theme.MatplotlibV2);

    /// <inheritdoc />
    MinMaxRange IThemedColorBar.ColorBarRangeUnder(Theme theme) => LevelsUnder(theme).BarRangeOver(Z.Data.DrawableRange());

    /// <inheritdoc />
    INormalizer? IThemedColorBar.ColorBarNormalizerUnder(Theme theme) => LevelsUnder(theme).BarNormalizerUnder(theme, this.ColorMappingUnder(theme).Normalizer);

    /// <inheritdoc />
    /// <remarks>A line at every level, in the colour and width of its line on the plot.</remarks>
    ColorBarLines? IThemedColorBar.ColorBarLinesUnder(Theme theme)
    {
        double[] levels = LevelsUnder(theme) ?? [];
        var mapping = this.ColorMappingUnder(theme);
        var colors = new Color[levels.Length];
        for (int i = 0; i < levels.Length; i++) colors[i] = LineColorOf(levels[i], mapping, levels);
        return new ColorBarLines(levels, colors, LineWidthUnder(theme));
    }

    /// <inheritdoc />
    internal override bool HoldsNoData => X.Length == 0;

    /// <inheritdoc />
    /// <remarks>The surface fills the extent of its points, so the axes end there, with no margin past them: sticky edges on all four
    /// sides, as a grid contour's (matplotlib's <c>ContourSet</c> makes the extent of the triangulation its sticky edges).</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => PinnedOverItsPoints(X.Data, Y.Data);

    static string ISeriesKind<TricontourSeries>.Kind => "tricontour";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        ZData = Z,
        Levels = Levels,
        ColorScale = this.ColorScaleOnTheWire(),
    };

    /// <summary>The reader of a <see cref="TricontourSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static TricontourSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Tricontour(dto.XData ?? [], dto.YData ?? [], dto.ZData ?? []);
        if (dto.Levels is { } levels) s.Levels = levels;
        s.RestoreColorScale(dto);
        return s;
    }

    /// <inheritdoc />
    /// <remarks>The unstructured points the triangulation is built over.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.XYZ(X.Data, Y.Data, Z.Data, Label ?? "z");

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
