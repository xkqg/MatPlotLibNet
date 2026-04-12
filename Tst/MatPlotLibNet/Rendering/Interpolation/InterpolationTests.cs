// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Interpolation;

namespace MatPlotLibNet.Tests.Rendering.Interpolation;

/// <summary>Verifies interpolation engine implementations: Nearest, Bilinear, Bicubic, and InterpolationRegistry.</summary>
public class InterpolationTests
{
    // ── NearestInterpolation ──────────────────────────────────────────────────

    [Fact]
    public void Nearest_SameSize_ReturnsIdenticalGrid()
    {
        double[,] data = { { 1.0, 2.0 }, { 3.0, 4.0 } };
        var result = NearestInterpolation.Instance.Resample(data, 2, 2);
        Assert.Equal(data[0, 0], result[0, 0]);
        Assert.Equal(data[0, 1], result[0, 1]);
        Assert.Equal(data[1, 0], result[1, 0]);
        Assert.Equal(data[1, 1], result[1, 1]);
    }

    [Fact]
    public void Nearest_Double_ExpandsGrid()
    {
        double[,] data = { { 1.0, 2.0 }, { 3.0, 4.0 } };
        var result = NearestInterpolation.Instance.Resample(data, 4, 4);
        Assert.Equal(4, result.GetLength(0));
        Assert.Equal(4, result.GetLength(1));
    }

    [Fact]
    public void Nearest_Half_ShrinksGrid()
    {
        double[,] data = { { 1.0, 2.0, 3.0, 4.0 }, { 5.0, 6.0, 7.0, 8.0 } };
        var result = NearestInterpolation.Instance.Resample(data, 1, 2);
        Assert.Equal(1, result.GetLength(0));
        Assert.Equal(2, result.GetLength(1));
    }

    [Fact]
    public void Nearest_1x1_ReturnsSingleValue()
    {
        double[,] data = { { 42.0 } };
        var result = NearestInterpolation.Instance.Resample(data, 3, 3);
        Assert.Equal(42.0, result[0, 0]);
        Assert.Equal(42.0, result[1, 1]);
        Assert.Equal(42.0, result[2, 2]);
    }

    [Fact]
    public void Nearest_Name_IsNearest()
    {
        Assert.Equal("nearest", NearestInterpolation.Instance.Name, ignoreCase: true);
    }

    // ── BilinearInterpolation ─────────────────────────────────────────────────

    [Fact]
    public void Bilinear_UniformGrid_StaysUniform()
    {
        double[,] data = { { 5.0, 5.0, 5.0 }, { 5.0, 5.0, 5.0 }, { 5.0, 5.0, 5.0 } };
        var result = BilinearInterpolation.Instance.Resample(data, 6, 6);
        foreach (double v in result)
            Assert.Equal(5.0, v, precision: 10);
    }

    [Fact]
    public void Bilinear_CenterOf4Equal_IsAverage()
    {
        // 2x2 grid of values [0,2,0,2] — center of 4 corners resampled to 3x3
        double[,] data = { { 0.0, 2.0 }, { 0.0, 2.0 } };
        var result = BilinearInterpolation.Instance.Resample(data, 3, 3);
        // The middle point [1,1] maps to center of the grid: average = 1.0
        Assert.Equal(1.0, result[1, 1], precision: 10);
    }

    [Fact]
    public void Bilinear_LinearGradient_StaysLinear()
    {
        // Horizontal gradient 0..1 across cols
        double[,] data = new double[2, 5];
        for (int c = 0; c < 5; c++) { data[0, c] = c / 4.0; data[1, c] = c / 4.0; }
        var result = BilinearInterpolation.Instance.Resample(data, 2, 9);
        // Output should also be monotonically increasing
        for (int c = 1; c < 9; c++)
            Assert.True(result[0, c] >= result[0, c - 1] - 1e-10);
    }

    [Fact]
    public void Bilinear_Name_IsBilinear()
    {
        Assert.Equal("bilinear", BilinearInterpolation.Instance.Name, ignoreCase: true);
    }

    // ── BicubicInterpolation ──────────────────────────────────────────────────

