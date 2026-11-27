// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The colour map a series that names none is drawn with, which the theme says: matplotlib's <c>image.cmap</c>,
/// <c>'jet'</c> in the classic style and <c>'viridis'</c> in the default one (matplotlib 3.11.2, <c>plt.style.context('classic')</c>:
/// <c>rcParams['image.cmap'] == 'jet'</c>; every <c>ScalarMappable</c> made with no <c>cmap</c> takes it). The library's own themes
/// draw viridis, as they always have. jet runs from <c>#00007F</c> to <c>#7F0000</c>, viridis from <c>#440154</c> to
/// <c>#FDE725</c>.</summary>
public sealed class DefaultColorMapTests
{
    private static string HeatmapSvg(Theme theme, bool withColorBar = false) => Plt.Create()
        .WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.Heatmap(new double[,] { { 0, 1 } });
            if (withColorBar) ax.WithColorBar();
        })
        .ToSvg();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // matplotlib 3.11.2: colormaps['jet'](0.0, bytes=True) is (0, 0, 127) and (1.0, bytes=True) is (127, 0, 0): the dark blue and the
        // dark red its renderers write, a byte truncated (half is 127, not 128).
        new DrawnToken("Classic_DrawsASeriesThatNamesNoMap_InJet", () => HeatmapSvg(Theme.MatplotlibClassic), "#00007F", "#7F0000") { Absent = ["#440154"], Comparison = StringComparison.OrdinalIgnoreCase },
        // A map the series names wins over the theme's.
        new DrawnToken("AMapTheSeriesNames_Wins", () => Plt.Create().WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 0, 1 } }, s => s.ColorMap = ColorMaps.Viridis))
            .ToSvg(), "#440154") { Absent = ["#00007F"], Comparison = StringComparison.OrdinalIgnoreCase },
        new DrawnToken("V2AndTheLibrarysThemes_DrawItInViridis (#1)", () => HeatmapSvg(Theme.MatplotlibV2), "#440154") { Comparison = StringComparison.OrdinalIgnoreCase },
        new DrawnToken("V2AndTheLibrarysThemes_DrawItInViridis (#2)", () => HeatmapSvg(Theme.Default), "#440154") { Comparison = StringComparison.OrdinalIgnoreCase },
        // A colour bar that names no map and stands beside a series that names none wears the theme's map too, as fig.colorbar takes the
        // mappable's.
        // The bar's first band is the top of its map: viridis' top (#FDE724, as the renderers write it) stands in the v2 figure
        // and in no classic one, whose heatmap cells are jet.
        new DrawnToken("Classic_AColourBarOfASeriesThatNamesNoMap_IsJet (viridisWithBar)", () => HeatmapSvg(Theme.MatplotlibV2, withColorBar: true), "#FDE724") { Comparison = StringComparison.OrdinalIgnoreCase },
        new DrawnToken("Classic_AColourBarOfASeriesThatNamesNoMap_IsJet (withBar)", () => HeatmapSvg(Theme.MatplotlibClassic, withColorBar: true)) { Absent = ["#FDE724"], Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>The theme carries its map: jet under classic, none under the other themes (viridis); a derived theme and the
    /// theme's JSON keep it, and a series asks the theme for it.</summary>
    [Fact]
    public void TheThemeCarriesTheMap()
    {
        Assert.Same(ColorMaps.Jet, Theme.MatplotlibClassic.Looks.ColorMap.Default);
        Assert.Null(Theme.MatplotlibV2.Looks.ColorMap.Default);
        Assert.Null(Theme.Default.Looks.ColorMap.Default);
        Assert.Same(ColorMaps.Jet, Theme.MatplotlibClassic.Cloned().Looks.ColorMap.Default);
        Assert.Equal("jet", Theme.MatplotlibClassic.ThroughDto().Looks.ColorMap.Default?.Name);
        Assert.Same(ColorMaps.Jet, Theme.MatplotlibClassic.Looks.ColorMap.Of(null));
        Assert.Same(ColorMaps.Viridis, Theme.MatplotlibV2.Looks.ColorMap.Of(null));
        Assert.Same(ColorMaps.Plasma, Theme.MatplotlibClassic.Looks.ColorMap.Of(ColorMaps.Plasma));
    }
}
