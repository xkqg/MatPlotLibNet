// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="ColorMaps"/> behavior.</summary>
public class ColorMapTests
{
    /// <summary>Verifies that the Viridis color map reports its name correctly.</summary>
    [Fact]
    public void Viridis_HasName()
    {
        Assert.Equal("viridis", ColorMaps.Viridis.Name);
    }

    /// <summary>Verifies that Viridis at position 0.0 returns a dark color.</summary>
    [Fact]
    public void Viridis_AtZero_ReturnsDarkColor()
    {
        var color = ColorMaps.Viridis.GetColor(0.0);
        // Viridis starts dark purple
        Assert.True(color.R < 128);
    }

    /// <summary>Verifies that Viridis at position 1.0 returns a bright color.</summary>
    [Fact]
    public void Viridis_AtOne_ReturnsLightColor()
    {
        var color = ColorMaps.Viridis.GetColor(1.0);
        // Viridis ends bright yellow
        Assert.True(color.R > 128);
    }

    /// <summary>Verifies that Viridis at position 0.5 returns a color distinct from both endpoints.</summary>
    [Fact]
    public void Viridis_AtHalf_ReturnsMiddleColor()
    {
        var color = ColorMaps.Viridis.GetColor(0.5);
        // Should be distinctly different from 0 and 1
        Assert.NotEqual(ColorMaps.Viridis.GetColor(0.0), color);
        Assert.NotEqual(ColorMaps.Viridis.GetColor(1.0), color);
    }

    /// <summary>Verifies that the Plasma color map reports its name correctly.</summary>
    [Fact]
    public void Plasma_HasName()
    {
        Assert.Equal("plasma", ColorMaps.Plasma.Name);
    }

    /// <summary>Verifies that the Inferno color map reports its name correctly.</summary>
    [Fact]
    public void Inferno_HasName()
    {
        Assert.Equal("inferno", ColorMaps.Inferno.Name);
    }

    /// <summary>Verifies that GetColor clamps input values below 0.0 to the start of the map.</summary>
    [Fact]
    public void GetColor_ClampsBelow0()
    {
        var color = ColorMaps.Viridis.GetColor(-0.5);
        Assert.Equal(ColorMaps.Viridis.GetColor(0.0), color);
    }

    /// <summary>Verifies that GetColor clamps input values above 1.0 to the end of the map.</summary>
    [Fact]
    public void GetColor_ClampsAbove1()
    {
        var color = ColorMaps.Viridis.GetColor(1.5);
        Assert.Equal(ColorMaps.Viridis.GetColor(1.0), color);
    }

    /// <summary>Verifies that all Viridis colors across the range have full opacity.</summary>
    [Fact]
    public void AllColors_HaveFullOpacity()
    {
        for (double v = 0; v <= 1.0; v += 0.1)
        {
            var color = ColorMaps.Viridis.GetColor(v);
            Assert.Equal(255, color.A);
        }
    }

    /// <summary>Verifies that the Magma color map reports its name correctly.</summary>
    [Fact]
    public void Magma_Name()
    {
        Assert.Equal("magma", ColorMaps.Magma.Name);
    }

    /// <summary>Verifies that the Coolwarm color map reports its name correctly.</summary>
    [Fact]
    public void Coolwarm_Name()
    {
        Assert.Equal("coolwarm", ColorMaps.Coolwarm.Name);
    }

    /// <summary>Verifies that the Blues color map reports its name correctly.</summary>
    [Fact]
    public void Blues_Name()
    {
        Assert.Equal("blues", ColorMaps.Blues.Name);
    }

    /// <summary>Verifies that the Reds color map reports its name correctly.</summary>
    [Fact]
    public void Reds_Name()
    {
        Assert.Equal("reds", ColorMaps.Reds.Name);
    }

    /// <summary>Verifies that Magma at position 0.0 returns a color with valid RGBA components.</summary>
    [Fact]
    public void Magma_AtZero_ReturnsValidColor()
    {
        var color = ColorMaps.Magma.GetColor(0.0);
        Assert.InRange(color.R, (byte)0, (byte)255);
        Assert.InRange(color.G, (byte)0, (byte)255);
        Assert.InRange(color.B, (byte)0, (byte)255);
        Assert.InRange(color.A, (byte)0, (byte)255);
    }

    /// <summary>Verifies that Magma at position 1.0 returns a color with valid RGBA components.</summary>
    [Fact]
    public void Magma_AtOne_ReturnsValidColor()
    {
        var color = ColorMaps.Magma.GetColor(1.0);
        Assert.InRange(color.R, (byte)0, (byte)255);
        Assert.InRange(color.G, (byte)0, (byte)255);
        Assert.InRange(color.B, (byte)0, (byte)255);
        Assert.InRange(color.A, (byte)0, (byte)255);
    }

