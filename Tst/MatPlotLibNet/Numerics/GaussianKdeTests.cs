// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="GaussianKde"/> bandwidth and evaluation math.</summary>
public class GaussianKdeTests
{
    // --- SilvermanBandwidth ---

    /// <summary>Bandwidth is positive for typical data.</summary>
    [Fact]
    public void SilvermanBandwidth_PositiveResult_ForTypicalData()
    {
        double[] data = [1.0, 2.0, 3.0, 4.0, 5.0];
        double bw = GaussianKde.Bandwidth(data, MatPlotLibNet.Numerics.GaussianKde.SilvermanFactor);
        Assert.True(bw > 0);
    }

    /// <summary>Bandwidth follows 1.06 * σ * n^(-0.2) for known data.</summary>
    [Fact]
    public void SilvermanBandwidth_FollowsFormula()
    {
        // data = {1,2,3,4,5}: mean=3, var=2.5, σ=√2.5, n=5
        double[] data = [1.0, 2.0, 3.0, 4.0, 5.0];
        double sigma = Math.Sqrt(2.5);
        double expected = 1.06 * sigma * Math.Pow(5, -0.2);
        double actual = GaussianKde.Bandwidth(data, MatPlotLibNet.Numerics.GaussianKde.SilvermanFactor);
        Assert.Equal(expected, actual, precision: 10);
    }

    /// <summary>Data without a spread (one value, or every value the same) has no σ to give; the size of its value stands in
    /// for it, R's <c>bw.nrd0</c> rule, so the bandwidth keeps the data's units: 1.06 · |value| · n^(-0.2). A fixed 1.0 (the
    /// rule before) drew a value of 42 as 42 ± 1 and a value of 0.001 as a bump 1000 times wider than the value itself.</summary>
    [Theory]
    [InlineData(new[] { 42.0 }, 42.0)]
    [InlineData(new[] { 5.0, 5.0, 5.0, 5.0 }, 5.0)]
    [InlineData(new[] { -7.0, -7.0 }, 7.0)]
    [InlineData(new[] { 0.001, 0.001, 0.001 }, 0.001)]
    public void SilvermanBandwidth_DataWithoutSpread_TakesTheSizeOfItsValueAsItsSpread(double[] data, double size)
    {
        double bw = GaussianKde.Bandwidth(data, MatPlotLibNet.Numerics.GaussianKde.SilvermanFactor);

        Assert.Equal(1.06 * size * Math.Pow(data.Length, -0.2), bw, 1e-15);
    }

    /// <summary>Data that is all 0 has no size either: 1 stands in for the spread.</summary>
    [Fact]
    public void SilvermanBandwidth_DataThatIsAllZero_Takes1AsItsSpread()
    {
        Assert.Equal(1.06 * Math.Pow(3, -0.2), GaussianKde.Bandwidth([0.0, 0.0, 0.0], GaussianKde.SilvermanFactor), 1e-15);
        Assert.Equal(1.06, GaussianKde.Bandwidth([0.0], GaussianKde.SilvermanFactor), 1e-15);
    }

    /// <summary>Three times 0.1 has no spread, though its mean (0.30000000000000004 / 3) is not 0.1: the deviations from that
    /// mean are a rounding error of 1e-17, not a spread. "No spread" is the smallest value equal to the largest, never a
    /// σ that happens to round to something other than 0 (before, the bandwidth came out near 1.6e-17).</summary>
    [Fact]
    public void SilvermanBandwidth_EqualValuesWhoseMeanRoundsAway_HaveNoSpread()
    {
        double bw = GaussianKde.Bandwidth([0.1, 0.1, 0.1], GaussianKde.SilvermanFactor);

        Assert.Equal(1.06 * 0.1 * Math.Pow(3, -0.2), bw, 1e-15);
    }

    /// <summary>The bandwidth is in the data's units: the same data in units 100 times smaller has a bandwidth 100 times
    /// larger, with and without a spread.</summary>
    [Theory]
    [InlineData(new[] { 1.0, 2.0, 2.5, 4.0, 7.5 })]
    [InlineData(new[] { 3.0, 3.0, 3.0 })]
    [InlineData(new[] { 42.0 })]
    public void SilvermanBandwidth_ScalesWithTheData(double[] data)
    {
        double bw = GaussianKde.Bandwidth(data, MatPlotLibNet.Numerics.GaussianKde.SilvermanFactor);
        double scaled = GaussianKde.Bandwidth([.. data.Select(v => v * 100)], GaussianKde.SilvermanFactor);

        Assert.Equal(1.0, scaled / (bw * 100), 1e-12);
    }

