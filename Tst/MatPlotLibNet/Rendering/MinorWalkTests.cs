// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one walk of the minor tick values between the major ticks of an axis (<see cref="MinorWalk"/>): the values
/// the minor ticks and the minor grid stand on, low to high, a step past each end, each rounded to ten decimals, none on a
/// major tick, and nothing allocated while walking.</summary>
public class MinorWalkTests
{
    /// <summary>The walk written out as a loop over the index, the oracle the walk must equal to the bit: each value worked out from
    /// its index and rounded to ten decimals, never added to the one before (the loop the drawing sites once wrote added, which
    /// stalls far from zero and drifts by a rounding at every step, <c>TickWalkStallTests</c>).</summary>
    private static List<double> IndexedLoop(double[] majors, double minorStep)
    {
        var values = new List<double>();
        if (majors.Length < 2) return values;
        double majorStep = majors[1] - majors[0];
        double rangeStart = majors[0] - majorStep;
        double rangeEnd = majors[^1] + majorStep;
        for (long i = 0; rangeStart + i * minorStep <= rangeEnd + minorStep * 0.01; i++)
        {
            double mt = Math.Round(rangeStart + i * minorStep, 10);
            if (Array.Exists(majors, t => Math.Abs(t - mt) < minorStep * 0.01)) continue;
            values.Add(mt);
        }
        return values;
    }

    private static List<double> Walk(double[] majors, double minorStep)
    {
        var values = new List<double>();
        foreach (double value in new MinorWalk(majors, minorStep)) values.Add(value);
        return values;
    }

    [Fact]
    public void ALinearRun_WalksAStepPastEachEnd_SkippingTheMajors()
    {
        Assert.Equal(
            [-1.0, -0.8, -0.6, -0.4, -0.2, 0.2, 0.4, 0.6, 0.8, 1.2, 1.4, 1.6, 1.8, 2.2, 2.4, 2.6, 2.8, 3.0],
            Walk([0.0, 1.0, 2.0], 0.2));
    }

    [Theory]
    [InlineData(new[] { 0.0, 1.0, 2.0 }, 0.2)]
    [InlineData(new[] { 0.0, 0.1, 0.2, 0.30000000000000004 }, 0.02)]
    [InlineData(new[] { 0.1, 0.2, 0.30000000000000004, 0.4 }, 0.020000000000000004)]
    [InlineData(new[] { -3.0, -1.5, 0.0, 1.5, 3.0 }, 0.3)]
    [InlineData(new[] { 19000.0, 19000.5, 19001.0 }, 10.0 / 86400.0)]
    [InlineData(new[] { 1.0, 10.0, 100.0, 1000.0 }, 1.8)]
    [InlineData(new[] { 0.0, 0.5, 1.0 }, 0.2)]
    [InlineData(new[] { 0.0, 1.0 }, 0.01)]
    [InlineData(new[] { 1e-7, 2e-7, 3e-7 }, 2e-8)]
    public void TheWalk_EqualsTheIndexedLoop_ToTheBit(double[] majors, double minorStep)
    {
        var expected = IndexedLoop(majors, minorStep);
        var walked = Walk(majors, minorStep);

        Assert.Equal(expected.Count, walked.Count);
        for (int i = 0; i < expected.Count; i++)
            Assert.Equal(BitConverter.DoubleToInt64Bits(expected[i]), BitConverter.DoubleToInt64Bits(walked[i]));
    }

    [Fact]
    public void AFloatCloseToAMajor_IsTakenForTheMajor_AndLeftOut()
    {
        double[] majors = [0.0, 0.1, 0.2, 0.30000000000000004];
        const double step = 0.02;

        var walked = Walk(majors, step);

        Assert.NotEmpty(walked);
        Assert.All(walked, v => Assert.DoesNotContain(majors, m => Math.Abs(m - v) < step * 0.01));
    }

