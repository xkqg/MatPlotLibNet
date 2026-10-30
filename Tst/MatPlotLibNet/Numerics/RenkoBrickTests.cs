// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>How closing prices become the bricks of a Renko chart, held to a hand trace of a 12-close series (13 bricks) that
/// tells a continuation from a reversal, a close on a trigger from one short of it, a close that completes several bricks from
/// one that completes none; to the anchor on the raw first close; and to a decimal brick size.</summary>
public class RenkoBrickTests
{
    private static string Describe(IReadOnlyList<RenkoBrick> bricks) =>
        string.Join(" | ", bricks.Select(b => $"{(b.Rising ? "up" : "down")} {b.Bottom:0.##}-{b.Top:0.##} @{b.SourceIndex}"));

    private static readonly double[] Twelve = [10, 10.4, 11.0, 13.2, 12.5, 11.0, 10.4, 7.8, 8.9, 10.0, 9.0, 15.9];

    [Fact]
    public void TheTwelveCloses_DrawThirteenBricks()
    {
        Assert.Equal(
            "up 10-11 @2 | up 11-12 @3 | up 12-13 @3 | down 11-12 @5 | down 10-11 @7 | down 9-10 @7 | down 8-9 @7 | up 9-10 @9 | up 10-11 @11 | up 11-12 @11 | up 12-13 @11 | up 13-14 @11 | up 14-15 @11",
            Describe(Twelve.RenkoBricks(1)));
    }

    [Fact]
    public void EveryBrick_IsOneBrickTall_AndItsCornerTouchesTheOneBeforeIt()
    {
        var bricks = Twelve.RenkoBricks(1);

        Assert.All(bricks, brick => Assert.Equal(1.0, brick.Top - brick.Bottom, 12));
        for (int i = 1; i < bricks.Count; i++)
        {
            double touch = bricks[i].Rising ? bricks[i - 1].Top : bricks[i - 1].Bottom;
            Assert.Equal(touch, bricks[i].Rising ? bricks[i].Bottom : bricks[i].Top, 12);
        }
    }

    [Fact]
    public void AClose_OnTheTrigger_CompletesTheBrick_AndOneShortOfItDoesNot()
    {
        Assert.Equal("up 10-11 @1", Describe(new double[] { 10, 11 }.RenkoBricks(1)));
        Assert.Empty(new double[] { 10, 10.99 }.RenkoBricks(1));
        Assert.Equal("down 9-10 @1", Describe(new double[] { 10, 9 }.RenkoBricks(1)));
        Assert.Empty(new double[] { 10, 9.01 }.RenkoBricks(1));
    }

    [Fact]
    public void AReversal_NeedsTwoBricksFromTheFarEdge_AContinuationOne()
    {
        // The last brick is up 11-12 (far edge 12, near edge 11): 13 continues it, 10 reverses it (a brick below the near edge),
        // and 10.5 is a brick short of either.
        Assert.Equal("up 10-11 @1 | up 11-12 @1 | up 12-13 @2", Describe(new[] { 10.0, 12.0, 13.0 }.RenkoBricks(1)));
        Assert.Equal("up 10-11 @1 | up 11-12 @1 | down 10-11 @2", Describe(new[] { 10.0, 12.0, 10.0 }.RenkoBricks(1)));
        Assert.Equal("up 10-11 @1 | up 11-12 @1", Describe(new[] { 10.0, 12.0, 10.5 }.RenkoBricks(1)));
    }

    [Fact]
    public void TheAnchor_IsTheRawFirstClose_NotAGridPrice()
    {
        // A first close of 10.4 and a brick of 1 draws 10.4 to 11.4; rounding the anchor to the grid would draw 10 to 11 a close earlier.
        Assert.Equal("up 10.4-11.4 @2", Describe(new[] { 10.4, 11.0, 11.5 }.RenkoBricks(1)));
    }

    [Fact]
    public void ADecimalBrick_IsNotShortOfOneBrickByRounding()
    {
        // 0.3 / 0.1 is 2.9999999999999996 and 0.1 + 0.1 + 0.1 is 0.30000000000000004: three bricks either way, never two.
        Assert.Equal(3, new[] { 0.0, 0.1, 0.2, 0.3 }.RenkoBricks(0.1).Count);
        Assert.Equal(3, new[] { 0.0, 0.3 }.RenkoBricks(0.1).Count);
        Assert.Equal(3, new[] { 0.0, -0.3 }.RenkoBricks(0.1).Count);
    }

    [Fact]
    public void ACloseThatCoversSeveralBricks_DrawsEachOfThemFromTheNearOne_AllCarryItsIndex()
    {
        Assert.Equal("up 0-1 @1 | up 1-2 @1 | up 2-3 @1", Describe(new double[] { 0, 3.4 }.RenkoBricks(1)));
        Assert.Equal("down -1-0 @1 | down -2--1 @1", Describe(new double[] { 0, -2.5 }.RenkoBricks(1)));
    }

    [Fact]
    public void APriceThatIsNotANumber_IsSkipped_AndTheIndexIsThePositionInTheGivenPrices()
    {
        double[] closes = [10, double.NaN, double.PositiveInfinity, 11];

        Assert.Equal("up 10-11 @3", Describe(closes.RenkoBricks(1)));
    }

    [Fact]
    public void PricesThatNeverMoveABrick_DrawNothing()
    {
        Assert.Empty(new double[] { 10, 10.5, 10.2, 9.7 }.RenkoBricks(1));
        Assert.Empty(Array.Empty<double>().RenkoBricks(1));
        Assert.Empty(new double[] { 10 }.RenkoBricks(1));
    }

    [Fact]
    public void ABrickSize_ScalesTheWholeChart()
    {
        double[] scaled = [.. Twelve.Select(price => price * 10)];

        var bricks = scaled.RenkoBricks(10);

        Assert.Equal(13, bricks.Count);
        Assert.Equal(Twelve.RenkoBricks(1).Select(b => (b.Rising, b.SourceIndex)), bricks.Select(b => (b.Rising, b.SourceIndex)));
        Assert.Equal(Twelve.RenkoBricks(1).Select(b => b.Bottom * 10), bricks.Select(b => b.Bottom));
    }
}
