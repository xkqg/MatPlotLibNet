// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies the HighContrast theme (WCAG AAA target).</summary>
public class HighContrastThemeTests
{
    [Fact]
    public void HighContrastTheme_Exists()
    {
        Assert.NotNull(Theme.HighContrast);
    }

    [Fact]
    public void HighContrastTheme_HasCorrectName()
    {
        Assert.Equal("high-contrast", Theme.HighContrast.Name);
    }

    [Fact]
    public void HighContrastTheme_HasWhiteBackground()
    {
        Assert.Equal(Colors.White, Theme.HighContrast.Background);
    }

    [Fact]
    public void HighContrastTheme_HasBlackForeground()
    {
        Assert.Equal(Colors.Black, Theme.HighContrast.ForegroundText);
    }

    [Fact]
    public void HighContrastTheme_HasBoldFont()
    {
        Assert.Equal(FontWeight.Bold, Theme.HighContrast.DefaultFont.Weight);
    }

    [Fact]
    public void HighContrastTheme_HasLargerFont()
    {
        Assert.True(Theme.HighContrast.DefaultFont.Size >= 13);
    }

    [Fact]
    public void HighContrastTheme_GridIsBolder()
    {
        Assert.True(Theme.HighContrast.DefaultGrid.LineWidth >= 1.5);
    }

    [Fact]
    public void HighContrastTheme_GridIsDarker()
    {
        // The grid color should be dark (#666666 or similar)
        var c = Theme.HighContrast.DefaultGrid.Color;
        int luminance = (c.R + c.G + c.B) / 3;
        Assert.True(luminance <= 120, $"Grid color is too light: R={c.R} G={c.G} B={c.B}");
    }

    [Fact]
    public void HighContrastTheme_HasAtLeastEightCycleColors()
    {
        Assert.True(Theme.HighContrast.CycleColors.Length >= 8);
    }

    [Fact]
    public void HighContrastTheme_CanBuildWithThemeBuilder()
    {
        var custom = Theme.CreateFrom(Theme.HighContrast).Build();
        // ThemeBuilder.Build() generates "custom-{baseName}"
        Assert.Equal("custom-high-contrast", custom.Name);
    }

    [Fact]
    public void HighContrastTheme_RendersSvg_WithoutError()
    {
        var svg = Plt.Create()
            .WithTheme(Theme.HighContrast)
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .ToSvg();

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
    }
}
