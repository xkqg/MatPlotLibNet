// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>State for drawing crosshair lines at the current mouse position.
/// Controls draw a vertical and horizontal line within the plot area.</summary>
/// <param name="PixelX">Mouse X pixel position.</param>
/// <param name="PixelY">Mouse Y pixel position.</param>
/// <param name="DataX">Data-space X coordinate.</param>
/// <param name="DataY">Data-space Y coordinate.</param>
/// <param name="AxesIndex">Index of the axes the mouse is over.</param>
/// <param name="PlotArea">Pixel-space plot area for clipping the crosshair lines.</param>
/// <param name="SnappedPoint">When non-null, the crosshair snaps to this nearest data point
/// and controls should draw a highlight marker + value callout.</param>
public readonly record struct CrosshairState(
    double PixelX, double PixelY,
    double DataX, double DataY,
    int AxesIndex, Rect PlotArea,
    NearestPointResult? SnappedPoint = null);
