// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>A data point annotation pinned by a click via the data cursor modifier.
/// Controls render this as a callout box at the pixel position.</summary>
/// <param name="SeriesLabel">Label of the series the point belongs to.</param>
/// <param name="DataX">Data-space X coordinate of the pinned point.</param>
/// <param name="DataY">Data-space Y coordinate of the pinned point.</param>
/// <param name="PixelX">Pixel-space X position for the callout.</param>
/// <param name="PixelY">Pixel-space Y position for the callout.</param>
/// <param name="AxesIndex">Index of the axes containing the pinned point.</param>
public readonly record struct PinnedAnnotation(
    string? SeriesLabel,
    double DataX, double DataY,
    double PixelX, double PixelY,
    int AxesIndex);
