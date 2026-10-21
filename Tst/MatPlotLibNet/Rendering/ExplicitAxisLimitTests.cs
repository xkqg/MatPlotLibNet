// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An explicit axis limit is the range drawn, as in matplotlib: a limit set by <c>SetXLim</c>/<c>SetYLim</c> or
/// applied by a zoom or pan event is exact even when the data reaches past it, the plot area clips what lies outside,
/// and a side left unset still scales itself to the data. Before, the explicit limit only seeded the range and every
/// series widened it to its own extent, so <c>SetXLim(1.5, 2.5)</c> on data 1..3 was drawn 1..3.</summary>
public class ExplicitAxisLimitTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Y = [10.0, 40.0, 20.0];

    /// <summary>The view the renderer wrote for the axes at <paramref name="index"/>: its plot area and the range it drew.</summary>
    private static (Rect Plot, DataRange Range) View(string svg, int index = 0)
    {
        var view = XDocument.Parse(svg).Descendants()
            .Single(e => (string?)e.Attribute("data-axes-index") == index.ToString(CultureInfo.InvariantCulture));
        double A(string name) => double.Parse(view.Attribute(name)!.Value, CultureInfo.InvariantCulture);
        return (new Rect(A("data-plot-x"), A("data-plot-y"), A("data-plot-w"), A("data-plot-h")),
                new DataRange(A("data-xmin"), A("data-xmax"), A("data-ymin"), A("data-ymax")));
    }

    private static void AssertRange(DataRange expected, DataRange actual, double tolerance = 1e-9)
    {
        string Show(DataRange r) => string.Create(CultureInfo.InvariantCulture, $"x {r.XMin:R}..{r.XMax:R}, y {r.YMin:R}..{r.YMax:R}");
        Assert.True(Math.Abs(expected.XMin - actual.XMin) <= tolerance && Math.Abs(expected.XMax - actual.XMax) <= tolerance
                 && Math.Abs(expected.YMin - actual.YMin) <= tolerance && Math.Abs(expected.YMax - actual.YMax) <= tolerance,
            $"expected {Show(expected)}, drawn {Show(actual)}");
    }

    [Fact]
    public void SetXLimAndSetYLim_NarrowerThanTheData_AreTheRangeDrawn()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .WithServerInteraction("c", s => s.EnableZoom())
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y).SetXLim(1.5, 2.5).SetYLim(15, 35))
            .ToSvg();

        AssertRange(new DataRange(1.5, 2.5, 15, 35), View(svg).Range);
    }

    [Fact]
    public void ComputeDataRanges_ExplicitLimitsNarrowerThanTheData_ReturnsThemExactly()
    {
        var axes = new Axes();
        axes.Plot(X, Y);
        axes.XAxis.Min = 1.5; axes.XAxis.Max = 2.5;
        axes.YAxis.Min = 15;  axes.YAxis.Max = 35;

        var range = new CartesianAxesRenderer(axes, new Rect(80, 60, 700, 400), new MatPlotLibNet.Rendering.Svg.SvgRenderContext(),
            MatPlotLibNet.Styling.Theme.MatplotlibV2).ComputeDataRanges();

        AssertRange(new DataRange(1.5, 2.5, 15, 35), range);
    }

    [Fact]
    public void AnUnsetSide_StillScalesItselfToTheData_WhileTheSetSideIsExact()
    {
        var axes = new Axes();
        axes.Plot(X, Y);
        axes.XAxis.Min = 1.5;     // max left to autoscale
        axes.YAxis.Max = 30;      // min left to autoscale

        var range = new CartesianAxesRenderer(axes, new Rect(80, 60, 700, 400), new MatPlotLibNet.Rendering.Svg.SvgRenderContext(),
            MatPlotLibNet.Styling.Theme.MatplotlibV2).ComputeDataRanges();

        Assert.Equal(1.5, range.XMin);
        Assert.True(range.XMax >= 3.0, $"x max {range.XMax} must still reach the data's 3");
        Assert.Equal(30, range.YMax);
        Assert.True(range.YMin <= 10.0, $"y min {range.YMin} must still reach the data's 10");
    }

    [Fact]
    public void ASetSide_ThatAllTheDataLiesBeyond_IsKept_AndTheFreeSideStaysOnTheOtherSideOfIt()
    {
        var axes = new Axes();
        axes.Plot(X, Y);
        axes.XAxis.Min = 5;       // every x (1..3) lies below the floor

        var range = new CartesianAxesRenderer(axes, new Rect(80, 60, 700, 400), new MatPlotLibNet.Rendering.Svg.SvgRenderContext(),
            MatPlotLibNet.Styling.Theme.MatplotlibV2).ComputeDataRanges();

        Assert.Equal(5, range.XMin);
        Assert.True(range.XMax > 5, $"x max {range.XMax} must lie above the floor 5");
    }

    [Fact]
    public void SecondaryYLimitNarrowerThanItsData_IsTheRangeItsSeriesAreDrawnWith()
    {
        var axes = new Axes();
        axes.Plot(X, Y);
        axes.PlotSecondary(X, [100.0, 500.0, 300.0]);
        axes.SecondaryYAxis!.Min = 200; axes.SecondaryYAxis.Max = 400;

        var renderer = new CartesianAxesRenderer(axes, new Rect(80, 60, 700, 400), new MatPlotLibNet.Rendering.Svg.SvgRenderContext(),
            MatPlotLibNet.Styling.Theme.MatplotlibV2);
        var sec = renderer.ComputeSecondaryDataRanges(1, 3);

        Assert.Equal(200, sec.YMin);
        Assert.Equal(400, sec.YMax);
    }

    [Fact]
    public void SecondaryXLimitNarrowerThanItsData_IsTheRangeItsSeriesAreDrawnWith()
    {
        var axes = new Axes();
        axes.Plot(X, Y);
        axes.PlotXSecondary([0.0, 50.0, 100.0], Y);
        axes.SecondaryXAxis!.Min = 25; axes.SecondaryXAxis.Max = 75;

        var renderer = new CartesianAxesRenderer(axes, new Rect(80, 60, 700, 400), new MatPlotLibNet.Rendering.Svg.SvgRenderContext(),
            MatPlotLibNet.Styling.Theme.MatplotlibV2);
        var sec = renderer.ComputeSecondaryXDataRanges(10, 40);

        Assert.Equal(25, sec.XMin);
        Assert.Equal(75, sec.XMax);
    }

    [Fact]
    public void AZoomEvent_InsideTheData_RedrawsExactlyTheLimitsItApplied()
    {
        var figure = Plt.Create().WithSize(600, 500).WithServerInteraction("c", s => s.All())
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y)).Build();

        new ZoomEvent("c", 0, 1.5, 2.5, 15, 35).ApplyTo(figure);

        AssertRange(new DataRange(1.5, 2.5, 15, 35), View(figure.ToSvg()).Range);
    }

    [Fact]
    public void APanEvent_RedrawsExactlyTheLimitsItApplied()
    {
        // The measurement that exposed the widening: a pan to x 0.6634..2.8634, y 11.07..44.07 on data x 1..3,
        // y 10..40 was redrawn x 0.6634..3, y 10..44.07.
        var figure = Plt.Create().WithSize(600, 500).WithServerInteraction("c", s => s.All())
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y)).Build();

        new PanEvent("c", 0, -0.3366, 1.07) { InView = new DataRange(1.0, 3.2, 10.0, 43.0) }.ApplyTo(figure);

        AssertRange(new DataRange(0.6634, 2.8634, 11.07, 44.07), View(figure.ToSvg()).Range, 1e-9);
    }

    [Fact]
    public void WithAnExplicitLimit_EverySeriesIsClippedToThePlotArea()
    {
        var svg = Plt.Create().WithSize(600, 500).WithServerInteraction("c", s => s.EnableZoom())
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, Y, s => s.Label = "line")
                .Scatter(X, Y, s => s.Label = "dots")
                .Bar(["a", "b", "c"], [5.0, 50.0, 25.0], s => s.Label = "bars")
                .SetXLim(1.5, 2.5).SetYLim(15, 35))
            .ToSvg();
        var doc = XDocument.Parse(svg);
        var plot = View(svg).Plot;
        var clips = doc.Descendants().Where(e => e.Name.LocalName == "clipPath")
            .ToDictionary(e => (string)e.Attribute("id")!, e => e.Elements().Single());

        var groups = doc.Descendants().Where(e => e.Name.LocalName == "g" && e.Attribute("data-series-index") is not null).ToList();
        Assert.Equal(3, groups.Count);
        foreach (var group in groups)
        {
            var clipped = group.Elements().Single(e => e.Name.LocalName == "g" && e.Attribute("clip-path") is not null);
            string id = clipped.Attribute("clip-path")!.Value["url(#".Length..^1];
            var rect = clips[id];
            double R(string a) => double.Parse(rect.Attribute(a)!.Value, CultureInfo.InvariantCulture);
            Assert.Equal(plot, new Rect(R("x"), R("y"), R("width"), R("height")));
            Assert.NotEmpty(clipped.Elements());
        }
    }

    [Fact]
    public void WithAnExplicitLimit_SecondaryAxisSeriesAreClippedToThePlotArea_Too()
    {
        var figure = Plt.Create().WithSize(600, 500).WithServerInteraction("c", s => s.EnableZoom())
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "main")).Build();
        var axes = figure.SubPlots[0];
        axes.PlotSecondary(X, [100.0, 500.0, 300.0]).Label = "right";
        axes.PlotXSecondary([0.0, 50.0, 100.0], Y).Label = "top";
        axes.SecondaryYAxis!.Min = 200; axes.SecondaryYAxis.Max = 400;
        var svg = figure.ToSvg();

        var groups = XDocument.Parse(svg).Descendants()
            .Where(e => e.Name.LocalName == "g" && e.Attribute("data-series-index") is not null).ToList();
        Assert.Equal(3, groups.Count);
        Assert.All(groups, g => Assert.Contains(g.Elements(), e => e.Name.LocalName == "g" && e.Attribute("clip-path") is not null));
    }

    [Fact]
    public void WithAnExplicitLimit_AnEndLabelOutsideThePlotArea_IsNotClipped()
    {
        var svg = Plt.Create().WithSize(600, 500).WithServerInteraction("c", s => s.EnableZoom())
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => { s.Label = "load"; s.LabelPlacement = SeriesLabelPlacement.LineEnd; })
                .SetYLim(0, 50))
            .ToSvg();

        var group = XDocument.Parse(svg).Descendants()
            .Single(e => e.Name.LocalName == "g" && e.Attribute("data-series-index") is not null);
        var label = group.Descendants().Single(e => e.Name.LocalName == "text" && e.Value == "load");
        Assert.DoesNotContain(label.Ancestors(), a => a.Attribute("clip-path") is not null);
    }

    [Fact]
    public void AnAutoscaledFigure_DrawsNoClip()
    {
        // Autoscale encloses every series, so nothing can lie outside the plot area: the figure keeps its bytes.
        var svg = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "A")).ToSvg();

        Assert.DoesNotContain("clip-path", svg, StringComparison.Ordinal);
    }
}
