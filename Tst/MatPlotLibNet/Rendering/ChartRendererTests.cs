// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="SvgTransform"/> behavior.</summary>
public class ChartRendererTests
{
    /// <summary>Verifies that rendering an empty figure produces valid SVG with opening and closing tags.</summary>
    [Fact]
    public void Render_EmptyFigure_ProducesValidSvg()
    {
        var figure = Plt.Create().Build();

        string svg = new SvgTransform().Render(figure);

        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    /// <summary>Verifies that a figure with a title includes the title text in the SVG output.</summary>
    [Fact]
    public void Render_FigureWithTitle_ContainsTitleText()
    {
        var figure = Plt.Create()
            .WithTitle("My Title")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        string svg = new SvgTransform().Render(figure);

        Assert.Contains("My Title", svg);
    }

    /// <summary>Verifies that a figure without a title does not contain stray title text.</summary>
    [Fact]
    public void Render_FigureWithoutTitle_DoesNotContainTitleText()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        string svg = new SvgTransform().Render(figure);

        // The figure has no title, so "My Title" should not appear
        Assert.DoesNotContain("My Title", svg);
    }

    /// <summary>Verifies that a single subplot renders axes with rect and line elements.</summary>
    [Fact]
    public void Render_SingleSubplot_ProducesAxes()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .Build();

        string svg = new SvgTransform().Render(figure);

        // Axes frame is rendered as a rect, and tick marks as line elements
        Assert.Contains("<rect", svg);
        Assert.Contains("<line", svg);
    }

    /// <summary>Verifies that two side-by-side subplots both have their titles in the SVG output.</summary>
    [Fact]
    public void Render_TwoSubplots_ProducesBothTitles()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax
                .WithTitle("Left Panel")
                .Plot([1.0, 2.0], [3.0, 4.0]))
            .AddSubPlot(1, 2, 2, ax => ax
                .WithTitle("Right Panel")
                .Plot([1.0, 2.0], [5.0, 6.0]))
            .Build();

        string svg = new SvgTransform().Render(figure);

        Assert.Contains("Left Panel", svg);
        Assert.Contains("Right Panel", svg);
    }

    /// <summary>Verifies that a 2x2 grid of subplots renders all four subplot titles.</summary>
    [Fact]
    public void Render_2x2Grid_AllSubplotsPresent()
    {
        var figure = Plt.Create()
            .AddSubPlot(2, 2, 1, ax => ax
                .WithTitle("TopLeft")
                .Plot([1.0, 2.0], [1.0, 2.0]))
            .AddSubPlot(2, 2, 2, ax => ax
                .WithTitle("TopRight")
                .Plot([1.0, 2.0], [3.0, 4.0]))
            .AddSubPlot(2, 2, 3, ax => ax
                .WithTitle("BottomLeft")
                .Plot([1.0, 2.0], [5.0, 6.0]))
            .AddSubPlot(2, 2, 4, ax => ax
                .WithTitle("BottomRight")
                .Plot([1.0, 2.0], [7.0, 8.0]))
            .Build();

        string svg = new SvgTransform().Render(figure);

        Assert.Contains("TopLeft", svg);
        Assert.Contains("TopRight", svg);
        Assert.Contains("BottomLeft", svg);
        Assert.Contains("BottomRight", svg);
    }

    /// <summary>Verifies that a custom background color appears as a rect with the specified hex color.</summary>
    [Fact]
    public void Render_WithBackground_ContainsBackgroundRect()
    {
        var figure = Plt.Create()
            .WithBackground(Color.FromHex("#AABBCC"))
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        string svg = new SvgTransform().Render(figure);

        Assert.Contains("<rect", svg);
        Assert.Contains("#AABBCC", svg);
    }

    /// <summary>Verifies that enabling grid lines produces line elements in the SVG output.</summary>
    [Fact]
    public void Render_WithGrid_ContainsGridLines()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0]);
        axes.Grid = axes.Grid with { Visible = true };

        string svg = new SvgTransform().Render(figure);

        // Grid lines are rendered as <line> elements
        Assert.Contains("<line", svg);
    }

    /// <summary>Verifies that a line series renders as a polyline element.</summary>
    [Fact]
    public void Render_LineSeries_ContainsPolyline()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
            .Build();

        string svg = new SvgTransform().Render(figure);

        Assert.Contains("<polyline", svg);
    }

    /// <summary>Verifies that a bar series renders as rect elements.</summary>
    [Fact]
    public void Render_BarSeries_ContainsRects()
    {
        var figure = Plt.Create()
            .Bar(["A", "B", "C"], [10.0, 20.0, 15.0])
            .Build();

        string svg = new SvgTransform().Render(figure);

        Assert.Contains("<rect", svg);
    }
}
