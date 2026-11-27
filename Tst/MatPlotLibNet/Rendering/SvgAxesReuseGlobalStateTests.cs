// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Models;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What the process measures text and writes glyphs with is part of every axes' markup: a different provider draws
/// every axes again.</summary>
[Collection("ChartServicesGlobalState")]
public sealed class SvgAxesReuseGlobalStateTests
{
    [Fact]
    public void AnotherTextMeasurer_DrawsEveryAxesAgain()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]).WithTitle("left"))
            .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2, 3], [3.0, 2, 1]).WithTitle("right"))
            .Build();
        SvgAxesReuseTests.Settle(figure);
        var kept = figure.DrawnSvgAxes!;
        using var heldMetrics = FontMetricsDefault.Holding(new TestFontMetrics((text, font) => new(text.Length * font.Size * 1.2, font.Size * 1.2)));
        string svg = figure.ToSvg();

        Assert.NotSame(kept[0], figure.DrawnSvgAxes![0]);
        Assert.Equal(SvgAxesReuseTests.FromScratch(figure), svg);
    }
}