    /// <summary>No data at all has no scale: 1.</summary>
    [Fact]
    public void SilvermanBandwidth_NoData_Is1()
    {
        Assert.Equal(1.0, GaussianKde.Bandwidth([], GaussianKde.SilvermanFactor));
    }

    // --- JointBandwidth: one bandwidth for several groups (ggridges' joint bandwidth) ---

    /// <summary>The joint bandwidth of several groups is the mean of their own Silverman bandwidths, ggridges' rule
    /// (<c>mean(vapply(xs, bw.nrd0))</c> in its <c>StatDensityRidges.setup_params</c>), with the library's one rule for a
    /// group's own bandwidth.</summary>
    [Fact]
    public void JointBandwidth_IsTheMeanOfTheGroupsOwnBandwidths()
    {
        double[] a = [1.0, 2.0, 3.0, 4.0, 5.0], b = [10.0, 12.0, 15.0];

        double joint = GaussianKde.JointBandwidth([a, b]);

        Assert.Equal((GaussianKde.Bandwidth(a, GaussianKde.SilvermanFactor) + GaussianKde.Bandwidth(b, GaussianKde.SilvermanFactor)) / 2, joint, 1e-15);
    }

    /// <summary>The shift between groups is not spread: the same sample at 0, 50 and 100 has the joint bandwidth of the one
    /// sample. Silverman over the three pooled counts the shift as spread and comes out about ten times wider, which smooths
    /// every ridge of a ridgeline into the next (M6: a month of daily highs between 0.9 and 3.4 was drawn from -10 to 15).</summary>
    [Fact]
    public void JointBandwidth_DoesNotCountTheShiftBetweenGroupsAsSpread()
    {
        double[] a = [1.0, 1.5, 2.5, 3.0, 4.5];
        double[][] shifted = [a, [.. a.Select(v => v + 50)], [.. a.Select(v => v + 100)]];

        double joint = GaussianKde.JointBandwidth(shifted);
        double pooled = GaussianKde.Bandwidth([.. shifted.SelectMany(s => s).Order()], GaussianKde.SilvermanFactor);

        Assert.Equal(GaussianKde.Bandwidth(a, GaussianKde.SilvermanFactor), joint, 1e-12);
        Assert.True(pooled > 10 * joint, $"pooled {pooled} against joint {joint}");
    }

    /// <summary>A group without a spread has no width of its own to give: it is left out of the mean. Its own bandwidth (the
    /// size of its value) is no width of the data, and a group of 1000s next to groups that spread by 1 would widen every
    /// ridge to hundreds.</summary>
    [Fact]
    public void JointBandwidth_LeavesOutAGroupWithoutSpread()
    {
        double[] spread = [999.0, 1000.0, 1000.5, 1001.0, 1002.0];

        Assert.Equal(GaussianKde.Bandwidth(spread, GaussianKde.SilvermanFactor), GaussianKde.JointBandwidth([[1000.0, 1000.0, 1000.0], spread]), 1e-15);
    }

    /// <summary>When no group spreads, the groups pooled are the only scale there is: Silverman over them, which takes the
    /// distance between the groups as the spread, or the size of the value when every observation is the same.</summary>
    [Fact]
    public void JointBandwidth_WhenNoGroupSpreads_IsSilvermanOverTheGroupsPooled()
    {
        Assert.Equal(GaussianKde.Bandwidth([5.0, 5.0, 7.0, 7.0, 7.0], GaussianKde.SilvermanFactor), GaussianKde.JointBandwidth([[5.0, 5.0], [7.0, 7.0, 7.0]]), 1e-15);
        Assert.Equal(1.06 * 5 * Math.Pow(5, -0.2), GaussianKde.JointBandwidth([[5.0, 5.0], [5.0, 5.0, 5.0]]), 1e-15);
    }

    /// <summary>The joint bandwidth is in the data's units, with and without a group that spreads.</summary>
    [Fact]
    public void JointBandwidth_ScalesWithTheData()
    {
        double[][] groups = [[1.0, 2.0, 4.0], [3.0, 3.5, 6.0, 9.0], [2.0, 2.0]];
        double[][] flat = [[5.0, 5.0], [7.0, 7.0]];

        foreach (double[][] g in new[] { groups, flat })
        {
            double joint = GaussianKde.JointBandwidth(g);
            double scaled = GaussianKde.JointBandwidth([.. g.Select(s => s.Select(v => v * 100).ToArray())]);
            Assert.Equal(1.0, scaled / (joint * 100), 1e-12);
        }
    }

    // --- Grid and DensityAt: the two halves of Evaluate, for an estimate on a grid of another sample ---

