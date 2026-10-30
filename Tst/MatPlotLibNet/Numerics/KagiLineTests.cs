// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>How closing prices become the lines of a Kagi chart, held to a hand trace of 15 closes whose five legs turn exactly on
/// the reversal amount, change width in the middle of a line (thin to thick at a shoulder, thick to thin at a waist), keep their
/// width where they do not break the last shoulder or waist, and ignore a close that is short of a reversal or no more than the
/// extreme; to a chart that opens downwards; and to a decimal reversal amount.</summary>
public class KagiLineTests
{
    private static string Describe(KagiLines lines) =>
        (string.Join(" | ", lines.Segments.Select(s => $"c{s.Column} {s.From:0.##}-{s.To:0.##} {(s.Thick ? "thick" : "thin")}"))
        + " || " + string.Join(" | ", lines.Connectors.Select(c => $"{c.Column}-{c.Column + 1} @{c.Price:0.##} {(c.Thick ? "thick" : "thin")}"))).TrimEnd();

    private static readonly double[] Fifteen = [10, 10.5, 12, 12, 13, 12, 11, 10, 11, 14, 15, 14, 12, 9, 10];

    [Fact]
    public void TheFifteenCloses_DrawFiveLegs_SplitAtTheShoulderAndTheWaist()
    {
        Assert.Equal(
            "c0 10-13 thin | c1 13-10 thin | c2 10-13 thin | c2 13-15 thick | c3 15-10 thick | c3 10-9 thin | c4 9-10 thin"
            + " || 0-1 @13 thin | 1-2 @10 thin | 2-3 @15 thick | 3-4 @9 thin",
            Describe(Fifteen.KagiLines(1)));
    }

    [Fact]
    public void AWidthChange_FallsOnThePriceOfTheBreak_NotOnAVertex()
    {
        var lines = Fifteen.KagiLines(1);

        // The close that took column 2 above the shoulder was 14 and the line went on to 15: the change is at 13, the shoulder.
        var thick = lines.Segments.Single(s => s.Column == 2 && s.Thick);
        Assert.Equal((13.0, 15.0), (thick.From, thick.To));
        // Column 3 went on to 9 past the waist at 10: the change is at 10, not at any close.
        var thin = lines.Segments.Single(s => s.Column == 3 && !s.Thick);
        Assert.Equal((10.0, 9.0), (thin.From, thin.To));
    }

    [Fact]
    public void ALegThatBeginsInOneWidth_EndsInTheOtherAndTheNextLegStartsThere()
    {
        var lines = Fifteen.KagiLines(1);

        // Column 2 ends thick and the connector after it is thick; column 3 starts thick.
        Assert.True(lines.Connectors.Single(c => c.Column == 2).Thick);
        Assert.True(lines.Segments.First(s => s.Column == 3).Thick);
        // Column 3 ends thin, the connector after it is thin, and column 4 is thin.
        Assert.False(lines.Connectors.Single(c => c.Column == 3).Thick);
        Assert.False(lines.Segments.Single(s => s.Column == 4).Thick);
    }

    [Fact]
    public void AChartThatOpensDownwards_CountsTheStartingPriceAsTheFirstShoulder()
    {
        // 10 -> 8 is a down leg; 9 turns it (8 + 1); 11 takes the up leg past the start (10), which is the first shoulder.
        Assert.Equal("c0 10-8 thin | c1 8-10 thin | c1 10-11 thick || 0-1 @8 thin", Describe(new double[] { 10, 8, 9, 11 }.KagiLines(1)));
    }

    [Fact]
    public void AChartThatOpensUpwards_CountsTheStartingPriceAsTheFirstWaist()
    {
        // 10 -> 12 is the first up leg; it turns at 11 (12 - 1); 9 takes the down leg below the start (10), the first waist,
        // but a thin leg only ever turns thick, so it stays thin: the width changes only for a thick leg below a waist.
        Assert.Equal("c0 10-12 thin | c1 12-9 thin || 0-1 @12 thin", Describe(new double[] { 10, 12, 11, 9 }.KagiLines(1)));
    }

    [Fact]
    public void ACloseShortOfAReversal_OrNoMoreThanTheExtreme_IsIgnored()
    {
        // 10.5 is half a reversal from the start; 12 repeated is not above the extreme.
        Assert.Equal("c0 10-12 thin ||", Describe(new[] { 10, 10.5, 12, 12, 11.5 }.KagiLines(1)));
    }

    [Fact]
    public void AReversal_ExactlyTheAmount_Turns()
    {
        Assert.Equal("c0 10-12 thin | c1 12-11 thin || 0-1 @12 thin", Describe(new double[] { 10, 12, 11 }.KagiLines(1)));
    }

    [Fact]
    public void ADecimalReversal_TurnsOnTheAmount_NotJustShortOfItByRounding()
    {
        // 0.3 - 0.2 is 0.09999999999999998 in doubles: a plain comparison with 0.1 would not turn.
        Assert.Equal("c0 0.1-0.3 thin | c1 0.3-0.2 thin || 0-1 @0.3 thin", Describe(new[] { 0.1, 0.3, 0.2 }.KagiLines(0.1)));
    }

    [Fact]
    public void APriceThatIsNotANumber_IsSkipped()
    {
        double[] closes = [10, double.NaN, 12, double.PositiveInfinity, 13, 12];

        Assert.Equal("c0 10-13 thin | c1 13-12 thin || 0-1 @13 thin", Describe(closes.KagiLines(1)));
    }

    [Fact]
    public void PricesThatNeverMoveAReversalFromTheFirst_DrawNothing()
    {
        Assert.Empty(new[] { 10, 10.4, 9.6 }.KagiLines(1).Segments);
        Assert.Empty(Array.Empty<double>().KagiLines(1).Segments);
        Assert.Empty(new double[] { 10 }.KagiLines(1).Segments);
    }

    [Fact]
    public void ThereIsOneConnectorFewerThanThereAreLegs()
    {
        var lines = Fifteen.KagiLines(1);

        Assert.Equal(5, lines.Segments.Select(s => s.Column).Distinct().Count());
        Assert.Equal(4, lines.Connectors.Count);
    }

    [Fact]
    public void AThickLegThatDoesNotBreakTheWaist_StaysThick()
    {
        // Column 3 of the fifteen closes goes down to 12 and on: here it stops at 10.5, above the waist at 10, so it is all thick.
        double[] closes = [10, 10.5, 12, 12, 13, 12, 11, 10, 11, 14, 15, 14, 12, 10.5, 11.5];

        Assert.Contains("c3 15-10.5 thick", Describe(closes.KagiLines(1)));
    }
}
