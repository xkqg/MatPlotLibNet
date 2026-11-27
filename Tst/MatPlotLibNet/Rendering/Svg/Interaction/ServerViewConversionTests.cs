// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Hover, brush select, wheel zoom, pan and Home convert the pointer with the view the renderer drew: the plot
/// area of the axes under the pointer and the range those axes actually show (after autoscale), not the explicit limits
/// of the first axes over the whole picture. The round trips apply what the page sends to the figure, render it again
/// and read where the data landed, so the numbers the page sends and the numbers the server applies are proved to be in
/// the same units.</summary>
public class ServerViewConversionTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Y = [10.0, 40.0, 20.0];

    /// <summary>An autoscaled 600 x 500 figure with every server interaction on: the way a caller writes it.</summary>
    private static readonly Action<FigureBuilder> Autoscaled = b => b
        .WithSize(600, 500)
        .WithServerInteraction("c", s => s.All())
        .AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "load"));
    /// <summary>The range the renderer drew for the <see cref="Autoscaled"/> figure, worked out from where it drew the
    /// points (1, 10) and (3, 20) and the edges of its plot area — not read from any range the page is given.</summary>
    private static DataRange DrawnRange(string svg)
    {
        var points = svg.PointsOf("load");
        var view = svg.Xml().Descendants().Single(e => e.Attribute("data-axes-index") is not null);
        double left = view.Number("data-plot-x"), top = view.Number("data-plot-y");
        double right = left + view.Number("data-plot-w"), bottom = top + view.Number("data-plot-h");
        double xPerPx = (3.0 - 1.0) / (points[2].X - points[0].X);
        double yPerPx = (20.0 - 10.0) / (points[0].Y - points[2].Y);
        return new DataRange(
            1.0 + (left - points[0].X) * xPerPx, 1.0 + (right - points[0].X) * xPerPx,
            10.0 - (bottom - points[0].Y) * yPerPx, 10.0 - (top - points[0].Y) * yPerPx);
    }

    // A drawn point is written to a hundredth of a pixel, so a gesture aimed at it lands within half a hundredth: on these
    // plots, a few hundred pixels across, that is a few hundred-thousandths of the range, inside the tolerances below.
    [Fact]
    public void Hover_OverADrawnPoint_OfAnAutoscaledFigure_ReportsThatPoint()
    {
        using var h = InteractionScriptHarness.FromBuilder(Autoscaled);
        var p = h.Svg.PointsOf("load")[1];

        var hover = h.HoverAt(p);
        Assert.Equal(2.0, hover.Num("x"), 2e-4);
        Assert.Equal(40.0, hover.Num("y"), 30e-4);
    }

    [Fact]
    public void BrushSelect_BetweenTwoDrawnPoints_ReportsTheirDataBounds()
    {
        using var h = InteractionScriptHarness.FromBuilder(Autoscaled);
        var mock = h.WireSignalRMock();
        var points = h.Svg.PointsOf("load");

        h.BrushBetween(points[0], points[2]);

        var brush = mock.Payload("OnBrushSelect");
        Assert.Equal(1.0, brush.Num("x1"), 2e-4);
        Assert.Equal(3.0, brush.Num("x2"), 2e-4);
        Assert.Equal(10.0, brush.Num("y1"), 30e-4);
        Assert.Equal(20.0, brush.Num("y2"), 30e-4);
    }

    [Fact]
    public void WheelZoomIn_OnADrawnPoint_AppliedToTheFigure_NarrowsTheLimitsAroundThatPoint()
    {
        using var h = InteractionScriptHarness.FromBuilder(Autoscaled);
        var mock = h.WireSignalRMock();
        var drawn = DrawnRange(h.Svg);
        var p = h.Svg.PointsOf("load")[1];

        h.Wheel(p);

        var zoom = mock.Payload("OnZoom");
        var figure = Autoscaled.Build();
        zoom.ToZoomEvent()
            .ApplyTo(figure);

        // A tenth narrower, about the data point under the pointer (2, 40): each edge moves a tenth closer to it.
        var axes = figure.SubPlots[0];
        Assert.Equal(2 - (2 - drawn.XMin) * ZoomNotch.In, axes.XAxis.Min!.Value, 1e-4);
        Assert.Equal(2 + (drawn.XMax - 2) * ZoomNotch.In, axes.XAxis.Max!.Value, 1e-4);
        Assert.Equal(40 - (40 - drawn.YMin) * ZoomNotch.In, axes.YAxis.Min!.Value, 1e-3);
        Assert.Equal(40 + (drawn.YMax - 40) * ZoomNotch.In, axes.YAxis.Max!.Value, 1e-3);
    }

    [Fact]
    public void WheelZoomOut_OnADrawnPoint_AppliedToTheFigure_RedrawsThatPointUnderThePointer()
    {
        using var h = InteractionScriptHarness.FromBuilder(Autoscaled);
        var mock = h.WireSignalRMock();
        var before = h.Svg.PointsOf("load");
        var p = before[1];

        h.Wheel(p, 100);

        var zoom = mock.Payload("OnZoom");
        var figure = Autoscaled.Build();
        zoom.ToZoomEvent()
            .ApplyTo(figure);
        var after = figure.ToSvg().PointsOf("load");

        // The redraw puts the point that was under the pointer back under it, and the points draw closer by one notch out.
        Assert.Equal(p.X, after[1].X, 0.01);
        Assert.Equal(p.Y, after[1].Y, 0.01);
        Assert.Equal((before[2].X - before[0].X) / ZoomNotch.Out, after[2].X - after[0].X, 0.01);
        Assert.Equal((before[2].Y - before[0].Y) / ZoomNotch.Out, after[2].Y - after[0].Y, 0.01);
    }

    [Fact]
    public void Pan_ByAKnownPixelDistance_AppliedToTheFigure_MovesTheLimitsByTheMatchingDataDistance()
    {
        using var h = InteractionScriptHarness.FromBuilder(Autoscaled);
        var mock = h.WireSignalRMock();
        var drawn = DrawnRange(h.Svg);
        var points = h.Svg.PointsOf("load");
        var p = points[1];

        h.Drag(p, new Point(p.X + 50, p.Y + 30));

        var pan = mock.Payload("OnPan");
        var figure = Autoscaled.Build();
        pan.ToPanEvent().ApplyTo(figure);

        // The data follows the pointer: 50 px right and 30 px down move the view by the data those pixels span, the
        // other way. Data per pixel is read from the drawn points: (1, 10) and (3, 20).
        double xPerPx = (3.0 - 1.0) / (points[2].X - points[0].X);
        double yPerPx = (20.0 - 10.0) / (points[0].Y - points[2].Y);
        var axes = figure.SubPlots[0];
        Assert.Equal(drawn.XMin - 50 * xPerPx, axes.XAxis.Min!.Value, 1e-4);
        Assert.Equal(drawn.XMax - 50 * xPerPx, axes.XAxis.Max!.Value, 1e-4);
        Assert.Equal(drawn.YMin + 30 * yPerPx, axes.YAxis.Min!.Value, 1e-3);
        Assert.Equal(drawn.YMax + 30 * yPerPx, axes.YAxis.Max!.Value, 1e-3);
    }

    [Fact]
    public void Home_AfterAZoom_AppliedToTheFigure_RestoresTheViewTheRendererFirstDrew()
    {
        using var h = InteractionScriptHarness.FromBuilder(Autoscaled);
        var before = h.Svg.PointsOf("load");
        var p = before[1];
        h.Wheel(p);
        var mock = h.WireSignalRMock();

        h.PressHome();

        var reset = mock.Payload("OnReset");
        var figure = Autoscaled.Build();
        reset.ToResetEvent()
            .ApplyTo(figure);
        var after = figure.ToSvg().PointsOf("load");

        for (int i = 0; i < before.Length; i++)
        {
            Assert.Equal(before[i].X, after[i].X, 0.01);
            Assert.Equal(before[i].Y, after[i].Y, 0.01);
        }
    }

    [Fact]
    public void Hover_OverTheSecondAxes_ReportsItsIndex_AndAValueOnItsOwnRange()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(800, 400)
            .WithServerInteraction("c", s => s.EnableHover())
            .AddSubPlot(1, 2, 1, ax => ax.Plot(X, Y, s => s.Label = "left"))
            .AddSubPlot(1, 2, 2, ax => ax.Plot([100.0, 200.0, 300.0], [0.5, 0.25, 0.75], s => s.Label = "right")));
        var p = h.Svg.PointsOf("right")[2];

        var hover = h.HoverAt(p);
        Assert.Equal(1, Convert.ToInt32(hover["axesIndex"]));
        Assert.Equal(300.0, hover.Num("x"), 300e-4);
        Assert.Equal(0.75, hover.Num("y"), 1e-4);
    }

    [Fact]
    public void Hover_OverAxesDrawnWithoutACartesianRange_StillReports_FromTheCachedLimits()
    {
        // A polar axes has no Cartesian range to convert with; the hover keeps the conversion it always had: the limits
        // cached on the root over the whole picture (the harness' synthetic 20 x 10 rect at (50, 30)).
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithServerInteraction("c", s => s.EnableHover())
            .AddSubPlot(1, 1, 1, ax => ax.PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0], s => s.Label = "orbit")));
        var hover = h.HoverAt(new Point(55, 35));
        Assert.Equal(0.25, hover.Num("x"), 1e-4);
        Assert.Equal(0.5, hover.Num("y"), 1e-4);
    }

    [Fact]
    public void WithServerInteraction_EveryAxesWritesItsView_UnderItsIndex_AndOnlyACartesianOneARange()
    {
        var doc = Plt.Create()
            .WithSize(800, 400)
            .WithServerInteraction("c", s => s.EnableZoom())
            .AddSubPlot(1, 2, 1, ax => ax.Plot(X, Y))
            .AddSubPlot(1, 2, 2, ax => ax.PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0]))
            .ToSvg().Xml();

        var views = doc.Descendants().Where(e => e.Attribute("data-axes-index") is not null).ToList();
        Assert.Equal(["0", "1"], views.Select(v => v.Attribute("data-axes-index")!.Value));
        Assert.All(views, v => Assert.True(v.Number("data-plot-w") > 0));
        Assert.NotNull(views[0].Attribute("data-xmin"));
        Assert.Null(views[1].Attribute("data-xmin"));
        Assert.Empty(views[0].Nodes());
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // The view is weight only the server-interaction script reads: every other figure keeps its bytes.
        new DrawnToken("WithoutServerInteraction_NoAxesCarriesAView", () => Plt.Create().WithLegendToggle().WithZoomPan().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "A")).ToSvg()) { Absent = ["data-axes-index"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
