// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
    public void MatplotlibClassic_HasGreyFigureBackground()
    {
        // matplotlib classic style: figure.facecolor=0.75 → RGB(191,191,191) = #BFBFBF
        Assert.Equal(Color.FromHex("#BFBFBF"), Theme.MatplotlibClassic.Background);
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
    public void MatplotlibClassic_FontSizeIsTwelvePointsInPixels()
    {
        // matplotlib classic font.size = 12 pt; our Font.Size is in pixels, so we pre-convert
        // at 100 DPI: 12 * 100/72 ≈ 16.67 px. See MatplotlibThemeFactory.PtToPx.
        Assert.Equal(12.0 * 100.0 / 72.0, Theme.MatplotlibClassic.DefaultFont.Size, 3);
    }

    [Fact]
    public void MatplotlibClassic_GridIsHiddenByDefault()
    {
        Assert.False(Theme.MatplotlibClassic.DefaultGrid.Visible);
    }

    /// <summary>The grid classic matplotlib draws when it is asked for (<c>grid.color: black</c>, <c>grid.linestyle: :</c>,
    /// <c>grid.linewidth: 0.5</c>), which is the grid the theme keeps hidden until then. It was the 0.8 point grey solid line
    /// the later styles draw, so a polar chart, whose rings are always drawn, wore the wrong grid under this theme.</summary>
    [Fact]
    public void MatplotlibClassic_GridWhenShown_IsClassicMatplotlibs()
    {
        var grid = Theme.MatplotlibClassic.DefaultGrid;

        Assert.Equal(Color.FromHex("#000000"), grid.Color);
        Assert.Equal(LineStyle.Dotted, grid.LineStyle);
        Assert.Equal(0.5, grid.LineWidth);
    }

    /// <summary>A polar chart draws its rings in the theme's grid: black, thin and dotted under the classic theme, so dotted
    /// polylines and no circles.</summary>
    [Fact]
    public void APolarChart_UnderClassic_DrawsItsRings_InTheClassicGrid()
    {
        string svg = Plt.Create().WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.PolarScatter([0.2, 0.5, 0.9], [0.3, 1.2, 2.5]))
            .ToSvg();

        Assert.DoesNotContain("<ellipse ", svg);
        Assert.Matches("""<polyline [^>]*stroke="#000000" stroke-width="0.5" stroke-dasharray=""", svg);
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
