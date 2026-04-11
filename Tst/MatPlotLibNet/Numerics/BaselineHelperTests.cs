// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="BaselineHelper"/> baseline computation for stacked area series.</summary>
public class BaselineHelperTests
{
    private static double[][] TwoLayers => [[1, 2, 3], [4, 5, 6]];

    // ── Zero baseline ──────────────────────────────────────────────────────────

    [Fact]
    public void Zero_AllBaselinesStartAtZero()
    {
        var baselines = BaselineHelper.ComputeBaselines(TwoLayers, StackedBaseline.Zero);
        Assert.Equal([0.0, 0.0, 0.0], baselines[0]);
    }

    [Fact]
    public void Zero_SecondLayerEqualsFirstLayerValues()
    {
        var baselines = BaselineHelper.ComputeBaselines(TwoLayers, StackedBaseline.Zero);
        Assert.Equal([1.0, 2.0, 3.0], baselines[1]);
    }

    [Fact]
    public void Zero_CumulativeMatchesLayerSums()
    {
        double[][] ySets = [[1, 2], [3, 4]];
        var baselines = BaselineHelper.ComputeBaselines(ySets, StackedBaseline.Zero);
        // layer 0 starts at 0; layer 1 starts at ySets[0] = [1, 2]
        Assert.Equal([0.0, 0.0], baselines[0]);
        Assert.Equal([1.0, 2.0], baselines[1]);
    }

    // ── Symmetric baseline ────────────────────────────────────────────────────

    [Fact]
    public void Symmetric_MidpointIsZero()
    {
        // ySets = [[2, 2], [2, 2]] → total = [4, 4] → midpoint = [2, 2] → shifted so midpoint at 0
        double[][] ySets = [[2, 2], [2, 2]];
        var baselines = BaselineHelper.ComputeBaselines(ySets, StackedBaseline.Symmetric);
        // Layer 0 baseline starts at -totalSum/2 = -2 at each point
        Assert.Equal([-2.0, -2.0], baselines[0]);
    }

    [Fact]
    public void Symmetric_TotalHeightPreserved()
    {
        // Total span from baselines[0][i] to baselines[0][i] + sum of all layers should equal total sum
        double[][] ySets = [[1, 2], [3, 4]];
        var baselines = BaselineHelper.ComputeBaselines(ySets, StackedBaseline.Symmetric);
        // total at point 0: 1+3=4, half=2 → layer0 baseline = -2
        // total at point 1: 2+4=6, half=3 → layer0 baseline = -3
        Assert.Equal(-2.0, baselines[0][0], precision: 10);
        Assert.Equal(-3.0, baselines[0][1], precision: 10);
        // layer 1 baseline = layer0 baseline + ySets[0] = -2+1=-1 and -3+2=-1
        Assert.Equal(-1.0, baselines[1][0], precision: 10);
        Assert.Equal(-1.0, baselines[1][1], precision: 10);
    }

    // ── Wiggle baseline ───────────────────────────────────────────────────────

    [Fact]
    public void Wiggle_FirstBaselineIsNegativeHalfSum()
    {
        // baselines[0][j] = -0.5 * totalSum[j]
        double[][] ySets = [[2, 4], [2, 4]];
        var baselines = BaselineHelper.ComputeBaselines(ySets, StackedBaseline.Wiggle);
        double expectedB0 = -0.5 * (2 + 2); // -2 at point 0
        Assert.Equal(expectedB0, baselines[0][0], precision: 10);
    }

    [Fact]
    public void Wiggle_ProducesValidBaselinesShape()
    {
        var baselines = BaselineHelper.ComputeBaselines(TwoLayers, StackedBaseline.Wiggle);
        Assert.Equal(2, baselines.Length);          // one per layer
        Assert.Equal(3, baselines[0].Length);       // one per point
        Assert.Equal(3, baselines[1].Length);
    }

    // ── WeightedWiggle baseline ───────────────────────────────────────────────

    [Fact]
    public void WeightedWiggle_ProducesValidBaselines()
    {
        var baselines = BaselineHelper.ComputeBaselines(TwoLayers, StackedBaseline.WeightedWiggle);
        Assert.Equal(2, baselines.Length);
        Assert.Equal(3, baselines[0].Length);
        Assert.Equal(3, baselines[1].Length);
    }

    [Fact]
    public void WeightedWiggle_PositiveData_FirstBaselineIsNegative()
    {
        // WeightedWiggle shifts the stack so the center of mass sits near y = 0
        // → layer 0 baseline must be negative when all values are positive
        double[][] ySets = [[3, 3, 3], [3, 3, 3]];
        var baselines = BaselineHelper.ComputeBaselines(ySets, StackedBaseline.WeightedWiggle);
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
        // [[2,2],[2,2]] total=4, symmetric baseline starts at -2 → yMin should be -2
        var s = new StackedAreaSeries([0.0, 1.0], [[2.0, 2.0], [2.0, 2.0]]) { Baseline = StackedBaseline.Symmetric };
        var range = s.ComputeDataRange(null!);
        Assert.True(range.YMin < 0, $"Expected YMin < 0 but got {range.YMin}");
    }
}
