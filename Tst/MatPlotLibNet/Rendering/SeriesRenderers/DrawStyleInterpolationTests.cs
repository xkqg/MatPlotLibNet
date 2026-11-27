// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase 4 — full coverage for <see cref="DrawStyleInterpolation"/>
/// (was 5.5%, target 90%). Step expansion logic underpins every step plot variant
/// in the library; uncovered branches here would silently corrupt all step charts.</summary>
public class DrawStyleInterpolationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(DrawStyle.Default)]
    public void Apply_NoStyleOrTheDefaultStyle_ReturnsInputUnchanged(DrawStyle? style)
    {
        double[] x = { 1, 2, 3 };
        double[] y = { 4, 5, 6 };
        var result = new XYData(x, y).SteppedBy(style);
        Assert.Same(x, result.X);
        Assert.Same(y, result.Y);
    }

    [Theory]
    [InlineData(DrawStyle.StepsPre)]
    [InlineData(DrawStyle.StepsPost)]
    [InlineData(DrawStyle.StepsMid)]
    public void SinglePoint_ReturnsInputUnchanged(DrawStyle style)
    {
        // x.Length < 2 → bypass interpolation
        double[] x = { 1 };
        double[] y = { 2 };
        var result = new XYData(x, y).SteppedBy(style);
        Assert.Same(x, result.X);
        Assert.Same(y, result.Y);
    }

    [Theory]
    [InlineData(DrawStyle.StepsPre)]
    [InlineData(DrawStyle.StepsPost)]
    [InlineData(DrawStyle.StepsMid)]
    public void EmptyInput_ReturnsInputUnchanged(DrawStyle style)
    {
        var result = new XYData(Array.Empty<double>(), Array.Empty<double>()).SteppedBy(style);
        Assert.Empty(result.X);
        Assert.Empty(result.Y);
    }

    /// <summary>Three points x = y = 0, 1, 2 under each step style. Pre is matplotlib's steps-pre (cbook.pts_to_prestep): the jump falls at
    /// the earlier point, so the line rises at x[i-1] to y[i] and then runs flat to x[i]. Post emits (x[i], y[i]) then (x[i+1], y[i]). Mid
    /// puts the step halfway between consecutive x values: 0.5 and 1.5.</summary>
    [Theory]
    [InlineData(DrawStyle.StepsPre, new[] { 0d, 0, 1, 1, 2 }, new[] { 0d, 1, 1, 2, 2 })]
    [InlineData(DrawStyle.StepsPost, new[] { 0d, 1, 1, 2, 2 }, new[] { 0d, 0, 1, 1, 2 })]
    [InlineData(DrawStyle.StepsMid, new[] { 0d, 0.5, 0.5, 1, 1.5, 1.5, 2 }, new[] { 0d, 0, 1, 1, 1, 2, 2 })]
    public void ThreePoints_ProduceTheExpectedStep(DrawStyle style, double[] expectedX, double[] expectedY)
    {
        double[] x = { 0, 1, 2 };
        double[] y = { 0, 1, 2 };
        var r = new XYData(x, y).SteppedBy(style);
        Assert.Equal(expectedX.Length, r.X.Length);
        Assert.Equal(expectedX, r.X);
        Assert.Equal(expectedY, r.Y);
    }

    [Fact]
    public void StepsPre_TwoPoints_ProducesThree()
    {
        // Minimum non-trivial input. (n-1)*2 + 1 = 3 output points.
        double[] x = { 0, 10 };
        double[] y = { 5, 15 };
        var r = new XYData(x, y).SteppedBy(DrawStyle.StepsPre);
        Assert.Equal(3, r.X.Length);
        Assert.Equal(new[] { 0d, 0, 10 }, r.X);
        Assert.Equal(new[] { 5d, 15, 15 }, r.Y);
    }

    [Fact]
    public void StepsMid_NaNInY_PropagatesNaN()
    {
        // StepsMid duplicates Y values; NaN must flow through without crashing.
        double[] x = { 0, 1, 2 };
        double[] y = { 0, double.NaN, 2 };
        var r = new XYData(x, y).SteppedBy(DrawStyle.StepsMid);
        Assert.True(r.Y.Any(double.IsNaN), "NaN should propagate through interpolation");
    }

    [Fact]
    public void StepsPost_VeryLarge_ProducesCorrectLength()
    {
        // Output length for StepsPost on n points is 2n-1.
        int n = 1000;
        double[] x = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
        double[] y = Enumerable.Range(0, n).Select(i => (double)i * 0.5).ToArray();
        var r = new XYData(x, y).SteppedBy(DrawStyle.StepsPost);
        Assert.Equal(2 * n - 1, r.X.Length);
        Assert.Equal(2 * n - 1, r.Y.Length);
    }

    /// <summary>Every data point keeps its place in the stepped points: the stepped point at <c>IndexOfPoint</c> is the data point
    /// itself, for every style and every point (a fill's region is cut from the stepped top by it).</summary>
    [Theory]
    [InlineData(null)]
    [InlineData(DrawStyle.Default)]
    [InlineData(DrawStyle.StepsPre)]
    [InlineData(DrawStyle.StepsMid)]
    [InlineData(DrawStyle.StepsPost)]
    public void EachDataPoint_StandsAtItsIndexInTheSteppedPoints(DrawStyle? style)
    {
        double[] x = [0, 1, 3, 4, 7], y = [2, 5, 1, 4, 3];

        var stepped = new XYData(x, y).SteppedBy(style);

        for (int i = 0; i < x.Length; i++)
        {
            int at = (style ?? DrawStyle.Default).IndexOfPoint(i, x.Length);
            Assert.Equal(x[i], stepped.X[at]);
            Assert.Equal(y[i], stepped.Y[at]);
        }
        Assert.Equal(stepped.X.Length - 1, (style ?? DrawStyle.Default).IndexOfPoint(x.Length - 1, x.Length));
    }

    [Fact]
    public void ALonePoint_IsNotStepped_AndKeepsItsIndex() =>
        Assert.Equal(0, DrawStyle.StepsMid.IndexOfPoint(0, 1));
}
