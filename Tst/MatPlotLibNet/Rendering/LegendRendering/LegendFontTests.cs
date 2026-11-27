// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.LegendRendering;

/// <summary>The fonts a legend names a size for are roles of the theme (<see cref="ThemeFontExtensions"/>), each one rule: the entries
/// at the size the legend names, else the tick labels' size times the theme's <c>legend.fontsize</c>; the end labels of the series
/// (a name at a line's end) at the size the legend names, else the tick labels' own; the title at the size the legend names, else the
/// look's step above the tick labels, in the look's weight. The layout measured the end labels and built the title font itself.</summary>
public sealed class LegendFontTests
{
    [Theory]
    [MemberData(nameof(ThemeCases.DefaultDarkAndMatplotlib), MemberType = typeof(ThemeCases))]
    public void TheEndLabels_AreTheTickFont_AtTheSizeTheLegendNames(string name)
    {
        var theme = name.AsTheme();

        Assert.Equal(theme.TickFont(), theme.EndLabelFontOf(new Legend()));
        Assert.Equal(theme.TickFont() with { Size = 17 }, theme.EndLabelFontOf(new Legend { FontSize = 17 }));
    }

    [Theory]
    [MemberData(nameof(ThemeCases.DefaultDarkAndMatplotlib), MemberType = typeof(ThemeCases))]
    public void TheTitle_IsTheLooksStepAboveTheTickFont_InTheLooksWeight_OrTheSizeTheLegendNames(string name)
    {
        var theme = name.AsTheme();
        var look = theme.Looks.Legend;
        var tick = theme.TickFont();

        Assert.Equal(tick with { Size = tick.Size + look.TitleSizeStep, Weight = look.TitleWeight }, theme.LegendTitleFontOf(new Legend()));
        Assert.Equal(tick with { Size = 19, Weight = look.TitleWeight }, theme.LegendTitleFontOf(new Legend { TitleFontSize = 19 }));
    }

    [Theory]
    [MemberData(nameof(ThemeCases.DefaultDarkAndMatplotlib), MemberType = typeof(ThemeCases))]
    public void TheEntries_AreTheTickFont_ScaledByTheTheme_OrAtTheSizeTheLegendNames(string name)
    {
        var theme = name.AsTheme();
        var tick = theme.TickFont();

        Assert.Equal(tick with { Size = tick.Size * theme.Looks.Legend.FontScale }, theme.LegendFontOf(new Legend()));
        Assert.Equal(tick with { Size = 17 }, theme.LegendFontOf(new Legend { FontSize = 17 }));
    }
}
