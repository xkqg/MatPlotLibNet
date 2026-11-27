// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// Unit tests for the <see cref="Range1D"/> pipeline. Each pipeline method is verified in
/// isolation here; the integration of all methods is verified at the renderer level in
/// <see cref="CartesianAxesRendererRangeTests"/>. Splitting the coverage this way keeps each
/// test focused on one responsibility.
/// </summary>
public class Range1DTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Empty / FromAxis / Merge
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Empty_IsInverted_SoFirstMergeOverwritesBothEndpoints()
    {
        var seed = Range1D.Empty;
        Assert.Equal(double.MaxValue, seed.Lo);
        Assert.Equal(double.MinValue, seed.Hi);
        Assert.True(seed.IsEmpty);
    }

    [Fact]
    public void FromAxis_UnboundedAxis_ReturnsEmpty()
    {
        var range = Range1D.FromAxis(new Axis());
        Assert.True(range.IsEmpty);
    }

    [Fact]
    public void FromAxis_BoundedAxis_SeedsFromUserLimits()
    {
        var axis = new Axis { Min = 1.0, Max = 9.0 };
        var range = Range1D.FromAxis(axis);
        Assert.Equal(1.0, range.Lo);
        Assert.Equal(9.0, range.Hi);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // WithExplicitLimits — a set side is exact, an unset side keeps the fold
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void WithExplicitLimits_BothSidesSet_NarrowerThanTheFold_AreExact()
    {
        var range = new AxisAutoscale(new Axis { Min = 1.5, Max = 2.5 }, isX: false, Theme.Default).WithExplicitLimits(new Range1D(1, 3));
        Assert.Equal(new Range1D(1.5, 2.5), range);
    }

    [Fact]
    public void WithExplicitLimits_NoSideSet_KeepsTheFold()
    {
        Assert.Equal(new Range1D(1, 3), new AxisAutoscale(new Axis(), isX: false, Theme.Default).WithExplicitLimits(new Range1D(1, 3)));
    }

    [Fact]
    public void WithExplicitLimits_OnlyMaxSet_KeepsTheFoldedMin()
    {
        Assert.Equal(new Range1D(1, 2), new AxisAutoscale(new Axis { Max = 2 }, isX: false, Theme.Default).WithExplicitLimits(new Range1D(1, 3)));
    }

    [Fact]
    public void WithExplicitLimits_OnlyMinSet_AboveAllTheData_PutsTheMaxOneUnitAbove()
    {
        Assert.Equal(new Range1D(5, 6), new AxisAutoscale(new Axis { Min = 5 }, isX: false, Theme.Default).WithExplicitLimits(new Range1D(1, 3)));
    }

    [Fact]
    public void WithExplicitLimits_OnlyMaxSet_BelowAllTheData_PutsTheMinOneUnitBelow()
    {
        Assert.Equal(new Range1D(-1, 0), new AxisAutoscale(new Axis { Max = 0 }, isX: false, Theme.Default).WithExplicitLimits(new Range1D(1, 3)));
    }

    [Fact]
    public void WithExplicitLimits_OnlyMaxSet_NoData_PutsTheMinOneUnitBelow()
    {
        Assert.Equal(new Range1D(4, 5), new AxisAutoscale(new Axis { Max = 5 }, isX: false, Theme.Default).WithExplicitLimits(Range1D.Empty));
    }

    [Fact]
    public void WithExplicitLimits_BothSetAndInverted_AreKeptAsSet()
    {
        // Both sides are the caller's: nothing to correct.
        Assert.Equal(new Range1D(9, 1), new AxisAutoscale(new Axis { Min = 9, Max = 1 }, isX: false, Theme.Default).WithExplicitLimits(new Range1D(1, 9)));
    }

    [Theory]
    [InlineData(3.0, 7.0, 1.0, 10.0, 1.0, 10.0)]   // the series pushes the bounds outward: the running range expands
    [InlineData(0.0, 20.0, 5.0, 15.0, 0.0, 20.0)]  // the series is inside: nothing changes
    [InlineData(3.0, 7.0, null, null, 3.0, 7.0)]   // a series with no bounds leaves the running range untouched
    public void Merge_ExpandsTheRunningRange_ToTheBoundsOfTheSeries(double lo, double hi, double? seriesLo, double? seriesHi, double expectedLo, double expectedHi)
    {
        var range = new Range1D(lo, hi).Merge(seriesLo, seriesHi);
        Assert.Equal(expectedLo, range.Lo);
        Assert.Equal(expectedHi, range.Hi);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Normalized — degenerate-state guards
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Normalized_Empty_ReturnsZeroToOne()
    {
        var range = Range1D.Empty.Normalized();
        Assert.Equal(0, range.Lo);
        Assert.Equal(1, range.Hi);
    }

    [Fact]
    public void Normalized_Lopsided_ExtendsHiByOne()
    {
        // Series reported Min=5 but not Max → Lo=5, Hi=MinValue.
        var lopsided = new Range1D(5, double.MinValue);
        var normalized = lopsided.Normalized();
        Assert.Equal(5, normalized.Lo);
        Assert.Equal(6, normalized.Hi);
    }

    [Fact]
    public void Normalized_ZeroWidth_CentresWithHalfBreathingRoomEachSide()
    {
        var normalized = new Range1D(7, 7).Normalized();
        Assert.Equal(6.5, normalized.Lo);
        Assert.Equal(7.5, normalized.Hi);
    }

    [Fact]
    public void Normalized_RealRange_IsIdentity()
    {
        var range = new Range1D(1, 10).Normalized();
        Assert.Equal(1, range.Lo);
        Assert.Equal(10, range.Hi);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Padded — 5% margin, honouring user-set Axis.Min / Max
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Padded_5Percent_ExpandsBothEdgesByHalfAUnit()
    {
        var padded = new Range1D(0, 10).Padded(0.05, new Axis());
        Assert.Equal(-0.5, padded.Lo);
        Assert.Equal(10.5, padded.Hi);
    }

    [Fact]
    public void Padded_UserSetMin_LeavesLoUntouched()
    {
        var axis = new Axis { Min = 0.0 };
        var padded = new Range1D(0, 10).Padded(0.05, axis);
        Assert.Equal(0, padded.Lo);
        Assert.Equal(10.5, padded.Hi);
    }

    [Fact]
    public void Padded_ZeroMargin_IsIdentity()
    {
        var padded = new Range1D(0, 10).Padded(0, new Axis());
        Assert.Equal(0, padded.Lo);
        Assert.Equal(10, padded.Hi);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ClampSticky — matplotlib sticky_edges equivalent
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ClampSticky_PaddingPushedPastSticky_SnapsBackToSticky()
    {
        // Unpadded [0, 10] with 5% margin → padded [-0.5, 10.5].
        // BarSeries registers StickyYMin=0. Padded lo=-0.5 < 0 AND unpadded 0 >= 0 → clamp to 0.
        var clamped = new Range1D(-0.5, 10.5)
            .ClampSticky(stickyMin: 0, stickyMax: null, unpadded: new Range1D(0, 10), new Axis());
        Assert.Equal(0, clamped.Lo);
        Assert.Equal(10.5, clamped.Hi);
    }

    [Fact]
    public void ClampSticky_DataExtendsPastSticky_DoesNotClamp()
    {
        // Overlay scenario: one series has sticky 0, another has real data at -3.
        // Unpadded -3 < sticky 0 → DON'T snap back (would clip legitimate data).
        var clamped = new Range1D(-3.5, 10.5)
            .ClampSticky(stickyMin: 0, stickyMax: null, unpadded: new Range1D(-3, 10), new Axis());
        Assert.Equal(-3.5, clamped.Lo);
    }

    [Fact]
    public void ClampSticky_UserSetAxisMin_NeverSnapsBack()
    {
        var clamped = new Range1D(-0.5, 10.5)
            .ClampSticky(stickyMin: 0, stickyMax: null, unpadded: new Range1D(0, 10), new Axis { Min = -0.5 });
        Assert.Equal(-0.5, clamped.Lo);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ExpandedToNiceBoundsIfAuto — the regression-guard path
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>A padded range is widened outward to the nice bounds of its unpadded data, and never pulled inward: nice data stays as it
    /// is, an eventplot's [-0.5, 3.5] padded to [-0.7, 3.7] widens to [-1, 4], a padded range wider than the nice bounds ([-5, 15] over
    /// [0, 10]) is left alone, and a range with a sticky edge (a bar) is not widened at all.</summary>
    [Theory]
    [InlineData(0, 10, 0, 10, false, 0, 10)] // already nice: idempotent
    [InlineData(-0.7, 3.7, -0.5, 3.5, false, -1, 4)] // half-integer data rounds outward
    [InlineData(-5, 15, 0, 10, false, -5, 15)] // never pulled inward
    [InlineData(-0.5, 10.5, 0, 10, true, -0.5, 10.5)] // a sticky edge: skipped
    public void ExpandedToNiceBoundsIfAuto_WidensOutwardOnly(double lo, double hi, double unpaddedLo, double unpaddedHi, bool hasSticky, double expectedLo, double expectedHi)
    {
        var range = new Range1D(lo, hi)
            .ExpandedToNiceBoundsIfAuto(new Axis(), unpadded: new Range1D(unpaddedLo, unpaddedHi), hasSticky, new AutoLocator());

        Assert.Equal(expectedLo, range.Lo);
        Assert.Equal(expectedHi, range.Hi);
    }

    [Fact]
    public void ExpandedToNiceBoundsIfAuto_UserSetAxisMin_Skipped()
    {
        var range = new Range1D(1, 10)
            .ExpandedToNiceBoundsIfAuto(new Axis { Min = 1 }, unpadded: new Range1D(1, 10), hasSticky: false, new AutoLocator());
        Assert.Equal(1, range.Lo);
    }

    // An axis whose scale brings a locator (log decades, symlog, the dates of an X axis) is placed by that locator, not by the
    // linear nice bounds, whether or not the locator was ever set on the axis.

    [Fact]
    public void ExpandedToNiceBoundsIfAuto_ALinearAxis_IsWidened()
    {
        var range = new Range1D(-43.25, 94.25)
            .ExpandedToNiceBoundsIfAuto(new Axis(), unpadded: new Range1D(-37, 88), hasSticky: false, new AutoLocator());
        Assert.Equal(new Range1D(-50, 100), range);
    }

    [Theory]
    [InlineData(AxisScale.Log)]
    [InlineData(AxisScale.SymLog)]
    public void ExpandedToNiceBoundsIfAuto_AnAxisWhoseScaleBringsALocator_IsNotWidened(AxisScale scale)
    {
        var range = new Range1D(-43.25, 94.25)
            .ExpandedToNiceBoundsIfAuto(new Axis { Scale = scale }, unpadded: new Range1D(-37, 88), hasSticky: false, new AutoLocator());
        Assert.Equal(new Range1D(-43.25, 94.25), range);
    }

    [Fact]
    public void ExpandedToNiceBoundsIfAuto_ADateXAxis_IsNotWidened()
    {
        var range = new AxisAutoscale(new Axis { Scale = AxisScale.Date }, isX: true, Theme.Default)
            .ExpandedToNiceBounds(new Range1D(19000.04, 19003.56), new Range1D(19000.2, 19003.4), hasSticky: false, new AutoLocator());
        Assert.Equal(new Range1D(19000.04, 19003.56), range);
    }

    /// <summary>Whether the scale brings a locator is asked without making one: a range worked out every frame allocates
    /// nothing for it.</summary>
    [Fact]
    public void ExpandedToNiceBoundsIfAuto_OfASymlogAxis_AllocatesNothing()
    {
        var axis = new Axis { Scale = AxisScale.SymLog };
        var locator = new AutoLocator();
        var auto = new AxisAutoscale(axis, isX: false, Theme.Default);
        var range = auto.ExpandedToNiceBounds(new Range1D(-43.25, 94.25), new Range1D(-37, 88), false, locator);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
            range = auto.ExpandedToNiceBounds(new Range1D(-43.25, 94.25), new Range1D(-37, 88), false, locator);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.Equal(new Range1D(-43.25, 94.25), range);
    }

    /// <summary>A date scale brings its ticks only on an X axis; the overload that is not told the orientation reads the axis
    /// as a Y axis, which is widened like a linear one.</summary>
    [Fact]
    public void ExpandedToNiceBoundsIfAuto_ADateYAxis_IsWidened()
    {
        var range = new Range1D(19000.04, 19003.56)
            .ExpandedToNiceBoundsIfAuto(new Axis { Scale = AxisScale.Date }, new Range1D(19000.2, 19003.4), hasSticky: false,
                new AutoLocator());
        Assert.Equal(19000, range.Lo);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Two-stage Padded — 3D axes renderer uses Padded × 2 (stage-1 margin then
    // view-margin 1/48). These tests document that composition before it is used
    // in the 3-D axes' ComputeCubeRange (AxesRangeExtensions).
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TwoStagePadded_CumulativeExpansion_BothStagesApply()
    {
        // Verified against matplotlib bar3d xlim comment:
        // data (0,2), margin=0.05 → stage1 (-0.1, 2.1); viewMargin=1/48 → stage2 ≈ (-0.1458, 2.1458).
        var stage1 = new Range1D(0, 2).Padded(0.05, new Axis());
        Assert.Equal(-0.1, stage1.Lo, precision: 10);
        Assert.Equal(2.1, stage1.Hi, precision: 10);

        var stage2 = stage1.Padded(1.0 / 48, new Axis());
        Assert.True(stage2.Lo < stage1.Lo, "stage2 Lo must be further left than stage1 Lo");
        Assert.True(stage2.Hi > stage1.Hi, "stage2 Hi must be further right than stage1 Hi");
        Assert.Equal(-0.1 - 2.2 / 48, stage2.Lo, precision: 10);
        Assert.Equal(2.1 + 2.2 / 48, stage2.Hi, precision: 10);
    }

    [Fact]
    public void TwoStagePadded_UserSetLimits_SkipsBothStages()
    {
        // When the user pins both axis limits the two padding stages must both be no-ops.
        var axis = new Axis { Min = 0.0, Max = 2.0 };
        var result = new Range1D(0, 2).Padded(0.05, axis).Padded(1.0 / 48, axis);
        Assert.Equal(0, result.Lo);
        Assert.Equal(2, result.Hi);
    }

    [Fact]
    public void TwoStagePadded_ClampStickyAfterBothPasses_ZFloor()
    {
        // Mirrors the Bar3D Z-axis pipeline: zMargin=0 (perspective), viewMargin=1/48.
        // zlim comment in ThreeDAxesRenderer: (-0.125, 6.125) before clamp → snaps to (0, 6.125).
        var stage1 = new Range1D(0, 6).Padded(0.0, new Axis());     // no margin → (0, 6)
        var stage2 = stage1.Padded(1.0 / 48, new Axis());           // → (-0.125, 6.125)
        Assert.Equal(-6.0 / 48, stage2.Lo, precision: 10);
        Assert.Equal(6 + 6.0 / 48, stage2.Hi, precision: 10);

        var clamped = stage2.ClampSticky(stickyMin: 0, stickyMax: null,
            unpadded: new Range1D(0, 6), new Axis());
        Assert.Equal(0, clamped.Lo);
        Assert.Equal(6 + 6.0 / 48, clamped.Hi, precision: 10);
    }

    /// <summary>Every arm of matplotlib's <c>transforms._nonsingular</c> (3.11.2, read with python; <c>increasing=True</c>), each row
    /// printed from <c>_nonsingular(vmin, vmax, expander=0.1)</c>: an end that is not finite, both ends 0, both ends near 0, a reversed
    /// range, a range of no width, one narrower than <c>tiny</c> of its larger end, and one wide enough.</summary>
    [Theory]
    [InlineData(double.NaN, 1.0, -0.1, 0.1)]
    [InlineData(0.0, double.PositiveInfinity, -0.1, 0.1)]
    [InlineData(0.0, 0.0, -0.1, 0.1)]
    [InlineData(1e-300, 1e-300, -0.1, 0.1)]
    [InlineData(5.0, 2.0, 2.0, 5.0)]
    [InlineData(5.0, 5.0, 4.5, 5.5)]
    [InlineData(-5.0, -5.0, -5.5, -4.5)]
    [InlineData(2.0, 7.0, 2.0, 7.0)]
    public void NonsingularBy_IsMatplotlibsNonsingular(double lo, double hi, double expectedLo, double expectedHi)
    {
        var opened = new Range1D(lo, hi).NonsingularBy(expander: 0.1, tiny: 1e-15);

        Assert.Equal(new Range1D(expectedLo, expectedHi), opened);
    }

    /// <summary>A span centred on a point runs half its length to each side of it.</summary>
    [Theory]
    [InlineData(10.0, 4.0, 8.0, 12.0)]
    [InlineData(0.0, 0.8, -0.4, 0.4)]
    [InlineData(-3.0, 0.0, -3.0, -3.0)]
    public void Around_RunsHalfTheLengthToEachSideOfTheCentre(double centre, double length, double lo, double hi)
    {
        Assert.Equal(new Range1D(lo, hi), Range1D.Around(centre, length));
    }

    /// <summary>A span centred in a room leaves as much of the room before it as after it, worked from the room's start as matplotlib's
    /// <c>Bbox.anchored(c='C')</c> works it (<c>x0 + (W - w) * 0.5</c>): to the last bit, where the middle of the room less half the
    /// span lands one bit away (67.182 + (425.243 - 324.79) / 2 is 117.40849999999999, (67.182 + 425.243 / 2) - 324.79 / 2 is
    /// 117.40849999999998).</summary>
    [Fact]
    public void CentredIn_SharesTheRoomLeftOver_AsMatplotlibsAnchoredDoes()
    {
        Assert.Equal(new Range1D(115, 135), Range1D.CentredIn(100, 50, 20));

        var span = Range1D.CentredIn(67.182, 425.243, 324.79);

        Assert.Equal(67.182 + (425.243 - 324.79) * 0.5, span.Lo);
        Assert.NotEqual(Range1D.Around(67.182 + 425.243 / 2, 324.79).Lo, span.Lo);
    }

    /// <summary>The range of nothing (both ends at their sentinels) is not lopsided: only a range whose low end was reported and whose high
    /// end was not is.</summary>
    [Fact]
    public void TheRangeOfNothing_IsNotLopsided_ALowEndAlone_Is()
    {
        Assert.False(new Range1D(double.MaxValue, double.MinValue).IsLopsided);
        Assert.True(new Range1D(1, double.MinValue).IsLopsided);
    }
}
