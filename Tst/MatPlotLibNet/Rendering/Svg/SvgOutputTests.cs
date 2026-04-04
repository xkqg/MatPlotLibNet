// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Svg;

public class SvgOutputTests
{
    [Fact]
    public void SimpleLineChart_ProducesValidSvg()
    {
        var figure = Plt.Create()
            .WithTitle("Test")
            .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);

        Assert.StartsWith("<svg", svg.TrimStart());
        Assert.Contains("</svg>", svg);
        Assert.Contains("Test", svg); // title appears
    }

    [Fact]
    public void EmptyFigure_ProducesValidSvg()
    {
        var figure = Plt.Create().Build();
        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.StartsWith("<svg", svg.TrimStart());
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public void BarChart_ContainsRectElements()
    {
        var figure = Plt.Create()
            .Bar(["A", "B", "C"], [10.0, 20.0, 15.0])
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void ScatterChart_ContainsCircleElements()
    {
        var figure = Plt.Create()
            .Scatter([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void SvgHasViewBox()
    {
        var figure = Plt.Create()
            .WithSize(1024, 768)
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("viewBox=", svg);
        Assert.Contains("1024", svg);
        Assert.Contains("768", svg);
    }

    [Fact]
    public void MultiSubPlot_ProducesMultiplePlotAreas()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0, 2.0], [3.0, 4.0]))
            .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2.0], [5.0, 6.0]))
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.StartsWith("<svg", svg.TrimStart());
        // Should contain polyline elements for both plots
        Assert.Contains("<polyline", svg);
    }
}
