// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

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
        Assert.All(oy, v => Assert.True(double.IsFinite(v), $"Non-finite value {v}"));
        // First and last points preserved
        Assert.Equal(0, oy[0], 1e-9);
        Assert.Equal(6, oy[^1], 1e-6);
    }

    [Fact]
    public void LineSeries_SmoothFalse_Default()
    {
        var s = new LineSeries([0, 1, 2], [0, 1, 0]);
        Assert.False(s.Smooth);
        Assert.Equal(10, s.SmoothResolution);
    }

    [Fact]
    public void LineSeries_Smooth_RoundTripsJson()
    {
        var s = new LineSeries([0.0, 1.0, 2.0], [0.0, 1.0, 0.0])
        {
            Smooth = true,
            SmoothResolution = 5
        };
        var dto = s.ToSeriesDto();
        Assert.Equal(true, dto.Smooth);
        Assert.Equal(5, dto.SmoothResolution);
    }

    [Fact]
    public void LineSeries_SmoothFalse_DtoOmitsFields()
    {
        var s = new LineSeries([0.0, 1.0], [0.0, 1.0]);
        var dto = s.ToSeriesDto();
        Assert.Null(dto.Smooth);
        Assert.Null(dto.SmoothResolution);
    }
}
