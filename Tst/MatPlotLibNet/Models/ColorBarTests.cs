// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="ColorBar"/> default values and record behavior.</summary>
public class ColorBarTests
{
    /// <summary>Verifies that Visible defaults to false.</summary>
    [Fact]
    public void Default_IsNotVisible()
    {
        var cb = new ColorBar();
        Assert.False(cb.Visible);
    }

    /// <summary>Verifies that Width defaults to 20.</summary>
    [Fact]
    public void Default_Width_Is20()
    {
        var cb = new ColorBar();
        Assert.Equal(20, cb.Width);
    }

    /// <summary>Verifies that Padding defaults to 10.</summary>
    [Fact]
    public void Default_Padding_Is10()
    {
        var cb = new ColorBar();
        Assert.Equal(10, cb.Padding);
    }

    /// <summary>Verifies that with expression creates a modified copy.</summary>
    [Fact]
    public void WithExpression_CreatesModifiedCopy()
    {
        var cb = new ColorBar() with { Visible = true, Label = "Intensity" };
        Assert.True(cb.Visible);
        Assert.Equal("Intensity", cb.Label);
        Assert.Equal(20, cb.Width);
    }

    /// <summary>Verifies that ColorMap defaults to null.</summary>
    [Fact]
    public void Default_ColorMap_IsNull()
    {
        var cb = new ColorBar();
        Assert.Null(cb.ColorMap);
    }

    // --- B7: ColorBarExtend ---

    [Fact]
    public void Extend_DefaultsToNeither()
    {
        var cb = new ColorBar();
        Assert.Equal(ColorBarExtend.Neither, cb.Extend);
    }

    [Fact]
    public void Extend_Min_CanBeSet()
    {
        var cb = new ColorBar() with { Extend = ColorBarExtend.Min };
        Assert.Equal(ColorBarExtend.Min, cb.Extend);
    }

    [Fact]
    public void Extend_Max_CanBeSet()
    {
        var cb = new ColorBar() with { Extend = ColorBarExtend.Max };
        Assert.Equal(ColorBarExtend.Max, cb.Extend);
    }

    [Fact]
    public void Extend_Both_CanBeSet()
    {
        var cb = new ColorBar() with { Extend = ColorBarExtend.Both };
        Assert.Equal(ColorBarExtend.Both, cb.Extend);
    }

    [Fact]
    public void ColorBarExtend_HasFourMembers()
    {
        Assert.Equal(4, Enum.GetValues<ColorBarExtend>().Length);
    }

    // --- 2F: ColorBar Enrichment ---

    [Fact]
    public void ColorBar_Orientation_DefaultsToVertical()
    {
        var cb = new ColorBar();
        Assert.Equal(ColorBarOrientation.Vertical, cb.Orientation);
    }

    [Fact]
    public void ColorBar_Shrink_DefaultsTo1()
    {
        var cb = new ColorBar();
        Assert.Equal(1.0, cb.Shrink);
    }

    [Fact]
    public void ColorBar_DrawEdges_DefaultsToFalse()
    {
        var cb = new ColorBar();
        Assert.False(cb.DrawEdges);
    }

    [Fact]
    public void ColorBar_Aspect_DefaultsTo20()
    {
        var cb = new ColorBar();
        Assert.Equal(20.0, cb.Aspect);
    }

    [Fact]
    public void ColorBarOrientation_HasTwoValues()
    {
        var values = Enum.GetValues<ColorBarOrientation>();
        Assert.Equal(2, values.Length);
        Assert.Contains(ColorBarOrientation.Vertical, values);
        Assert.Contains(ColorBarOrientation.Horizontal, values);
    }

    [Fact]
    public void ColorBar_Orientation_Horizontal_CanBeSet()
    {
        var cb = new ColorBar() with { Orientation = ColorBarOrientation.Horizontal };
        Assert.Equal(ColorBarOrientation.Horizontal, cb.Orientation);
    }

    [Fact]
    public void ColorBar_Shrink_CanBeSet()
    {
        var cb = new ColorBar() with { Shrink = 0.75 };
        Assert.Equal(0.75, cb.Shrink);
    }
}
