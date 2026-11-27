// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary><see cref="SquarifiedLayout"/> lays out the children of one treemap node. The rectangles are held to the ones the
/// treemap renderer drew when it held the layout itself, bit for bit: the layout uses only exact arithmetic, so the numbers are
/// the same on every machine and a treemap draws the bytes it always drew.</summary>
public class SquarifiedLayoutTests
{
    private static TreeNode Parent(params double[] values) =>
        new() { Label = "root", Children = [.. values.Select((v, i) => new TreeNode { Label = "n" + i, Value = v })] };

    private static void AssertCells(SquarifiedCell[] cells, string[] order, Rect[] bounds)
    {
        Assert.Equal(order, cells.Select(c => c.Node.Label));
        Assert.Equal(bounds, cells.Select(c => c.Bounds));
    }

    [Fact]
    public void OneChild_FillsTheBounds_LessHalfThePaddingOnEachSide() =>
        AssertCells(SquarifiedLayout.Of(Parent(5), new Rect(10, 20, 300, 200), 2), ["n0"], [new(11, 21, 298, 198)]);

    [Fact]
    public void TwoChildren_ShareTheBounds_InProportionToTheirValues() =>
        AssertCells(SquarifiedLayout.Of(Parent(3, 1), new Rect(0, 0, 400, 100), 0), ["n0", "n1"],
            [new(0, 0, 300, 100), new(300, 0, 100, 100)]);

    [Fact]
    public void Ties_KeepTheStoredOrder_LargestFirst() =>
        AssertCells(SquarifiedLayout.Of(Parent(2, 4, 2, 4, 2), new Rect(0, 0, 120, 90), 1), ["n1", "n3", "n0", "n2", "n4"],
        [
            new(0.5, 0.5, 67.57142857142857, 44),
            new(0.5, 45.5, 67.57142857142857, 44),
            new(69.07142857142857, 0.5, 50.42857142857143, 29),
            new(69.07142857142857, 30.5, 50.42857142857143, 29),
            new(69.07142857142857, 60.5, 50.42857142857143, 29),
        ]);

    [Fact]
    public void ManyChildren_InAWideArea_AreLaidOutInColumns() =>
        AssertCells(SquarifiedLayout.Of(Parent(6, 6, 4, 3, 2, 2, 1), new Rect(0, 0, 600, 400), 0), ["n0", "n1", "n2", "n3", "n4", "n5", "n6"],
        [
            new(0, 0, 300, 200),
            new(0, 200, 300, 200),
            new(300, 0, 171.42857142857142, 233.33333333333334),
            new(471.42857142857144, 0, 128.57142857142856, 233.33333333333334),
            new(300, 233.33333333333334, 120, 166.66666666666666),
            new(420, 233.33333333333334, 120, 166.66666666666666),
            new(540, 233.33333333333334, 60, 166.66666666666666),
        ]);

    [Fact]
    public void ManyChildren_InATallArea_AreLaidOutInRows() =>
        AssertCells(SquarifiedLayout.Of(Parent(6, 6, 4, 3, 2, 2, 1), new Rect(5, 5, 100, 300), 3), ["n0", "n1", "n2", "n3", "n4", "n5", "n6"],
        [
            new(6.5, 6.5, 97, 72),
            new(6.5, 81.5, 97, 72),
            new(6.5, 156.5, 97, 47),
            new(6.5, 206.5, 59.5, 57),
            new(6.5, 266.5, 59.5, 37),
            new(69, 206.5, 34.5, 63.66666666666667),
            new(69, 273.1666666666667, 34.50000000000001, 30.33333333333333),
        ]);

