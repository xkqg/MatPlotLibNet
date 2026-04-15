// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

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

    /// <summary>Verifies that ticks are within [min, max].</summary>
    [Fact]
    public void Locate_AllTicksWithinRange()
    {
        var locator = new AutoLocator();
        double[] ticks = locator.Locate(0, 10);
        Assert.All(ticks, t => Assert.InRange(t, 0, 10));
    }

    /// <summary>Verifies that at least one tick is returned for a normal range.</summary>
    [Fact]
    public void Locate_NormalRange_ReturnsAtLeastOneTick()
    {
        var locator = new AutoLocator();
        double[] ticks = locator.Locate(0, 10);
        Assert.NotEmpty(ticks);
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

    /// <summary>Verifies that ticks are in ascending order.</summary>
    [Fact]
    public void Locate_ReturnsAscendingOrder()
    {
        var locator = new AutoLocator();
        double[] ticks = locator.Locate(-3.7, 8.4);
        for (int i = 1; i < ticks.Length; i++)
            Assert.True(ticks[i] > ticks[i - 1]);
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
}
