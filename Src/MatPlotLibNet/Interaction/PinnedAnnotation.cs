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
    int AxesIndex)
{
    /// <summary>Position of the clicked point in the series' own arrays, or <c>-1</c> when the annotation was
    /// constructed without one.
    /// <para>The label is not an identifier: a series without one is named <c>"Series 0"</c> at the moment the
    /// hit is resolved, and two series can carry the same label. The index is what lets an application open the
    /// row the reader clicked instead of searching its data for a matching pair of doubles.</para></summary>
    public int PointIndex { get; init; } = -1;

    /// <summary>Position of the series within the axes, or <c>-1</c> when the annotation was constructed
    /// without one. Together with <see cref="PointIndex"/> it addresses exactly one sample.</summary>
    public int SeriesIndex { get; init; } = -1;
}
