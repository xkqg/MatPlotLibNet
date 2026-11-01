// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Where the one tick drawing (<see cref="AxisTickExtensions"/>) stands a tick of a given value, and the plot its minor
/// ticks are kept inside. The value is what the label reads; the placement decides the pixel. A <see cref="DataTransform"/>
/// stands a tick at its own value, as every axis does; a <see cref="LinkedTickPlacement"/> stands the tick of a linked axis where
/// the primary axis shows the value it was mapped from, so a label and its position can differ.</summary>
internal interface ITickPlacement
{
    /// <summary>The pixel a tick stands at: the value along the edge, the coordinate across it as the caller gives it; only the
    /// coordinate along the edge is read. NaN (or an infinity) where the placement has no place for the value.</summary>
    /// <param name="x">The X value: the tick's value on a top or bottom edge, the coordinate across the edge on a left or right
    /// one.</param>
    /// <param name="y">The Y value: the tick's value on a left or right edge, the coordinate across the edge on a top or bottom
    /// one.</param>
    /// <returns>The pixel position.</returns>
    Point DataToPixel(double x, double y);

    /// <summary>The plot area in pixels: a minor tick that stands outside it is not drawn.</summary>
    Rect PlotBounds { get; }
}
