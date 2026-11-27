// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The cells of a matrix that stand out: the column and the row of each, in the order they are met.</summary>
/// <param name="Columns">The column of each cell.</param>
/// <param name="Rows">The row of each cell.</param>
internal readonly record struct MatrixCells(double[] Columns, double[] Rows);

/// <summary>Extension methods on raw 2-D double arrays.</summary>
internal static class MatrixExtensions
{
    extension(double[,] src)
    {
        /// <summary>The cells whose magnitude is above <paramref name="threshold"/>, row by row: a cell exactly at the threshold
        /// is not one, a negative value counts by its size, an infinity is a value like any other that is not zero, and a missing
        /// value (NaN) is above nothing.</summary>
        /// <param name="threshold">The magnitude a cell must exceed; 0 takes every cell that is not zero.</param>
        /// <returns>The column and the row of each cell found.</returns>
        internal MatrixCells CellsAbove(double threshold)
        {
            var columns = new List<double>();
            var rows = new List<double>();
            for (int r = 0; r < src.GetLength(0); r++)
            {
                for (int c = 0; c < src.GetLength(1); c++)
                {
                    // NaN is above nothing, so a missing value falls out here without a branch of its own.
                    if (Math.Abs(src[r, c]) > threshold)
                    {
                        columns.Add(c);
                        rows.Add(r);
                    }
                }
            }

            return new MatrixCells([.. columns], [.. rows]);
        }

        /// <summary>Returns a new 2-D array with rows and columns reordered by the given
        /// index permutations. Both arrays must be valid permutations of <c>[0, N-1]</c>;
        /// callers are responsible for validation before calling.</summary>
        internal double[,] Permute(int[] rowOrder, int[] colOrder)
        {
            int rows = rowOrder.Length, cols = colOrder.Length;
            var result = new double[rows, cols];
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                result[r, c] = src[rowOrder[r], colOrder[c]];
            return result;
        }

        /// <summary>The matrix turned on its side: row <c>r</c>, column <c>c</c> of the result is row <c>c</c>, column <c>r</c> of
        /// this one.</summary>
        internal double[,] Transposed()
        {
            int rows = src.GetLength(0), cols = src.GetLength(1);
            var result = new double[cols, rows];
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                result[c, r] = src[r, c];
            return result;
        }

        /// <summary>The Euclidean distance between every pair of rows, as a symmetric matrix with a zero diagonal: the distances a
        /// clustering of the rows reads.</summary>
        internal double[,] RowDistances()
        {
            int rows = src.GetLength(0), cols = src.GetLength(1);
            var distances = new double[rows, rows];
            for (int i = 0; i < rows; i++)
            for (int j = i + 1; j < rows; j++)
            {
                double sum = 0;
                for (int k = 0; k < cols; k++)
                {
                    double difference = src[i, k] - src[j, k];
                    sum += difference * difference;
                }

                double distance = Math.Sqrt(sum);
                distances[i, j] = distance;
                distances[j, i] = distance;
            }

            return distances;
        }

        /// <summary>The Euclidean distance between every pair of columns: the distances between the rows of the matrix turned on
        /// its side.</summary>
        internal double[,] ColumnDistances() => src.Transposed().RowDistances();
    }
}
