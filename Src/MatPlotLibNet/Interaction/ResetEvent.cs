// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>A client-initiated reset that takes the target axes back to the view the reader started from. The figure
/// remembers that view the first time an interaction moves the axes (<c>Figure.Home</c>), so Home goes back to it
/// however many times the picture has been repainted or republished since: the numbers a browser reads off the latest
/// drawing, or a control reads off the latest layout, are the zoomed ones by then. They are used only when the figure
/// remembers nothing for the axes, which is when nothing has moved it. Kept as a distinct type for hub routing and
/// telemetry; it stacks on <see cref="AxisRangeEvent"/> with <see cref="ZoomEvent"/>.</summary>
public sealed record ResetEvent(
    string ChartId,
    int AxesIndex,
    double XMin,
    double XMax,
    double YMin,
    double YMax) : AxisRangeEvent(ChartId, AxesIndex, XMin, XMax, YMin, YMax)
{
    /// <inheritdoc />
    internal override bool RestoresHome => true;
}
