// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Cell-visibility mask for symmetric heatmaps such as correlation matrices.</summary>
/// <remarks>The mask predicate is purely geometric: it inspects only the row and column index, never
/// the data value. Use <see cref="UpperTriangleStrict"/> or <see cref="LowerTriangleStrict"/> to also
/// hide the diagonal — appropriate for correlation matrices where the diagonal is constant 1.</remarks>
public enum HeatmapMaskMode
{
    /// <summary>No masking; every cell is rendered.</summary>
    None = 0,

    /// <summary>Hide cells above the main diagonal (cells where <c>col &gt; row</c>); diagonal kept.</summary>
    UpperTriangle = 1,

    /// <summary>Hide cells below the main diagonal (cells where <c>col &lt; row</c>); diagonal kept.</summary>
    LowerTriangle = 2,

    /// <summary>Hide the upper triangle including the diagonal (cells where <c>col &gt;= row</c>).</summary>
    UpperTriangleStrict = 3,

    /// <summary>Hide the lower triangle including the diagonal (cells where <c>col &lt;= row</c>).</summary>
    LowerTriangleStrict = 4,
}

/// <summary>Geometric predicate extension for <see cref="HeatmapMaskMode"/>.</summary>
public static class HeatmapMaskModeExtensions
{
    /// <summary>Returns <see langword="true"/> when the cell at (<paramref name="row"/>, <paramref name="col"/>)
    /// should be hidden under this mask mode.</summary>
    public static bool Hides(this HeatmapMaskMode mode, int row, int col) => mode switch
    {
        HeatmapMaskMode.UpperTriangle        => col > row,
        HeatmapMaskMode.LowerTriangle        => col < row,
        HeatmapMaskMode.UpperTriangleStrict  => col >= row,
        HeatmapMaskMode.LowerTriangleStrict  => col <= row,
        _                                    => false,
    };
}
