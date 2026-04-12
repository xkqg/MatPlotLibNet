// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling.Themes;

/// <summary>Verifies the MatplotlibClassic theme — mimics matplotlib's pre-2.0 default look.</summary>
public class MatplotlibClassicThemeTests
{
    [Fact]
    public void MatplotlibClassic_Exists()
    {
        Assert.NotNull(Theme.MatplotlibClassic);
    }

    [Fact]
    public void MatplotlibClassic_HasCorrectName()
    {
        Assert.Equal("matplotlib-classic", Theme.MatplotlibClassic.Name);
    }

    [Fact]
    public void MatplotlibClassic_HasWhiteBackground()
    {
        Assert.Equal(Colors.White, Theme.MatplotlibClassic.Background);
    }

    [Fact]
    public void MatplotlibClassic_HasWhiteAxesBackground()
    {
        Assert.Equal(Colors.White, Theme.MatplotlibClassic.AxesBackground);
    }

    [Fact]
    public void MatplotlibClassic_HasPureBlackForeground()
    {
        Assert.Equal(Colors.Black, Theme.MatplotlibClassic.ForegroundText);
    }

    [Fact]
    public void MatplotlibClassic_FontFamilyContainsDejaVuSans()
    {
        Assert.Contains("DejaVu Sans", Theme.MatplotlibClassic.DefaultFont.Family);
    }

    [Fact]
    public void MatplotlibClassic_FontSizeIsTwelve()
    {
        Assert.Equal(12.0, Theme.MatplotlibClassic.DefaultFont.Size);
    }

    [Fact]
    public void MatplotlibClassic_GridIsHiddenByDefault()
    {
        Assert.False(Theme.MatplotlibClassic.DefaultGrid.Visible);
    }

    [Fact]
    public void MatplotlibClassic_HasSevenClassicCycleColors()
    {
        Assert.Equal(7, Theme.MatplotlibClassic.CycleColors.Length);
    }

    [Fact]
    public void MatplotlibClassic_FirstCycleColorIsPureBlue()
    {
        Assert.Equal(Color.FromHex("#0000FF"), Theme.MatplotlibClassic.CycleColors[0]);
    }

    [Fact]
    public void MatplotlibClassic_CycleColorsMatchClassicBgrcmyk()
    {
        var c = Theme.MatplotlibClassic.CycleColors;
        Assert.Equal(Color.FromHex("#0000FF"), c[0]); // blue
        Assert.Equal(Color.FromHex("#008000"), c[1]); // green
        Assert.Equal(Color.FromHex("#FF0000"), c[2]); // red
        Assert.Equal(Color.FromHex("#00BFBF"), c[3]); // cyan
        Assert.Equal(Color.FromHex("#BF00BF"), c[4]); // magenta
        Assert.Equal(Color.FromHex("#BFBF00"), c[5]); // yellow
        Assert.Equal(Color.FromHex("#000000"), c[6]); // black
    }

    [Fact]
    public void MatplotlibClassic_CanBuildWithThemeBuilder()
    {
        var custom = Theme.CreateFrom(Theme.MatplotlibClassic).Build();
        Assert.Equal("custom-matplotlib-classic", custom.Name);
    }

    [Fact]
    public void MatplotlibClassic_RendersSvg_WithoutError()
    {
        var svg = Plt.Create()
            .WithTheme(Theme.MatplotlibClassic)
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .ToSvg();

        Assert.NotEmpty(svg);
        Assert.Contains("<svg", svg);
    }
}
