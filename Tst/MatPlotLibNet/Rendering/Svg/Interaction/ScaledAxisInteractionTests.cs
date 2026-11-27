// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>On a log or symlog axis, hover, brush select, the data cursor, wheel zoom, pan and Home work in the units of
/// the data, not in the scaled (log10 / symlog) space the axis is drawn in. The view and every series group carry the
/// axis' scale; the script interpolates the pointer in the drawn space and inverts the scale, the same rule
/// <see cref="DataTransform.PixelToData"/> follows. The pointer is placed on the pixel the renderer drew a known point at,
/// so the expected value is the data itself. Round trips apply what the page sends and render again.</summary>
public class ScaledAxisInteractionTests
{
    private static Action<FigureBuilder> Figure(double[] x, double[] y, Func<AxesBuilder, AxesBuilder> scale) => b => b
        .WithSize(600, 500)
        .WithServerInteraction("c", s => s.All())
        .AddSubPlot(1, 1, 1, ax => scale(ax.Plot(x, y, s => s.Label = "data")));
    // ── hover: log X, log Y, symlog, sign-mixed symlog ──

    public static TheoryData<double[], double[], Func<AxesBuilder, AxesBuilder>, double, double> LogAxes => new()
    {
        { [1.0, 10.0, 1000.0], [1.0, 2.0, 3.0], ax => ax.SetXScale(AxisScale.Log), 10.0, 2.0 },
        { [1.0, 2.0, 3.0], [1.0, 500.0, 20.0], ax => ax.SetYScale(AxisScale.Log), 2.0, 500.0 },
    };

