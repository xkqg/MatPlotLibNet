// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Downsampling;

namespace MatPlotLibNet.Tests.Rendering.Downsampling;

/// <summary>The point budget of a line (<c>MaxDisplayPoints</c>) is applied in one place, <c>XYData.AtMost</c>: the samples as they are
/// while they are within the budget or there is none, else the largest-triangle-three-buckets reduction to it. A plain line's cull and a
/// monotonic line's slice each end in it, where each typed the same three lines.</summary>
public class XYDataBudgetTests
{
    private static XYData Samples(int count) =>
        new([.. Enumerable.Range(0, count).Select(i => (double)i)], [.. Enumerable.Range(0, count).Select(i => Math.Sin(i * 0.1))]);

    [Fact]
    public void WithNoBudget_TheSamplesAreAsTheyAre()
    {
        var data = Samples(500);

        var kept = data.AtMost(null);

        Assert.Same(data.X, kept.X);
        Assert.Same(data.Y, kept.Y);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(501)]
    public void WithinTheBudget_TheSamplesAreAsTheyAre(int budget)
    {
        var data = Samples(500);

        var kept = data.AtMost(budget);

        Assert.Same(data.X, kept.X);
        Assert.Same(data.Y, kept.Y);
    }

    [Fact]
    public void OverTheBudget_TheSamplesAreReducedToItAsLttbReducesThem()
    {
        var data = Samples(500);

        var kept = data.AtMost(100);
        var expected = new LttbDownsampler().Downsample(data.X, data.Y, 100);

        Assert.Equal(100, kept.X.Length);
        Assert.Equal(expected.X, kept.X);
        Assert.Equal(expected.Y, kept.Y);
    }
}
