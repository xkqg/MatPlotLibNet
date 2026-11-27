// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Numerics;

public class MonotoneCubicSplineTests
{
    [Fact]
    public void Interpolate_OutputLength_IsCorrect()
    {
        double[] x = [0, 1, 2, 3];
        double[] y = [0, 1, 0, 1];
        var (ox, oy) = MonotoneCubicSpline.Interpolate(x, y, resolution: 5);
        // (n-1)*resolution + 1 = 3*5 + 1 = 16
        Assert.Equal(16, ox.Length);
        Assert.Equal(16, oy.Length);
    }

    [Fact]
    public void Interpolate_PreservesEndpoints()
    {
        double[] x = [0, 1, 2];
        double[] y = [3, 7, 5];
        var (ox, oy) = MonotoneCubicSpline.Interpolate(x, y, resolution: 10);
        Assert.Equal(x[0], ox[0], 1e-9);
        Assert.Equal(y[0], oy[0], 1e-9);
        Assert.Equal(x[^1], ox[^1], 1e-6);
        Assert.Equal(y[^1], oy[^1], 1e-6);
    }

    [Fact]
    public void Interpolate_MonotoneData_NoOvershoot()
    {
        // Strictly increasing data — output should also be strictly increasing
        double[] x = [0, 1, 2, 3, 4];
        double[] y = [0, 1, 2, 3, 4];
        var (_, oy) = MonotoneCubicSpline.Interpolate(x, y, resolution: 20);
        for (int i = 1; i < oy.Length; i++)
            Assert.True(oy[i] >= oy[i - 1] - 1e-9,
                $"Overshoot at index {i}: y[{i}]={oy[i]:F6} < y[{i-1}]={oy[i-1]:F6}");
    }

    [Fact]
    public void Interpolate_SmoothFalse_ReturnsSamePointCount()
    {
        double[] x = [0, 1, 2];
        double[] y = [0, 1, 2];
        // resolution=1 means no subdivision — returns input unchanged
        var (ox, oy) = MonotoneCubicSpline.Interpolate(x, y, resolution: 1);
        Assert.Same(x, ox);
        Assert.Same(y, oy);
    }

    [Fact]
    public void Interpolate_TwoPoints_ReturnsLinearSegment()
    {
        double[] x = [0, 1];
        double[] y = [0, 2];
        var (ox, oy) = MonotoneCubicSpline.Interpolate(x, y, resolution: 4);
        // output length = 1*4 + 1 = 5
        Assert.Equal(5, ox.Length);
        // midpoint should be near (0.5, 1.0)
        Assert.InRange(ox[2], 0.49, 0.51);
        Assert.InRange(oy[2], 0.99, 1.01);
    }

    [Fact]
    public void Interpolate_SinglePoint_ReturnsSameArrays()
    {
        double[] x = [5];
        double[] y = [3];
        var (ox, oy) = MonotoneCubicSpline.Interpolate(x, y);
        Assert.Same(x, ox);
        Assert.Same(y, oy);
    }

    /// <summary>
    /// Covers the Fritsch-Carlson overshoot enforcement branch (sq > 9).
    /// A sharp peak (low → very high → low) creates secant slopes whose squared
    /// alpha+beta exceed 9 at the apex, triggering the tau-scaling step that
    /// caps the tangents to preserve monotonicity.
    /// </summary>
    [Fact]
    public void Interpolate_SharpPeak_TriggersOvershootClipping()
    {
        // A sharp inverted-V peak — slopes around index 2 differ wildly in sign+magnitude
        double[] x = [0, 1, 2, 3, 4];
        double[] y = [0, 1, 1000, 1, 0];
        var (ox, oy) = MonotoneCubicSpline.Interpolate(x, y, resolution: 8);

        // Output should not overshoot the peak
        Assert.True(oy.Max() <= 1000.0 + 1e-6,
            $"Output overshoots input peak (max={oy.Max():F2})");
        // Endpoints preserved
        Assert.Equal(0, ox[0], 1e-9);
        Assert.Equal(0, oy[0], 1e-9);
        Assert.Equal(4, ox[^1], 1e-6);
        Assert.Equal(0, oy[^1], 1e-6);
    }

