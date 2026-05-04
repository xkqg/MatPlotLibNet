// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Bivariate-distribution renderer chosen for the off-diagonal cells of a
/// <c>PairGridSeries</c>.</summary>
/// <remarks>Ordinals are explicit and append-only. <see cref="Hexbin"/> activated in v1.10
/// for high-cardinality EDA where scatter overplotting masks density.</remarks>
public enum PairGridOffDiagonalKind
{
    /// <summary>Render each off-diagonal cell as a scatter of (variable <c>i</c>, variable <c>j</c>).
    /// Default.</summary>
    Scatter = 0,

    /// <summary>Suppress the off-diagonal cells entirely (diagonal-only view).</summary>
    None = 1,

    /// <summary>Render each off-diagonal cell as a flat-top hexagonal density grid. The
    /// fill colour of each hex encodes the point count via
    /// <see cref="MatPlotLibNet.Models.Series.PairGridSeries.OffDiagonalColorMap"/>
    /// (defaults to Viridis). Recommended over <see cref="Scatter"/> when sample counts
    /// per cell exceed ~1000 — at that density scatter overplots and density structure
    /// is invisible. <b>Hue groups are ignored when this kind is selected:</b> a single
    /// aggregate density is rendered, mirroring seaborn's convention. Use
    /// <see cref="Scatter"/> + a hue palette if per-group separation is required.</summary>
    Hexbin = 2,
}
