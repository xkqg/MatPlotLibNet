// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>The complete layout output of <see cref="IChartRenderer.ComputeLayout"/>: plot-area
/// rectangles for each subplot plus the per-subplot legend item bounds used for interactive
/// legend toggling.</summary>
/// <param name="PlotAreas">Pixel-space bounding rectangles for each subplot, in subplot order.</param>
/// <param name="LegendItems">Per-subplot list of legend item bounds. The outer list is parallel
/// to <paramref name="PlotAreas"/>; each inner list contains one entry per labelled series
/// whose legend swatch was rendered. Empty when a subplot has no visible legend.</param>
public sealed record LayoutResult(
    IReadOnlyList<Rect> PlotAreas,
    IReadOnlyList<IReadOnlyList<LegendItemBounds>> LegendItems);