    /// <summary>A child worth less than nothing among others still takes the cell the layout always gave it.</summary>
    [Fact]
    public void ANegativeChild_AmongPositiveOnes_KeepsItsCell() =>
        AssertCells(SquarifiedLayout.Of(Parent(5, -1, 2), new Rect(0, 0, 100, 100), 0), ["n0", "n2", "n1"],
        [
            new(0, 0, 83.33333333333334, 100),
            new(83.33333333333334, 0, 33.333333333333314, 100.00000000000006),
            new(116.66666666666666, 0, 1, 100.00000000000006),
        ]);

    /// <summary>Two hundred children of random whole values: the cells and their order, folded into one number that was
    /// taken from the layout the renderer held.</summary>
    [Fact]
    public void TwoHundredChildren_AreLaidOutAsBefore()
    {
        var random = new Random(7);
        var parent = Parent([.. Enumerable.Range(0, 200).Select(_ => (double)random.Next(1, 50))]);

        var cells = SquarifiedLayout.Of(parent, new Rect(3, 7, 777, 555), 1.5);

        long hash = 17;
        foreach (var cell in cells)
            foreach (var d in new[] { cell.Bounds.X, cell.Bounds.Y, cell.Bounds.Width, cell.Bounds.Height })
                hash = unchecked(hash * 31 + BitConverter.DoubleToInt64Bits(d));
        Assert.Equal("n97", cells[0].Node.Label);
        Assert.Equal("n103", cells[^1].Node.Label);
        Assert.Equal(212293224252165213L, hash);
    }

    [Fact]
    public void ALeaf_HasNoCells() => Assert.Empty(SquarifiedLayout.Of(new TreeNode { Value = 3 }, new Rect(0, 0, 10, 10), 0));

    /// <summary>Children worth nothing, and children worth less than nothing, have no cells.</summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(-3, 1)]
    public void ChildrenWorthNothingOrLess_HaveNoCells(double first, double second) =>
        Assert.Empty(SquarifiedLayout.Of(Parent(first, second), new Rect(0, 0, 10, 10), 0));

    /// <summary>A value that is not a number, or an infinite one, makes the children's total one too: there is no area to
    /// share, so none is placed, the rule the sunburst's partition follows (<c>TreeNodePartitionExtensions.Shares</c>).</summary>
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ChildrenWhoseTotalIsNotAFiniteNumber_HaveNoCells(double value) =>
        Assert.Empty(SquarifiedLayout.Of(Parent(3, value, 1), new Rect(0, 0, 100, 100), 0));

    /// <summary>squarify lays its tiles out in a Y that runs up (<c>ax.bar(..., bottom=y)</c> from y = 0): the same layout turned over in
    /// the area it fills, so the largest tile stands at the bottom left. A cell keeps its size and its padding; it moves to the
    /// mirror image of its place inside the bounds.</summary>
    [Fact]
    public void FromTheBottom_IsTheSameLayoutTurnedOverInTheBounds()
    {
        var bounds = new Rect(5, 7, 600, 400);
        var down = SquarifiedLayout.Of(Parent(6, 6, 4, 3, 2, 2, 1), bounds, 2);
        var up = SquarifiedLayout.Of(Parent(6, 6, 4, 3, 2, 2, 1), bounds, 2, fromBottom: true);

        Assert.Equal(down.Select(c => c.Node.Label), up.Select(c => c.Node.Label));
        for (int i = 0; i < down.Length; i++)
        {
            Assert.Equal(down[i].Bounds.X, up[i].Bounds.X);
            Assert.Equal(down[i].Bounds.Width, up[i].Bounds.Width);
            Assert.Equal(down[i].Bounds.Height, up[i].Bounds.Height);
            Assert.Equal(bounds.Y + bounds.Height - (down[i].Bounds.Y - bounds.Y) - down[i].Bounds.Height, up[i].Bounds.Y, 9);
        }
        // The largest child stands at the bottom left.
        Assert.Equal(bounds.X + 1, up[0].Bounds.X);
        Assert.Equal(bounds.Y + bounds.Height - 1, up[0].Bounds.Y + up[0].Bounds.Height, 9);
    }
}
