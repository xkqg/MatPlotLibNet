// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The hexagonal grid of <c>hexbin</c> is matplotlib's: <c>nx</c> hexagons across the extent of the data and
/// <c>ny = int(nx / sqrt(3))</c> rows, two interleaved lattices of cells (<c>(nx + 1) * (ny + 1)</c> on the grid, <c>nx * ny</c>
/// between them), each point counted in the nearer of the two by the metric <c>dx^2 + 3 dy^2</c>, hexagons with a vertex up and down
/// and a vertical edge on either side. matplotlib 3.11.2, <c>Axes.hexbin</c>; the numbers below are what its collection reports
/// (<c>get_offsets</c>, <c>get_array</c>) for the same twelve points.</summary>
public class HexLatticeTests
{
    private static readonly double[] X = [0.0, 0.4, 0.5, 1.7, 2.0, 2.2, 3.1, 3.3, 4.0, 4.4, 0.9, 2.9];
    private static readonly double[] Y = [0.0, 0.2, 1.5, 0.3, 2.2, 2.4, 1.0, 1.1, 3.0, 3.9, 2.8, 0.0];

    private static HexLattice Lattice() => HexLattice.Over(HexExtent.Of(X, Y)!.Value, gridSize: 4);

    // ── the extent of the data ──

    [Fact]
    public void TheExtent_IsTheRangeOfTheDataThatCanBePlaced()
    {
        var extent = HexExtent.Of([1.0, double.NaN, 3.0, 2.0], [10.0, 99.0, 30.0, double.PositiveInfinity])!.Value;

        Assert.Equal(new HexExtent(1.0, 3.0, 10.0, 30.0), extent);
    }

    [Fact]
    public void TheExtent_OfNothingThatCanBePlaced_IsNone()
    {
        Assert.Null(HexExtent.Of([], []));
        Assert.Null(HexExtent.Of([double.NaN], [1.0]));
    }

    /// <summary>matplotlib 3.11.2, <c>transforms._nonsingular(v, v, expander=0.1)</c>: a range of one value widens by a tenth of
    /// that value to each side, or to -0.1 .. 0.1 about zero.</summary>
    [Theory]
    [InlineData(5.0, 4.5, 5.5)]
    [InlineData(-5.0, -5.5, -4.5)]
    [InlineData(0.0, -0.1, 0.1)]
    public void TheExtent_OfOneValue_IsWidenedByATenthOfIt(double value, double lo, double hi)
    {
        var extent = HexExtent.Of([value], [value])!.Value;

        Assert.Equal(lo, extent.XMin, 12);
        Assert.Equal(hi, extent.XMax, 12);
        Assert.Equal(lo, extent.YMin, 12);
        Assert.Equal(hi, extent.YMax, 12);
    }

    // ── the grid ──

    [Fact]
    public void TheGrid_HasMatplotlibsCells()
    {
        var lattice = Lattice();

        Assert.Equal(23, lattice.CellCount);   // 5 * 3 on the grid, 4 * 2 between: nx = 4, ny = int(4 / sqrt(3)) = 2
    }

    /// <summary>A grid size below 2 would leave no row (<c>int(nx / sqrt(3))</c> is 0), and a grid size below 1 no column: each is at
    /// least one.</summary>
    [Theory]
    [InlineData(0, 1, 1, 5)]
    [InlineData(1, 1, 1, 5)]
    [InlineData(2, 2, 1, 3 * 2 + 2 * 1)]
    [InlineData(20, 20, 11, 21 * 12 + 20 * 11)]
    public void TheGrid_HasAtLeastOneColumnAndOneRow(int gridSize, int columns, int rows, int cells)
    {
        var lattice = HexLattice.Over(new HexExtent(0, 1, 0, 1), gridSize);

        Assert.Equal(columns, lattice.Columns);
        Assert.Equal(rows, lattice.Rows);
        Assert.Equal(cells, lattice.CellCount);
    }

    /// <summary>A grid size past what any picture can show (a hexagon narrower than a hundredth of a pixel of a 20,000-pixel chart) is
    /// held at 2,048 columns, so the cells of a grid, which are counted in an array, cannot run past what an array holds.</summary>
    [Theory]
    [InlineData(2048, 2048)]
    [InlineData(2049, 2048)]
    [InlineData(int.MaxValue, 2048)]
    public void TheGrid_HasAtMost2048Columns(int gridSize, int columns)
    {
        var lattice = HexLattice.Over(new HexExtent(0, 1, 0, 1), gridSize);

        Assert.Equal(columns, lattice.Columns);
        Assert.Equal(2049 * 1183 + 2048 * 1182, lattice.CellCount);   // ny = int(2048 / sqrt(3)) = 1182
    }