    /// <summary>The grid Evaluate estimates on is <c>Grid</c>: <paramref name="numPoints"/> evenly spaced values from
    /// min − 3h to max + 3h, bit for bit.</summary>
    [Theory]
    [InlineData(100)]
    [InlineData(7)]
    public void Grid_IsTheGridEvaluateEstimatesOn(int numPoints)
    {
        double[] sorted = [-1.5, 0.25, 0.5, 3.0];

        double[] grid = GaussianKde.Grid(sorted[0], sorted[^1], 0.7, numPoints);

        Assert.Equal(GaussianKde.Evaluate(sorted, 0.7, numPoints).X, grid);
        Assert.Equal(-1.5 - 3 * 0.7, grid[0]);
        Assert.Equal(3.0 + 3 * 0.7, grid[^1], 1e-12);
        Assert.Equal(numPoints, grid.Length);
    }

    /// <summary>The grid steps evenly from the first value to the last, each value worked out as the first plus the step times
    /// its index, as it always was: the same bits for the same ends.</summary>
    [Fact]
    public void Grid_IsTheFirstValuePlusTheStepTimesTheIndex()
    {
        const double min = 0.1, max = 7.3, h = 0.37;
        double lo = min - 3 * h, step = (max + 3 * h - lo) / 199;

        double[] grid = GaussianKde.Grid(min, max, h, 200);

        for (int j = 0; j < 200; j++) Assert.Equal(lo + step * j, grid[j]);
    }

    /// <summary>The density Evaluate returns is <c>DensityAt</c> on that grid, bit for bit: one formula.</summary>
    [Fact]
    public void DensityAt_OnEvaluatesGrid_IsEvaluatesDensity()
    {
        double[] sorted = [-1.5, 0.25, 0.5, 3.0];
        var curve = GaussianKde.Evaluate(sorted, 0.7, 100);

        Assert.Equal(curve.Y, GaussianKde.DensityAt(sorted, 0.7, curve.X));
    }

    /// <summary>DensityAt estimates at the points it is given, which need not be a grid of the same sample: one Gaussian
    /// kernel of width h on each observation, averaged. At one observation of 0 with h = 1 that is the standard normal
    /// density.</summary>
    [Fact]
    public void DensityAt_EstimatesAtTheGivenPoints()
    {
        double[] density = GaussianKde.DensityAt([0.0], 1.0, [0.0, 1.0, -2.0]);

        Assert.Equal(0.3989422804014327, density[0], 1e-15);
        Assert.Equal(0.24197072451914337, density[1], 1e-15);
        Assert.Equal(0.05399096651318806, density[2], 1e-15);
    }

    // --- Evaluate ---

    /// <summary>Evaluate returns exactly numPoints evaluation points.</summary>
    [Fact]
    public void Evaluate_ReturnsRequestedNumberOfPoints()
    {
        double[] sorted = [1.0, 2.0, 3.0];
        var (xs, density) = GaussianKde.Evaluate(sorted, 0.5, numPoints: 50);
        Assert.Equal(50, xs.Length);
        Assert.Equal(50, density.Length);
    }

    /// <summary>Default numPoints is 100.</summary>
    [Fact]
    public void Evaluate_DefaultNumPoints_Is100()
    {
        double[] sorted = [1.0, 2.0, 3.0];
        var (xs, _) = GaussianKde.Evaluate(sorted, 0.5, 100);
        Assert.Equal(100, xs.Length);
    }

    /// <summary>Density peak is near the mean for symmetric data.</summary>
    [Fact]
    public void Evaluate_PeakNearMean_ForSymmetricData()
    {
        double[] sorted = [-2.0, -1.0, 0.0, 1.0, 2.0];
        double bw = GaussianKde.Bandwidth(sorted, GaussianKde.SilvermanFactor);
        var (xs, density) = GaussianKde.Evaluate(sorted, bw, 100);
        int peakIdx = Array.IndexOf(density, density.Max());
        double peakX = xs[peakIdx];
        Assert.True(Math.Abs(peakX) < 0.5, $"Peak at {peakX}, expected near 0");
    }

    /// <summary>All density values are non-negative.</summary>
    [Fact]
    public void Evaluate_AllDensitiesNonNegative()
    {
        double[] sorted = [1.0, 2.0, 3.0, 4.0, 5.0];
        double bw = GaussianKde.Bandwidth(sorted, GaussianKde.SilvermanFactor);
        var (_, density) = GaussianKde.Evaluate(sorted, bw, 100);
        Assert.All(density, d => Assert.True(d >= 0));
    }

