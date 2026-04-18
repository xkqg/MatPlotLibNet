// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Edge-case coverage to push <see cref="MonotoneCubicSpline"/> from 86.6% to 100%.
/// Existing <see cref="MonotoneCubicSplineTests"/> covers the happy path; this file
/// hits the monotonicity-enforcement branch (sq &gt; 9), the duplicate-X branch
/// (dx == 0 → slope = 0), and various degenerate inputs.</summary>
public class MonotoneCubicSplineEdgeCaseTests
{
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
        Assert.All(oy, v => Assert.False(double.IsNaN(v), $"NaN at output value {v}"));
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
