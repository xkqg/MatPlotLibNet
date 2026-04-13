// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="Theme"/> behavior.</summary>
public class ThemeTests
{
    /// <summary>Verifies that the default theme has a white background.</summary>
    [Fact]
    public void DefaultTheme_HasWhiteBackground()
    {
        Assert.Equal(Colors.White, Theme.Default.Background);
    }

    /// <summary>Verifies that the default theme has at least 10 cycle colors.</summary>
    [Fact]
    public void DefaultTheme_HasColorCycle()
    {
        Assert.NotEmpty(Theme.Default.CycleColors);
        Assert.True(Theme.Default.CycleColors.Length >= 10);
    }

    /// <summary>Verifies that the default theme has black foreground text.</summary>
    [Fact]
    public void DefaultTheme_HasBlackForeground()
    {
        Assert.Equal(Colors.Black, Theme.Default.ForegroundText);
    }

    /// <summary>Verifies that the default theme provides a non-null default font.</summary>
    [Fact]
    public void DefaultTheme_HasDefaultFont()
    {
        Assert.NotNull(Theme.Default.DefaultFont);
    }

    /// <summary>Verifies that the dark theme has a non-white background.</summary>
    [Fact]
    public void DarkTheme_HasDarkBackground()
    {
        Assert.NotEqual(Colors.White, Theme.Dark.Background);
    }

    /// <summary>Verifies that the dark theme has a non-black foreground text.</summary>
    [Fact]
    public void DarkTheme_HasLightForeground()
    {
        Assert.NotEqual(Colors.Black, Theme.Dark.ForegroundText);
    }

    /// <summary>Verifies that the Seaborn theme is available.</summary>
    [Fact]
    public void SeabornTheme_Exists()
    {
        Assert.NotNull(Theme.Seaborn);
    }

    /// <summary>Verifies that the Ggplot theme is available.</summary>
    [Fact]
    public void GgplotTheme_Exists()
    {
        Assert.NotNull(Theme.Ggplot);
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
    /// Verifies that Theme.MatplotlibClassic carries a fractional DefaultSpacing that
    /// scales left margin to 12.5% of figure width — matching matplotlib's subplot.left=0.125.
    /// </summary>
    [Fact]
    public void MatplotlibClassic_DefaultSpacing_IsFractional()
    {
        var sp = Theme.MatplotlibClassic.DefaultSpacing;
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
    /// Verifies that resolving Theme.MatplotlibClassic spacing at 800×600 gives the
    /// correct absolute left margin: Math.Round(800 × 0.125) = 100 px.
    /// </summary>
    [Fact]
    public void MatplotlibClassic_Spacing_ResolvesCorrectly_At800x600()
    {
        var sp = Theme.MatplotlibClassic.DefaultSpacing!.Resolve(800, 600);
        Assert.Equal(100.0, sp.MarginLeft);   // 800 * 0.125
        Assert.Equal(80.0,  sp.MarginRight);  // 800 * 0.10
        Assert.Equal(72.0,  sp.MarginTop);    // 600 * 0.12  (matplotlib top=0.88 → margin=0.12)
        Assert.Equal(66.0,  sp.MarginBottom); // 600 * 0.11  (matplotlib bottom=0.11)
    }

    /// <summary>Verifies that Theme.MatplotlibV2 also carries a fractional DefaultSpacing.</summary>
    [Fact]
    public void MatplotlibV2_DefaultSpacing_IsFractional()
    {
        var sp = Theme.MatplotlibV2.DefaultSpacing;
        Assert.NotNull(sp);
        Assert.True(sp.IsFractional);
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
        double plotAreaTop = renderer.RenderBackground(figure, ctx);
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
        double plotAreaTop = renderer.RenderBackground(figure, ctx);
        var plotAreas = renderer.ComputeSubPlotLayout(figure, plotAreaTop);

        Assert.Equal(60.0, plotAreas[0].X); // default MarginLeft = 60
    }
}
