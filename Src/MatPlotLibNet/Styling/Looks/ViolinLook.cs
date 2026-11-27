// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>One line a violin draws over its body (the bar between its extrema, its median, its mean) where its author names
/// nothing: its colour, the rule of its width and its dash.</summary>
/// <param name="Color">The colour of the line; <see langword="null"/> draws it in the violin's stats colour
/// (<see cref="Rendering.SeriesSlot.StatsColorOf"/>).</param>
/// <param name="Width">The rule its width is drawn by (<see cref="LineWidthRule"/>).</param>
/// <param name="Style">The dash of the line.</param>
internal readonly record struct ViolinStatsLine(Color? Color, LineWidthRule Width, LineStyle Style)
{
    /// <summary>A line of the stats, as matplotlib draws every one of a violin's (<c>violinplot</c>'s <c>hlines</c> and <c>vlines</c>,
    /// a <c>LineCollection</c>): solid, in the stats colour, as wide as <c>lines.linewidth</c> where the theme names it and a line's
    /// resting width where it does not.</summary>
    internal static ViolinStatsLine Stats { get; } = new(null, LineWidthRule.LineCollection, LineStyle.Solid);
}

/// <summary>The lines a violin draws over its body where its author names nothing. The matplotlib themes draw each as a line of the
/// stats (<see cref="Matplotlib"/>); the library's own themes mark the median and the mean in a look of their own
/// (<see cref="Library"/>).</summary>
internal sealed record ViolinLook : IThemeKeys<ViolinLook>
{
    /// <inheritdoc />
    /// <remarks>The lines are named as a whole: matplotlib's (<see cref="Matplotlib"/>) or the library's.</remarks>
    public void WriteKeys(ThemeDto keys) => keys.ViolinsLikeMatplotlib = (this == Matplotlib).OrNullAtRest(false);

    /// <inheritdoc />
    public static ViolinLook ReadKeys(ThemeDto keys) => keys.ViolinsLikeMatplotlib == true ? Matplotlib : Library;

    /// <summary>The library's own violins: the extrema a line of the stats, the median a white line 2 pixels wide, the mean a green
    /// dashed line at a line's resting width, each own width drawn whatever width the theme names.</summary>
    internal static ViolinLook Library { get; } = new();

    /// <summary>The violins of matplotlib 3.11.2: every line a line of the stats (<see cref="ViolinStatsLine.Stats"/>);
    /// <c>violinplot(showmeans=True, showmedians=True)</c> draws <c>cmeans</c> and <c>cmedians</c> solid, in the stats colour, at
    /// <c>lines.linewidth</c> (1.5 pt under the default style, 1 pt under the classic one). A violin given no position stands at
    /// <c>range(1, N + 1)</c> (<see cref="DatasetStanding.Matplotlib"/>), the axis covers the bodies and keeps no room past them, a body is
    /// as wide as its width at its widest (<c>Axes.violin</c>: <c>vals = 0.5 * width * vals / vals.max()</c> to either side), and every
    /// line over a body ends at <c>Axes.violin</c>'s <c>line_ends</c>, a quarter of the width to either side.</summary>
    internal static ViolinLook Matplotlib { get; } = new()
    {
        Median = ViolinStatsLine.Stats, Mean = ViolinStatsLine.Stats, Datasets = DatasetStanding.Matplotlib, RoomPastTheViolins = null,
        BodyReach = 0.5, ExtremaReach = LineEnds, StatsReach = LineEnds,
    };

    /// <summary>matplotlib's <c>Axes.violin</c>: its <c>line_ends</c> of -0.25 and 0.25, the share of a violin's width its extrema, median and
    /// mean lines reach to either side of its position.</summary>
    private const double LineEnds = 0.25;

    /// <summary>The bar from the lowest to the highest observation and its two ticks.</summary>
    public ViolinStatsLine Extrema { get; init; } = ViolinStatsLine.Stats;

    /// <summary>The line across the violin at its median.</summary>
    public ViolinStatsLine Median { get; init; } = new(Colors.White, LineWidthRule.TwoPixelOwn, LineStyle.Solid);

    /// <summary>The line across the violin at its mean.</summary>
    public ViolinStatsLine Mean { get; init; } = new(Colors.Green, LineWidthRule.Own, LineStyle.Dashed);

    /// <summary>Where the violins stand when their author names no position (<see cref="DatasetStanding"/>, the rule a box reads too).</summary>
    public DatasetStanding Datasets { get; init; } = DatasetStanding.Library;

    /// <summary>The room the axis along the positions keeps past the outer violins when their author names no position, in units of the
    /// positions; <see langword="null"/> keeps none, so the axis covers what the violins draw.</summary>
    public double? RoomPastTheViolins { get; init; } = 1;

    /// <summary>How far a body reaches to either side of its position at its widest, as a share of its width.</summary>
    public double BodyReach { get; init; } = 1;

    /// <summary>How far the ticks at the extrema reach to either side of a violin's position, as a share of its width.</summary>
    public double ExtremaReach { get; init; } = 0.3;

    /// <summary>How far the median and the mean reach to either side of a violin's position, as a share of its width.</summary>
    public double StatsReach { get; init; } = 0.5;
}
