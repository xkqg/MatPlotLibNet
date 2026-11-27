// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>A contour of the values of a grid, as matplotlib's <c>QuadContourSet</c> is the one class behind <c>contour</c> and
/// <c>contourf</c>: the grid, the levels it is cut at, the colour map the levels are coloured by, the range it fills and the colour
/// bar it backs, whether it draws the iso-lines (<see cref="ContourSeries"/>) or fills the bands between them
/// (<see cref="ContourfSeries"/>). Only the library derives from it.</summary>
public abstract class GridContourSeries : ChartSeries, IColormappable, IColorBarDataProvider, IThemedColorBar, IAsksOfAxes, IEdgedPatch
{
    // The bands of a filled contour have no edge of their own and no edge under any theme (matplotlib draws them with none): the series
    // says nothing of an edge (the defaults of IEdgedPatch) and is a patch-like kind only so that its fill and its hatch are styled by
    // the one rule.

    /// <summary>The count of levels a new contour aims for.</summary>
    internal const int RestingLevels = 10;

    /// <summary>Holds the grid.</summary>
    /// <param name="xData">The X coordinate of each grid column.</param>
    /// <param name="yData">The Y coordinate of each grid row.</param>
    /// <param name="zData">The value at each grid point, indexed [row, column].</param>
    private protected GridContourSeries(double[] xData, double[] yData, double[,] zData)
    {
        XData = xData;
        YData = yData;
        ZData = zData;
    }

    /// <inheritdoc cref="AxesAsk.TightUnder"/>
    AxesAsk IAsksOfAxes.AskUnder(Theme theme) => AxesAsk.TightUnder(theme);

    /// <summary>The X coordinate of each grid column, one per column of <see cref="ZData"/>.</summary>
    public double[] XData { get; }

    /// <summary>The Y coordinate of each grid row, one per row of <see cref="ZData"/>.</summary>
    public double[] YData { get; }

    /// <summary>The value of the surface at each grid point, indexed [row, column] with rows along Y; the iso-lines trace levels of
    /// these values and the bands are cut from them.</summary>
    public double[,] ZData { get; }

    /// <summary>How many levels to aim for when <see cref="LevelValues"/> names none, as matplotlib's <c>contour(..., levels=N)</c> and
    /// <c>contourf(..., levels=N)</c>: the levels are round numbers that span the grid's values, about this many apart from the one at or
    /// below the lowest value to the one at or above the highest (10 over values from -0.685 to 1.056 gives -0.8 to 1.2 in steps of 0.2),
    /// the same for lines and bands, so a contour laid over a filled one draws its lines on the edges of its bands. Under a
    /// <see cref="LogNormalizer"/> (a filled contour's <see cref="ContourfSeries.Normalizer"/>) the levels are powers of ten, as
    /// matplotlib's <c>contourf(..., norm=LogNorm())</c> lays them (<c>LogLocator</c>): about this many apart from the one at or below the
    /// lowest value above 0 to the one at or above the highest. A level the grid never reaches crosses no cell and adds no line. A value
    /// below 1 counts as 1. Default <see cref="RestingLevels"/>.</summary>
    public int Levels { get; set => Change(ref field, value); } = RestingLevels;

    /// <summary>The levels, when you want them at values of your own choosing. Left <see langword="null"/> or empty,
    /// <see cref="Levels"/> decides. Once it holds values, those are the levels and <see cref="Levels"/> is ignored: they are taken in
    /// ascending order, each once, and a value that is not a number or is infinite is left out. A line stands on each level and each
    /// pair of neighbouring levels bounds one band; a level outside the range of the grid crosses no cell, so it adds no line. The
    /// colours follow the levels: the colour map runs from the first level to the last, a level the grid never reaches included, as in
    /// matplotlib, and so does the colour bar, whatever range the grid itself spans. The lowest band is painted under the whole plot, so a
    /// value below the first level shows that band's colour, and a value above the last level shows the highest band's. When none of the
    /// values can be drawn, nothing is drawn; fewer than two leave no band.</summary>
    public double[]? LevelValues { get; set => Change(ref field, value); }

    /// <inheritdoc />
    /// <remarks>The bands take the colour the map gives their level, and so does a line. Left <see langword="null"/>, the matplotlib
    /// themes colour through their own map (viridis, and jet under the classic theme), as matplotlib does; under the library's own
    /// themes the bands take the theme's map and the lines are drawn in the theme's neutral grey.</remarks>
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <summary>The scale the levels are laid on: <see langword="null"/>, a linear scale, unless the series draws through a normaliser of
    /// its own.</summary>
    private protected virtual ScaleRule? LevelScale => null;

