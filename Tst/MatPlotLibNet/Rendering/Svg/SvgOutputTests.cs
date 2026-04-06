// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies <see cref="SvgTransform"/> SVG output behavior.</summary>
public class SvgOutputTests
{
    /// <summary>Verifies that a simple line chart produces valid SVG with opening/closing tags and title.</summary>
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

    /// <summary>Verifies that a scatter chart renders circle elements for each data point.</summary>
    [Fact]
    public void ScatterChart_ContainsCircleElements()
    {
        var figure = Plt.Create()
            .Scatter([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<circle", svg);
    }

    /// <summary>Verifies that the SVG output includes a viewBox attribute matching the configured size.</summary>
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

    /// <summary>Verifies that an annotation appears as text in the SVG output.</summary>
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

    /// <summary>Verifies that a horizontal reference line renders as a line element.</summary>
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

    /// <summary>Verifies that a radar chart renders as a polygon element.</summary>
    [Fact]
    public void RadarChart_ContainsPolygonElement()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Radar(["A", "B", "C", "D"], [3.0, 5.0, 2.0, 4.0]))
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Verifies that an error bar chart renders line and circle elements for whiskers and points.</summary>
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

    /// <summary>Verifies that a step chart renders as a polyline element.</summary>
    [Fact]
    public void StepChart_ContainsPolylineElement()
    {
        var figure = Plt.Create()
            .Step([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .Build();

        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<polyline", svg);
    }

    /// <summary>Verifies that a fill-between area chart renders as a polygon element.</summary>
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
