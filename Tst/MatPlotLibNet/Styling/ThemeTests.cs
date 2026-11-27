// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="Theme"/> behavior.</summary>
public class ThemeTests
{
    /// <summary>What a theme is, as its row states it: the name it answers to, the ground of its figure and of its axes, its text
    /// colour, its font (a family it names and its size in pixels), and whether its grid shows before it is asked for. A theme's colour
    /// cycle is held by the owner of its table (<c>Tab10CycleOwnerTests</c>, the Okabe-Ito and classic cycle facts).</summary>
    /// <param name="Theme">The theme, as <see cref="ThemeCases"/> reads a name.</param>
    /// <param name="Name">The name it answers to.</param>
    /// <param name="Background">The figure's ground.</param>
    /// <param name="AxesBackground">The axes' ground.</param>
    /// <param name="Foreground">The colour of its text.</param>
    /// <param name="FontFamily">A family its font names.</param>
    /// <param name="FontPixels">The size of its font in pixels.</param>
    /// <param name="GridShown">Whether its grid shows before it is asked for.</param>
    public readonly record struct ThemeFact(string Theme, string Name, string Background, string AxesBackground, string Foreground,
        string FontFamily, double FontPixels, bool GridShown);

    /// <summary>One row per theme. matplotlib's default style sets font.size 10 and its classic one 12, in points, turned into pixels at
    /// 100 dpi; both keep the grid hidden until it is asked for, and classic grounds its figure in grey (figure.facecolor 0.75).</summary>
    public static TheoryData<ThemeFact> Facts => new()
    {
        new ThemeFact("default", "default", "#FFFFFF", "#FFFFFF", "#000000", "sans-serif", 13, true),
        new ThemeFact("dark", "dark", "#1C1C1C", "#2D2D2D", "#E0E0E0", "sans-serif", 13, true),
        new ThemeFact("v2", "matplotlib-v2", "#FFFFFF", "#FFFFFF", "#000000", "DejaVu Sans", 10.0 * 100 / 72, false),
        new ThemeFact("classic", "matplotlib-classic", "#BFBFBF", "#FFFFFF", "#000000", "DejaVu Sans", 12.0 * 100 / 72, false),
        new ThemeFact("HighContrast", "high-contrast", "#FFFFFF", "#FFFFFF", "#000000", "DejaVu Sans", 13, true),
        new ThemeFact("ColorBlindSafe", "colorblind-safe", "#FFFFFF", "#FFFFFF", "#000000", "sans-serif", 13, true),
    };

    [Theory]
    [MemberData(nameof(Facts))]
    public void ATheme_IsWhatItsRowSays(ThemeFact row)
    {
        var theme = row.Theme.AsTheme();

        Assert.Equal(row.Name, theme.Name);
        Assert.Equal(Color.FromHex(row.Background), theme.Background);
        Assert.Equal(Color.FromHex(row.AxesBackground), theme.AxesBackground);
        Assert.Equal(Color.FromHex(row.Foreground), theme.ForegroundText);
        Assert.Contains(row.FontFamily, theme.DefaultFont.Family);
        Assert.Equal(row.FontPixels, theme.DefaultFont.Size, 3);
        Assert.Equal(row.GridShown, theme.DefaultGrid.Visible);
    }

    /// <summary>matplotlib's two styles write their text black (text.color is 'black' in both) and draw, when the grid is asked for, the
    /// grid each one names (matplotlib 3.11.2): the default style <c>grid.color '#b0b0b0'</c>, solid, <c>grid.linewidth 0.8</c>; the classic
    /// one black, dotted (<c>grid.linestyle ':'</c>), half a point; <c>grid.alpha</c> is 1.0 in both. A grid width is in pixels, so the
    /// points are turned into pixels at 100 dpi, as for the spine and the fonts (a polar chart's rings and spokes and a 3-D box's walls
    /// always wear this grid).</summary>
    [Theory]
    [InlineData("v2", "#B0B0B0", LineStyle.Solid, 0.8)]
    [InlineData("classic", "#000000", LineStyle.Dotted, 0.5)]
    public void AMatplotlibStyle_WritesBlack_AndDrawsTheGridItNames(string style, string gridColour, LineStyle gridStyle, double gridPoints)
    {
        var theme = style.AsTheme();
        var grid = theme.DefaultGrid;

        Assert.Equal(Colors.Black, theme.DefaultFont.Color);
        Assert.Equal(Color.FromHex(gridColour), grid.Color);
        Assert.Equal(gridStyle, grid.LineStyle);
        Assert.Equal(gridPoints.PointsInPixels, grid.LineWidth);
        Assert.Equal(1.0, grid.Alpha);
    }

    /// <summary>Verifies that the theme builder can override the background color.</summary>
    [Fact]
    public void ThemeBuilder_OverridesBackground()
    {
        var theme = Theme.CreateFrom(Theme.Default)
            .WithBackground(Color.FromHex("#333333"))
            .Build();

        Assert.Equal(Color.FromHex("#333333"), theme.Background);
    }

    /// <summary>Verifies that the theme builder can override the cycle colors array.</summary>
    [Fact]
    public void ThemeBuilder_OverridesCycleColors()
    {
        var colors = new[] { Colors.Red, Colors.Blue, Colors.Green };
        var theme = Theme.CreateFrom(Theme.Default)
            .WithCycleColors(colors)
            .Build();

        Assert.Equal(3, theme.CycleColors.Length);
        Assert.Equal(Colors.Red, theme.CycleColors[0]);
    }

    /// <summary>Verifies that the theme builder can override the default font.</summary>
    [Fact]
    public void ThemeBuilder_OverridesFont()
    {
        var theme = Theme.CreateFrom(Theme.Default)
            .WithFont(f => f with { Size = 18 })
            .Build();

        Assert.Equal(18, theme.DefaultFont.Size);
    }

