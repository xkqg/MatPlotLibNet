// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Paired anchor point and text alignment used when placing an outer label (text
/// drawn beside a rectangle rather than inside it). Returned by
/// <see cref="SankeySeriesRenderer.ComputeNodeLabelAnchor"/>.</summary>
/// <param name="Anchor">Pixel position at which the label is anchored.</param>
/// <param name="Alignment">Which side of the anchor the text should flow toward.</param>
internal readonly record struct LabelAnchor(Point Anchor, TextAlignment Alignment);
