// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>How closing prices become the columns of a Point and Figure chart, held to published worked examples (Domo, the
/// e-learn 10x3 rule) and to a hand trace of a 15-price series that tells floor from ceiling, from rounding, and a reversal
/// of two boxes from one of three.</summary>
public class PointAndFigureColumnTests
{
    private static string Describe(IReadOnlyList<PointAndFigureColumn> columns) =>
        string.Join(" | ", columns.Select(c => $"{(c.Rising ? "X" : "O")} {c.From}-{c.To}"));

    private static readonly double[] Fifteen =
        [20.4, 20.9, 22.3, 23.0, 21.0, 24.4, 21.0, 20.2, 18.6, 21.9, 22.0, 25.8, 22.0, 22.9, 15.1];

    [Fact]
    public void TheFifteenPrices_DrawFourColumns_X20To24_O23To19_X20To25_O24To16()
    {
        Assert.Equal("X 20-24 | O 23-19 | X 20-25 | O 24-16", Describe(Fifteen.PointAndFigureColumns(1, 3)));
    }

    [Fact]
    public void TheDomoExample_DrawsX48To51_O50To47_X48To53()
    {
        double[] closes = [48, 49, 50, 51, 50, 49, 48, 47, 48, 49, 50, 51, 52, 53];

        Assert.Equal("X 48-51 | O 50-47 | X 48-53", Describe(closes.PointAndFigureColumns(1, 3)));
    }

    [Fact]
    public void TheElearnTenByThreeRule_ReversesAt510_NotAt517()
    {
        double[] closes = [500, 547, 515, 511, 510];

        // Box 10: levels 500..540 are boxes 50..54; the reversal is three boxes below the last plotted box (540), at 510.
        Assert.Equal("X 50-54 | O 53-51", Describe(closes.PointAndFigureColumns(10, 3)));
        Assert.Equal("X 50-54", Describe(closes[..4].PointAndFigureColumns(10, 3)));
    }

    [Fact]
    public void AReversalOfOneOrTwoBoxes_DrawsMoreColumnsThanOfThree()
    {
        Assert.True(Fifteen.PointAndFigureColumns(1, 1).Count > 4);
        Assert.True(Fifteen.PointAndFigureColumns(1, 2).Count > 4);
    }

    [Fact]
    public void AnXRoundsDown_AndAnORoundsUp_OnABoxBoundary()
    {
        // 20.0 is on the boundary of box 20: an X column reaches it, an O column reaches it too, and neither adds a box past it.
        Assert.Equal("X 20-21", Describe(new double[] { 20.5, 21.0 }.PointAndFigureColumns(1, 3)));
        Assert.Equal("O 21-20", Describe(new double[] { 20.5, 20.0 }.PointAndFigureColumns(1, 3)));
    }

    [Fact]
    public void ACloseThatNeitherExtendsNorReverses_IsIgnored()
    {
        // The column stands at 24; 22.0 is two boxes down, one short of the three that reverse it.
        Assert.Equal("X 20-24", Describe(new double[] { 20.4, 24.5, 22.0 }.PointAndFigureColumns(1, 3)));
        Assert.Equal("X 20-24 | O 23-21", Describe(new double[] { 20.4, 24.5, 21.0 }.PointAndFigureColumns(1, 3)));
    }

    [Fact]
    public void TheFirstColumn_IsDecidedByTheFirstCloseThatLeavesTheBoxOfTheFirstPrice()
    {
        // 20.4 stands in box 20 going up and at the top of box 21 going down: 18.6 reaches box 19, 22.3 reaches box 22.
        Assert.Equal("O 21-19", Describe(new double[] { 20.4, 20.9, 20.3, 18.6 }.PointAndFigureColumns(1, 3)));
        Assert.Equal("X 20-22", Describe(new double[] { 20.4, 20.9, 22.3 }.PointAndFigureColumns(1, 3)));
    }

    [Theory]
    [InlineData(0.1, 0.05, 0.3, 0, 3)]       // 0.3 / 0.1 is 2.9999999999999996: a plain floor says box 2
    [InlineData(0.01, 0.5, 0.57, 50, 57)]    // 0.57 / 0.01 is 56.99999999999999: a plain floor says box 56
    public void ADecimalBoxSize_PutsThePriceInItsOwnBox(double box, double first, double second, int from, int to)
    {
        var column = Assert.Single(new[] { first, second }.PointAndFigureColumns(box, 3));

        Assert.True(column.Rising);
        Assert.Equal((from, to), (column.From, column.To));
    }

    [Fact]
    public void APriceThatIsNotANumber_IsSkipped()
    {
        double[] closes = [20.4, double.NaN, 22.3, double.PositiveInfinity, 23.0];

        Assert.Equal("X 20-23", Describe(closes.PointAndFigureColumns(1, 3)));
    }

    [Fact]
    public void PricesThatNeverLeaveTheStartingBox_DrawNothing()
    {
        Assert.Empty(new double[] { 20.4, 20.9, 20.5 }.PointAndFigureColumns(1, 3));
        Assert.Empty(Array.Empty<double>().PointAndFigureColumns(1, 3));
        Assert.Empty(new double[] { 20.4 }.PointAndFigureColumns(1, 3));
    }

    [Fact]
    public void TheReversalIsMeasuredFromTheLastPlottedBox_NotFromTheLastPrice()
    {
        // The column ends at box 54 (540) though the price reached 547: it reverses at 510, three boxes below 540, not at 517,
        // three boxes below 547. 512 is past 517 and not yet at 510.
        double[] closes = [500, 547, 519, 512];

        Assert.Equal("X 50-54", Describe(closes.PointAndFigureColumns(10, 3)));
    }
}
