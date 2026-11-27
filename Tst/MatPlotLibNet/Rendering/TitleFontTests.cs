// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The font of an axes title and of a figure title under each theme. matplotlib 3.11.2: <c>axes.titlesize</c> is
/// <c>'large'</c> (1.2 times <c>font.size</c>, <c>font_manager.font_scalings</c>) and <c>axes.titleweight</c> <c>'normal'</c> in
/// both styles; <c>figure.titlesize</c> is <c>'large'</c> in the default style and <c>'medium'</c> (1.0) in the classic one, and
/// <c>figure.titleweight</c> <c>'normal'</c>. Measured: <c>ax.set_title</c> 12.0 pt under the default style (font 10), 14.4 pt
/// under classic (font 12); <c>fig.suptitle</c> 12.0 pt under both, all normal weight. The library's own themes keep the title two
/// points over the text and the figure title four, both bold.</summary>
public sealed class TitleFontTests
{
    [Fact]
    public void V2_TitleIsLargeAndNormalWeight_FigureTitleLarge()
    {
        var theme = Theme.MatplotlibV2;

        Assert.Equal(theme.DefaultFont.Size * 1.2, theme.TitleFont().Size, 9);
        Assert.Equal(FontWeight.Normal, theme.TitleFont().Weight);
        Assert.Equal(theme.DefaultFont.Size * 1.2, theme.SupTitleFont().Size, 9);
        Assert.Equal(FontWeight.Normal, theme.SupTitleFont().Weight);
    }

    [Fact]
    public void Classic_TitleIsLargeAndNormalWeight_FigureTitleMedium()
    {
        var theme = Theme.MatplotlibClassic;

        Assert.Equal(theme.DefaultFont.Size * 1.2, theme.TitleFont().Size, 9);
        Assert.Equal(FontWeight.Normal, theme.TitleFont().Weight);
        Assert.Equal(theme.DefaultFont.Size, theme.SupTitleFont().Size, 9);
        Assert.Equal(FontWeight.Normal, theme.SupTitleFont().Weight);
    }

    [Fact]
    public void TheLibrarysThemes_KeepABoldTitleTwoPointsOver_AndABoldFigureTitleFour()
    {
        var theme = Theme.Default;

        Assert.Equal(theme.DefaultFont.Size + 2, theme.TitleFont().Size, 9);
        Assert.Equal(FontWeight.Bold, theme.TitleFont().Weight);
        Assert.Equal(theme.DefaultFont.Size + 4, theme.SupTitleFont().Size, 9);
        Assert.Equal(FontWeight.Bold, theme.SupTitleFont().Weight);
    }

    /// <summary>The title rule is the theme's: a theme derived from classic and the theme read back from JSON keep it, and it
    /// scales with the theme's font, as matplotlib's relative sizes do.</summary>
    [Fact]
    public void TheTitleRule_SurvivesADerivedThemeAndTheJson_AndFollowsTheFont()
    {
        var derived = new ThemeBuilder(Theme.MatplotlibClassic).WithFont(f => f with { Size = 20 }).Build();
        var read = ThemeDto.From(Theme.MatplotlibClassic).ToTheme();

        Assert.Equal(24, derived.TitleFont().Size, 9);
        Assert.Equal(20, derived.SupTitleFont().Size, 9);
        Assert.Equal(FontWeight.Normal, derived.TitleFont().Weight);
        Assert.Equal(Theme.MatplotlibClassic.TitleFont(), read.TitleFont());
        Assert.Equal(Theme.MatplotlibClassic.SupTitleFont(), read.SupTitleFont());
    }
}