    /// <summary>
    /// Covers the <c>dx == 0</c> secant-slope branch where adjacent X values are equal.
    /// This forces the secant slope to 0 (rather than dividing by zero), and the
    /// monotonicity step then resets adjacent tangents to 0.
    /// </summary>
    [Fact]
    public void Interpolate_DuplicateXValue_HandlesZeroSecantSlope()
    {
        // x[1] == x[2] forces dx=0 → secant slope d[1] = 0
        double[] x = [0, 1, 1, 2];
        double[] y = [0, 1, 5, 6];
        var (ox, oy) = MonotoneCubicSpline.Interpolate(x, y, resolution: 4);

        // Should not throw or produce NaN/Inf
        oy.AssertAllFinite("output");
        // First and last points preserved
        Assert.Equal(0, oy[0], 1e-9);
        Assert.Equal(6, oy[^1], 1e-6);
    }

    /// <summary>Every knot is a sample of the curve: the sample at <c>SampleOf</c> is the knot itself (a smoothed fill's region is cut
    /// from the curve through the whole run by it, so the fill's top lies on the line).</summary>
    [Theory]
    [InlineData(10)]
    [InlineData(3)]
    [InlineData(1)]
    public void EachKnot_IsTheSampleAtItsIndex(int resolution)
    {
        double[] x = [0, 1, 3, 4, 7], y = [2, 5, 1, 4, 3];

        var curve = MonotoneCubicSpline.Interpolate(x, y, resolution);

        for (int k = 0; k < x.Length; k++)
        {
            int at = MonotoneCubicSpline.SampleOf(k, x.Length, resolution);
            Assert.Equal(x[k], curve.X[at]);
            Assert.Equal(y[k], curve.Y[at]);
        }
        Assert.Equal(curve.X.Length - 1, MonotoneCubicSpline.SampleOf(x.Length - 1, x.Length, resolution));
    }

    [Fact]
    public void ALoneKnot_IsItsOwnSample() => Assert.Equal(0, MonotoneCubicSpline.SampleOf(0, 1, 10));

    [Fact]
    public void EmptyInput_ReturnsInputUnchanged()
    {
        var (ox, oy) = MonotoneCubicSpline.Interpolate(EdgeCaseData.Empty, EdgeCaseData.Empty);
        Assert.Empty(ox);
        Assert.Empty(oy);
    }

    [Fact]
    public void DuplicateX_TriggersZeroSecantSlope_StillProducesOutput()
    {
        // Two consecutive X values are equal — secant slope at that interval is 0.
        // The monotonicity step then forces both adjacent tangents to 0. Output must
        // still have correct length and not contain NaN.
        double[] x = [0, 1, 1, 2];   // duplicate at index 1, 2
        double[] y = [0, 1, 1, 2];
        var (ox, oy) = MonotoneCubicSpline.Interpolate(x, y, resolution: 4);
        Assert.Equal(13, ox.Length);  // (4-1)*4+1
        oy.AssertAllFinite("output");
    }

    [Fact]
    public void SteepThenFlatData_TriggersMonotonicityClampingBranch()
    {
        // The "sq > 9" branch in the algorithm fires when the initial Hermite tangent
        // overshoots — typically at a sharp transition. Construct data with a steep
        // spike then plateau to force it.
        double[] x = [0, 1, 2, 3, 4];
        double[] y = [0, 100, 100, 100, 100];   // steep up, then plateau
        var (_, oy) = MonotoneCubicSpline.Interpolate(x, y, resolution: 10);
        // Monotonicity: output must never exceed the max input value (no overshoot above 100).
        Assert.All(oy, v => Assert.True(v <= 100.0001, $"Overshoot: {v} > 100"));
        Assert.All(oy, v => Assert.True(v >= -0.0001, $"Undershoot: {v} < 0"));
    }

    [Fact]
    public void DescendingY_PreservesMonotonicity()
    {
        // Strictly decreasing Y — output must also be monotonically decreasing.
        double[] x = [0, 1, 2, 3, 4];
        double[] y = [4, 3, 2, 1, 0];
        var (_, oy) = MonotoneCubicSpline.Interpolate(x, y, resolution: 8);
        for (int i = 1; i < oy.Length; i++)
            Assert.True(oy[i] <= oy[i - 1] + 1e-9,
                $"Monotonicity broken at index {i}: {oy[i]:F6} > {oy[i-1]:F6}");
    }

    [Fact]
    public void AllEqualY_FlatLineThroughout()
    {
        // All Y equal → output must be exactly flat.
        double[] x = [0, 1, 2, 3];
        double[] y = [5, 5, 5, 5];
        var (_, oy) = MonotoneCubicSpline.Interpolate(x, y, resolution: 6);
        Assert.All(oy, v => Assert.Equal(5.0, v, 1e-9));
    }
}
