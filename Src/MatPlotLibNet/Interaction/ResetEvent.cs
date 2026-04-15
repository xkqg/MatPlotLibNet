// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>A client-initiated reset that restores the target axes to its original limits
/// (captured at render time via <c>data-reset-*</c> attributes). Same mutation semantics as
/// <see cref="ZoomEvent"/> — both stack on <see cref="AxisRangeEvent"/> — but kept as a distinct
/// type for hub routing and telemetry.</summary>
public sealed record ResetEvent(
    string ChartId,
    int AxesIndex,
    double XMin,
    double XMax,
    double YMin,
    double YMax) : AxisRangeEvent(ChartId, AxesIndex, XMin, XMax, YMin, YMax);
