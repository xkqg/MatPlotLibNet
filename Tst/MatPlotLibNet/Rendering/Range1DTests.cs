// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;

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

    [Fact]
    public void Merge_ExpandsRunningRange_WhenSeriesPushesBoundsOutward()
    {
        var range = new Range1D(3, 7).Merge(1, 10);
        Assert.Equal(1, range.Lo);
        Assert.Equal(10, range.Hi);
    }

    [Fact]
    public void Merge_DoesNothing_WhenSeriesIsInside()
    {
        var range = new Range1D(0, 20).Merge(5, 15);
        Assert.Equal(0, range.Lo);
        Assert.Equal(20, range.Hi);
    }

    [Fact]
    public void Merge_IgnoresNulls_LeavingRunningRangeUntouched()
    {
        var range = new Range1D(3, 7).Merge(null, null);
        Assert.Equal(3, range.Lo);
        Assert.Equal(7, range.Hi);
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

    [Fact]
    public void ExpandedToNiceBoundsIfAuto_AlreadyNiceData_IsIdempotent()
    {
        // Padded [0, 10] with unpadded [0, 10] — nice bounds round to [0, 10], no widening past padded.
        var range = new Range1D(0, 10)
            .ExpandedToNiceBoundsIfAuto(new Axis(), unpadded: new Range1D(0, 10), hasSticky: false, new AutoLocator());
        Assert.Equal(0, range.Lo);
        Assert.Equal(10, range.Hi);
    }

    [Fact]
    public void ExpandedToNiceBoundsIfAuto_HalfIntegerData_RoundsOutward()
    {
        // Eventplot: unpadded [-0.5, 3.5] → nice [-1, 4]. Padded [-0.7, 3.7] widens OUTWARD.
        var range = new Range1D(-0.7, 3.7)
            .ExpandedToNiceBoundsIfAuto(new Axis(), unpadded: new Range1D(-0.5, 3.5), hasSticky: false, new AutoLocator());
        Assert.Equal(-1, range.Lo);
        Assert.Equal(4, range.Hi);
    }

    [Fact]
    public void ExpandedToNiceBoundsIfAuto_HasSticky_Skipped()
    {
        // BarSeries has sticky edges → nice-bound expansion must not fire at all.
        var range = new Range1D(-0.5, 10.5)
            .ExpandedToNiceBoundsIfAuto(new Axis(), unpadded: new Range1D(0, 10), hasSticky: true, new AutoLocator());
        Assert.Equal(-0.5, range.Lo);
        Assert.Equal(10.5, range.Hi);
    }

    [Fact]
    public void ExpandedToNiceBoundsIfAuto_UserSetAxisMin_Skipped()
    {
        var range = new Range1D(1, 10)
            .ExpandedToNiceBoundsIfAuto(new Axis { Min = 1 }, unpadded: new Range1D(1, 10), hasSticky: false, new AutoLocator());
        Assert.Equal(1, range.Lo);
    }

    [Fact]
    public void ExpandedToNiceBoundsIfAuto_NeverPullsInward()
    {
        // Padded [-5, 15] (from an explicit ctor, not a real padding call) with unpadded [0, 10].
        // Nice bounds of [0,10] round to [0,10] — TIGHTER than padded. The expansion must NOT
        // snap the padded range back to [0, 10]; it should be left alone.
        var range = new Range1D(-5, 15)
            .ExpandedToNiceBoundsIfAuto(new Axis(), unpadded: new Range1D(0, 10), hasSticky: false, new AutoLocator());
        Assert.Equal(-5, range.Lo);
        Assert.Equal(15, range.Hi);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Two-stage Padded — 3D axes renderer uses Padded × 2 (stage-1 margin then
    // view-margin 1/48). These tests document that composition before it is used
    // in ThreeDAxesRenderer.Compute3DDataRanges.
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
        // Mirrors the Bar3D Z-axis pipeline: zMargin=0 (ortho), viewMargin=1/48.
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
}
