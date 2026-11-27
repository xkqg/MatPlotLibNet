// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Where one variable of a pair grid begins and how far it reaches: the lowest finite sample and the distance to the
/// highest.</summary>
/// <param name="Min">The lowest finite sample.</param>
/// <param name="Span">The distance from the lowest to the highest finite sample; never zero.</param>
internal readonly record struct AxisSpan(double Min, double Span)
{
    /// <summary>NaN-safe span of the first <paramref name="count"/> samples of <paramref name="data"/>, with a guard for a
    /// range that has no width. An axis with no finite samples falls back to <c>(0, 1)</c>.</summary>
    /// <param name="data">The samples of one variable.</param>
    /// <param name="count">How many of them to read.</param>
    internal static AxisSpan Of(double[] data, int count)
    {
        if ((count == data.Length ? data : data[..count]).DrawableRange() is not { } range) return new AxisSpan(0, 1);
        double top = range.Hi == range.Lo ? range.Lo + 1 : range.Hi;
        return new AxisSpan(range.Lo, top - range.Lo);
    }
}

/// <summary>The frame one cell of a pair grid draws in: the span of its X variable and of its Y variable, mapped onto the cell's
/// rectangle. Shared by every off-diagonal painter, so painters take no back-reference to <see cref="PairGridSeriesRenderer"/>.</summary>
/// <param name="X">The span of the variable along the cell's X direction.</param>
/// <param name="Y">The span of the variable along the cell's Y direction.</param>
/// <param name="Cell">The pixel rectangle of the cell.</param>
internal readonly record struct PairGridCellFrame(AxisSpan X, AxisSpan Y, Rect Cell)
{
    /// <summary>Maps a (data-x, data-y) pair into pixel coordinates within the cell; Y grows upward in data and downward in
    /// pixels.</summary>
    internal Point Map(double x, double y)
    {
        double px = Cell.X      + (x - X.Min) / X.Span * Cell.Width;
        double py = Cell.Bottom - (y - Y.Min) / Y.Span * Cell.Height;
        return new Point(px, py);
    }
}

/// <summary>What an off-diagonal painter draws in one cell: the series, the samples of the two variables, the frame, the colour
/// of a plain sample and, when the grid is split by hue, the colour of each group.</summary>
/// <param name="Series">The pair grid.</param>
/// <param name="XData">The samples of the variable along the cell's X direction.</param>
/// <param name="YData">The samples of the variable along the cell's Y direction.</param>
/// <param name="Count">How many samples of each are read.</param>
/// <param name="Frame">The frame the samples are mapped into.</param>
/// <param name="BaseColor">The colour of a sample when the grid is not split by hue.</param>
/// <param name="HueColors">The colour of each hue group, or <see langword="null"/> when the grid is not split by hue.</param>
/// <param name="Density">How a cell that colours by density turns its counts into colours: the series' off-diagonal map, else the
/// theme's, linearly (<see cref="ColorMapping.Linear"/>), under the theme that says how a range of no width is opened.</param>
/// <param name="DotRadius">The radius, in pixels, of a scatter dot under the theme the grid is drawn in
/// (<c>IDotSeries.DiameterUnder</c>, halved).</param>
internal readonly record struct PairGridCell(
    PairGridSeries Series, double[] XData, double[] YData, int Count,
    PairGridCellFrame Frame, Color BaseColor, Dictionary<int, Color>? HueColors, ColorMapping Density,
    double DotRadius)
{
    /// <summary>Whether the samples are coloured by their hue group.</summary>
    internal bool HueActive => HueColors is not null;
}
