// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Pixel-space rectangle state during a Ctrl+drag zoom gesture.
/// Controls draw this as a dashed blue rectangle overlay.</summary>
/// <param name="StartPixelX">X pixel where the drag began.</param>
/// <param name="StartPixelY">Y pixel where the drag began.</param>
/// <param name="CurrentPixelX">Current mouse X pixel.</param>
/// <param name="CurrentPixelY">Current mouse Y pixel.</param>
/// <param name="AxesIndex">Index of the axes being zoomed.</param>
public readonly record struct RectangleZoomState(
    double StartPixelX, double StartPixelY,
    double CurrentPixelX, double CurrentPixelY,
    int AxesIndex);
