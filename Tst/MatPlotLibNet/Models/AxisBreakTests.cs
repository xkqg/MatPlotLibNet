// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="AxisBreak"/> model and <see cref="AxisBreakMapper"/> helper.</summary>
public class AxisBreakTests
{
    // ---- AxisBreak record ---------------------------------------------------

    [Fact]
    public void AxisBreak_StoresFromTo()
    {
        var b = new AxisBreak(50.0, 100.0);
        Assert.Equal(50.0, b.From);
        Assert.Equal(100.0, b.To);
    }

    [Fact]
    public void AxisBreak_DefaultStyle_IsZigzag()
    {
        var b = new AxisBreak(50.0, 100.0);
        Assert.Equal(BreakStyle.Zigzag, b.Style);
    }

    [Fact]
    public void AxisBreak_StoresStyle()
    {
        var b = new AxisBreak(10.0, 20.0, BreakStyle.Straight);
        Assert.Equal(BreakStyle.Straight, b.Style);
    }

    // ---- Axes XBreaks/YBreaks -----------------------------------------------

    [Fact]
    public void Axes_XBreaks_DefaultEmpty()
    {
        var axes = new Axes();
        Assert.Empty(axes.XBreaks);
    }

    [Fact]
    public void Axes_YBreaks_DefaultEmpty()
    {
        var axes = new Axes();
        Assert.Empty(axes.YBreaks);
    }

    [Fact]
    public void AddXBreak_Adds()
    {
        var axes = new Axes();
        axes.AddXBreak(50, 100);
        Assert.Single(axes.XBreaks);
    }

    [Fact]
    public void AddYBreak_Adds()
    {
        var axes = new Axes();
        axes.AddYBreak(200, 500);
        Assert.Single(axes.YBreaks);
    }

    [Fact]
    public void MultipleBreaks_AllStored()
    {
        var axes = new Axes();
        axes.AddXBreak(10, 20).AddXBreak(50, 60);
        Assert.Equal(2, axes.XBreaks.Count);
    }

    [Fact]
    public void AddXBreak_ReturnsAxes_ForChaining()
    {
        var axes = new Axes();
        var result = axes.AddXBreak(10, 20);
        Assert.Same(axes, result);
    }

    // ---- AxisBreakMapper.IsInBreak -----------------------------------------

    [Fact]
    public void IsInBreak_True_WhenInRange()
    {
        var breaks = new[] { new AxisBreak(80, 120) };
        Assert.True(AxisBreakMapper.IsInBreak(breaks, 100));
    }

    [Fact]
    public void IsInBreak_False_WhenOutsideRange()
    {
        var breaks = new[] { new AxisBreak(80, 120) };
        Assert.False(AxisBreakMapper.IsInBreak(breaks, 50));
    }

    [Fact]
    public void IsInBreak_False_AtBreakBoundary_From()
    {
        var breaks = new[] { new AxisBreak(80, 120) };
        Assert.False(AxisBreakMapper.IsInBreak(breaks, 80));
    }

    // ---- AxisBreakMapper.CompressedRange -----------------------------------

    [Fact]
    public void CompressedRange_SingleBreak_ReducesRange()
    {
        var breaks = new[] { new AxisBreak(80, 120) };
        var (cMin, cMax) = AxisBreakMapper.CompressedRange(breaks, 0, 200);
        Assert.Equal(0.0, cMin, 1e-9);
        Assert.Equal(160.0, cMax, 1e-9); // 200 - (120-80) = 160
    }

    [Fact]
    public void CompressedRange_NoBreaks_Unchanged()
    {
        var breaks = Array.Empty<AxisBreak>();
        var (cMin, cMax) = AxisBreakMapper.CompressedRange(breaks, 0, 100);
        Assert.Equal(0.0, cMin, 1e-9);
        Assert.Equal(100.0, cMax, 1e-9);
    }

    // ---- AxisBreakMapper.Remap --------------------------------------------

    [Fact]
    public void Remap_BeforeBreak_Unchanged()
    {
        var breaks = new[] { new AxisBreak(80, 120) };
        double remapped = AxisBreakMapper.Remap(breaks, 30, 0, 200);
        Assert.Equal(30.0, remapped, 1e-9);
    }

    [Fact]
    public void Remap_AfterBreak_ShiftedByGap()
    {
        var breaks = new[] { new AxisBreak(80, 120) };
        double remapped = AxisBreakMapper.Remap(breaks, 150, 0, 200);
        Assert.Equal(110.0, remapped, 1e-9); // 150 - 40 = 110
    }

    [Fact]
    public void Remap_InBreak_ReturnsNaN()
    {
        var breaks = new[] { new AxisBreak(80, 120) };
        double remapped = AxisBreakMapper.Remap(breaks, 100, 0, 200);
        Assert.True(double.IsNaN(remapped));
    }
}
