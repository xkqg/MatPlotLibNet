// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="OhlcBarSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class OhlcBarSeriesTests
{
    /// <summary>An OHLC bar is lines (a <c>vlines</c> and two <c>hlines</c> in the generator of the reference), and matplotlib's
    /// <c>LineCollection</c> given no width takes <c>lines.linewidth</c> in points: 1.5 under the default style, 1.0 under the classic
    /// one (3.11.2). The library's own themes keep the 1.5 pixels an OHLC bar has always been drawn at.</summary>
    [Fact]
    public void ABarGivenNoWidth_IsDrawnAsWideAsItsThemeDrawsALine()
    {
        var series = new OhlcBarSeries([10], [15], [8], [13]);

        Assert.Equal(1.5 * 100 / 72, Theme.MatplotlibV2.SeriesSlot(0).LineWidthOf(series.GivenLineWidth, LineWidthRule.LineCollection), 9);
        Assert.Equal(1.0 * 100 / 72, Theme.MatplotlibClassic.SeriesSlot(0).LineWidthOf(series.GivenLineWidth, LineWidthRule.LineCollection), 9);
        Assert.Equal(1.5, Theme.Default.SeriesSlot(0).LineWidthOf(series.GivenLineWidth, LineWidthRule.LineCollection));
    }

    [Fact]
    public void AGivenWidth_IsDrawnAsItIsUnderEveryTheme_AndWrittenToTheJson()
    {
        var series = new OhlcBarSeries([10], [15], [8], [13]) { LineWidth = 1.5 };

        Assert.Equal(1.5, Theme.MatplotlibClassic.SeriesSlot(0).LineWidthOf(series.GivenLineWidth, LineWidthRule.LineCollection));
        Assert.Equal(1.5, Theme.MatplotlibV2.SeriesSlot(0).LineWidthOf(series.GivenLineWidth, LineWidthRule.LineCollection));
        Assert.Equal(1.5, series.ToSeriesDto().LineWidth);
    }
}
