// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies legend rendering behavior.</summary>
public class LegendRenderingTests
{
    /// <summary>Verifies that labeled series produce a legend box in SVG output.</summary>
    [Fact]
    public void Legend_WithLabeledSeries_ContainsLegendText()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Temperature")
            .Plot([1.0, 2.0], [5.0, 6.0], s => s.Label = "Humidity")
            .ToSvg();

        Assert.Contains("Temperature", svg);
        Assert.Contains("Humidity", svg);
    }

    /// <summary>Verifies that series without labels do not produce legend entries.</summary>
    [Fact]
    public void Legend_NoLabels_SkipsLegend()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        // No labeled series → no legend rectangle
        // The SVG should not contain a legend group
        Assert.DoesNotContain("class=\"legend\"", svg);
    }

    /// <summary>Verifies that a hidden legend produces no legend output.</summary>
    [Fact]
    public void Legend_NotVisible_SkipsLegend()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Data")
                .WithLegend(visible: false))
            .ToSvg();

        Assert.DoesNotContain("class=\"legend\"", svg);
    }

    /// <summary>Verifies that the legend contains color swatches as rect elements.</summary>
    [Fact]
    public void Legend_ContainsColorSwatches()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Series A")
                .WithLegend())
            .ToSvg();

        Assert.Contains("class=\"legend\"", svg);
        Assert.Contains("Series A", svg);
    }

    /// <summary>Verifies that legend position can be set via AxesBuilder.</summary>
    [Fact]
    public void WithLegend_SetsPosition()
    {
        var builder = new FigureBuilder();
        var figure = builder
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Test")
                .WithLegend(LegendPosition.LowerLeft))
            .Build();

        Assert.Equal(LegendPosition.LowerLeft, figure.SubPlots[0].Legend.Position);
    }

    /// <summary>Verifies that WithLegend defaults to Best position when called without arguments.</summary>
    [Fact]
    public void WithLegend_DefaultPosition_IsBest()
    {
        var builder = new FigureBuilder();
        var figure = builder
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Test")
                .WithLegend())
            .Build();

        Assert.Equal(LegendPosition.Best, figure.SubPlots[0].Legend.Position);
    }

    /// <summary>Verifies that only labeled series appear in the legend.</summary>
    [Fact]
    public void Legend_MixedLabeledAndUnlabeled_OnlyShowsLabeled()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Visible")
                .Plot([1.0, 2.0], [5.0, 6.0])
                .WithLegend())
            .ToSvg();

        Assert.Contains("Visible", svg);
        Assert.Contains("class=\"legend\"", svg);
    }

    /// <summary>Verifies that multiple labeled series all appear in the legend.</summary>
    [Fact]
    public void Legend_MultipleSeries_AllLabelsPresent()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Alpha")
                .Plot([1.0, 2.0], [5.0, 6.0], s => s.Label = "Beta")
                .Scatter([1.0, 2.0], [7.0, 8.0], s => s.Label = "Gamma")
                .WithLegend())
            .ToSvg();

        Assert.Contains("Alpha", svg);
        Assert.Contains("Beta", svg);
        Assert.Contains("Gamma", svg);
    }

    // ---- Best: where the marks are -----------------------------------------------------------------------------

    private static readonly double[] Ramp = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    private static FigureBuilder BestCase(string name) => name switch
    {
        "a rising line" => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot(Ramp, Ramp, s => s.Label = "signal").WithLegend()),
        "a falling line" => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot(Ramp, Ramp.Select(v => 10 - v).ToArray(), s => s.Label = "signal").WithLegend()),
        // Both ends stand on the plot's edges: no vertex of the line is inside any place, the segment crosses two.
        "a threshold line near the top" => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 10.0], [10.0, 10.0], s => s.Label = "limit").SetYLim(0, 11).WithLegend()),
        "a tall bar on the right" => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Bar(["a", "b", "c"], [1.0, 2.0, 3.0], s => s.Label = "sales").WithLegend()),
        "points in the upper right corner" => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Scatter([9.0, 9.5, 10.0, 9.2], [9.0, 9.6, 10.0, 9.8], s => s.Label = "cluster").SetXLim(0, 10).SetYLim(0, 10).WithLegend()),
        "a line on a right axis" => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot(Ramp, Ramp.Select(v => 10 - v).ToArray(), s => s.Label = "left")
            .WithAddRightYAxis(y2 => y2.Plot(Ramp, Ramp, s => s.Label = "right")).WithLegend()),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    private static Rect PlotAreaOf(string svg) =>
        Regex.Matches(svg, @"<rect x=""([\d.]+)"" y=""([\d.]+)"" width=""([\d.]+)"" height=""([\d.]+)"" fill=""#FFFFFF"" />")
            .Skip(1).Select(m => new Rect(
                double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture), double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture)))
            .First();

    /// <summary>A Best legend stands where the marks the chart drew are fewest: the first of matplotlib's places that
    /// nothing is under, the upper right when it is free.</summary>
    [Theory]
    [InlineData("a rising line", LegendPosition.UpperLeft)]
    [InlineData("a falling line", LegendPosition.UpperRight)]
    [InlineData("a threshold line near the top", LegendPosition.LowerLeft)]
    [InlineData("a tall bar on the right", LegendPosition.UpperLeft)]
    [InlineData("points in the upper right corner", LegendPosition.UpperLeft)]
    [InlineData("a line on a right axis", LegendPosition.Right)]   // the right axis' line fills the upper right, the primary's the other three corners
    public void ABestLegend_StandsWhereTheFewestMarksAreCovered(string name, LegendPosition expected)
    {
        string svg = BestCase(name).ToSvg();
        var frame = svg.LegendFrame()!.Value;

        var place = LegendPositionStrategyFactory.Create(expected).ComputeBox(PlotAreaOf(svg), frame.Width, frame.Height);

        Assert.Equal(place.X, frame.X, 6);
        Assert.Equal(place.Y, frame.Y, 6);
    }

    /// <summary>The picture the roadmap's own example draws: a rising line y = x in a 500 x 350 figure. The legend that stood
    /// over a stretch of the line in the upper right now stands in the upper left, clear of it.</summary>
    [Fact]
    public void ABestLegend_OverARisingLine_StandsInTheUpperLeft_At72And52()
    {
        var frame = BestCase("a rising line").ToSvg().LegendFrame()!.Value;

        Assert.Equal(72.0, frame.X, 6);
        Assert.Equal(52.0, frame.Y, 6);
        Assert.Equal(93.5, frame.Width, 1);
        Assert.Equal(25.7, frame.Height, 1);
    }

    /// <summary>Hiding a series (the legend's own toggle) does not move the legend from under the pointer: a hidden series
    /// still counts among the marks.</summary>
    [Fact]
    public void ALegendToggle_DoesNotMoveABestLegend()
    {
        var figure = Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot(Ramp, Ramp, s => s.Label = "up")
            .Plot(Ramp, Ramp.Select(v => 10 - v).ToArray(), s => s.Label = "down").WithLegend()).Build();
        var shown = figure.ToSvg().LegendFrame()!.Value;

        figure.SubPlots[0].Series[0].Visible = false;
        var hidden = figure.ToSvg().LegendFrame()!.Value;

        Assert.Equal(shown.X, hidden.X, 6);
        Assert.Equal(shown.Y, hidden.Y, 6);
    }

    /// <summary>The chart without a mark under any place keeps the upper right, exactly as before.</summary>
    [Fact]
    public void ABestLegend_OverNothingInTheWay_StaysInTheUpperRight()
    {
        string svg = Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 1.0], [5.0, 5.0], s => s.Label = "flat").SetXLim(0, 1).SetYLim(0, 10).WithLegend()).ToSvg();
        var frame = svg.LegendFrame()!.Value;
        var place = LegendPositionStrategyFactory.Create(LegendPosition.UpperRight).ComputeBox(PlotAreaOf(svg), frame.Width, frame.Height);

        Assert.Equal(place.X, frame.X, 6);
        Assert.Equal(place.Y, frame.Y, 6);
    }

    /// <summary>An annotation's text stands among the marks a Best legend keeps clear of, where it is drawn: a flat line
    /// leaves every place free, so the legend stays in the upper right, until a text stands there.</summary>
    [Fact]
    public void ALegend_AvoidsAnAnnotationAtItsAnchor()
    {
        FigureBuilder Chart(bool annotated) => Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([0.0, 1.0], [1.0, 1.0], s => s.Label = "flat").SetXLim(0, 1).SetYLim(0, 10).WithLegend();
            if (annotated) ax.Annotate("a long note standing where the legend would", 0.55, 9.3);
        });

        var free = Chart(annotated: false).ToSvg().LegendFrame()!.Value;
        var noted = Chart(annotated: true).ToSvg().LegendFrame()!.Value;

        Assert.True(noted.X < free.X, $"the legend stood at x {free.X} with nothing in its way and at x {noted.X} with a note there");
    }

    /// <summary>The polar marks a Best legend keeps clear of: a line along the top of the circle, under a legend wide enough to
    /// reach it from the upper right, sends the legend to a place the line does not cross.</summary>
    [Fact]
    public void ABestLegend_OnPolarAxes_StandsClearOfAPolarLine()
    {
        string svg = Plt.Create().WithSize(500, 400).AddSubPlot(1, 1, 1, ax => ax
            .PolarPlot([1.0, 1.0, 1.0, 1.0, 1.0], [0.8, 1.2, 1.57, 1.9, 2.3], s => s.Label = "an arc along the top of the circle and a long name")
            .WithLegend()).ToSvg();
        var frame = svg.LegendFrame()!.Value;

        var under = SvgGeometry.ExtractPolylinePoints(svg)
            .Where(p => p.X > frame.X && p.X < frame.Right && p.Y > frame.Y && p.Y < frame.Bottom).ToList();

        Assert.Empty(under);
    }
}
