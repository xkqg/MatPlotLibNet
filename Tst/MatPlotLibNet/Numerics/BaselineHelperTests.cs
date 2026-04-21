// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="StackedBaselineExtensions.ComputeFor"/> baseline computation
/// for stacked area series (refactored from static <c>BaselineHelper</c>).</summary>
public class BaselineHelperTests
{
    private static double[][] TwoLayers => [[1, 2, 3], [4, 5, 6]];

    // ── Zero baseline ──────────────────────────────────────────────────────────

    [Fact]
    public void Zero_AllBaselinesStartAtZero()
    {
        var baselines = StackedBaseline.Zero.ComputeFor(TwoLayers);
        Assert.Equal([0.0, 0.0, 0.0], baselines[0]);
    }

    [Fact]
    public void Zero_SecondLayerEqualsFirstLayerValues()
    {
        var baselines = StackedBaseline.Zero.ComputeFor(TwoLayers);
        Assert.Equal([1.0, 2.0, 3.0], baselines[1]);
    }

    [Fact]
    public void Zero_CumulativeMatchesLayerSums()
    {
        double[][] ySets = [[1, 2], [3, 4]];
        var baselines = StackedBaseline.Zero.ComputeFor(ySets);
        Assert.Equal([0.0, 0.0], baselines[0]);
        Assert.Equal([1.0, 2.0], baselines[1]);
    }

    // ── Symmetric baseline ────────────────────────────────────────────────────

    [Fact]
    public void Symmetric_MidpointIsZero()
    {
        double[][] ySets = [[2, 2], [2, 2]];
        var baselines = StackedBaseline.Symmetric.ComputeFor(ySets);
        Assert.Equal([-2.0, -2.0], baselines[0]);
    }

    [Fact]
    public void Symmetric_TotalHeightPreserved()
    {
        double[][] ySets = [[1, 2], [3, 4]];
        var baselines = StackedBaseline.Symmetric.ComputeFor(ySets);
        Assert.Equal(-2.0, baselines[0][0], precision: 10);
        Assert.Equal(-3.0, baselines[0][1], precision: 10);
        Assert.Equal(-1.0, baselines[1][0], precision: 10);
        Assert.Equal(-1.0, baselines[1][1], precision: 10);
    }

    // ── Wiggle baseline ───────────────────────────────────────────────────────

    [Fact]
    public void Wiggle_FirstBaselineIsNegativeHalfSum()
    {
        double[][] ySets = [[2, 4], [2, 4]];
        var baselines = StackedBaseline.Wiggle.ComputeFor(ySets);
        double expectedB0 = -0.5 * (2 + 2);
        Assert.Equal(expectedB0, baselines[0][0], precision: 10);
    }

    [Fact]
    public void Wiggle_ProducesValidBaselinesShape()
    {
        var baselines = StackedBaseline.Wiggle.ComputeFor(TwoLayers);
        Assert.Equal(2, baselines.Length);
        Assert.Equal(3, baselines[0].Length);
        Assert.Equal(3, baselines[1].Length);
    }

    // ── WeightedWiggle baseline ───────────────────────────────────────────────

    [Fact]
    public void WeightedWiggle_ProducesValidBaselines()
    {
        var baselines = StackedBaseline.WeightedWiggle.ComputeFor(TwoLayers);
        Assert.Equal(2, baselines.Length);
        Assert.Equal(3, baselines[0].Length);
        Assert.Equal(3, baselines[1].Length);
    }

    [Fact]
    public void WeightedWiggle_PositiveData_FirstBaselineIsNegative()
    {
        double[][] ySets = [[3, 3, 3], [3, 3, 3]];
        var baselines = StackedBaseline.WeightedWiggle.ComputeFor(ySets);
        Assert.True(baselines[0][0] < 0, $"Expected baselines[0][0] < 0 but got {baselines[0][0]}");
    }

    // ── StackedAreaSeries model ───────────────────────────────────────────────

    [Fact]
    public void StackedAreaSeries_Baseline_DefaultsToZero()
    {
        var s = new StackedAreaSeries([1, 2], [[1, 2]]);
        Assert.Equal(StackedBaseline.Zero, s.Baseline);
    }

    [Fact]
    public void StackedAreaSeries_Baseline_CanBeSet()
    {
        var s = new StackedAreaSeries([1, 2], [[1, 2]]) { Baseline = StackedBaseline.Symmetric };
        Assert.Equal(StackedBaseline.Symmetric, s.Baseline);
    }

    [Fact]
    public void StackedAreaSeries_DataRange_ZeroStartsAtZero()
    {
        var s = new StackedAreaSeries([1.0, 2.0], [[1.0, 2.0], [3.0, 4.0]]) { Baseline = StackedBaseline.Zero };
        var range = s.ComputeDataRange(null!);
        Assert.Equal(0.0, range.YMin);
    }

    [Fact]
    public void StackedAreaSeries_DataRange_SymmetricGoesNegative()
    {
        var s = new StackedAreaSeries([0.0, 1.0], [[2.0, 2.0], [2.0, 2.0]]) { Baseline = StackedBaseline.Symmetric };
        var range = s.ComputeDataRange(null!);
        Assert.True(range.YMin < 0, $"Expected YMin < 0 but got {range.YMin}");
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(StackedBaseline.Zero)]
    [InlineData(StackedBaseline.Symmetric)]
    public void EmptyYSets_ReturnsEmptyBaselines(StackedBaseline baseline)
    {
        var baselines = baseline.ComputeFor([]);
        Assert.Empty(baselines);
    }

    [Fact]
    public void WeightedWiggle_AllZeroData_FallsBackToWiggle()
    {
        double[][] ySets = [[0.0, 0.0, 0.0], [0.0, 0.0, 0.0]];
        var ww = StackedBaseline.WeightedWiggle.ComputeFor(ySets);
        var w  = StackedBaseline.Wiggle.ComputeFor(ySets);
        Assert.Equal(w[0], ww[0]);
        Assert.Equal(w[1], ww[1]);
    }

    [Fact]
    public void EmptyInnerArrays_ProducesEmptyPerLayerBaselines()
    {
        double[][] ySets = [[], []];
        var baselines = StackedBaseline.WeightedWiggle.ComputeFor(ySets);
        Assert.Equal(2, baselines.Length);
        Assert.Empty(baselines[0]);
        Assert.Empty(baselines[1]);
    }

    [Fact]
    public void RaggedLayers_OutOfBoundsValuesTreatedAsZero()
    {
        double[][] ySets = [[1, 2, 3], [10]];
        var baselines = StackedBaseline.Zero.ComputeFor(ySets);
        Assert.Equal(2.0, baselines[1][1]);
        Assert.Equal(3.0, baselines[1][2]);
    }

    // ── J.0.c — extension form consistent across all four strategies ──────────

    [Theory]
    [InlineData(StackedBaseline.Zero)]
    [InlineData(StackedBaseline.Symmetric)]
    [InlineData(StackedBaseline.Wiggle)]
    [InlineData(StackedBaseline.WeightedWiggle)]
    public void ComputeFor_ExtensionDeterministic(StackedBaseline baseline)
    {
        var first  = baseline.ComputeFor(TwoLayers);
        var second = baseline.ComputeFor(TwoLayers);
        Assert.Equal(first, second);
    }
}
