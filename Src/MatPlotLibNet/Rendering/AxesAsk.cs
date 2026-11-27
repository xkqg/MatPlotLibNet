// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>What a series asks of the axes it stands on, and what the series of an axes ask together (<c>AxesRangeExtensions.AskUnder</c>,
/// one walk over them): the way its range ends, which way its Y axis runs, the aspect of its plot and how much it raises the margins.</summary>
/// <param name="Tight">True when a series asks for a tight range: both axes end where the data and the margin end, rounded out to no
/// tick under any theme.</param>
/// <param name="RowsDown">True when a series runs the Y axis down, its first row on top, as matplotlib's <c>imshow</c> does; the axes keeps
/// it only while its Y axis has no limit of its own.</param>
/// <param name="Aspect">The aspect a series asks of the plot (<see cref="Models.Axes.Aspect"/>), <see langword="null"/> when it asks none; of
/// the series of an axes, the first that asks one.</param>
/// <param name="MarginRaise">How much a series raises the margin of each axis above the one it is padded by (a rug's
/// <c>ax.margins(y=ymarg + 2 * height)</c>), as a fraction of the range; of the series of an axes, the sum. A margin given to the axis is
/// drawn as given.</param>
internal readonly record struct AxesAsk(bool Tight, bool RowsDown, double? Aspect = null, AxisPair<double> MarginRaise = default)
{
    /// <summary>What a map asks under every theme: its own limits, which no theme rounds out (cartopy's <c>set_global</c>), and a unit of Y
    /// as long as a unit of X (cartopy's <c>GeoAxes</c>: <c>set_aspect(1)</c>).</summary>
    internal static AxesAsk Map { get; } = new(Tight: true, RowsDown: false, Aspect: 1.0);

    /// <summary>What a series asks that calls matplotlib's autoscale tight, under <paramref name="theme"/>: a tight range under a theme
    /// that names an autolimit mode, the matplotlib themes (a line, a filled or a triangle contour: <c>ContourSet</c>'s
    /// <c>self.axes.autoscale_view(tight=True)</c>; a rug: seaborn's <c>ax.margins(...)</c>, whose <c>tight=True</c> default sets the
    /// axes' <c>_tight</c>); nothing under the library's own themes, which keep the bounds such a series was always drawn in.</summary>
    /// <param name="theme">The theme the axes is drawn under.</param>
    /// <returns>What the series asks of its axes.</returns>
    internal static AxesAsk TightUnder(Theme theme) => new(Tight: theme.Looks.Range.AutolimitRule.NamedByTheme, RowsDown: false);
}

/// <summary>A series that asks something of the axes it stands on (<see cref="AxesAsk"/>): a hexbin, a contour, an image, a map, a rug. It
/// says what itself; the range, the layout and the SVG writer read the axes' fold, never the kind of a series.</summary>
internal interface IAsksOfAxes
{
    /// <summary>What the series asks of its axes under <paramref name="theme"/>.</summary>
    /// <param name="theme">The theme the axes is drawn under.</param>
    /// <returns>The ask; <see langword="default"/> when it asks nothing under that theme.</returns>
    AxesAsk AskUnder(Theme theme);
}
