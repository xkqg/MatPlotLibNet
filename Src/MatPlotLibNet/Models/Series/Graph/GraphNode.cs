// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>One node in a <see cref="NetworkGraphSeries"/>: a unique identifier,
/// optional pre-computed position (used when <see cref="GraphLayout.Manual"/> is selected),
/// optional per-node colour and size scalars, and optional display label.</summary>
/// <param name="Id">Unique identifier for the node. Used by <see cref="GraphEdge.From"/>
/// and <see cref="GraphEdge.To"/> to wire connections.</param>
/// <param name="X">Pre-computed X coordinate in data space. Read by
/// <see cref="GraphLayout.Manual"/>; ignored by every other layout.</param>
/// <param name="Y">Pre-computed Y coordinate in data space. Read by
/// <see cref="GraphLayout.Manual"/>; ignored by every other layout.</param>
/// <param name="ColorScalar">Per-node value mapped through the series' colour map (0..1
/// after normalisation by the renderer). Default <c>0.0</c>.</param>
/// <param name="SizeScalar">Per-node multiplier on
/// <see cref="NetworkGraphSeries.NodeRadiusScale"/> to derive the rendered radius in
/// pixels. Default <c>1.0</c>.</param>
/// <param name="Label">Optional display label rendered next to the node when
/// <see cref="NetworkGraphSeries.ShowNodeLabels"/> is true. Defaults to
/// <see langword="null"/>; the renderer falls back to <see cref="Id"/>.</param>
public readonly record struct GraphNode(
    string Id,
    double X = 0.0,
    double Y = 0.0,
    double ColorScalar = 0.0,
    double SizeScalar = 1.0,
    string? Label = null);
