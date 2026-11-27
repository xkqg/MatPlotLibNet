// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Serialization;

/// <summary>Rectangular/jagged 2D array to and from a JSON list-of-lists, both directions. A <c>double[,]</c> or
/// <c>double[][]</c> (<see cref="Models.Series.PairGridSeries"/>'s per-variable samples) is not itself
/// JSON-serializable; every grid-shaped series (Heatmap, Contour(f), Image, Pcolormesh, Clustermap, Streamplot,
/// 3-D surface/wireframe, PairGrid) converts through here on the way in and out.</summary>
internal static class Grid2DSerializationExtensions
{
    extension(double[,] data)
    {
        /// <summary>Converts this rectangular 2D array to a jagged list-of-lists for JSON serialization.</summary>
        internal List<List<double>>? To2DList()
        {
            int rows = data.GetLength(0), cols = data.GetLength(1);
            var result = new List<List<double>>(rows);
            for (int r = 0; r < rows; r++)
            {
                var row = new List<double>(cols);
                for (int c = 0; c < cols; c++) row.Add(data[r, c]);
                result.Add(row);
            }
            return result;
        }
    }

    extension(List<List<double>>? data)
    {
        /// <summary>Converts this JSON jagged list-of-lists back to a rectangular 2D array; an empty array when
        /// <see langword="null"/> or empty.</summary>
        internal double[,] From2DList()
        {
            if (data is null || data.Count == 0)
            {
                return new double[0, 0];
            }
            int rows = data.Count, cols = data[0].Count;
            var result = new double[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    result[r, c] = data[r][c];
                }
            }
            return result;
        }

        /// <summary>Converts this JSON list-of-lists back to a jagged <c>double[][]</c>; an empty array when
        /// <see langword="null"/> or empty.</summary>
        internal double[][] FromJaggedList()
        {
            if (data is null || data.Count == 0)
            {
                return [];
            }
            var result = new double[data.Count][];
            for (int i = 0; i < data.Count; i++)
            {
                result[i] = [.. data[i]];
            }
            return result;
        }
    }

    extension(double[][] data)
    {
        /// <summary>Converts this jagged <c>double[][]</c> (variable-major) to a list-of-lists for JSON
        /// serialization. Used by <see cref="Models.Series.PairGridSeries"/> where each sub-array is one
        /// variable's samples. Returns an empty list for empty input — never <see langword="null"/>.</summary>
        internal List<List<double>> JaggedTo2DList()
        {
            var result = new List<List<double>>(data.Length);
            for (int i = 0; i < data.Length; i++) result.Add([.. data[i]]);
            return result;
        }
    }
}
