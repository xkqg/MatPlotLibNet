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
    /// <summary>The new range of every Y axis stacked past the primary, already computed by the
    /// modifier that sent this event (<see cref="IChartLayout.GetSecondaryDataRanges"/> read through the SAME
    /// pixel rectangle this event zooms/resets the primary to, via <c>ChartLayoutViewExtensions.SecondaryViewsOf</c>).
    /// <see langword="init"/>-only so every existing construction site keeps working; absent/empty leaves every
    /// such axis where it was (today's behavior).</summary>
    public IReadOnlyList<SecondaryAxisRange>? SecondaryRanges { get; init; }

    /// <summary>Whether this event takes the axes back to the view the reader started from (<see cref="ResetEvent"/>)
    /// instead of to the range it carries. The numbers it carries are then only what is used when the figure remembers
    /// no first view for the axes, because nothing has moved it.</summary>
    internal virtual bool RestoresHome => false;

    /// <inheritdoc />
    internal override bool ChangesView => true;

    /// <inheritdoc />
    public sealed override void ApplyTo(Figure figure) => TryApplyTo(figure);

    /// <inheritdoc />
    internal sealed override bool TryApplyTo(Figure figure)
    {
        if (!TryTargetMovableAxes(figure, out var axes)) return false;

        var linkedAxes = XLinkedAxes(figure);
        if (RestoresHome)
        {
            // Every axes the sender linked to this one goes home with it, each to the view it started from.
            bool wentHome = false;
            foreach (var linked in linkedAxes)
            {
                wentHome |= figure.Home.GoHome(linked);
            }

            if (wentHome) return true;
        }
        else
        {
            foreach (var linked in linkedAxes)
            {
                figure.Home.Remember(linked);
            }
        }

        foreach (var linked in linkedAxes)
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

        axes.ApplySecondaryRanges(SecondaryRanges);
        return true;
    }
}
