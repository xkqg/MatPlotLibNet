// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;
using MatPlotLibNet;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="StackedAreaSeries"/> construction, data range, serialization, and rendering.</summary>
public class StackedAreaSeriesTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[][] YSets = [[1.0, 2.0, 3.0], [2.0, 3.0, 1.0]];

    /// <summary>Verifies that the Y data range is 0 to the max cumulative sum across all X positions.</summary>
    [Fact]
    public void ComputeDataRange_YIsZeroToCumulativeMax()
    {
        // At x=1: 1+2=3, at x=2: 2+3=5, at x=3: 3+1=4 => max cumulative = 5
        var series = new StackedAreaSeries(X, YSets);
        var range = series.ComputeDataRange(null!);
        Assert.Equal(0.0, range.YMin);
        Assert.Equal(5.0, range.YMax);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  Verifies that rendering produces SVG output containing a polygon element.
        new DrawnToken("Render_ProducesSvg", () => Plt.Create()
            .StackPlot(X, YSets)
            .ToSvg(), "polygon"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Verifies that three layers produce the correct cumulative sums.</summary>
    [Fact]
    public void ThreeLayers_CumulativeIsCorrect()
    {
        double[] x = [1.0, 2.0];
        double[][] ySets = [[1.0, 2.0], [3.0, 4.0], [5.0, 6.0]];
        // At x=1: 1+3+5=9, at x=2: 2+4+6=12 => max cumulative = 12
        var series = new StackedAreaSeries(x, ySets);
        var range = series.ComputeDataRange(null!);
        Assert.Equal(0.0, range.YMin);
        Assert.Equal(12.0, range.YMax);
        Assert.Equal(1.0, range.XMin);
        Assert.Equal(2.0, range.XMax);
    }

    // ── Wave J.1 — branch close-out ──────────────────────────────────────
    /// <summary>Jagged YSets: layer 0 has fewer points than X — exercises the
    /// <c>i &lt; YSets[layer].Length ? YSets[layer][i] : 0.0</c> false arm.
    /// BaselineHelper pads baselines to max(ySets[l].Length) so the loop runs
    /// safely to X.Length; the missing index falls back to 0.0.</summary>
    [Fact]
    public void ComputeDataRange_JaggedYSets_PadsShortLayerWithZero()
    {
        double[] x = [1.0, 2.0, 3.0];
        double[][] ySets = [[5.0, 3.0]]; // layer 0: 2 values; X: 3 → index 2 pads 0.0
        var series = new StackedAreaSeries(x, ySets);
        var range = series.ComputeDataRange(null!);
        // yMax = max(5, 3, 0+0) = 5; yMin = 0 (Zero baseline, all non-negative)
        Assert.Equal(5.0, range.YMax);
        Assert.Equal(0.0, range.YMin);
    }

    [Fact]
    public void StackedAreaSeries_NonZeroBaseline_NoStickyYMin()
    {
        // `Baseline == Zero && yMin >= 0 ? 0 : null`
        // Wiggle baseline → false arm → stickyYMin = null
        var s = new StackedAreaSeries(
            x: [1.0, 2, 3],
            ySets: [[1.0, 2.0, 1.5]])
        {
            Baseline = StackedBaseline.Wiggle
        };
        var dr = s.ComputeDataRange(null!);
        Assert.Null(dr.StickyYMin);
    }

    [Fact]
    public void StackedAreaSeries_PositiveValues_HasStickyZeroBaseline()
    {
        // True arm → stickyYMin = 0
        var s = new StackedAreaSeries(
            x: [1.0, 2, 3],
            ySets: [[1.0, 2.0, 1.5], [0.5, 0.8, 0.6]]);
        var dr = s.ComputeDataRange(null!);
        Assert.Equal(0, dr.StickyYMin);
    }

    /// <summary>StackedAreaSeries.ComputeDataRange — a layer with no drawable value has no top to reach: the range takes
    /// the drawable layer edges only, so an all-NaN layer adds no top of its own (it used to report the sentinel 1, a top
    /// nothing was drawn at). A single Zero-baseline layer's `bot` is the constant 0 (see
    /// <c>StackedBaselineExtensions.ComputeZero</c>), the one edge left, so the range is 0 to 0, as matplotlib's data
    /// limits keep the finite baseline of a fill whose top is missing.</summary>
    [Fact]
    public void StackedAreaSeries_AllNaNLayer_ContributesNoTopOfItsOwn()
    {
        var s = new StackedAreaSeries(
            x: [1.0, 2.0],
            ySets: [[double.NaN, double.NaN]]);
        var dr = s.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0, dr.YMax);
        Assert.Equal(0, dr.YMin);
    }

    /// <summary>StackedAreaSeries — empty-X early-return. Returns the
    /// (0,1,0,1) sentinel range without touching the baseline helper.</summary>
    [Fact]
    public void StackedAreaSeries_EmptyX_ReturnsSentinelRange()
    {
        var s = new StackedAreaSeries([], [[1.0, 2.0]]);
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0, range.XMin);
        Assert.Equal(1, range.XMax);
    }

    /// <summary>StackedAreaSeries — empty YSets early-return (second arm).</summary>
    [Fact]
    public void StackedAreaSeries_EmptyYSets_ReturnsSentinelRange()
    {
        var s = new StackedAreaSeries([1.0, 2.0], []);
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0, range.XMin);
        Assert.Equal(1, range.XMax);
    }

    /// <summary>StackedAreaSeries — negative Y-min branch: the
    /// `Baseline == StackedBaseline.Zero &amp;&amp; yMin &gt;= 0` ternary's false arm
    /// when stacked values include negatives. StickyYMin must be null in that case.</summary>
    [Fact]
    public void StackedAreaSeries_NegativeBottom_StickyYMinIsNull()
    {
        var s = new StackedAreaSeries([0.0, 1.0], [[-1.0, -2.0], [3.0, 4.0]])
        {
            Baseline = StackedBaseline.Zero
        };
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.Null(range.StickyYMin);
    }
}
