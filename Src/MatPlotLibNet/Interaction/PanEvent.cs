// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>A client-initiated pan that translates the target axes by a delta in data space.
/// Shape differs from <see cref="AxisRangeEvent"/> (delta vs absolute), so <see cref="PanEvent"/>
/// inherits directly from <see cref="FigureInteractionEvent"/> and supplies its own
/// <see cref="ApplyTo"/>. Null axis limits (unset / auto-ranged) result in a no-op: we cannot
/// translate a range that has not been materialised yet.</summary>
public sealed record PanEvent(
    string ChartId,
    int AxesIndex,
    double DxData,
    double DyData) : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <summary>The range that was on screen when the pointer moved, or <c>null</c> when the sender did not say.
    /// <para>An axes that scales itself has no limits to shift, so a pan applied to one used to do nothing until
    /// the reader had zoomed once. The pointer's side knows what is on screen; carrying it here lets the pan pin
    /// that range and move it, whether the event is applied in the control or on a server across SignalR. An
    /// <c>init</c> member so every existing construction site keeps working, and a limit the axes already has
    /// still wins over it.</para></summary>
    public DataRange? InView { get; init; }

    /// <inheritdoc />
    public override void ApplyTo(Figure figure)
    {
        var axes = TargetAxes(figure);

        foreach (var linked in XLinkedAxes(figure))
        {
            if ((linked.XAxis.Min ?? InView?.XMin) is double xMin && (linked.XAxis.Max ?? InView?.XMax) is double xMax)
            {
                linked.XAxis.Min = xMin + DxData;
                linked.XAxis.Max = xMax + DxData;
            }
        }

        if (!axes.HasFixedYRange && (axes.YAxis.Min ?? InView?.YMin) is double yMin && (axes.YAxis.Max ?? InView?.YMax) is double yMax)
        {
            axes.YAxis.Min = yMin + DyData;
            axes.YAxis.Max = yMax + DyData;
        }
    }
}