    /// <summary>The levels under <paramref name="theme"/>: the ones <see cref="LevelValues"/> names, else matplotlib's round levels for
    /// <see cref="Levels"/> over the values that can be drawn (<c>ContourLevels.Auto</c>, which reads the theme's autolimit mode as
    /// matplotlib's locator does, and lays decades over the values above 0 on a logarithmic <see cref="LevelScale"/>);
    /// <see langword="null"/> when no level was named and the grid has no two values 1e-12 or more apart. The one rule the lines, the
    /// bands, their colours and the colour bar of every contour of a grid read.</summary>
    /// <param name="theme">The theme of the figure.</param>
    /// <returns>The levels, ascending.</returns>
    internal double[]? LevelsUnder(Theme theme)
    {
        var levelScale = LevelScale;
        return ContourLevels.Under(theme, LevelValues, ZData.DrawableRange(levelScale), Levels, levelScale);
    }

    /// <inheritdoc />
    /// <remarks>From the first level to the last under a figure's default theme, the span the colours run over (the classic theme can
    /// take a larger step: the bar of a figure drawn under it follows that); the values of the grid when it has no two levels.</remarks>
    public MinMaxRange GetColorBarRange() => ((IThemedColorBar)this).ColorBarRangeUnder(Theme.MatplotlibV2);

    /// <inheritdoc />
    /// <remarks>The one rule of a contour's bar (<c>ContourLevels.BarRangeOver</c>): the first level to the last, else the values that
    /// can be drawn.</remarks>
    MinMaxRange IThemedColorBar.ColorBarRangeUnder(Theme theme) => LevelsUnder(theme).BarRangeOver(ZData.DrawableRange());

    /// <inheritdoc />
    /// <remarks>Under the themes that draw a colour bar as matplotlib's <c>Colorbar</c> does (<see cref="ColorBarLook.StepPerColour"/>)
    /// a band between each two levels, the levels evenly spaced and named, as matplotlib's bar of a <c>ContourSet</c>
    /// (<c>ContourLevels.BarNormalizerUnder</c>), lines or bands alike; under the library's own it is a gradient through
    /// <see cref="BarNormalizerUnder"/>.</remarks>
    INormalizer? IThemedColorBar.ColorBarNormalizerUnder(Theme theme) => LevelsUnder(theme).BarNormalizerUnder(theme, BarNormalizerUnder(theme));

    /// <summary>The normaliser the bar is drawn through where it is not cut at the levels: linear, unless the series draws through a
    /// normaliser of its own.</summary>
    /// <param name="theme">The theme of the figure.</param>
    /// <returns>The normaliser.</returns>
    private protected virtual INormalizer BarNormalizerUnder(Theme theme) => LinearNormalizer.Instance;

    /// <inheritdoc />
    /// <remarks>None here: the bar of a contour that fills its bands is filled; a contour of lines draws a line at every level.</remarks>
    ColorBarLines? IThemedColorBar.ColorBarLinesUnder(Theme theme) => BarLinesUnder(theme);

    /// <summary>The lines across the colour bar under <paramref name="theme"/>: none, unless the series draws lines.</summary>
    /// <param name="theme">The theme of the figure.</param>
    /// <returns>The lines, or <see langword="null"/>.</returns>
    private protected virtual ColorBarLines? BarLinesUnder(Theme theme) => null;

    /// <inheritdoc />
    /// <remarks>Over the drawable grid coordinates only (a missing one, NaN, and an infinity are left out), pinned:
    /// the grid fills its rectangle, so no margin pushes whitespace between the grid's edge
    /// and the spines. A grid with no column or no row adds nothing.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        XData.Length == 0 || YData.Length == 0 ? default : DataRangeContribution.PinnedOver(XData, YData);

    /// <summary>The fields every contour of a grid writes: the grid and the levels, the count left out at rest. Each kind adds its own
    /// name, its colour scale and the fields of its own.</summary>
    /// <returns>The fields.</returns>
    private protected SeriesDto GridOnTheWire() => new()
    {
        XData = XData,
        YData = YData,
        HeatmapData = ZData.To2DList(),
        Levels = Levels.OrNullAtRest(RestingLevels),
        LevelValues = LevelValues,
    };

    /// <summary>Reads back what <see cref="GridOnTheWire"/> wrote: the levels, a count only when one was written.</summary>
    /// <param name="dto">The serialization DTO.</param>
    private protected void RestoreGrid(SeriesDto dto)
    {
        if (dto.Levels is { } levels) Levels = levels;
        LevelValues = dto.LevelValues;
    }

    /// <inheritdoc />
    /// <remarks>Long form over the grid: the x and y the cell sits at (the index of the column or the row where the grid names no
    /// coordinate), and its z.</remarks>
    public override ChartDataTable? ToDataTable() => ChartDataTable.OverGrid(XData, YData, new GridValues(Label ?? "z", ZData));
}
