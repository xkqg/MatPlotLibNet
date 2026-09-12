// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class for 3D grid series with X[], Y[], Z[,] data.</summary>
public abstract class GridSeries3D : ChartSeries, I3DGridSeries
{
    public double[] X { get; }

    public double[] Y { get; }

    public double[,] Z { get; }

    /// <summary>Initializes with X, Y, and Z data.</summary>
    protected GridSeries3D(double[] x, double[] y, double[,] z) { X = x; Y = y; Z = z; }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <summary>Converts the Z[rows, cols] grid to a <see cref="List{T}"/> of rows for JSON serialization.</summary>
    protected List<List<double>> ZToListList()
    {
        int rows = Z.GetLength(0), cols = Z.GetLength(1);
        var result = new List<List<double>>(rows);
        for (int r = 0; r < rows; r++)
        {
            var row = new List<double>(cols);
            for (int c = 0; c < cols; c++) row.Add(Z[r, c]);
            result.Add(row);
        }
        return result;
    }

    /// <inheritdoc />
    /// <remarks>Long form over the surface grid: the x and y a vertex sits at, and its height.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int rows = Z.GetLength(0), cols = Z.GetLength(1);
        var out_ = new List<IReadOnlyList<DataCell>>(rows * cols);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                out_.Add(
                [
                    DataCell.FromNumber(c < X.Length ? X[c] : c),
                    DataCell.FromNumber(r < Y.Length ? Y[r] : r),
                    DataCell.FromNumber(Z[r, c]),
                ]);
            }
        }

        return new ChartDataTable(null, [new("x", Axis: DataAxis.X), new("y", Axis: DataAxis.Y), new(Label ?? "z")], out_);
    }
}
