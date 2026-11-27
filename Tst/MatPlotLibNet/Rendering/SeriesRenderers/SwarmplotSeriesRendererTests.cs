// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Verifies <see cref="Rendering.SeriesRenderers.SwarmplotSeriesRenderer"/> greedy circle-packing output.</summary>
public class SwarmplotSeriesRendererTests
{
    [Fact]
    public void SinglePoint_ReturnsCategoryCenter()
    {
        double[] result = SwarmplotSeriesRenderer.Compute([1.0], 0.1, 2.0);
        Assert.Single(result);
        Assert.Equal(2.0, result[0]);
    }

    [Fact]
    public void EmptyInput_ReturnsEmpty()
    {
        double[] result = SwarmplotSeriesRenderer.Compute([], 0.1, 0.0);
        Assert.Empty(result);
    }

    [Fact]
    public void SmallSet_NoOverlap()
    {
        double[] values = [1.0, 1.5, 2.0, 2.5, 3.0];
        double radius = 0.05;
        double[] result = SwarmplotSeriesRenderer.Compute(values, radius, 0.0);
        Assert.Equal(values.Length, result.Length);
        // Verify no two points overlap in 2D (x,y = result[i], values[i])
        double diameter = radius * 2;
        for (int i = 0; i < result.Length; i++)
        for (int j = i + 1; j < result.Length; j++)
        {
            double dx = result[i] - result[j];
            double dy = values[i] - values[j];
            Assert.True(dx * dx + dy * dy >= diameter * diameter * 0.95, // 5% tolerance for floating point
                $"Points {i} and {j} overlap");
        }
    }

    [Fact]
    public void EqualValues_Exprands()
    {
        // All same Y value — must spread out in X
        double[] values = [1.0, 1.0, 1.0];
        double[] result = SwarmplotSeriesRenderer.Compute(values, 0.1, 0.0);
        Assert.Equal(3, result.Length);
        // Not all at same X
        Assert.False(result.All(x => Math.Abs(x - result[0]) < 1e-9), "All equal values should spread in X");
    }

    // seaborn 0.13.2, categorical.py Beeswarm.beeswarm and add_gutters, read with python on this box:
    //   b = Beeswarm(width=80, orient="x"); xyr = [[100, y, 3.5] for y in ys]; b.beeswarm(xyr)[:, 0]
    //   ys = [10, 11, 12, 12.5, 13, 20, 20, 20.5, 21, 40]  (matplotlib's pixel y, up)
    private static readonly double[] SeabornYs = [10.0, 11.0, 12.0, 12.5, 13.0, 20.0, 20.0, 20.5, 21.0, 40.0];

    private static readonly double[] SeabornXs =
    [
        100.0, 92.72538660821071, 107.04361412912434, 114.37484015210303, 85.68177247908638,
        100.0, 92.65, 107.3312260229787, 85.37538660821072, 100.0,
    ];

    [Fact]
    public void OnPixels_PlacesTheDotsAsSeabornsBeeswarmDoes()
    {
        double[] result = SwarmplotSeriesRenderer.OnPixels(100.0, SeabornYs, 3.5, 40.0);

        Assert.Equal(SeabornXs.Length, result.Length);
        for (int i = 0; i < result.Length; i++)
            Assert.Equal(SeabornXs[i], result[i], 9);
    }

    [Fact]
    public void OnPixels_HoldsTheDotsInsideTheGutters()
    {
        // add_gutters(points, center=100, half width 4): [100, 96, 104, 104, 96, 100, 96, 104, 96, 100]
        double[] result = SwarmplotSeriesRenderer.OnPixels(100.0, SeabornYs, 3.5, 4.0);

        Assert.Equal(new double[] { 100.0, 96.0, 104.0, 104.0, 96.0, 100.0, 96.0, 104.0, 96.0, 100.0 }, result);
    }

    [Fact]
    public void OnPixels_GivesTheAnswerInTheOrderOfTheInput()
    {
        // The same dots with no two at one height (np.argsort leaves a tie in no defined order), the highest first in the input
        // and placed last, as seaborn sorts by height: [100, 92.7254, 107.0436, 114.3748, 85.6818, 100, 92.6530, 107.3312, 85.3512, 100]
        double[] heights = [10.0, 11.0, 12.0, 12.5, 13.0, 20.0, 20.2, 20.5, 21.0, 40.0];
        double[] expected = [100.0, 92.72538660821071, 107.04361412912434, 114.37484015210303, 85.68177247908638, 100.0, 92.65300061249492, 107.3312260229787, 85.35115837919517, 100.0];

        double[] result = SwarmplotSeriesRenderer.OnPixels(100.0, [.. heights.Reverse()], 3.5, 40.0);

        Assert.Equal(expected.Reverse().ToArray(), result);
    }

    [Fact]
    public void OnPixels_LeavesAMissingDotWhereItIsAndLaysTheOthersOutWithoutIt()
    {
        double[] withGap = [10.0, double.NaN, 11.0];

        double[] result = SwarmplotSeriesRenderer.OnPixels(100.0, withGap, 3.5, 40.0);

        Assert.True(double.IsNaN(result[1]));
        Assert.Equal(SwarmplotSeriesRenderer.OnPixels(100.0, [10.0, 11.0], 3.5, 40.0), new[] { result[0], result[2] });
    }

    [Fact]
    public void OnPixels_KeepsALoneDotAtTheMidline()
    {
        Assert.Equal(new double[] { 100.0 }, SwarmplotSeriesRenderer.OnPixels(100.0, [10.0], 3.5, 40.0));
        Assert.Empty(SwarmplotSeriesRenderer.OnPixels(100.0, [], 3.5, 40.0));
    }

    /// <summary>Forty dots at one height: every dot has every earlier dot for a neighbour, so a dot has more than 32 candidates (the
    /// larger sort of the layout). Seaborn's rule leaves the dots in a row, none touching another, the first on the midline.</summary>
    [Fact]
    public void OnPixels_ACrowdAtOneHeight_LeavesNoTwoDotsTouching()
    {
        double[] heights = [.. Enumerable.Repeat(10.0, 40)];

        double[] result = SwarmplotSeriesRenderer.OnPixels(100.0, heights, 3.5, 10_000.0);

        Assert.Equal(100.0, result[0]);
        for (int i = 0; i < result.Length; i++)
        for (int j = i + 1; j < result.Length; j++)
            Assert.True(Math.Abs(result[i] - result[j]) >= 7.0, $"dots {i} and {j} touch");
    }

    [Fact]
    public void OnPixels_SpreadsAGroupOfMoreThan1000DotsByJitter_InsideTheGutters()
    {
        double[] heights = [.. Enumerable.Range(0, 1001).Select(i => i / 10.0)];

        double[] result = SwarmplotSeriesRenderer.OnPixels(100.0, [.. heights, double.NaN], 3.5, 8.0);

        Assert.Equal(1002, result.Length);
        Assert.True(double.IsNaN(result[^1]));
        Assert.All(result[..1001], x => Assert.InRange(x, 92.0, 108.0));
        Assert.Contains(result[..1001], x => Math.Abs(x - 100.0) > 1.0);
    }

    [Fact]
    public void LargeDataset_FallsBackToJitter()
    {
        // N > 1000 triggers fallback
        double[] values = Enumerable.Range(0, 1001).Select(i => (double)i / 100).ToArray();
        double[] result = SwarmplotSeriesRenderer.Compute(values, 0.01, 5.0);
        Assert.Equal(values.Length, result.Length);
        // All results should be finite
        result.AssertAllFinite();
    }
}
