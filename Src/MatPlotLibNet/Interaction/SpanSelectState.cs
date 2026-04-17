// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>Pixel-space state during an Alt+drag span selection.
/// Controls draw a full-height shaded vertical band within the plot area.</summary>
/// <param name="StartPixelX">X pixel where the drag began.</param>
/// <param name="CurrentPixelX">Current mouse X pixel.</param>
/// <param name="AxesIndex">Index of the axes being selected.</param>
/// <param name="PlotArea">Pixel-space plot area for clipping the band.</param>
public readonly record struct SpanSelectState(
    double StartPixelX, double CurrentPixelX,
    int AxesIndex, Rect PlotArea);
