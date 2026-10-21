// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>A client-initiated pan that translates the target axes by a delta in the space each axis is drawn in: data
/// units on a linear axis, decades (log10) on a log axis, symlog units on a symlog axis. A pan is a shift on screen, and
/// only in that space is a shift the same for both ends of the range; on a linear axis it is the plain data delta it
/// always was. Shape differs from <see cref="AxisRangeEvent"/> (delta vs absolute), so <see cref="PanEvent"/>
/// inherits directly from <see cref="FigureInteractionEvent"/> and supplies its own <see cref="ApplyTo"/>. An axis with
/// no limits and no <see cref="InView"/> is left alone: there is no range to translate.</summary>
public sealed record PanEvent(
    string ChartId,
    int AxesIndex,
    double DxData,
    double DyData) : FigureInteractionEvent(ChartId, AxesIndex)
{
    /// <summary>The range that was on screen when the pointer moved, in data units, or <c>null</c> when the sender did
    /// not say.
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
                Shift(linked.XAxis, xMin, xMax, DxData);
        }

        if (!axes.HasFixedYRange && (axes.YAxis.Min ?? InView?.YMin) is double yMin && (axes.YAxis.Max ?? InView?.YMax) is double yMax)
            Shift(axes.YAxis, yMin, yMax, DyData);
    }

    /// <summary>Sets <paramref name="axis"/>' limits to <paramref name="min"/>..<paramref name="max"/> moved by
    /// <paramref name="delta"/> in the space the axis is drawn in (<see cref="AxisScaleExtensions"/>).</summary>
    private static void Shift(Axis axis, double min, double max, double delta)
    {
        axis.Min = axis.Scale.Inverse(axis.Scale.Forward(min, axis.SymLogLinThresh) + delta, axis.SymLogLinThresh);
        axis.Max = axis.Scale.Inverse(axis.Scale.Forward(max, axis.SymLogLinThresh) + delta, axis.SymLogLinThresh);
    }
}