    [Theory]
    [MemberData(nameof(LogAxes))]
    public void Hover_OnALogAxis_ReportsTheDataValue(double[] x, double[] y, Func<AxesBuilder, AxesBuilder> scale, double hoveredX, double hoveredY)
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure(x, y, scale));

        var hover = h.HoverAt(h.Svg.PointsOf("data")[1]);

        hoveredX.AssertCloseTo(hover.Num("x"), relative: 1e-3, floor: 1e-3, what: "x");
        hoveredY.AssertCloseTo(hover.Num("y"), relative: 1e-3, floor: 1e-3, what: "y");
    }

    [Fact]
    public void Hover_OnALogitYAxis_ReportsTheDataValue()
    {
        // The view names the scale logit; the script undoes it with the logistic, 1 / (1 + 10^-v).
        using var h = InteractionScriptHarness.FromBuilder(Figure([1.0, 2.0, 3.0], [0.01, 0.3, 0.99], ax => ax.SetYScale(AxisScale.Logit)));

        var hover = h.HoverAt(h.Svg.PointsOf("data")[1]);

        Assert.Contains("data-yscale=\"logit\"", h.Svg, StringComparison.Ordinal);
        0.3.AssertCloseTo(hover.Num("y"), relative: 1e-3, floor: 1e-3, what: "y");
    }

    [Fact]
    public void Hover_OnASymlogYAxis_ReportsTheDataValue()
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure([1.0, 2.0, 3.0], [0.5, 300.0, 20.0], ax => ax.WithSymlogYScale(2.0)));

        var points = h.Svg.PointsOf("data");

        300.0.AssertCloseTo(h.HoverAt(points[1]).Num("y"), relative: 1e-3, floor: 1e-3, what: "y of the point past the threshold");
    }

    [Fact]
    public void Hover_OnASignMixedSymlogAxis_ReportsEveryPointOnBothSidesOfZero()
    {
        double[] y = [-400.0, 0.5, 60.0];
        using var h = InteractionScriptHarness.FromBuilder(Figure([1.0, 2.0, 3.0], y, ax => ax.WithSymlogYScale()));
        var points = h.Svg.PointsOf("data");

        for (int i = 0; i < y.Length; i++)
        {
            using var hi = InteractionScriptHarness.FromBuilder(Figure([1.0, 2.0, 3.0], y, ax => ax.WithSymlogYScale()));
            y[i].AssertCloseTo(hi.HoverAt(points[i]).Num("y"), relative: 1e-3, floor: 1e-3, what: $"y of point {i}");
        }
    }

    [Fact]
    public void Hover_OnASymlogXAxis_ReportsTheDataValue()
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure([-300.0, 0.25, 80.0], [1.0, 2.0, 3.0], ax => ax.WithSymlogXScale()));

        (-300.0).AssertCloseTo(h.HoverAt(h.Svg.PointsOf("data")[0]).Num("x"), relative: 1e-3, floor: 1e-3, what: "x");
    }

    // ── brush select and the data cursor ──

    [Fact]
    public void BrushSelect_OnALogYAxis_ReportsTheDataBounds()
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure([1.0, 2.0, 3.0], [1.0, 500.0, 20.0], ax => ax.SetYScale(AxisScale.Log)));
        var mock = h.WireSignalRMock();
        var points = h.Svg.PointsOf("data");

        h.BrushBetween(points[0], points[2]);

        var brush = mock.Payload("OnBrushSelect");
        1.0.AssertCloseTo(brush.Num("x1"), relative: 1e-3, floor: 1e-3, what: "x1");
        3.0.AssertCloseTo(brush.Num("x2"), relative: 1e-3, floor: 1e-3, what: "x2");
        1.0.AssertCloseTo(brush.Num("y1"), relative: 1e-3, floor: 1e-3, what: "y1");
        20.0.AssertCloseTo(brush.Num("y2"), relative: 1e-3, floor: 1e-3, what: "y2");
    }

    [Fact]
    public void DataCursor_OnALogXAxis_ReportsTheClickedPointsDataValue()
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure([1.0, 10.0, 1000.0], [1.0, 2.0, 3.0], ax => ax.SetXScale(AxisScale.Log)));
        var annotation = h.ClickDataCursor("data", 2);
        1000.0.AssertCloseTo(annotation.Num("dataX"), relative: 1e-3, floor: 1e-3, what: "dataX");
        3.0.AssertCloseTo(annotation.Num("dataY"), relative: 1e-3, floor: 1e-3, what: "dataY");
    }

    // ── wheel, pan and Home round trips ──

    [Fact]
    public void WheelZoomIn_OnALogXAxis_AppliedToTheFigure_KeepsThePointUnderThePointer_AndSpreadsTheDecadesEvenly()
    {
        var configure = Figure([1.0, 10.0, 1000.0], [1.0, 2.0, 3.0], ax => ax.SetXScale(AxisScale.Log));
        using var h = InteractionScriptHarness.FromBuilder(configure);
        var mock = h.WireSignalRMock();
        var before = h.Svg.PointsOf("data");
        var p = before[1];

        h.Wheel(p);

        var zoom = mock.Payload("OnZoom");
        var figure = configure.Build();
        zoom.ToZoomEvent().ApplyTo(figure);
        var after = figure.ToSvg().PointsOf("data");

        Assert.True(zoom.Num("xMin") > 0, $"a log axis cannot start at {zoom.Num("xMin")}");
        Assert.InRange(after[1].X, p.X - 0.02, p.X + 0.02);
        Assert.InRange(after[1].Y, p.Y - 0.02, p.Y + 0.02);
        // Zooming in one notch spreads the points by its inverse, and on a log axis the same for every decade.
        Assert.InRange(after[2].X - after[1].X, (before[2].X - before[1].X) / ZoomNotch.In - 0.05, (before[2].X - before[1].X) / ZoomNotch.In + 0.05);
        Assert.InRange(after[1].X - after[0].X, (before[1].X - before[0].X) / ZoomNotch.In - 0.05, (before[1].X - before[0].X) / ZoomNotch.In + 0.05);
    }

    [Fact]
    public void Pan_OnALogXAxis_AppliedToTheFigure_MovesEveryPointByTheDraggedPixels()
    {
        var configure = Figure([1.0, 10.0, 1000.0], [1.0, 2.0, 3.0], ax => ax.SetXScale(AxisScale.Log));
        using var h = InteractionScriptHarness.FromBuilder(configure);
        var mock = h.WireSignalRMock();
        var before = h.Svg.PointsOf("data");
        var p = before[1];

        h.Drag(p, new Point(p.X + 50, p.Y + 30));

        var pan = mock.Payload("OnPan");
        var figure = configure.Build();
        pan.ToPanEvent().ApplyTo(figure);
        var after = figure.ToSvg().PointsOf("data");

        for (int i = 0; i < before.Length; i++)
        {
            Assert.InRange(after[i].X - before[i].X, 49.95, 50.05);
            Assert.InRange(after[i].Y - before[i].Y, 29.95, 30.05);
        }
    }

    [Fact]
    public void Home_AfterAZoom_OnASymlogYAxis_AppliedToTheFigure_RestoresTheFirstDrawing()
    {
        var configure = Figure([1.0, 2.0, 3.0], [-400.0, 0.5, 60.0], ax => ax.WithSymlogYScale());
        InteractionScriptHarness.AssertHomeRestoresTheFirstDrawing(configure);
    }

    // ── the view carries the scale; a linear one carries nothing new ──

    [Fact]
    public void TheView_OfALinearAxes_CarriesNoScale()
    {
        var svg = Plt.Create().WithServerInteraction("c", s => s.All())
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0])).ToSvg();

        var mapped = svg.Xml().Descendants().Where(e => e.Attribute("data-xmin") is not null).ToList();
        Assert.NotEmpty(mapped);
        Assert.All(mapped, e => Assert.DoesNotContain(e.Attributes(),
            a => a.Name.LocalName is "data-xscale" or "data-yscale" or "data-xlinthresh" or "data-ylinthresh"));
    }

    [Fact]
    public void TheView_OfASymlogAxes_NamesTheScaleAndItsThreshold_AndALogAxisNamesItsScale()
    {
        var svg = Plt.Create().WithServerInteraction("c", s => s.All())
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 10.0], [-5.0, 5.0]).SetXScale(AxisScale.Log).WithSymlogYScale(0.5)).ToSvg();

        var view = svg.Xml().Descendants().Single(e => e.Attribute("data-axes-index") is not null);
        Assert.Equal("log", (string?)view.Attribute("data-xscale"));
        Assert.Null(view.Attribute("data-xlinthresh"));
        Assert.Equal("symlog", (string?)view.Attribute("data-yscale"));
        Assert.Equal("0.5", (string?)view.Attribute("data-ylinthresh"));
    }
}
