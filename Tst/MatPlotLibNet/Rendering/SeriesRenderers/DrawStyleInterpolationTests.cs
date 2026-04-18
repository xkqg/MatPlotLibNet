// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase 4 — full coverage for <see cref="DrawStyleInterpolation"/>
/// (was 5.5%, target 90%). Step expansion logic underpins every step plot variant
/// in the library; uncovered branches here would silently corrupt all step charts.</summary>
public class DrawStyleInterpolationTests
{
    [Fact]
    public void Apply_NullStyle_ReturnsInputUnchanged()
    {
        double[] x = { 1, 2, 3 };
        double[] y = { 4, 5, 6 };
        var result = DrawStyleInterpolation.Apply(x, y, null);
        Assert.Same(x, result.X);
        Assert.Same(y, result.Y);
    }

    [Fact]
    public void Apply_DefaultStyle_ReturnsInputUnchanged()
    {
        double[] x = { 1, 2, 3 };
        double[] y = { 4, 5, 6 };
        var result = DrawStyleInterpolation.Apply(x, y, DrawStyle.Default);
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
        var result = DrawStyleInterpolation.Apply(x, y, style);
        Assert.Same(x, result.X);
        Assert.Same(y, result.Y);
    }

    [Theory]
    [InlineData(DrawStyle.StepsPre)]
    [InlineData(DrawStyle.StepsPost)]
    [InlineData(DrawStyle.StepsMid)]
    public void EmptyInput_ReturnsInputUnchanged(DrawStyle style)
    {
        var result = DrawStyleInterpolation.Apply(Array.Empty<double>(), Array.Empty<double>(), style);
        Assert.Empty(result.X);
        Assert.Empty(result.Y);
    }

    [Fact]
    public void StepsPre_ThreePoints_ProducesExpectedShape()
    {
        // StepsPre: at each subsequent x, first emit (x[i], y[i-1]) then (x[i], y[i]).
        // Input:  x=[0,1,2], y=[0,1,2]
        // Output: (0,0), (1,0), (1,1), (2,1), (2,2)  →  5 points
        double[] x = { 0, 1, 2 };
        double[] y = { 0, 1, 2 };
        var r = DrawStyleInterpolation.Apply(x, y, DrawStyle.StepsPre);
        Assert.Equal(5, r.X.Length);
        Assert.Equal(new[] { 0d, 1, 1, 2, 2 }, r.X);
        Assert.Equal(new[] { 0d, 0, 1, 1, 2 }, r.Y);
    }

    [Fact]
    public void StepsPost_ThreePoints_ProducesExpectedShape()
    {
        // StepsPost: at each x, emit (x[i], y[i]) then (x[i+1], y[i]).
        // Input:  x=[0,1,2], y=[0,1,2]
        // Output: (0,0), (1,0), (1,1), (2,1), (2,2)
        double[] x = { 0, 1, 2 };
        double[] y = { 0, 1, 2 };
        var r = DrawStyleInterpolation.Apply(x, y, DrawStyle.StepsPost);
        Assert.Equal(5, r.X.Length);
        Assert.Equal(new[] { 0d, 1, 1, 2, 2 }, r.X);
        Assert.Equal(new[] { 0d, 0, 1, 1, 2 }, r.Y);
    }

    [Fact]
    public void StepsMid_ThreePoints_PlacesStepAtMidpoint()
    {
        // StepsMid: step transition halfway between consecutive x values.
        // Midpoints between x[0]=0,x[1]=1 → 0.5; between x[1]=1,x[2]=2 → 1.5.
        double[] x = { 0, 1, 2 };
        double[] y = { 0, 1, 2 };
        var r = DrawStyleInterpolation.Apply(x, y, DrawStyle.StepsMid);
        // Expected sequence: (0,0), (0.5,0), (0.5,1), (1,1), (1.5,1), (1.5,2), (2,2)
        Assert.Equal(7, r.X.Length);
        Assert.Equal(new[] { 0d, 0.5, 0.5, 1, 1.5, 1.5, 2 }, r.X);
        Assert.Equal(new[] { 0d, 0,   1,   1, 1,   2,   2 }, r.Y);
    }

    [Fact]
    public void StepsPre_TwoPoints_ProducesThree()
    {
        // Minimum non-trivial input. (n-1)*2 + 1 = 3 output points.
        double[] x = { 0, 10 };
        double[] y = { 5, 15 };
        var r = DrawStyleInterpolation.Apply(x, y, DrawStyle.StepsPre);
        Assert.Equal(3, r.X.Length);
        Assert.Equal(new[] { 0d, 10, 10 }, r.X);
        Assert.Equal(new[] { 5d,  5, 15 }, r.Y);
    }

    [Fact]
    public void StepsMid_NaNInY_PropagatesNaN()
    {
        // StepsMid duplicates Y values; NaN must flow through without crashing.
        double[] x = { 0, 1, 2 };
        double[] y = { 0, double.NaN, 2 };
        var r = DrawStyleInterpolation.Apply(x, y, DrawStyle.StepsMid);
        Assert.True(r.Y.Any(double.IsNaN), "NaN should propagate through interpolation");
    }

    [Fact]
    public void StepsPost_VeryLarge_ProducesCorrectLength()
    {
        // Output length for StepsPost on n points is 2n-1.
        int n = 1000;
        double[] x = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
        double[] y = Enumerable.Range(0, n).Select(i => (double)i * 0.5).ToArray();
        var r = DrawStyleInterpolation.Apply(x, y, DrawStyle.StepsPost);
        Assert.Equal(2 * n - 1, r.X.Length);
        Assert.Equal(2 * n - 1, r.Y.Length);
    }
}
