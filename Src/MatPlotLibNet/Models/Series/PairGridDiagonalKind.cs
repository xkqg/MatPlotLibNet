// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Univariate-distribution renderer chosen for the diagonal cells of a
/// <c>PairGridSeries</c>.</summary>
/// <remarks>Ordinals are explicit and append-only: a future addition (e.g.
/// <c>Boxplot</c>) MUST take a fresh ordinal &gt; <see cref="None"/> to keep the
/// JSON-by-name round-trip stable across versions.</remarks>
public enum PairGridDiagonalKind
{
    /// <summary>Render each diagonal cell as a histogram of variable <c>i</c>.
    /// Default — cheapest, deterministic, no smoothing parameters.</summary>
    Histogram = 0,

    /// <summary>Render each diagonal cell as a kernel density estimate.</summary>
    Kde = 1,

    /// <summary>Suppress the diagonal cells entirely (off-diagonal-only view).</summary>
    None = 2,
}