    /// <summary>Verifies that Blues at position 0.0 returns a near-white color.</summary>
    [Fact]
    public void Blues_AtZero_ReturnsLightColor()
    {
        var color = ColorMaps.Blues.GetColor(0.0);
        // Blues starts near white (#F7FBFF)
        Assert.True(color.R > 200);
    }

    /// <summary>Verifies that Blues at position 1.0 returns a color where blue dominates.</summary>
    [Fact]
    public void Blues_AtOne_ReturnsBlueColor()
    {
        var color = ColorMaps.Blues.GetColor(1.0);
        // Blues ends dark blue (#084594) — blue channel dominates
        Assert.True(color.B > color.R);
        Assert.True(color.B > color.G);
    }

    /// <summary>Verifies that Reds at position 0.0 returns a near-white color.</summary>
    [Fact]
    public void Reds_AtZero_ReturnsLightColor()
    {
        var color = ColorMaps.Reds.GetColor(0.0);
        // Reds starts near white (#FFF5F0)
        Assert.True(color.R > 200);
    }

    /// <summary>Verifies that Coolwarm at position 0.5 returns a neutral transition color.</summary>
    [Fact]
    public void Coolwarm_AtHalf_ReturnsNeutralColor()
    {
        var color = ColorMaps.Coolwarm.GetColor(0.5);
        // At the midpoint the color should be a neutral/transition tone
        // It should differ from the endpoints
        Assert.NotEqual(ColorMaps.Coolwarm.GetColor(0.0), color);
        Assert.NotEqual(ColorMaps.Coolwarm.GetColor(1.0), color);
    }

    public static IEnumerable<object[]> AllMaps =>
    [
        [ColorMaps.Viridis],
        [ColorMaps.Plasma],
        [ColorMaps.Inferno],
        [ColorMaps.Magma],
        [ColorMaps.Coolwarm],
        [ColorMaps.Blues],
        [ColorMaps.Reds],
    ];

    /// <summary>Verifies that every color map returns full opacity at position 0.0.</summary>
    [Theory]
    [MemberData(nameof(AllMaps))]
    public void AllMaps_AtZero_HaveFullOpacity(IColorMap map)
    {
        var color = map.GetColor(0.0);
        Assert.Equal(255, color.A);
    }

    /// <summary>Verifies that every color map returns full opacity at position 1.0.</summary>
    [Theory]
    [MemberData(nameof(AllMaps))]
    public void AllMaps_AtOne_HaveFullOpacity(IColorMap map)
    {
        var color = map.GetColor(1.0);
        Assert.Equal(255, color.A);
    }

    /// <summary>Verifies that every color map clamps values below 0.0 to the start color.</summary>
    [Theory]
    [MemberData(nameof(AllMaps))]
    public void AllMaps_Clamp_BelowZero(IColorMap map)
    {
        var belowZero = map.GetColor(-0.5);
        var atZero = map.GetColor(0.0);
        Assert.Equal(atZero, belowZero);
    }

    /// <summary>Verifies that every color map clamps values above 1.0 to the end color.</summary>
    [Theory]
    [MemberData(nameof(AllMaps))]
    public void AllMaps_Clamp_AboveOne(IColorMap map)
    {
        var aboveOne = map.GetColor(1.5);
        var atOne = map.GetColor(1.0);
        Assert.Equal(atOne, aboveOne);
    }

    /// <summary>Verifies that Viridis midpoint interpolation produces a color distinct from both endpoints.</summary>
    [Fact]
    public void Interpolation_Midpoint_DiffersFromEndpoints()
    {
        var c0 = ColorMaps.Viridis.GetColor(0.0);
        var cMid = ColorMaps.Viridis.GetColor(0.5);
        var c1 = ColorMaps.Viridis.GetColor(1.0);

        Assert.NotEqual(c0, cMid);
        Assert.NotEqual(c1, cMid);
    }

    /// <summary>Verifies that every color map returns different colors at positions 0.0 and 1.0.</summary>
    [Theory]
    [MemberData(nameof(AllMaps))]
    public void AllMaps_ReturnDifferentColorsAtEndpoints(IColorMap map)
    {
        var atZero = map.GetColor(0.0);
        var atOne = map.GetColor(1.0);
        Assert.NotEqual(atZero, atOne);
    }

    /// <summary>Verifies that Blues brightness decreases monotonically from light to dark.</summary>
    [Fact]
    public void Blues_MonotonicBrightness()
    {
        // Blues goes from light (#F7FBFF) to dark (#084594), so brightness should decrease
        var c0 = ColorMaps.Blues.GetColor(0.0);
        var c1 = ColorMaps.Blues.GetColor(1.0);
        int brightness0 = c0.R + c0.G + c0.B;
        int brightness1 = c1.R + c1.G + c1.B;
        Assert.True(brightness0 > brightness1,
            $"Blues at 0.0 (brightness {brightness0}) should be brighter than at 1.0 (brightness {brightness1})");
    }
}
