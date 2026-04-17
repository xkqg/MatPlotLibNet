// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Zooms the axes to the rectangle defined by the user's Ctrl+drag gesture.
/// Inherits sealed <see cref="AxisRangeEvent.ApplyTo"/> — axis limits are set directly.</summary>
public sealed record RectangleZoomEvent(
    string ChartId,
    int AxesIndex,
    double XMin,
    double XMax,
    double YMin,
    double YMax) : AxisRangeEvent(ChartId, AxesIndex, XMin, XMax, YMin, YMax);
