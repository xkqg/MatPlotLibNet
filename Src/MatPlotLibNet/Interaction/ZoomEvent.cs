// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>A client-initiated zoom that rewrites the target axes' X and Y limits to the
/// supplied values. Emitted by wheel scroll or pinch gestures in the browser.</summary>
public sealed record ZoomEvent(
    string ChartId,
    int AxesIndex,
    double XMin,
    double XMax,
    double YMin,
    double YMax) : AxisRangeEvent(ChartId, AxesIndex, XMin, XMax, YMin, YMax);
