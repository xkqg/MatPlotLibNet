// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>The circle a round chart is drawn in: a pie, a donut, a dial, a radar web, a sunburst, a chord ring and a polar axes all
/// stand in the plot bounds as a centre and a radius that is a share of half the shorter side, less an inset. The one place that
/// rule is worked out (<see cref="In"/>); each chart names the share it takes, never the arithmetic.</summary>
/// <param name="Centre">The centre of the circle, in pixels.</param>
/// <param name="Radius">The radius of the outermost ring, in pixels.</param>
internal readonly record struct RoundFrame(Point Centre, double Radius)
{
    /// <summary>The share of half the shorter side a pie, a donut and a dial fill: a tenth of it is left round them for their labels.</summary>
    internal const double DiscShare = 0.8;

    /// <summary>The share of half the shorter side a radar web fills: its spoke labels stand outside it.</summary>
    internal const double WebShare = 0.75;

    /// <summary>The whole of half the shorter side: a chart that takes an inset instead of a share (a sunburst, a chord ring) and a
    /// polar axes, whose circle fills the square at the centre of its cell.</summary>
    internal const double FullShare = 1.0;

    /// <summary>How far down the plot bounds the centre of a dial stands, as a share of their height: a dial is a half circle, and
    /// its flat side sits low.</summary>
    internal const double DialCentreDown = 0.75;

    /// <summary>The circle a round chart is drawn in inside <paramref name="bounds"/>: centred across them (and
    /// <paramref name="down"/> of the way down), with a radius of <paramref name="share"/> of half the shorter side, less
    /// <paramref name="inset"/> pixels.</summary>
    /// <param name="bounds">The rectangle the chart is drawn in.</param>
    /// <param name="share">The share of half the shorter side the radius takes (<see cref="FullShare"/> by default).</param>
    /// <param name="inset">Pixels taken off the radius, where a chart leaves room for what stands outside its ring.</param>
    /// <param name="down">How far down the bounds the centre stands, as a share of their height (a half: the middle).</param>
    /// <returns>The frame.</returns>
    internal static RoundFrame In(Rect bounds, double share = FullShare, double inset = 0, double down = 0.5) =>
        new(new Point(bounds.Center.X, bounds.Y + bounds.Height * down), bounds.ShorterSide / 2 * share - inset);
}