    [Theory]
    [InlineData(new double[0])]
    [InlineData(new[] { 5.0 })]
    public void FewerThanTwoMajors_WalkNothing(double[] majors) => Assert.Empty(Walk(majors, 0.2));

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.2)]
    [InlineData(double.NaN)]
    public void AStepThatIsNotAPositiveNumber_WalksNothing_RatherThanSpinning(double minorStep) =>
        Assert.Empty(Walk([0.0, 1.0, 2.0], minorStep));

    [Fact]
    public void TwoEqualMajors_WalkNothing_RatherThanSpinning() => Assert.Empty(Walk([1.0, 1.0], 0.0));

    private static List<double> Between(TickConfig minor, double[] majors)
    {
        var values = new List<double>();
        foreach (double value in Theme.Default.Looks.Ticks.MinorValuesBetween(minor, majors)) values.Add(value);
        return values;
    }

    [Fact]
    public void MinorSettingsWithoutASpacing_SplitTheMajorStepInFive() =>
        Assert.Equal(Walk([0.0, 1.0, 2.0], 0.2), Between(new TickConfig(), [0.0, 1.0, 2.0]));

    [Fact]
    public void MinorSettingsWithASpacing_WalkAtThatSpacing() =>
        Assert.Equal([-1.0, -0.5, 0.5, 1.5, 2.5, 3.0], Between(new TickConfig { Spacing = 0.5 }, [0.0, 1.0, 2.0]));

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void ANonPositiveSpacing_FallsBackToTheFiveWaySplit(double spacing) =>
        Assert.Equal(Walk([0.0, 1.0, 2.0], 0.2), Between(new TickConfig { Spacing = spacing }, [0.0, 1.0, 2.0]));

    [Fact]
    public void MinorSettingsOverFewerThanTwoMajors_WalkNothing()
    {
        Assert.Empty(Between(new TickConfig(), [3.0]));
        Assert.Empty(Between(new TickConfig { Spacing = 0.5 }, []));
    }

    private static readonly Rect Plot = new(10, 20, 400, 300);

    private static TickRun Run(DataTransform transform, EdgeTerms edge) => new([0.0, 1.0, 2.0], transform, edge);

    /// <summary>The edge a run along X (the bottom one) or along Y (the left one) lies on.</summary>
    private static EdgeTerms EdgeAlong(bool alongX) => alongX ? EdgeTerms.Bottom : EdgeTerms.Left;

    [Theory]
    [InlineData(0.0, 10.0)]
    [InlineData(1.0, 210.0)]
    [InlineData(2.0, 410.0)]
    public void AValueTheXRangeHolds_StandsWhereTheTransformPutsIt(double value, double x) =>
        Assert.Equal(x, Run(new DataTransform(0, 2, 0, 2, Plot), EdgeTerms.Bottom).PlaceOnPlot(value));

    [Theory]
    [InlineData(0.0, 320.0)]
    [InlineData(2.0, 20.0)]
    public void AValueTheYRangeHolds_StandsWhereTheTransformPutsIt(double value, double y) =>
        Assert.Equal(y, Run(new DataTransform(0, 2, 0, 2, Plot), EdgeTerms.Left).PlaceOnPlot(value));

    [Theory]
    [InlineData(-0.5, true)]
    [InlineData(2.5, true)]
    [InlineData(-0.5, false)]
    [InlineData(2.5, false)]
    public void AValueOutsideThePlot_StandsNowhere(double value, bool alongX) =>
        Assert.Null(Run(new DataTransform(0, 2, 0, 2, Plot), EdgeAlong(alongX)).PlaceOnPlot(value));

    [Theory]
    [InlineData(2.0000000000000004, true)]
    [InlineData(-1e-12, true)]
    [InlineData(2.0000000000000004, false)]
    [InlineData(-1e-12, false)]
    public void AValueARoundingErrorPastAnEnd_StillStandsOnThePlot(double value, bool alongX) =>
        // matplotlib keeps a tick that lies within 1e-10 of the view's length past its ends (_interval_contains_close):
        // a tick a locator computed for the end of a range lands a rounding error either side of it, and is still drawn.
        Assert.NotNull(Run(new DataTransform(0, 2, 0, 2, Plot), EdgeAlong(alongX)).PlaceOnPlot(value));

    [Theory]
    [InlineData(2.000001, true)]
    [InlineData(-0.000001, true)]
    [InlineData(2.000001, false)]
    [InlineData(-0.000001, false)]
    public void AValueAMillionthPastAnEnd_StandsNowhere(double value, bool alongX) =>
        Assert.Null(Run(new DataTransform(0, 2, 0, 2, Plot), EdgeAlong(alongX)).PlaceOnPlot(value));

    /// <summary>A date axis: mapping a date near 46,277 days across a range of minutes or seconds to a pixel loses about
    /// seven digits, so the tick at an end of the range lands that rounding either side of the edge, more than a
    /// ten-billionth of the plot's length. Where the axis is drawn, before the pixel, the tick equals the end, so it stands on
    /// the plot at every plot size, as matplotlib keeps it.</summary>
    [Theory]
    [InlineData(30 * 60.0, true)]
    [InlineData(30 * 60.0, false)]
    [InlineData(60.0, true)]
    [InlineData(60.0, false)]
    public void TheTicksAtTheEndsOfADateRange_StandOnThePlot_AtEverySize(double seconds, bool alongX)
    {
        var start = new DateTime(2026, 9, 12, 14, 0, 0, DateTimeKind.Utc);
        double lo = start.ToOADate(), hi = start.AddSeconds(seconds).ToOADate();
        for (double size = 300; size < 400; size += 0.01)
        {
            var transform = alongX
                ? new DataTransform(lo, hi, 0, 1, new Rect(60, 40, size, 400))
                : new DataTransform(0, 1, lo, hi, new Rect(60, 40, 400, size));
            var run = new TickRun([lo, hi], transform, EdgeAlong(alongX));
            Assert.NotNull(run.PlaceOnPlot(lo));
            Assert.NotNull(run.PlaceOnPlot(hi));
        }
    }

    /// <summary>The ends of a reversed range stand on the plot as the ends of a range that runs the other way do.</summary>
    [Fact]
    public void TheEndsOfAReversedRange_StandOnThePlot_AndAValuePastThemNowhere()
    {
        var run = Run(new DataTransform(2, 0, 0, 2, Plot), EdgeTerms.Bottom);

        Assert.Equal(410.0, run.PlaceOnPlot(0.0));
        Assert.Equal(10.0, run.PlaceOnPlot(2.0));
        Assert.Null(run.PlaceOnPlot(2.5));
    }

    /// <summary>On an axis whose range has no width only the one value it shows stands on the plot, at its centre; any other
    /// value lies past both ends, and is not drawn on top of it.</summary>
    [Fact]
    public void OnAnAxisWithNoWidth_OnlyTheValueItShowsStandsOnThePlot()
    {
        var run = Run(new DataTransform(1, 1, 0, 2, Plot), EdgeTerms.Bottom);

        Assert.Equal(210.0, run.PlaceOnPlot(1.0));
        Assert.Null(run.PlaceOnPlot(5.0));
        Assert.Null(run.PlaceOnPlot(double.NaN));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AValueTheScaleHasNoPlaceFor_StandsNowhere(bool alongX)
    {
        var log = Plot.LogLog(1000, 1000);
        Assert.Null(new TickRun([1.0, 10.0, 100.0], log, EdgeAlong(alongX)).PlaceOnPlot(-8.0));
        Assert.NotNull(new TickRun([1.0, 10.0, 100.0], log, EdgeAlong(alongX)).PlaceOnPlot(10.0));
    }

    [Fact]
    public void WalkingAllocatesNothing()
    {
        double[] majors = [0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0];
        double sum = 0;
        foreach (double value in new MinorWalk(majors, 0.2)) sum += value; // warm the code

        long before = GC.GetAllocatedBytesForCurrentThread();
        foreach (double value in new MinorWalk(majors, 0.2)) sum += value;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.True(sum != 0);
    }
}
