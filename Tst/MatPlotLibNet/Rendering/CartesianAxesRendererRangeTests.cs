// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// Integration guards for <see cref="CartesianAxesRenderer.ComputeDataRanges"/> — proves
/// the renderer correctly wires the <see cref="Range1D"/> pipeline (aggregate → normalize →
/// pad → clamp sticky → nice-bound). Per-step semantics are unit-tested in
/// <see cref="Range1DTests"/>; this file only asserts the end-to-end integration that a
/// unit test cannot reach: shared-axis chain walking, sticky edges registered by real
/// series (<c>BarSeries</c>, etc.), the <see cref="AxesRangeExtensions.SnapshotContributions"/>
/// single-pass optimisation, and the secondary-axis range computation. A subtle ordering bug
/// in April 2026 fed the already-padded range into the nice-bound expander, bloating
/// <c>[0,10]</c> data out to <c>[-2,12]</c> on every auto-ranged plot — the first test below
/// is the direct regression guard for that incident.
/// </summary>
public class CartesianAxesRendererRangeTests
{
    private static CartesianAxesRenderer BuildRenderer(Axes axes)
    {
        var plotArea = new Rect(80, 60, 700, 400);
        var ctx = new SvgRenderContext();
        var theme = Theme.MatplotlibV2;
        return new CartesianAxesRenderer(axes, plotArea, ctx, theme);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Nice-bound expansion must not bloat ranges that are already on integers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Direct regression guard: data <c>[0, 10]</c> on both axes with the default 5%
    /// theme margin must stay inside <c>[-0.6, 10.6]</c>. The buggy code produced
    /// <c>[-2, 12]</c> on every chart because it ran nice-bound rounding on the
    /// already-padded range.
    /// </summary>
    [Fact]
    public void NiceBoundExpansion_IntegerData_DoesNotBloatBeyondPadding()
    {
        var axes = new Axes();
        axes.Plot([0.0, 2.5, 5.0, 7.5, 10.0], [0.0, 0.25, 0.5, 0.75, 1.0]);
        var range = BuildRenderer(axes).ComputeDataRanges();

        Assert.InRange(range.XMin, -0.6, 0.0);
        Assert.InRange(range.XMax, 10.0, 10.6);
        Assert.InRange(range.YMin, -0.06, 0.0);
        Assert.InRange(range.YMax, 1.0, 1.06);
    }

    // Unit-level half-integer rounding is covered by
    // Range1DTests.ExpandedToNiceBoundsIfAuto_HalfIntegerData_RoundsOutward — no need
    // to duplicate it as a renderer-level integration test.

    /// <summary>
    /// Sticky edges (registered by <c>BarSeries</c>, <c>HistogramSeries</c>, etc.) must
    /// suppress nice-bound expansion entirely — the bar baseline at <c>y=0</c> must
    /// touch the bottom spine exactly, not float above it because the expansion
    /// rounded downward.
    /// </summary>
    [Fact]
    public void NiceBoundExpansion_SkippedWhenSeriesRegistersStickyEdges()
    {
        var axes = new Axes();
        axes.Bar(["A", "B", "C"], [3.0, 7.0, 5.0]);
        var range = BuildRenderer(axes).ComputeDataRanges();

        Assert.Equal(0.0, range.YMin);
    }

    /// <summary>
    /// A user-supplied axis limit wins over padding and nice-bound rounding. The branch
    /// gates all three passes on <c>Axis.Min.HasValue</c> / <c>Axis.Max.HasValue</c>;
    /// this test proves that user-set limits pass through ComputeDataRanges unchanged
    /// provided the data fits inside them (the seed values from the axis become the
    /// floor/ceiling of the reduction). Data outside the limits would still widen the
    /// computed range — that is a separate concern handled by clip/transform later.
    /// </summary>
    [Fact]
    public void NiceBoundExpansion_UserSetLimits_AreNotPaddedOrRounded()
    {
        var axes = new Axes();
        axes.Plot([3.0, 7.0], [3.0, 7.0]);
        axes.XAxis.Min = 1.0;
        axes.XAxis.Max = 9.0;
        var range = BuildRenderer(axes).ComputeDataRanges();

        Assert.Equal(1.0, range.XMin);
        Assert.Equal(9.0, range.XMax);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Shared-axis aggregation (sharex= / sharey= chain walk)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// When <c>axes.ShareXWith</c> is set, the computed X range must be the <em>union</em>
    /// of this axes' data AND every linked axes' data. Previously only verified by a
    /// weak <c>svg.Contains("0")</c> smoke test in <see cref="SharedAxesRenderTests"/>.
    /// </summary>
    [Fact]
    public void ShareXWith_AggregatesXRangeFromLinkedAxes()
    {
        var ax1 = new Axes();
        ax1.Plot([0.0, 10.0], [1.0, 2.0]);
        var ax2 = new Axes();
        ax2.Plot([5.0, 15.0], [3.0, 4.0]);
        ax2.ShareXWith = ax1;

        var range = BuildRenderer(ax2).ComputeDataRanges();

        Assert.True(range.XMin <= 0.0, $"XMin should be <= 0 (from ax1), got {range.XMin}");
        Assert.True(range.XMax >= 15.0, $"XMax should be >= 15 (from ax2), got {range.XMax}");
    }

    /// <summary>
    /// Y-axis sharing mirrors X-axis sharing — the union of Y ranges across the
    /// chain must propagate to the sharing axes.
    /// </summary>
    [Fact]
    public void ShareYWith_AggregatesYRangeFromLinkedAxes()
    {
        var ax1 = new Axes();
        ax1.Plot([1.0, 2.0], [0.0, 100.0]);
        var ax2 = new Axes();
        ax2.Plot([1.0, 2.0], [50.0, 200.0]);
        ax2.ShareYWith = ax1;

        var range = BuildRenderer(ax2).ComputeDataRanges();

        Assert.True(range.YMin <= 0.0, $"YMin should be <= 0 (from ax1), got {range.YMin}");
        Assert.True(range.YMax >= 200.0, $"YMax should be >= 200 (from ax2), got {range.YMax}");
    }

    /// <summary>
    /// A cyclic share chain (A → B → A) must not send <see cref="CartesianAxesRenderer.ComputeDataRanges"/>
    /// into an infinite loop. The engine uses a <c>HashSet&lt;Axes&gt;</c> visited guard;
    /// this test exercises it.
    /// </summary>
    [Fact]
    public async Task ShareXWith_CyclicChain_DoesNotInfiniteLoop()
    {
        var ax1 = new Axes();
        ax1.Plot([0.0, 5.0], [0.0, 1.0]);
        var ax2 = new Axes();
        ax2.Plot([10.0, 20.0], [2.0, 3.0]);
        ax1.ShareXWith = ax2;
        ax2.ShareXWith = ax1;

        // Race ComputeDataRanges against a 2-second timeout — a regression would hang the
        // method and the delay task would win, failing the assertion.
        var work = Task.Run(() => BuildRenderer(ax2).ComputeDataRanges());
        var timeout = Task.Delay(TimeSpan.FromSeconds(2));
        var finished = await Task.WhenAny(work, timeout);
        Assert.Same(work, finished);

        var range = await work;
        Assert.True(range.XMin <= 0.0 && range.XMax >= 20.0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Secondary axis (twinX / twinY) — independent Y scale rendered on the right edge
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>ComputeSecondaryDataRanges</c> aggregates the secondary (TwinX) series range
    /// using the primary axes' X range as the shared X extent. The Y range is
    /// computed independently from the secondary series' own values.
    /// </summary>
    [Fact]
    public void SecondaryYAxis_ComputesIndependentYRange()
    {
        var axes = new Axes();
        axes.Plot([0.0, 10.0], [0.0, 1.0]);          // primary series, Y in [0, 1]
        axes.PlotSecondary([0.0, 10.0], [100.0, 500.0]);  // twin-X series, Y in [100, 500]

        var renderer = BuildRenderer(axes);
        var primaryRange = renderer.ComputeDataRanges();
        var secRange     = renderer.ComputeSecondaryDataRanges(primaryRange.XMin, primaryRange.XMax);

        // Primary Y is in the [0, 1] band — should be nowhere near 100+
        Assert.True(primaryRange.YMax <= 10.0);
        // Secondary Y must cover the [100, 500] range
        Assert.True(secRange.YMin <= 100.0, $"Secondary YMin should be <= 100, got {secRange.YMin}");
        Assert.True(secRange.YMax >= 500.0, $"Secondary YMax should be >= 500, got {secRange.YMax}");
        // Secondary X mirrors primary X (shared bottom axis)
        Assert.Equal(primaryRange.XMin, secRange.XMin);
        Assert.Equal(primaryRange.XMax, secRange.XMax);
    }
}
