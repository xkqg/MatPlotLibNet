// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>Intermediate tier shared by every event that directly overwrites the X and Y axis
/// limits (<see cref="ZoomEvent"/>, <see cref="ResetEvent"/>). Factoring the mutation here
/// guarantees concrete subclasses cannot accidentally diverge from axis-range semantics —
/// <see cref="ApplyTo"/> is sealed.</summary>
public abstract record AxisRangeEvent(
    string ChartId,
    int AxesIndex,
    double XMin,
    double XMax,
    double YMin,
    double YMax) : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <summary>Every outward-stacked secondary Y axis' new range (ROADMAP.md row 100), already computed by the
    /// modifier that sent this event (<see cref="IChartLayout.GetSecondaryDataRanges"/> read through the SAME
    /// pixel rectangle this event zooms/resets the primary to, via <c>ChartLayoutViewExtensions.SecondaryViewsOf</c>).
    /// <see langword="init"/>-only so every existing construction site keeps working; absent/empty leaves every
    /// secondary axis where it was (today's behavior).</summary>
    public IReadOnlyList<SecondaryAxisRange>? SecondaryRanges { get; init; }

    /// <inheritdoc />
    public sealed override void ApplyTo(Figure figure)
    {
        var axes = TargetAxes(figure);
        foreach (var linked in XLinkedAxes(figure))
        {
            linked.XAxis.Min = XMin;
            linked.XAxis.Max = XMax;
        }

        // A timeline's bar fills the axes whatever the range, so there is no vertical position to move to.
        if (!axes.HasFixedYRange)
        {
            axes.YAxis.Min = YMin;
            axes.YAxis.Max = YMax;
        }

        AxisRangeEventExtensions.ApplySecondaryRanges(axes, SecondaryRanges);
    }
}
