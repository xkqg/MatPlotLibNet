// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
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
    public void AnnotatedChart_ContainsTextElement()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0]);
        axes.Annotate("peak", 2.0, 20.0);

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("peak", svg);
    }

    [Fact]
    public void ReferenceLineChart_ContainsLineElement()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Plot([1.0, 2.0], [3.0, 4.0]);
        axes.AxHLine(3.5);

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void RadarChart_ContainsPolygonElement()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Radar(["A", "B", "C", "D"], [3.0, 5.0, 2.0, 4.0]))
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<polygon", svg);
    }

    [Fact]
    public void ErrorBarChart_ContainsLineElements()
    {
        var figure = Plt.Create()
            .ErrorBar([1.0, 2.0, 3.0], [10.0, 20.0, 15.0], [1.0, 1.0, 1.0], [2.0, 2.0, 2.0])
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<line", svg);
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void StepChart_ContainsPolylineElement()
    {
        var figure = Plt.Create()
            .Step([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<polyline", svg);
    }

    [Fact]
    public void AreaChart_ContainsPolygonElement()
    {
        var figure = Plt.Create()
            .FillBetween([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<polygon", svg);
    }

}