    /// <summary>Density integrates approximately to 1 (trapezoidal rule).</summary>
    [Fact]
    public void Evaluate_IntegratesApproximatelyToOne()
    {
        double[] sorted = [1.0, 2.0, 2.5, 3.0, 4.0, 5.0, 5.5, 6.0];
        double bw = GaussianKde.Bandwidth(sorted, GaussianKde.SilvermanFactor);
        var (xs, density) = GaussianKde.Evaluate(sorted, bw, numPoints: 500);
        double integral = 0;
        for (int i = 1; i < xs.Length; i++)
            integral += 0.5 * (density[i - 1] + density[i]) * (xs[i] - xs[i - 1]);
        Assert.True(Math.Abs(integral - 1.0) < 0.02, $"Integral = {integral}, expected ~1.0");
    }

    /// <summary>Empty data returns empty arrays without throwing.</summary>
    [Fact]
    public void Evaluate_EmptyData_ReturnsEmpty()
    {
        var (xs, density) = GaussianKde.Evaluate([], 1.0, 100);
        Assert.Empty(xs);
        Assert.Empty(density);
    }

    /// <summary>X range spans [min-3h, max+3h].</summary>
    [Fact]
    public void Evaluate_XRange_CoversDataPlusThreeBandwidths()
    {
        double[] sorted = [2.0, 3.0, 4.0];
        double bw = 0.5;
        var (xs, _) = GaussianKde.Evaluate(sorted, bw, 100);
        Assert.Equal(sorted[0] - 3 * bw, xs[0], precision: 10);
        Assert.Equal(sorted[^1] + 3 * bw, xs[^1], precision: 10);
    }

    // ── DensityCurve ──────────────────────────────────────────────────────────

    /// <summary>The density curve of a sample is estimated from its drawable observations, ascending, with Silverman's
    /// bandwidth: a missing value and an infinity are not observations.</summary>
    [Fact]
    public void DensityCurve_IsTheDensityOfTheDrawableObservationsSorted_WithSilvermansBandwidth()
    {
        double[] sorted = [1.0, 2, 2.5, 3, 7];
        var expected = GaussianKde.Evaluate(sorted, GaussianKde.Bandwidth(sorted, GaussianKde.SilvermanFactor), 100);

        var curve = new[] { 7.0, double.NaN, 2.5, 1, double.PositiveInfinity, 3, 2 }.DensityCurve(null, ThemeLooks.Library.DensityCurveRule)!.Value;

        Assert.Equal(expected.X, curve.X);
        Assert.Equal(expected.Y, curve.Y);
    }

    /// <summary>A bandwidth given is used when it is a positive finite number; any other falls back to Silverman's.</summary>
    [Theory]
    [InlineData(0.4, 0.4)]
    [InlineData(0.0, double.NaN)]
    [InlineData(-1.0, double.NaN)]
    [InlineData(double.PositiveInfinity, double.NaN)]
    [InlineData(double.NaN, double.NaN)]
    public void DensityCurve_TakesTheBandwidthGiven_OnlyWhenItIsAPositiveFiniteNumber(double given, double used)
    {
        double[] sorted = [1.0, 2, 2.5, 3, 7];
        double bandwidth = double.IsNaN(used) ? GaussianKde.Bandwidth(sorted, GaussianKde.SilvermanFactor) : used;

        var curve = sorted.DensityCurve(given, ThemeLooks.Library.DensityCurveRule)!.Value;

        Assert.Equal(GaussianKde.Evaluate(sorted, bandwidth, 100).Y, curve.Y);
    }

    /// <summary>A sample with no drawable observation has no density curve.</summary>
    [Fact]
    public void DensityCurve_OfNothingDrawable_IsNothing()
    {
        Assert.Null(Array.Empty<double>().DensityCurve(null, ThemeLooks.Library.DensityCurveRule));
        Assert.Null(new[] { double.NaN, double.NegativeInfinity }.DensityCurve(null, ThemeLooks.Library.DensityCurveRule));
    }

    /// <summary>The densities of several groups on one grid are each the density of the group alone: the same bandwidth and the
    /// same grid for all of them, in the order of the groups.</summary>
    [Fact]
    public void JointDensities_AreTheDensityOfEachGroupOnTheSharedGrid()
    {
        double[][] groups = [[1.0, 2.0, 2.5, 4.0], [3.0, 3.5, 7.0]];
        double[] grid = GaussianKde.Grid(1.0, 7.0, 0.8, 25);

        double[][] densities = GaussianKde.JointDensities(groups, 0.8, grid);

        Assert.Equal(2, densities.Length);
        Assert.Equal(GaussianKde.DensityAt(groups[0], 0.8, grid), densities[0]);
        Assert.Equal(GaussianKde.DensityAt(groups[1], 0.8, grid), densities[1]);
    }

    /// <summary>No groups have no densities.</summary>
    [Fact]
    public void JointDensities_OfNoGroups_AreNone() =>
        Assert.Empty(GaussianKde.JointDensities([], 1.0, [0.0, 1.0]));
}
