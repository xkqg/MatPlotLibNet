// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="EventplotSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class EventplotSeriesTests
{
    /// <summary>matplotlib 3.11.2: <c>ax.eventplot(...)[0].get_linewidth()</c> reads 1.5 under the default style and 1.0 under the
    /// classic one, <c>lines.linewidth</c> in points (an <c>EventCollection</c> given no width takes it). The library's own themes keep
    /// the 1 pixel an eventplot has always been drawn at.</summary>
    [Fact]
    public void AnEventplotGivenNoWidth_IsDrawnAsWideAsItsThemeDrawsALine()
    {
        var series = new EventplotSeries([[1.0]]);

        Assert.Equal(1.5 * 100 / 72, Theme.MatplotlibV2.SeriesSlot(0).LineWidthOf(series.GivenLineWidth, LineWidthRule.OnePixelCollection), 9);
        Assert.Equal(1.0 * 100 / 72, Theme.MatplotlibClassic.SeriesSlot(0).LineWidthOf(series.GivenLineWidth, LineWidthRule.OnePixelCollection), 9);
        Assert.Equal(1.0, Theme.Default.SeriesSlot(0).LineWidthOf(series.GivenLineWidth, LineWidthRule.OnePixelCollection));
        Assert.Null(series.ToSeriesDto().LineWidth);
    }

    [Fact]
    public void AGivenWidth_IsDrawnUnderEveryTheme_AndWrittenToTheJson()
    {
        var series = new EventplotSeries([[1.0]]) { LineWidth = 1.0 };

        Assert.Equal(1.0, Theme.MatplotlibV2.SeriesSlot(0).LineWidthOf(series.GivenLineWidth, LineWidthRule.OnePixelCollection));
        Assert.Equal(1.0, series.ToSeriesDto().LineWidth);
    }
}
