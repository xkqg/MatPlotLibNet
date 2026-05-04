// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Extension methods on raw 2-D double arrays.</summary>
internal static class MatrixExtensions
{
    /// <summary>Returns a new 2-D array with rows and columns reordered by the given
    /// index permutations. Both arrays must be valid permutations of <c>[0, N-1]</c>;
    /// callers are responsible for validation before calling.</summary>
    internal static double[,] Permute(this double[,] src, int[] rowOrder, int[] colOrder)
    {
        int rows = rowOrder.Length, cols = colOrder.Length;
        var result = new double[rows, cols];
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
            result[r, c] = src[rowOrder[r], colOrder[c]];
        return result;
    }
}