    /// <summary>The first cells of matplotlib's offsets: the grid lattice column by column (x = i * sx, y = j * sy), the in-between
    /// lattice after it ((i + 0.5) * sx, (j + 0.5) * sy). Here sx = 1.1 and sy = 1.95.</summary>
    [Fact]
    public void TheCells_AreInMatplotlibsOrder()
    {
        var lattice = Lattice();

        AssertCentre(lattice, 0, 0.0, 0.0);
        AssertCentre(lattice, 1, 0.0, 1.95);
        AssertCentre(lattice, 2, 0.0, 3.9);
        AssertCentre(lattice, 3, 1.1, 0.0);
        AssertCentre(lattice, 15, 0.55, 0.975);   // the first of the in-between lattice
        AssertCentre(lattice, 16, 0.55, 2.925);
        AssertCentre(lattice, 17, 1.65, 0.975);
    }

    private static void AssertCentre(HexLattice lattice, int cell, double x, double y)
    {
        var centre = lattice.CentreOf(cell);
        Assert.Equal(x, centre.X, 6);
        Assert.Equal(y, centre.Y, 6);
    }

    // ── the counts ──

    /// <summary>The nine cells of matplotlib's picture that hold something, with their counts, in cell order.</summary>
    [Fact]
    public void TheCounts_AreMatplotlibs()
    {
        var lattice = Lattice();

        int[] counts = lattice.Count(X, Y);

        var occupied = Enumerable.Range(0, counts.Length).Where(cell => counts[cell] > 0)
            .Select(cell => (X: Math.Round(lattice.CentreOf(cell).X, 6), Y: Math.Round(lattice.CentreOf(cell).Y, 6), Count: counts[cell]))
            .ToList();
        Assert.Equal(
        [
            (0.0, 0.0, 2), (2.2, 0.0, 1), (2.2, 1.95, 2), (3.3, 0.0, 1), (4.4, 3.9, 1),
            (0.55, 0.975, 1), (0.55, 2.925, 1), (2.75, 0.975, 2), (3.85, 2.925, 1),
        ], occupied);
        Assert.Equal(12, counts.Sum());
    }

    [Fact]
    public void ACoincidentPoint_AddsToTheCellItIsIn()
    {
        var lattice = HexLattice.Over(new HexExtent(0, 4, 0, 4), gridSize: 5);

        int[] counts = lattice.Count([2.0, 2.0, 2.0], [2.0, 2.0, 2.0]);

        Assert.Equal(3, counts.Max());
        Assert.Equal(3, counts.Sum());
    }

    [Fact]
    public void APointThatCannotBePlaced_IsNotCounted()
    {
        var lattice = HexLattice.Over(new HexExtent(0, 4, 0, 4), gridSize: 5);

        int[] counts = lattice.Count([1.0, double.NaN, 2.0, double.PositiveInfinity], [1.0, 1.0, double.NaN, 2.0]);

        Assert.Equal(1, counts.Sum());
    }

    [Fact]
    public void APointOutsideTheExtent_IsNotCounted()
    {
        var lattice = HexLattice.Over(new HexExtent(0, 4, 0, 4), gridSize: 5);

        int[] counts = lattice.Count([-1.0, 5.0, 2.0], [2.0, 2.0, 9.0]);

        Assert.Equal(0, counts.Sum());
    }

    [Fact]
    public void ThePointsOfTheExtent_AreAllCounted()
    {
        var random = new Random(11);
        double[] x = [.. Enumerable.Range(0, 500).Select(_ => random.NextDouble() * 15.0)];
        double[] y = [.. Enumerable.Range(0, 500).Select(_ => random.NextDouble() * 10.0)];

        var lattice = HexLattice.Over(HexExtent.Of(x, y)!.Value, gridSize: 12);

        Assert.Equal(500, lattice.Count(x, y).Sum());
    }

    // ── the hexagon ──

    /// <summary>matplotlib's polygon, <c>[sx, sy / 3] * [[.5, -.5], [.5, .5], [0, 1], [-.5, .5], [-.5, -.5], [0, -1]]</c>: a vertex
    /// up and down, a vertical edge each side, sx wide and 2 sy / 3 tall.</summary>
    [Fact]
    public void TheOutline_IsMatplotlibsPolygon()
    {
        var lattice = Lattice();   // sx = 1.1, sy = 1.95

        var outline = new Point[6];
        lattice.OutlineAt(new Point(10, 20), outline);

        double[][] expected = [[10.55, 19.675], [10.55, 20.325], [10, 20.65], [9.45, 20.325], [9.45, 19.675], [10, 19.35]];
        for (int i = 0; i < 6; i++)
        {
            Assert.Equal(expected[i][0], outline[i].X, 6);
            Assert.Equal(expected[i][1], outline[i].Y, 6);
        }
    }
}
