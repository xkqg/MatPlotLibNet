// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling;

public class ColorMapTests
{
    [Fact]
    public void Viridis_HasName()
    {
        Assert.Equal("viridis", ColorMaps.Viridis.Name);
    }

    [Fact]
    public void Viridis_AtZero_ReturnsDarkColor()
    {
        var color = ColorMaps.Viridis.GetColor(0.0);
        // Viridis starts dark purple
        Assert.True(color.R < 128);
    }

    [Fact]
    public void Viridis_AtOne_ReturnsLightColor()
    {
        var color = ColorMaps.Viridis.GetColor(1.0);
        // Viridis ends bright yellow
        Assert.True(color.R > 128);
    }

    [Fact]
    public void Viridis_AtHalf_ReturnsMiddleColor()
    {
        var color = ColorMaps.Viridis.GetColor(0.5);
        // Should be distinctly different from 0 and 1
        Assert.NotEqual(ColorMaps.Viridis.GetColor(0.0), color);
        Assert.NotEqual(ColorMaps.Viridis.GetColor(1.0), color);
    }

    [Fact]
    public void Plasma_HasName()
    {
        Assert.Equal("plasma", ColorMaps.Plasma.Name);
    }

    [Fact]
    public void Inferno_HasName()
    {
        Assert.Equal("inferno", ColorMaps.Inferno.Name);
    }

    [Fact]
    public void GetColor_ClampsBelow0()
    {
        var color = ColorMaps.Viridis.GetColor(-0.5);
        Assert.Equal(ColorMaps.Viridis.GetColor(0.0), color);
    }

    [Fact]
    public void GetColor_ClampsAbove1()
    {
        var color = ColorMaps.Viridis.GetColor(1.5);
        Assert.Equal(ColorMaps.Viridis.GetColor(1.0), color);
    }

    [Fact]
    public void AllColors_HaveFullOpacity()
    {
        for (double v = 0; v <= 1.0; v += 0.1)
        {
            var color = ColorMaps.Viridis.GetColor(v);
            Assert.Equal(255, color.A);
        }
    }
}
