// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Selects which triangle of the N×N pair-grid is rendered.</summary>
/// <remarks>Ordinals are explicit and append-only.</remarks>
public enum PairGridTriangle
{
    /// <summary>Render the full N×N grid. Default.</summary>
    Both = 0,

    /// <summary>Render only the lower triangle (rows ≥ columns); upper triangle cells are skipped.</summary>
    LowerOnly = 1,

    /// <summary>Render only the upper triangle (rows ≤ columns); lower triangle cells are skipped.</summary>
    UpperOnly = 2,
}
