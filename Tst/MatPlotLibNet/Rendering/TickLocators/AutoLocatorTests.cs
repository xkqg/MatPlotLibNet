// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>Verifies <see cref="AutoLocator"/> behavior.</summary>
public class AutoLocatorTests
{
    /// <summary>Verifies that the locator implements ITickLocator.</summary>
    [Fact]
    public void ImplementsITickLocator()
    {
        ITickLocator locator = new AutoLocator();
        Assert.NotNull(locator);
    }

    /// <summary>Verifies that the default target count of 5 produces approximately 5 ticks.</summary>
    [Fact]
    public void Locate_DefaultTargetCount_ProducesReasonableCount()
    {
        var locator = new AutoLocator();
        double[] ticks = locator.Locate(0, 10);
        Assert.InRange(ticks.Length, 3, 8);
    }

    /// <summary>Verifies nice-number alignment: 0..10 yields [0, 2, 4, 6, 8, 10].</summary>
    [Fact]
    public void Locate_ZeroToTen_YieldsNiceNumbers()
    {
        var locator = new AutoLocator(5);
        double[] ticks = locator.Locate(0, 10);
        Assert.All(ticks, t => Assert.Equal(0, t % 2, 8));
    }

    /// <summary>Verifies that a zero-width range returns just the single value.</summary>
    [Fact]
    public void Locate_ZeroRange_ReturnsSingleValue()
    {
        var locator = new AutoLocator();
        double[] ticks = locator.Locate(5, 5);
        Assert.Single(ticks);
        Assert.Equal(5, ticks[0]);
    }

