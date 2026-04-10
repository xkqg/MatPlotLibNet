// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="BeeswarmLayout"/> greedy circle-packing output.</summary>
public class BeeswarmLayoutTests
{
    [Fact]
    public void SinglePoint_ReturnsCategoryCenter()
    {
        double[] result = BeeswarmLayout.Compute([1.0], 0.1, 2.0);
        Assert.Single(result);
        Assert.Equal(2.0, result[0]);
    }

    [Fact]
    public void EmptyInput_ReturnsEmpty()
    {
        double[] result = BeeswarmLayout.Compute([], 0.1, 0.0);
        Assert.Empty(result);
    }

    [Fact]
    public void SmallSet_NoOverlap()
    {
        double[] values = [1.0, 1.5, 2.0, 2.5, 3.0];
        double radius = 0.05;
        double[] result = BeeswarmLayout.Compute(values, radius, 0.0);
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
        double[] result = BeeswarmLayout.Compute(values, 0.1, 0.0);
        Assert.Equal(3, result.Length);
        // Not all at same X
        Assert.False(result.All(x => Math.Abs(x - result[0]) < 1e-9), "All equal values should spread in X");
    }

    [Fact]
    public void LargeDataset_FallsBackToJitter()
    {
        // N > 1000 triggers fallback
        double[] values = Enumerable.Range(0, 1001).Select(i => (double)i / 100).ToArray();
        double[] result = BeeswarmLayout.Compute(values, 0.01, 5.0);
        Assert.Equal(values.Length, result.Length);
        // All results should be finite
        Assert.All(result, x => Assert.True(double.IsFinite(x)));
    }
}