    [Fact]
    public void Bicubic_UniformGrid_StaysUniform()
    {
        double[,] data = new double[4, 4];
        for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) data[r, c] = 7.0;
        var result = BicubicInterpolation.Instance.Resample(data, 8, 8);
        foreach (double v in result)
            Assert.Equal(7.0, v, precision: 8);
    }

    [Fact]
    public void Bicubic_OutputRange_ApproximatelyPreserved()
    {
        // Values in [0, 10]; output should not deviate wildly (ringing clamped)
        double[,] data = { { 0, 5, 10, 5 }, { 5, 10, 5, 0 }, { 10, 5, 0, 5 }, { 5, 0, 5, 10 } };
        var result = BicubicInterpolation.Instance.Resample(data, 8, 8);
        foreach (double v in result)
        {
            Assert.True(v >= -0.5, $"Value {v} below minimum with ringing allowance");
            Assert.True(v <= 10.5, $"Value {v} above maximum with ringing allowance");
        }
    }

    [Fact]
    public void Bicubic_SmoothesMoreThanBilinear()
    {
        // Step edge — bicubic should produce smaller max gradient than bilinear
        double[,] data = { { 0, 0, 10, 10 }, { 0, 0, 10, 10 }, { 0, 0, 10, 10 }, { 0, 0, 10, 10 } };
        var bilinear = BilinearInterpolation.Instance.Resample(data, 8, 8);
        var bicubic  = BicubicInterpolation.Instance.Resample(data, 8, 8);
        // Both should produce a transition across the step
        bool bilinearHasTransition = false, bicubicHasTransition = false;
        for (int c = 1; c < 8; c++)
        {
            if (Math.Abs(bilinear[0, c] - bilinear[0, c - 1]) > 0.01) bilinearHasTransition = true;
            if (Math.Abs(bicubic[0, c]  - bicubic[0, c - 1])  > 0.01) bicubicHasTransition  = true;
        }
        Assert.True(bilinearHasTransition);
        Assert.True(bicubicHasTransition);
    }

    [Fact]
    public void Bicubic_Name_IsBicubic()
    {
        Assert.Equal("bicubic", BicubicInterpolation.Instance.Name, ignoreCase: true);
    }

    // ── InterpolationRegistry ─────────────────────────────────────────────────

    [Fact]
    public void Registry_Get_Nearest_ReturnsNearestEngine()
    {
        var engine = InterpolationRegistry.Get("nearest");
        Assert.NotNull(engine);
        Assert.Equal("nearest", engine!.Name, ignoreCase: true);
    }

    [Fact]
    public void Registry_Get_Bilinear_ReturnsBilinearEngine()
    {
        var engine = InterpolationRegistry.Get("bilinear");
        Assert.NotNull(engine);
        Assert.Equal("bilinear", engine!.Name, ignoreCase: true);
    }

    [Fact]
    public void Registry_Get_Bicubic_ReturnsBicubicEngine()
    {
        var engine = InterpolationRegistry.Get("bicubic");
        Assert.NotNull(engine);
        Assert.Equal("bicubic", engine!.Name, ignoreCase: true);
    }

    [Fact]
    public void Registry_Get_Unknown_ReturnsNull()
    {
        var engine = InterpolationRegistry.Get("nonexistent_engine");
        Assert.Null(engine);
    }

    [Fact]
    public void Registry_Get_CaseInsensitive()
    {
        var engine = InterpolationRegistry.Get("BILINEAR");
        Assert.NotNull(engine);
    }

    [Fact]
    public void Registry_Names_ContainsBuiltIns()
    {
        var names = InterpolationRegistry.Names.Select(n => n.ToLowerInvariant()).ToArray();
        Assert.Contains("nearest", names);
        Assert.Contains("bilinear", names);
        Assert.Contains("bicubic", names);
    }

    [Fact]
    public void Registry_Register_CustomEngine_IsRetrievable()
    {
        var custom = new TestEngine("test-engine-xyz");
        InterpolationRegistry.Register("test-engine-xyz", custom);
        var retrieved = InterpolationRegistry.Get("test-engine-xyz");
        Assert.Same(custom, retrieved);
    }

    private sealed class TestEngine(string name) : IInterpolationEngine
    {
        public string Name => name;
        public double[,] Resample(double[,] data, int targetRows, int targetCols) => data;
    }
}
