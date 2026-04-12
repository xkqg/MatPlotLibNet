// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies the Okabe-Ito colormap and ColorBlindSafe theme.</summary>
public class ColorBlindSafeTests
{
    [Fact]
    public void OkabeIto_ColorMap_HasEightColors()
    {
        var map = QualitativeColorMaps.OkabeIto;
        // ListedColorMap with 8 entries: sample at 8 equally spaced positions
        var colors = new HashSet<Color>();
        for (int i = 0; i < 8; i++)
            colors.Add(map.GetColor((i + 0.5) / 8.0));
        Assert.Equal(8, colors.Count);
    }

    [Fact]
    public void OkabeIto_ColorMap_RegisteredByName()
    {
        var map = ColorMapRegistry.Get("okabe_ito");
        Assert.NotNull(map);
    }

    [Fact]
    public void OkabeIto_ReversedVariant_Registered()
    {
        var map = ColorMapRegistry.Get("okabe_ito_r");
        Assert.NotNull(map);
    }

    [Fact]
    public void OkabeIto_FirstColor_IsOrange()
    {
        var map = QualitativeColorMaps.OkabeIto;
        var first = map.GetColor(0.0625); // midpoint of first slot (1/16)
        Assert.Equal(Color.FromHex("#E69F00"), first);
    }

    [Fact]
    public void OkabeIto_LastColor_IsBlack()
    {
        var map = QualitativeColorMaps.OkabeIto;
        var last = map.GetColor(0.9375); // midpoint of last slot (15/16)
        Assert.Equal(Color.FromHex("#000000"), last);
    }

    [Fact]
    public void ColorBlindSafeTheme_Exists()
    {
        Assert.NotNull(Theme.ColorBlindSafe);
    }

    [Fact]
    public void ColorBlindSafeTheme_HasCorrectName()
    {
        Assert.Equal("colorblind-safe", Theme.ColorBlindSafe.Name);
    }

    [Fact]
    public void ColorBlindSafeTheme_HasEightCycleColors()
    {
        Assert.Equal(8, Theme.ColorBlindSafe.CycleColors.Length);
    }

    [Fact]
    public void ColorBlindSafeTheme_HasWhiteBackground()
    {
        Assert.Equal(Colors.White, Theme.ColorBlindSafe.Background);
    }

    [Fact]
    public void ColorBlindSafeTheme_CycleColorsMatchOkabeIto()
    {
        var theme = Theme.ColorBlindSafe;
        Assert.Equal(Color.FromHex("#E69F00"), theme.CycleColors[0]); // orange
        Assert.Equal(Color.FromHex("#000000"), theme.CycleColors[7]); // black
    }

    [Fact]
    public void ColorBlindSafeTheme_RendersSvg_WithoutError()
    {
        var svg = Plt.Create()
            .WithTheme(Theme.ColorBlindSafe)
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .ToSvg();

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
    }
}
