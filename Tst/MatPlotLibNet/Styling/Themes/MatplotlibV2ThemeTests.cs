// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling.Themes;

/// <summary>Verifies the MatplotlibV2 theme — mimics matplotlib's modern (since 2017) default look.</summary>
public class MatplotlibV2ThemeTests
{
    [Fact]
    public void MatplotlibV2_Exists()
    {
        Assert.NotNull(Theme.MatplotlibV2);
    }

    [Fact]
    public void MatplotlibV2_HasCorrectName()
    {
        Assert.Equal("matplotlib-v2", Theme.MatplotlibV2.Name);
    }

    [Fact]
    public void MatplotlibV2_HasWhiteBackground()
    {
        Assert.Equal(Colors.White, Theme.MatplotlibV2.Background);
    }

    [Fact]
    public void MatplotlibV2_HasWhiteAxesBackground()
    {
        Assert.Equal(Colors.White, Theme.MatplotlibV2.AxesBackground);
    }

    [Fact]
    public void MatplotlibV2_HasSoftBlackForeground()
    {
        Assert.Equal(Color.FromHex("#262626"), Theme.MatplotlibV2.ForegroundText);
    }

    [Fact]
    public void MatplotlibV2_FontFamilyContainsDejaVuSans()
    {
        Assert.Contains("DejaVu Sans", Theme.MatplotlibV2.DefaultFont.Family);
    }

    [Fact]
    public void MatplotlibV2_FontSizeIsTen()
    {
        Assert.Equal(10.0, Theme.MatplotlibV2.DefaultFont.Size);
    }

    [Fact]
    public void MatplotlibV2_GridIsHiddenByDefault()
    {
        Assert.False(Theme.MatplotlibV2.DefaultGrid.Visible);
    }

    [Fact]
    public void MatplotlibV2_HasTenTab10CycleColors()
    {
        Assert.Equal(10, Theme.MatplotlibV2.CycleColors.Length);
    }

    [Fact]
    public void MatplotlibV2_FirstCycleColorIsTab10Blue()
    {
        Assert.Equal(Color.FromHex("#1f77b4"), Theme.MatplotlibV2.CycleColors[0]);
    }

    [Fact]
    public void MatplotlibV2_LastCycleColorIsTab10Cyan()
    {
        Assert.Equal(Color.FromHex("#17becf"), Theme.MatplotlibV2.CycleColors[9]);
    }

    [Fact]
    public void MatplotlibV2_CycleColorsMatchTab10()
    {
        var c = Theme.MatplotlibV2.CycleColors;
        Assert.Equal(Color.FromHex("#1f77b4"), c[0]); // blue
        Assert.Equal(Color.FromHex("#ff7f0e"), c[1]); // orange
        Assert.Equal(Color.FromHex("#2ca02c"), c[2]); // green
        Assert.Equal(Color.FromHex("#d62728"), c[3]); // red
        Assert.Equal(Color.FromHex("#9467bd"), c[4]); // purple
        Assert.Equal(Color.FromHex("#8c564b"), c[5]); // brown
        Assert.Equal(Color.FromHex("#e377c2"), c[6]); // pink
        Assert.Equal(Color.FromHex("#7f7f7f"), c[7]); // gray
        Assert.Equal(Color.FromHex("#bcbd22"), c[8]); // olive
        Assert.Equal(Color.FromHex("#17becf"), c[9]); // cyan
    }

    [Fact]
    public void MatplotlibV2_CanBuildWithThemeBuilder()
    {
        var custom = Theme.CreateFrom(Theme.MatplotlibV2).Build();
        Assert.Equal("custom-matplotlib-v2", custom.Name);
    }

    [Fact]
    public void MatplotlibV2_RendersSvg_WithoutError()
    {
        var svg = Plt.Create()
            .WithTheme(Theme.MatplotlibV2)
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .ToSvg();

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void MatplotlibV2_DiffersFromDefaultTheme_BySoftBlackText()
    {
        // Theme.Default uses pure black; MatplotlibV2 uses soft black (#262626)
        Assert.NotEqual(Theme.Default.ForegroundText, Theme.MatplotlibV2.ForegroundText);
    }
}
