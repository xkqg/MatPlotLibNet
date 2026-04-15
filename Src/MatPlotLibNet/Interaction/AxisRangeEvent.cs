// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

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
    /// <inheritdoc />
    public sealed override void ApplyTo(Figure figure)
    {
        var axes = TargetAxes(figure);
        axes.XAxis.Min = XMin;
        axes.XAxis.Max = XMax;
        axes.YAxis.Min = YMin;
        axes.YAxis.Max = YMax;
    }
}
