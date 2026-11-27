// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>What a raw 2-D array offers a chart: its rows and columns in another order, and the cells that stand out.</summary>
public class MatrixExtensionsTests
{
    private static readonly double[,] Matrix =
    {
        { 0, 2, double.NaN },
        { -3, 0, 1 },
        { double.PositiveInfinity, 0.5, 0 },
    };

    [Fact]
    public void TheCellsAboveZero_AreEveryCellThatIsNotZero_RowByRow()
    {
        var cells = Matrix.CellsAbove(0);

        // A missing value is above nothing; a negative value counts by its size; an infinity is a value like any other.
        Assert.Equal([1.0, 0.0, 2.0, 0.0, 1.0], cells.Columns);
        Assert.Equal([0.0, 1.0, 1.0, 2.0, 2.0], cells.Rows);
    }

    [Fact]
    public void ACellExactlyAtTheThreshold_IsNotAboveIt()
    {
        var cells = Matrix.CellsAbove(2);

        Assert.Equal([0.0, 0.0], cells.Columns);
        Assert.Equal([1.0, 2.0], cells.Rows);
    }

    [Fact]
    public void OnlyAnInfinity_IsAboveTheLargestNumber()
    {
        var cells = Matrix.CellsAbove(double.MaxValue);

        Assert.Equal([0.0], cells.Columns);
        Assert.Equal([2.0], cells.Rows);
        Assert.Empty(new double[,] { { 0, 0 } }.CellsAbove(0).Columns);
    }

    [Fact]
    public void Permuted_ReordersRowsAndColumns()
    {
        double[,] result = new double[,] { { 1, 2 }, { 3, 4 } }.Permute([1, 0], [1, 0]);

        Assert.Equal(new double[,] { { 4, 3 }, { 2, 1 } }, result);
    }

    [Fact]
    public void Transposed_TurnsRowsIntoColumns()
    {
        Assert.Equal(new double[,] { { 1, 4 }, { 2, 5 }, { 3, 6 } }, new double[,] { { 1, 2, 3 }, { 4, 5, 6 } }.Transposed());
        Assert.Equal(new double[0, 0], new double[0, 0].Transposed());
    }

    /// <summary>The distance between two rows is the Euclidean length of their difference; the matrix of them is symmetric with a
    /// zero diagonal.</summary>
    [Fact]
    public void RowDistances_AreTheEuclideanDistancesBetweenEveryPairOfRows()
    {
        var distances = new double[,] { { 0, 0 }, { 3, 4 }, { 6, 8 } }.RowDistances();

        Assert.Equal(new double[,] { { 0, 5, 10 }, { 5, 0, 5 }, { 10, 5, 0 } }, distances);
    }

    /// <summary>The distances between columns are the distances between the rows of the matrix turned on its side.</summary>
    [Fact]
    public void ColumnDistances_AreTheDistancesBetweenEveryPairOfColumns()
    {
        var distances = new double[,] { { 0, 3, 6 }, { 0, 4, 8 } }.ColumnDistances();

        Assert.Equal(new double[,] { { 0, 5, 10 }, { 5, 0, 5 }, { 10, 5, 0 } }, distances);
    }

    [Fact]
    public void AMatrixOfOneRow_HasOneDistance_Zero() =>
        Assert.Equal(new double[,] { { 0 } }, new double[,] { { 1, 2 } }.RowDistances());
}
