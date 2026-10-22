// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Serialization;
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
    //
    // The expected numbers are matplotlib's: stackplot(baseline='wiggle') in
    // https://github.com/matplotlib/matplotlib/blob/main/lib/matplotlib/stackplot.py
    //     m = number of layers
    //     first_line = -(sum over layers i of y[i] * (m - 0.5 - i)) / m      (i counted from 0, bottom layer first)
    // The first line is the bottom edge of layer 0; each layer stands on the one below it.
    // The old code gave every layer the weight 0.5 (first line = -total / 2), which is the Symmetric baseline.

    // Three layers of different heights and a slope, four points (x = 0..3):
    //     layer 0: 1 2 2 1
    //     layer 1: 1 3 2 2
    //     layer 2: 4 5 8 7        totals per point: 6 10 12 10
    private static double[][] UnequalLayers => [[1, 2, 2, 1], [1, 3, 2, 2], [4, 5, 8, 7]];

    /// <summary>Hand-computed from the formula above. m = 3, so the weights of the layers are 2.5, 1.5 and 0.5.
    /// <code>
    /// point 0: -(2.5*1 + 1.5*1 + 0.5*4) / 3 = -(2.5 + 1.5 + 2.0) / 3 = -6 / 3  = -2
    /// point 1: -(2.5*2 + 1.5*3 + 0.5*5) / 3 = -(5.0 + 4.5 + 2.5) / 3 = -12 / 3 = -4
    /// point 2: -(2.5*2 + 1.5*2 + 0.5*8) / 3 = -(5.0 + 3.0 + 4.0) / 3 = -12 / 3 = -4
    /// point 3: -(2.5*1 + 1.5*2 + 0.5*7) / 3 = -(2.5 + 3.0 + 3.5) / 3 = -9 / 3  = -3
    /// </code>
    /// Layer 1 starts where layer 0 ends (first line + layer 0), layer 2 where layer 1 ends:
    /// <code>
    /// layer 0 starts at [-2, -4, -4, -3]
    /// layer 1 starts at [-2+1, -4+2, -4+2, -3+1]       = [-1, -2, -2, -2]
    /// layer 2 starts at [-1+1, -2+3, -2+2, -2+2]       = [ 0,  1,  0,  0]
    /// </code>
    /// The Symmetric baseline of the same data starts at -total/2 = [-3, -5, -6, -5], so the two differ.</summary>
    [Fact]
    public void Wiggle_UnequalLayers_MatchesMatplotlib()
    {
        var baselines = StackedBaseline.Wiggle.ComputeFor(UnequalLayers);
        AssertBaselines([[-2, -4, -4, -3], [-1, -2, -2, -2], [0, 1, 0, 0]], baselines);
    }

    /// <summary>The symptom of the old rule: on layers of unequal height the wiggle baseline was the symmetric one.</summary>
    [Fact]
    public void Wiggle_UnequalLayers_IsNotTheSymmetricBaseline()
    {
        var wiggle = StackedBaseline.Wiggle.ComputeFor(UnequalLayers);
        var symmetric = StackedBaseline.Symmetric.ComputeFor(UnequalLayers);
        AssertBaselines([[-3, -5, -6, -5], [-2, -3, -4, -4], [-1, 0, -2, -2]], symmetric);
        Assert.NotEqual(symmetric[0], wiggle[0]);
    }

    /// <summary>When every layer has the same height at a point the weights average out: wiggle and symmetric coincide.
    /// This is all the old test of the first baseline proved, since its two layers were equal.</summary>
    [Fact]
    public void Wiggle_EqualLayers_CoincidesWithSymmetric()
    {
        double[][] ySets = [[2, 4], [2, 4]];
        var wiggle = StackedBaseline.Wiggle.ComputeFor(ySets);
        // -(1.5*2 + 0.5*2) / 2 = -2 and -(1.5*4 + 0.5*4) / 2 = -4
        AssertBaselines([[-2, -4], [0, 0]], wiggle);
        AssertBaselines(StackedBaseline.Symmetric.ComputeFor(ySets), wiggle);
    }

    /// <summary>One layer: weight 0.5 of the only layer, -y/2, the same as symmetric (matplotlib: m = 1).</summary>
    [Fact]
    public void Wiggle_SingleLayer_IsCentredOnZero()
    {
        var baselines = StackedBaseline.Wiggle.ComputeFor([[2, 4, 6]]);
        AssertBaselines([[-1, -2, -3]], baselines);
    }

    /// <summary>Two layers, weights 1.5 and 0.5, divided by m = 2:
    /// <code>
    /// point 0: layers (1, 3): -(1.5*1 + 0.5*3) / 2 = -3 / 2 = -1.5
    /// point 1: layers (3, 1): -(1.5*3 + 0.5*1) / 2 = -5 / 2 = -2.5
    /// </code>
    /// Layer 1 starts at [-1.5+1, -2.5+3] = [-0.5, 0.5]. Symmetric would start at -2 and -2.</summary>
    [Fact]
    public void Wiggle_TwoLayers_LowerLayerWeighsMore()
    {
        var baselines = StackedBaseline.Wiggle.ComputeFor([[1, 3], [3, 1]]);
        AssertBaselines([[-1.5, -2.5], [-0.5, 0.5]], baselines);
    }

    /// <summary>A layer shorter than the others counts as 0 past its end, in the first line and in the stack:
    /// <code>
    /// layer 0: 1 2 3     layer 1: 10 (then 0 0)
    /// first line: -(1.5*1 + 0.5*10) / 2 = -3.25;  -(1.5*2) / 2 = -1.5;  -(1.5*3) / 2 = -2.25
    /// layer 1 starts at [-3.25+1, -1.5+2, -2.25+3] = [-2.25, 0.5, 0.75]
    /// </code></summary>
    [Fact]
    public void Wiggle_RaggedLayers_ShortLayerCountsAsZero()
    {
        var baselines = StackedBaseline.Wiggle.ComputeFor([[1, 2, 3], [10]]);
        AssertBaselines([[-3.25, -1.5, -2.25], [-2.25, 0.5, 0.75]], baselines);
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
    //
    // matplotlib: stackplot(baseline='weighted_wiggle'), same file. For every layer i and point j, with
    //     total[j]   = sum over layers of y[i][j];   stack[i][j] = y[0][j] + ... + y[i][j]
    //     increase   = y[i][j] - y[i][j-1]           (y[i][0] for j = 0)
    //     moveUp     = (total[j] - stack[i][j] + 0.5*y[i][j]) / total[j]    (0 when total[j] <= 0; 0.5 at j = 0)
    //     centre[j]  = sum over layers of (moveUp - 0.5) * increase
    //     first_line[j] = (centre[0] + ... + centre[j]) - 0.5 * total[j]
    // The slope term is the running sum of centre: a layer that grows while it lies mostly above the middle
    // pulls the first line up. The old code had no slope term at all (first line = -0.5 * a weighted mean of the layers).

    /// <summary>Hand-computed for <see cref="UnequalLayers"/> (totals 6 10 12 10, stacks 1 2 2 1 / 2 5 4 3 / 6 10 12 10).
    /// <code>
    /// increase   layer 0:  1  1  0 -1      layer 1:  1  2 -1  0      layer 2:  4  1  3 -1
    /// total - stack + 0.5*y (layer 0, 1, 2), then divided by the total of the point:
    ///   layer 0: 5.5  9   11  9.5   ->  0.5 0.9   11/12 0.95    (point 0 is fixed at 0.5)
    ///   layer 1: 4.5  6.5  9  8     ->  0.5 0.65  0.75  0.8
    ///   layer 2: 2    2.5  4  3.5   ->  0.5 0.25  1/3   0.35
    /// (moveUp - 0.5) * increase, summed over the layers:
    ///   point 1: 0.4*1 + 0.15*2 + (-0.25)*1       =  0.4 + 0.3 - 0.25   =  0.45
    ///   point 2: 5/12*0 + 0.25*(-1) + (-1/6)*3    =  0 - 0.25 - 0.5     = -0.75
    ///   point 3: 0.45*(-1) + 0.3*0 + (-0.15)*(-1) = -0.45 + 0 + 0.15    = -0.30
    /// running sum of those: [0, 0.45, -0.30, -0.60]
    /// first line = running sum - total/2 = [0-3, 0.45-5, -0.30-6, -0.60-5] = [-3, -4.55, -6.3, -5.6]
    /// </code>
    /// Layer 1 starts at first line + layer 0 = [-2, -2.55, -4.3, -4.6], layer 2 at + layer 1 = [-1, 0.45, -2.3, -2.6].
    /// Point 0 equals the symmetric baseline (-3, -2, -1); the next points do not, and do not equal the wiggle one either.</summary>
    [Fact]
    public void WeightedWiggle_UnequalLayers_MatchesMatplotlib()
    {
        var baselines = StackedBaseline.WeightedWiggle.ComputeFor(UnequalLayers);
        AssertBaselines([[-3, -4.55, -6.3, -5.6], [-2, -2.55, -4.3, -4.6], [-1, 0.45, -2.3, -2.6]], baselines);
    }

    /// <summary>One layer: moveUp is 0.5 everywhere, the slope term is 0, the first line is -y/2.</summary>
    [Fact]
    public void WeightedWiggle_SingleLayer_IsCentredOnZero()
    {
        var baselines = StackedBaseline.WeightedWiggle.ComputeFor([[2, 4, 6]]);
        AssertBaselines([[-1, -2, -3]], baselines);
    }

    /// <summary>Two layers (1, 3) and (3, 1), both points sum to 4. Layer 0 grows by 2, layer 1 shrinks by 2.
    /// <code>
    /// point 1: total - stack + 0.5*y is 4-3+1.5 = 2.5 for layer 0 and 4-4+0.5 = 0.5 for layer 1
    ///          moveUp = 2.5/4 = 0.625 and 0.5/4 = 0.125, so (moveUp - 0.5) * increase = 0.125*2 + (-0.375)*(-2) = 1
    /// first line = running sum - total/2 = [0 - 2, 1 - 2] = [-2, -1];  layer 1 starts at [-2+1, -1+3] = [-1, 2]
    /// </code></summary>
    [Fact]
    public void WeightedWiggle_TwoLayers_SlopeTermMovesTheBaseline()
    {
        var baselines = StackedBaseline.WeightedWiggle.ComputeFor([[1, 3], [3, 1]]);
        AssertBaselines([[-2, -1], [-1, 2]], baselines);
    }

    /// <summary>A point whose layers sum to 0 has no weights (matplotlib divides by 1/total only where total &gt; 0):
    /// moveUp is 0 there, so the point still contributes (0 - 0.5) * increase to the running sum.
    /// Layers (1,0,2) and (2,0,1): increase (1,-1,2) and (2,-2,1); the running sum is [0, 1.5, 1.5].</summary>
    [Fact]
    public void WeightedWiggle_PointWithZeroTotal_HasNoWeights()
    {
        var baselines = StackedBaseline.WeightedWiggle.ComputeFor([[1, 0, 2], [2, 0, 1]]);
        AssertBaselines([[-1.5, 1.5, 0], [-0.5, 1.5, 2]], baselines);
    }

    /// <summary>A point whose layers sum below 0 has no weights either (the test is total &gt; 0, not total != 0):
    /// layers (1,-2,3) and (2,1,-1) have the totals 3, -1 and 2.</summary>
    [Fact]
    public void WeightedWiggle_PointWithNegativeTotal_HasNoWeights()
    {
        var baselines = StackedBaseline.WeightedWiggle.ComputeFor([[1, -2, 3], [2, 1, -1]]);
        AssertBaselines([[-1.5, 2.5, 1.25], [-0.5, 0.5, 4.25]], baselines);
    }

    // ── A missing value is a gap at its own point, for every strategy ─────────

    /// <summary>Layers (1, NaN, 2, 1) and (1, 1, 2, 2). The first line of the wiggle is per point, so only point 1 is lost:
    /// <code>
    /// point 0: -(1.5*1 + 0.5*1) / 2 = -1      point 2: -(1.5*2 + 0.5*2) / 2 = -2      point 3: -(1.5*1 + 0.5*2) / 2 = -1.25
    /// layer 1 starts at [-1+1, NaN, -2+2, -1.25+1] = [0, NaN, 0, -0.25]
    /// </code></summary>
    [Fact]
    public void Wiggle_MissingValue_LeavesAGapAtItsOwnPointOnly()
    {
        var baselines = StackedBaseline.Wiggle.ComputeFor([[1, double.NaN, 2, 1], [1, 1, 2, 2]]);
        AssertBaselines([[-1, double.NaN, -2, -1.25], [0, double.NaN, 0, -0.25]], baselines);
    }

    /// <summary>The same layers. matplotlib sums the slope terms with <c>cumsum</c>, so there one NaN makes every later point
    /// NaN; here a point whose step cannot be measured adds no slope term:
    /// <code>
    /// point 1: the total is NaN                         -> no slope term, first line NaN
    /// point 2: layer 0 steps from NaN to 2              -> no slope term, first line = 0 - 4/2 = -2
    /// point 3: total 3, layer 0 (1) stacked 1, step -1: moveUp = (3-1+0.5)/3 = 5/6, (5/6 - 0.5) * (-1) = -1/3
    ///          layer 1 (2) stacked 3, step 0: nothing   -> slope term -1/3, first line = -1/3 - 3/2 = -11/6
    /// layer 1 starts at [-1+1, NaN, -2+2, -11/6+1] = [0, NaN, 0, -5/6]
    /// </code></summary>
    [Fact]
    public void WeightedWiggle_MissingValue_LeavesAGapAtItsOwnPointOnly()
    {
        var baselines = StackedBaseline.WeightedWiggle.ComputeFor([[1, double.NaN, 2, 1], [1, 1, 2, 2]]);
        AssertBaselines([[-1, double.NaN, -2, -11.0 / 6], [0, double.NaN, 0, -5.0 / 6]], baselines);
    }

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

    // ── The series and the renderer read the same baselines ───────────────────

    /// <summary>The data range spans the lowest baseline and the highest layer edge of the chosen strategy.
    /// For <see cref="UnequalLayers"/> the wiggle edges run from -4 (layer 0 at points 1 and 2) to 8 (layer 2 starts at
    /// [0, 1, 0, 0] and adds [4, 5, 8, 7]); the weighted wiggle ones from -6.3 to 5.7 (layer 2 starts at
    /// [-1, 0.45, -2.3, -2.6]); the symmetric ones from -6 to 6.</summary>
    [Theory]
    [InlineData(StackedBaseline.Wiggle, -4.0, 8.0)]
    [InlineData(StackedBaseline.WeightedWiggle, -6.3, 5.7)]
    [InlineData(StackedBaseline.Symmetric, -6.0, 6.0)]
    public void StackedAreaSeries_DataRange_SpansTheChosenBaseline(StackedBaseline baseline, double yMin, double yMax)
    {
        var s = new StackedAreaSeries([0.0, 1.0, 2.0, 3.0], UnequalLayers) { Baseline = baseline };
        var range = s.ComputeDataRange(null!);
        Assert.Equal(yMin, range.YMin!.Value, precision: 10);
        Assert.Equal(yMax, range.YMax!.Value, precision: 10);
    }

    /// <summary>The drawn picture changes with the strategy: before, wiggle drew exactly what symmetric drew.</summary>
    [Fact]
    public void StackedAreaSeries_Render_WiggleDrawsADifferentPictureThanSymmetric()
    {
        double[] x = [0, 1, 2, 3];
        string Draw(StackedBaseline baseline) => Plt.Create().StackPlot(x, UnequalLayers, s => s.Baseline = baseline).ToSvg();

        var symmetric = Draw(StackedBaseline.Symmetric);
        var wiggle = Draw(StackedBaseline.Wiggle);
        var weighted = Draw(StackedBaseline.WeightedWiggle);

        Assert.NotEqual(symmetric, wiggle);
        Assert.NotEqual(symmetric, weighted);
        Assert.NotEqual(wiggle, weighted);
    }

    /// <summary>Wiggle and weighted wiggle survive ToJson and FromJson, and draw the same picture afterwards.</summary>
    [Theory]
    [InlineData(StackedBaseline.Wiggle)]
    [InlineData(StackedBaseline.WeightedWiggle)]
    public void StackedAreaSeries_Baseline_RoundTripsThroughJson(StackedBaseline baseline)
    {
        var serializer = new ChartSerializer();
        var figure = Plt.Create().StackPlot([0.0, 1, 2, 3], UnequalLayers, s => s.Baseline = baseline).Build();

        var restored = serializer.FromJson(serializer.ToJson(figure));

        Assert.Equal(baseline, restored.SubPlots[0].Series.OfType<StackedAreaSeries>().Single().Baseline);
        Assert.Equal(figure.ToSvg(), restored.ToSvg());
    }

    // ── The top edge of a layer: one rule for the series and for the stream graph ──────────────────────────────

    /// <summary>A layer's top edge is its baseline plus its own value. For <see cref="UnequalLayers"/> on the wiggle
    /// (baselines [-2, -4, -4, -3], [-1, -2, -2, -2], [0, 1, 0, 0]) the tops are [-1, -2, -2, -2], [0, 1, 0, 0] and
    /// [4, 6, 8, 7]: each layer's top is the baseline of the one above it, and the last layer's is the top of the stream.</summary>
    [Fact]
    public void TopEdges_AreTheBaselinePlusTheLayer()
    {
        var baselines = StackedBaseline.Wiggle.ComputeFor(UnequalLayers);

        var tops = baselines.TopEdgesFor(UnequalLayers, 4);

        AssertBaselines([[-1, -2, -2, -2], [0, 1, 0, 0], [4, 6, 8, 7]], tops);
        AssertBaselines([.. baselines.Skip(1), tops[^1]], tops);
    }

    /// <summary>A layer shorter than the stack counts as 0 past its end, as it does in the baselines: layers (1, 2, 3) and
    /// (10) on the zero baseline stand at [0, 0, 0] and [1, 2, 3], so the tops are [1, 2, 3] and [11, 2, 3].</summary>
    [Fact]
    public void TopEdges_RaggedLayers_ShortLayerCountsAsZeroPastItsEnd()
    {
        double[][] ySets = [[1, 2, 3], [10]];
        var baselines = StackedBaseline.Zero.ComputeFor(ySets, 3);

        var tops = baselines.TopEdgesFor(ySets, 3);

        AssertBaselines([[1, 2, 3], [11, 2, 3]], tops);
    }

    /// <summary>A missing value (NaN) in a layer is a missing top edge at its point, and the layers stacked on it have a
    /// missing baseline there: the gap is the layer's and the ones above it, as the renderer breaks them.</summary>
    [Fact]
    public void TopEdges_MissingValue_IsAMissingEdgeAtItsPointOnly()
    {
        double[][] ySets = [[1, double.NaN, 2], [1, 1, 2]];
        var baselines = StackedBaseline.Zero.ComputeFor(ySets, 3);

        var tops = baselines.TopEdgesFor(ySets, 3);

        AssertBaselines([[1, double.NaN, 2], [2, double.NaN, 4]], tops);
    }

    private static void AssertBaselines(double[][] expected, double[][] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int layer = 0; layer < expected.Length; layer++)
        {
            Assert.Equal(expected[layer].Length, actual[layer].Length);
            for (int j = 0; j < expected[layer].Length; j++)
                Assert.True((double.IsNaN(expected[layer][j]) && double.IsNaN(actual[layer][j]))
                            || Math.Abs(expected[layer][j] - actual[layer][j]) < 1e-10,
                    $"layer {layer}, point {j}: expected {expected[layer][j]} but got {actual[layer][j]}");
        }
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

    /// <summary>No layer, no baseline, for every strategy: the wiggles used to throw IndexOutOfRangeException here
    /// (the first line was written before the layer count was looked at).</summary>
    [Theory]
    [InlineData(StackedBaseline.Zero)]
    [InlineData(StackedBaseline.Symmetric)]
    [InlineData(StackedBaseline.Wiggle)]
    [InlineData(StackedBaseline.WeightedWiggle)]
    public void EmptyYSets_ReturnsEmptyBaselines(StackedBaseline baseline)
    {
        var baselines = baseline.ComputeFor([]);
        Assert.Empty(baselines);
    }

    /// <summary>Nothing to stack: both wiggles stay flat on zero (matplotlib: increase and total are 0, so is the first line).</summary>
    [Theory]
    [InlineData(StackedBaseline.Wiggle)]
    [InlineData(StackedBaseline.WeightedWiggle)]
    public void AllZeroData_BothWigglesAreFlatAtZero(StackedBaseline baseline)
    {
        double[][] ySets = [[0.0, 0.0, 0.0], [0.0, 0.0, 0.0]];
        AssertBaselines([[0, 0, 0], [0, 0, 0]], baseline.ComputeFor(ySets));
    }

    [Theory]
    [InlineData(StackedBaseline.Wiggle)]
    [InlineData(StackedBaseline.WeightedWiggle)]
    public void EmptyInnerArrays_ProducesEmptyPerLayerBaselines(StackedBaseline baseline)
    {
        double[][] ySets = [[], []];
        var baselines = baseline.ComputeFor(ySets);
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