    /// <summary>Verifies that a custom target count influences output count.</summary>
    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    public void Locate_CustomTargetCount_InfluencesCount(int target)
    {
        var locator = new AutoLocator(target);
        double[] ticks = locator.Locate(0, 100);
        Assert.InRange(ticks.Length, 2, target + 4);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ExpandToNiceBounds — matplotlib MaxNLocator.view_limits equivalent.
    // These cases exist specifically because a regression in April 2026 fed the
    // ALREADY-PADDED range into ExpandToNiceBounds, bloating [0,10] out to
    // [-2, 12] on every auto-ranged chart. See CartesianAxesRenderer.ComputeDataRanges.
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// An integer range that already sits on nice-number boundaries must round-trip
    /// unchanged — no drift inward or outward.
    /// </summary>
    [Fact]
    public void ExpandToNiceBounds_ZeroToTen_IsIdempotent()
    {
        var locator = new AutoLocator();
        var (lo, hi) = locator.ExpandToNiceBounds(0, 10);
        Assert.Equal(0, lo);
        Assert.Equal(10, hi);
    }

    /// <summary>
    /// Half-integer endpoints (eventplot: 4 rows at y=0..3 with halfLen=0.5 gives [-0.5, 3.5])
    /// must round outward to the next nice tick — [-1, 4].
    /// </summary>
    [Fact]
    public void ExpandToNiceBounds_HalfIntegerRange_RoundsOutwardToUnitStep()
    {
        var locator = new AutoLocator();
        var (lo, hi) = locator.ExpandToNiceBounds(-0.5, 3.5);
        Assert.Equal(-1, lo);
        Assert.Equal(4, hi);
    }

    /// <summary>
    /// Expansion must never pull the bounds inward — the output range is always a
    /// superset of the input. This is the core invariant that prevents the locator
    /// from clipping real data.
    /// </summary>
    [Theory]
    [InlineData(0.0, 10.0)]
    [InlineData(-0.5, 3.5)]
    [InlineData(-3.7, 8.4)]
    [InlineData(0.12, 0.97)]
    [InlineData(-1000, 1000)]
    [InlineData(1e-6, 2e-6)]
    public void ExpandToNiceBounds_NeverPullsInward(double rawLo, double rawHi)
    {
        var locator = new AutoLocator();
        var (lo, hi) = locator.ExpandToNiceBounds(rawLo, rawHi);
        Assert.True(lo <= rawLo, $"lo {lo} should be <= rawLo {rawLo}");
        Assert.True(hi >= rawHi, $"hi {hi} should be >= rawHi {rawHi}");
    }

    /// <summary>
    /// Degenerate ranges (zero-width or inverted) must pass through unchanged — we
    /// can't compute a nice step when the range is ill-defined.
    /// </summary>
    [Theory]
    [InlineData(5.0, 5.0)]
    [InlineData(5.0, 3.0)]
    public void ExpandToNiceBounds_DegenerateRange_ReturnsInput(double rawLo, double rawHi)
    {
        var locator = new AutoLocator();
        var (lo, hi) = locator.ExpandToNiceBounds(rawLo, rawHi);
        Assert.Equal(rawLo, lo);
        Assert.Equal(rawHi, hi);
    }

    /// <summary>A range far from zero and narrower than the spacing of doubles there (1e16 has a spacing of 2): the step
    /// is smaller than the spacing, so adding it to a tick changes nothing. The locator ends all the same, with finite,
    /// distinct ticks, the first and the last of them the ends of the range.</summary>
    [Fact]
    public async Task Locate_EndsWhenTheStepIsBelowTheSpacingOfDoubles()
    {
        var ticks = await Task.Run(() => new AutoLocator(8).Locate(1e16, 1e16 + 2), TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.NotEmpty(ticks);
        ticks.AssertAllFinite();
        Assert.Equal(ticks.Length, ticks.Distinct().Count());
        Assert.Equal(1e16, ticks[0]);
        Assert.Equal(1e16 + 2, ticks[^1]);
    }

    /// <summary>The default placement of an axis' ticks for a plot of a given length: the 8 intervals of a plot of
    /// <see cref="AutoLocator.FullDensityLength"/> px or more, fewer for a shorter one, a tick per 30 px held between 2 and 6.</summary>
    [Theory]
    [InlineData(240.0, 8)]
    [InlineData(1000.0, 8)]
    [InlineData(239.0, 6)]
    [InlineData(150.0, 5)]
    [InlineData(100.0, 3)]
    [InlineData(44.0, 2)]
    [InlineData(30.0, 2)]
    [InlineData(0.0, 2)]
    public void ForLength_AsksForTheIntervalsAPlotOfThatLengthHolds(double plotPixels, int intervals)
    {
        var located = AutoLocator.ForLength(plotPixels);

        Assert.Equal(new AutoTickLocatorDto(intervals), ((IJsonDescribed<TickLocatorDto>)located).Describe());
        Assert.Equal(new AutoLocator(intervals).Locate(-0.05, 1.05), located.Locate(-0.05, 1.05));
    }

    /// <summary>The length from which a plot gets the full 8 intervals.</summary>
    [Fact]
    public void FullDensityLength_Is240()
    {
        Assert.Equal(240.0, AutoLocator.FullDensityLength);
    }

    [Theory]
    [InlineData(double.NaN, 1.0)]
    [InlineData(0.0, double.NaN)]
    [InlineData(0.0, double.PositiveInfinity)]
    public async Task Locate_OfARangeThatCannotBeDrawn_EndsWithNoTick(double min, double max)
    {
        var ticks = await Task.Run(() => new AutoLocator(8).Locate(min, max), TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Empty(ticks);
    }

    /// <summary>The locator's ladder written as a loop that grows a list: from the multiple of the step at or above the low end (matplotlib's
    /// edge, <see cref="TickEdge.AtOrAbove"/>, widened by the offset <see cref="SteppedTicks.ScaleOffset"/> names), each tick from
    /// its index. It is the oracle the ticks of <see cref="AutoLocator.Locate"/> are held to, bit for bit.</summary>
    private static double[] Walked(double min, double max, int targetCount)
    {
        double range = max - min;
        if (range <= 0) return [min];
        double rawStep = range / targetCount;
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
        double normalized = rawStep / magnitude;
        double step = normalized switch { < 1.5 => magnitude, < 2.25 => 2 * magnitude, < 3.75 => 2.5 * magnitude, < 7.5 => 5 * magnitude, _ => 10 * magnitude };
        double first = TickEdge.Of(step, SteppedTicks.ScaleOffset(min, max)).AtOrAbove(min) * step;
        double limit = max + step * 0.01;
        var ticks = new List<double>();
        for (long i = 0; ; i++)
        {
            double t = first + i * step;
            if (!(t <= limit)) break;
            double rounded = Math.Round(t, 10);
            if (ticks.Count == 0 || rounded > ticks[^1]) ticks.Add(rounded);
        }
        return ticks.ToArray();
    }

    /// <summary>The ticks are the ones the list-growing loop gives: over ranges that start below, on and above zero, that span a
    /// few units or a few billions, whose step is smaller than the spacing of doubles at their start, and for every target count a
    /// plot length asks for.</summary>
    [Fact]
    public void Locate_GivesTheTicksOfTheListGrowingLoop()
    {
        var random = new Random(20241114);
        var ranges = new List<(double Min, double Max)> { (0, 1), (0, 10), (-5, 5), (-0.05, 1.05), (0.3, 0.31), (1e9, 1e9 + 40), (1e15, 1e15 + 8), (-1e-9, 3e-9), (123456.789, 123499.1), (7, 7.0000001) };
        for (int i = 0; i < 400; i++)
        {
            double start = (random.NextDouble() - 0.5) * Math.Pow(10, random.Next(-6, 9));
            ranges.Add((start, start + random.NextDouble() * Math.Pow(10, random.Next(-6, 9))));
        }

        foreach (int target in new[] { 2, 3, 5, 6, 8 })
            foreach (var (min, max) in ranges)
                Assert.Equal(Walked(min, max, target), new AutoLocator(target).Locate(min, max));
    }

    /// <summary>A draw asks for the ticks of every axis, so a call allocates the array it returns and nothing else: no list that
    /// grows, no copy of it.</summary>
    [Theory]
    [InlineData(0.0, 100.0)]
    [InlineData(-0.05, 1.05)]
    [InlineData(1e9, 1e9 + 40)]
    public void Locate_AllocatesTheArrayItReturnsAndNothingElse(double min, double max)
    {
        var locator = new AutoLocator(8);
        locator.Locate(min, max);   // the first call pays the compiler's tiering

        long before = GC.GetAllocatedBytesForCurrentThread();
        double[] ticks = locator.Locate(min, max);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated <= 24 + 8L * ticks.Length, $"{allocated} bytes for {ticks.Length} ticks (an array of them is {24 + 8L * ticks.Length})");
    }
}
