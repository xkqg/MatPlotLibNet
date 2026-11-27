// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The major ticks of one axis as they stand on the plot: where each major mark and label, each grid line and the
/// minor ticks between them stand, and whether the plot holds them (<see cref="PlaceOnPlot"/>). The run carries the edge it lies on,
/// so it asks its placement along that edge itself and no caller tells it a direction.</summary>
/// <param name="Ticks">The major tick values, in the axis' own unit: the minor ticks are split between them in that unit.</param>
/// <param name="Placement">Where a tick of a given value stands along the edge, when the plot shows it: the axis' data-to-pixel
/// transform, or the placement of a linked axis (<see cref="LinkedTickPlacement"/>).</param>
/// <param name="Edge">The edge of the axis whose ticks these are: the run lies along X on a top or a bottom edge, along Y on a left or
/// a right one. Drawn mirrored on the opposite edge, the run still lies the same way.</param>
internal readonly record struct TickRun(double[] Ticks, ITickPlacement Placement, EdgeTerms Edge)
{
    /// <summary>Where a tick of <paramref name="value"/> stands along the run's edge, in pixels, when the plot holds it: the
    /// run's placement shows the value (<see cref="ITickPlacement.OnPlot"/>: it lies on the range the axis shows, its ends
    /// included, give or take a ten-billionth of its width, judged before any pixel, as matplotlib's
    /// <c>_interval_contains_close</c> keeps a tick in view) and puts it at a number (a log scale has no place at or below 0,
    /// a break none inside it). A major tick mark and its label, a major grid line, a minor grid line and a minor tick mark
    /// are drawn only where this says, so no mark or label stands outside the plot, a grid line never runs over the margins,
    /// the minor grid stands exactly on the minor ticks, and the tick at an end of a range stands there at any plot
    /// size.</summary>
    /// <param name="value">The tick's value, in the unit of the run's major ticks.</param>
    /// <returns>The pixel coordinate along the edge, or <see langword="null"/> when the plot does not hold it.</returns>
    internal double? PlaceOnPlot(double value) => Placement.OnPlot(value, Edge.AlongX);

    /// <summary>Whether the plot holds any of the run's ticks (<see cref="PlaceOnPlot"/>): whether any mark and label is
    /// drawn.</summary>
    /// <returns><see langword="true"/> when at least one tick stands on the plot.</returns>
    internal bool HoldsAny()
    {
        foreach (double tick in Ticks)
            if (PlaceOnPlot(tick) is not null) return true;
        return false;
    }

    /// <summary>The scale the run's ticks are drawn on (<see cref="ITickPlacement.ScaleAlong"/> along its edge): a linked axis'
    /// placement answers for the axis it is linked to.</summary>
    internal ScaleRule Scale => Placement.ScaleAlong(Edge.AlongX);

    /// <summary>The ends of the view the run's placement shows along its edge, in the unit of its ticks
    /// (<see cref="ITickPlacement.ViewAlong"/>); <see langword="null"/> when the placement shows none.</summary>
    internal Range1D? View => Placement.ViewAlong(Edge.AlongX);

    /// <summary>The minor ticks of the run when the scale its ticks are drawn on places them (the scale's <c>MinorLocator</c>,
    /// <see cref="Scale"/>): over the view the run's placement shows (<see cref="View"/>), in the unit of its ticks, the run's ticks as
    /// the majors left out. The one rule the minor tick marks and the minor grid lines read.</summary>
    /// <param name="tickSpace">How many labels fit along the axis (the axis' <c>TickSpaceAlong</c>).</param>
    /// <param name="theme">The theme the axis is drawn under.</param>
    /// <returns>The minor ticks, or <see langword="null"/> when they split the major step.</returns>
    internal double[]? ScaleMinors(double tickSpace, Theme theme) =>
        Scale.MinorLocator is { } locator && View is { } view ? locator.MinorsOver(view, Ticks, tickSpace, theme) : null;
}
