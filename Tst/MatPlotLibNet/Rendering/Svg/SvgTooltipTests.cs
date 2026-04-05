// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Svg;

public class SvgTooltipTests
{
    [Fact]
    public void ScatterSeries_WithTooltips_ContainsTitleElements()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTooltips()
                .Scatter([1.0, 2.0], [3.0, 4.0]))
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<title>", svg);
    }

    [Fact]
    public void Tooltips_Disabled_NoTitleElements()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Scatter([1.0, 2.0], [3.0, 4.0]))
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.DoesNotContain("<title>", svg);
    }

    [Fact]
    public void ScatterSeries_WithTooltips_ContainsDataValues()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTooltips()
                .Scatter([1.5], [3.2]))
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("x=1.5", svg);
        Assert.Contains("y=3.2", svg);
    }
}