    /// <summary>Verifies that the theme builder preserves properties that were not overridden.</summary>
    [Fact]
    public void ThemeBuilder_PreservesUnchangedProperties()
    {
        var theme = Theme.CreateFrom(Theme.Default)
            .WithBackground(Colors.Red)
            .Build();

        Assert.Equal(Theme.Default.CycleColors.Length, theme.CycleColors.Length);
        Assert.Equal(Theme.Default.ForegroundText, theme.ForegroundText);
    }

    // --- Matplotlib fidelity: DefaultSpacing (v1.1.2) ---

    /// <summary>
    /// Verifies that Theme.Default has no DefaultSpacing — the compact dashboard margins
    /// are the intended default for non-matplotlib themes.
    /// </summary>
    [Fact]
    public void DefaultTheme_DefaultSpacing_IsNull()
    {
        Assert.Null(Theme.Default.DefaultSpacing);
    }

    /// <summary>
    /// Verifies that the matplotlib themes carry a fractional DefaultSpacing: the classic one scales its
    /// left margin to 12.5% of figure width — matching matplotlib's subplot.left=0.125.
    /// </summary>
    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void AMatplotlibTheme_DefaultSpacing_IsFractional(string themeId)
    {
        var sp = themeId.AsTheme().DefaultSpacing;
        Assert.NotNull(sp);
        Assert.True(sp.IsFractional);
    }

    [Fact]
    public void MatplotlibClassic_DefaultSpacing_LeftFraction_Is0125()
    {
        var sp = Theme.MatplotlibClassic.DefaultSpacing!;
        Assert.Equal(0.125, sp.FractLeft);
    }

    /// <summary>
    /// Resolving a matplotlib theme's spacing at 800×600 gives matplotlib's subplot params: <c>plt.style.use('classic')</c> sets figure.subplot
    /// left 0.125, right 0.9, bottom 0.1, top 0.9 (60 above and 60 below); the default style sets bottom 0.11 and top 0.88 (72 above, 66 below).
    /// </summary>
    [Theory]
    [InlineData("classic", 60.0, 60.0)]
    [InlineData("v2", 72.0, 66.0)]
    public void AMatplotlibTheme_Spacing_ResolvesCorrectly_At800x600(string themeId, double top, double bottom)
    {
        var sp = themeId.AsTheme().DefaultSpacing!.Resolve(800, 600);
        Assert.Equal(100.0, sp.MarginLeft);   // 800 * 0.125
        Assert.Equal(80.0,  sp.MarginRight);  // 800 * (1 - 0.9)
        Assert.Equal(top,   sp.MarginTop);
        Assert.Equal(bottom, sp.MarginBottom);
    }

    /// <summary>matplotlib 3.11.2 keeps <c>figure.subplot.wspace</c> and <c>figure.subplot.hspace</c> at 0.2 in the default style
    /// and in the classic one; both themes carry them as shares of the average axes (the library's own themes carry none).</summary>
    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void TheMatplotlibThemes_CarryTheGapsAsSharesOfTheAverageAxes(string themeId)
    {
        var sp = themeId.AsTheme().DefaultSpacing!;

        Assert.Equal(0.2, sp.HorizontalGapFraction);
        Assert.Equal(0.2, sp.VerticalGapFraction);
    }

    /// <summary>
    /// Verifies that ChartRenderer uses the theme's DefaultSpacing when the figure spacing
    /// has not been explicitly set: plot area left edge should be ~100 px from the left
    /// when using Theme.MatplotlibClassic at 800×600.
    /// </summary>
    [Fact]
    public void ChartRenderer_UsesThemeSpacing_WhenFigureSpacingIsDefault()
    {
        var figure = Plt.Create().WithSize(800, 600)
            .WithTheme(Theme.MatplotlibClassic)
            .Plot([1.0, 2.0, 3.0], [1.0, 4.0, 9.0])
            .Build();

        var renderer = new ChartRenderer();
        var ctx = new SvgRenderContext();
        double plotAreaTop = renderer.FrameOf(figure, ctx).PlotAreaTop;
        var plotAreas = renderer.ComputeSubPlotLayout(figure, plotAreaTop);

        // Left edge should come from 800 * 0.125 = 100 px, not the default 60 px
        Assert.True(plotAreas[0].X >= 95 && plotAreas[0].X <= 105,
            $"Expected left edge ~100 px (matplotlib 12.5% margin at 800 px), got {plotAreas[0].X}");
    }

    /// <summary>
    /// Verifies that ChartRenderer keeps the compact default margins when the figure
    /// uses the legacy library theme (explicitly opted into via <c>WithTheme(Theme.Default)</c>).
    /// v1.1.4 changed the library's default theme to <c>Theme.MatplotlibV2</c>, which ships
    /// fractional matplotlib spacing (MarginLeft=100 at 800px), so this test must pin the
    /// old theme explicitly to keep asserting against the legacy 60/20/40/50 margins.
    /// </summary>
    [Fact]
    public void ChartRenderer_UsesDefaultSpacing_WhenThemeHasNoDefaultSpacing()
    {
        var figure = Plt.Create().WithSize(800, 600)
            .WithTheme(Theme.Default)
            .Plot([1.0, 2.0], [1.0, 2.0])
            .Build();

        var renderer = new ChartRenderer();
        var ctx = new SvgRenderContext();
        double plotAreaTop = renderer.FrameOf(figure, ctx).PlotAreaTop;
        var plotAreas = renderer.ComputeSubPlotLayout(figure, plotAreaTop);

        Assert.Equal(60.0, plotAreas[0].X); // default MarginLeft = 60
    }
}
