// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

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

    private static Figure Build(Action<FigureBuilder> configure)
    {
        var fb = Plt.Create();
        configure(fb);
        return fb.Build();
    }

    /// <summary>Where the renderer drew the points of the series labelled <paramref name="label"/>, read from its
    /// polyline. The harness' screen CTM is the identity, so these are also the client coordinates of the points.</summary>
    private static Point[] DrawnPoints(string svg, string label)
    {
        var doc = XDocument.Parse(svg);
        var group = doc.Descendants().Single(e => e.Name.LocalName == "g"
            && (string?)e.Attribute("aria-label") == label && e.Attribute("data-series-index") is not null);
        var polyline = group.Descendants().Single(e => e.Name.LocalName == "polyline");
        return polyline.Attribute("points")!.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(','))
            .Select(p => new Point(double.Parse(p[0], CultureInfo.InvariantCulture), double.Parse(p[1], CultureInfo.InvariantCulture)))
            .ToArray();
    }

    /// <summary>The range the renderer drew for the <see cref="Autoscaled"/> figure, worked out from where it drew the
    /// points (1, 10) and (3, 20) and the edges of its plot area — not read from any range the page is given.</summary>
    private static DataRange DrawnRange(string svg)
    {
        var points = DrawnPoints(svg, "load");
        var view = XDocument.Parse(svg).Descendants().Single(e => e.Attribute("data-axes-index") is not null);
        double Attr(string name) => double.Parse(view.Attribute(name)!.Value, CultureInfo.InvariantCulture);
        double left = Attr("data-plot-x"), top = Attr("data-plot-y");
        double right = left + Attr("data-plot-w"), bottom = top + Attr("data-plot-h");
        double xPerPx = (3.0 - 1.0) / (points[2].X - points[0].X);
        double yPerPx = (20.0 - 10.0) / (points[0].Y - points[2].Y);
        return new DataRange(
            1.0 + (left - points[0].X) * xPerPx, 1.0 + (right - points[0].X) * xPerPx,
            10.0 - (bottom - points[0].Y) * yPerPx, 10.0 - (top - points[0].Y) * yPerPx);
    }

    private static DomElement Svg(InteractionScriptHarness h) => h.Document.QuerySelectorAllRaw("svg").Single();

    private static IDictionary<string, object?> Payload(SignalRInvokeMock mock, string method) =>
        Assert.IsAssignableFrom<IDictionary<string, object?>>(Assert.Single(mock.Calls, c => c.Method == method).Payload);

    private static double Num(IDictionary<string, object?> payload, string key) =>
        Convert.ToDouble(payload[key], CultureInfo.InvariantCulture);

    private static void AssertNear(double expected, double actual, double tolerance, string what) =>
        Assert.True(Math.Abs(actual - expected) <= tolerance,
            $"{what}: expected {expected.ToString(CultureInfo.InvariantCulture)}, got {actual.ToString("R", CultureInfo.InvariantCulture)}");

    [Fact]
    public void Hover_OverADrawnPoint_OfAnAutoscaledFigure_ReportsThatPoint()
    {
        using var h = InteractionScriptHarness.FromBuilder(Autoscaled);
        var mock = h.WireSignalRMock();
        var p = DrawnPoints(h.Svg, "load")[1];

        Svg(h).Fire(new DomEvent("pointermove") { target = Svg(h), clientX = p.X, clientY = p.Y });

        var hover = Payload(mock, "OnHover");
        AssertNear(2.0, Num(hover, "x"), 2e-6, "x");
        AssertNear(40.0, Num(hover, "y"), 30e-6, "y");
    }

    [Fact]
    public void BrushSelect_BetweenTwoDrawnPoints_ReportsTheirDataBounds()
    {
        using var h = InteractionScriptHarness.FromBuilder(Autoscaled);
        var mock = h.WireSignalRMock();
        var points = DrawnPoints(h.Svg, "load");
        var svg = Svg(h);

        svg.Fire(new DomEvent("pointerdown") { target = svg, clientX = points[0].X, clientY = points[0].Y, shiftKey = true });
        svg.Fire(new DomEvent("pointerup") { target = svg, clientX = points[2].X, clientY = points[2].Y, shiftKey = true });

        var brush = Payload(mock, "OnBrushSelect");
        AssertNear(1.0, Num(brush, "x1"), 2e-6, "x1");
        AssertNear(3.0, Num(brush, "x2"), 2e-6, "x2");
        AssertNear(10.0, Num(brush, "y1"), 30e-6, "y1");
        AssertNear(20.0, Num(brush, "y2"), 30e-6, "y2");
    }

    [Fact]
    public void WheelZoomIn_OnADrawnPoint_AppliedToTheFigure_NarrowsTheLimitsAroundThatPoint()
    {
        using var h = InteractionScriptHarness.FromBuilder(Autoscaled);
        var mock = h.WireSignalRMock();
        var drawn = DrawnRange(h.Svg);
        var p = DrawnPoints(h.Svg, "load")[1];

        Svg(h).Fire(new DomEvent("wheel") { target = Svg(h), clientX = p.X, clientY = p.Y, deltaY = -100 });

        var zoom = Payload(mock, "OnZoom");
        var figure = Build(Autoscaled);
        new ZoomEvent("c", Convert.ToInt32(zoom["axesIndex"]), Num(zoom, "xMin"), Num(zoom, "xMax"), Num(zoom, "yMin"), Num(zoom, "yMax"))
            .ApplyTo(figure);

        // A tenth narrower, about the data point under the pointer (2, 40): each edge moves a tenth closer to it.
        var axes = figure.SubPlots[0];
        AssertNear(2 - (2 - drawn.XMin) * 0.9, axes.XAxis.Min!.Value, 1e-6, "x min");
        AssertNear(2 + (drawn.XMax - 2) * 0.9, axes.XAxis.Max!.Value, 1e-6, "x max");
        AssertNear(40 - (40 - drawn.YMin) * 0.9, axes.YAxis.Min!.Value, 1e-5, "y min");
        AssertNear(40 + (drawn.YMax - 40) * 0.9, axes.YAxis.Max!.Value, 1e-5, "y max");
    }

    [Fact]
    public void WheelZoomOut_OnADrawnPoint_AppliedToTheFigure_RedrawsThatPointUnderThePointer()
    {
        using var h = InteractionScriptHarness.FromBuilder(Autoscaled);
        var mock = h.WireSignalRMock();
        var before = DrawnPoints(h.Svg, "load");
        var p = before[1];

        Svg(h).Fire(new DomEvent("wheel") { target = Svg(h), clientX = p.X, clientY = p.Y, deltaY = 100 });

        var zoom = Payload(mock, "OnZoom");
        var figure = Build(Autoscaled);
        new ZoomEvent("c", Convert.ToInt32(zoom["axesIndex"]), Num(zoom, "xMin"), Num(zoom, "xMax"), Num(zoom, "yMin"), Num(zoom, "yMax"))
            .ApplyTo(figure);
        var after = DrawnPoints(figure.ToSvg(), "load");

        // The redraw puts the point that was under the pointer back under it, and the points draw closer by 1 / 1.1.
        AssertNear(p.X, after[1].X, 0.01, "x pixel of (2, 40)");
        AssertNear(p.Y, after[1].Y, 0.01, "y pixel of (2, 40)");
        AssertNear((before[2].X - before[0].X) / 1.1, after[2].X - after[0].X, 0.01, "x spread");
        AssertNear((before[2].Y - before[0].Y) / 1.1, after[2].Y - after[0].Y, 0.01, "y spread");
    }

    [Fact]
    public void Pan_ByAKnownPixelDistance_AppliedToTheFigure_MovesTheLimitsByTheMatchingDataDistance()
    {
        using var h = InteractionScriptHarness.FromBuilder(Autoscaled);
        var mock = h.WireSignalRMock();
        var drawn = DrawnRange(h.Svg);
        var points = DrawnPoints(h.Svg, "load");
        var p = points[1];
        var svg = Svg(h);

        svg.Fire(new DomEvent("pointerdown") { target = svg, clientX = p.X, clientY = p.Y });
        svg.Fire(new DomEvent("pointermove") { target = svg, clientX = p.X + 50, clientY = p.Y + 30 });
        svg.Fire(new DomEvent("pointerup") { target = svg, clientX = p.X + 50, clientY = p.Y + 30 });

        var pan = Payload(mock, "OnPan");
        var inView = Assert.IsAssignableFrom<IDictionary<string, object?>>(pan["inView"]);
        var figure = Build(Autoscaled);
        new PanEvent("c", Convert.ToInt32(pan["axesIndex"]), Num(pan, "dxData"), Num(pan, "dyData"))
        {
            InView = new DataRange(Num(inView, "xMin"), Num(inView, "xMax"), Num(inView, "yMin"), Num(inView, "yMax")),
        }.ApplyTo(figure);

        // The data follows the pointer: 50 px right and 30 px down move the view by the data those pixels span, the
        // other way. Data per pixel is read from the drawn points: (1, 10) and (3, 20).
        double xPerPx = (3.0 - 1.0) / (points[2].X - points[0].X);
        double yPerPx = (20.0 - 10.0) / (points[0].Y - points[2].Y);
        var axes = figure.SubPlots[0];
        AssertNear(drawn.XMin - 50 * xPerPx, axes.XAxis.Min!.Value, 1e-6, "x min");
        AssertNear(drawn.XMax - 50 * xPerPx, axes.XAxis.Max!.Value, 1e-6, "x max");
        AssertNear(drawn.YMin + 30 * yPerPx, axes.YAxis.Min!.Value, 1e-5, "y min");
        AssertNear(drawn.YMax + 30 * yPerPx, axes.YAxis.Max!.Value, 1e-5, "y max");
    }

    [Fact]
    public void Home_AfterAZoom_AppliedToTheFigure_RestoresTheViewTheRendererFirstDrew()
    {
        using var h = InteractionScriptHarness.FromBuilder(Autoscaled);
        var before = DrawnPoints(h.Svg, "load");
        var p = before[1];
        Svg(h).Fire(new DomEvent("wheel") { target = Svg(h), clientX = p.X, clientY = p.Y, deltaY = -100 });
        var mock = h.WireSignalRMock();

        Svg(h).Fire(new DomEvent("keydown") { target = Svg(h), key = "Home" });

        var reset = Payload(mock, "OnReset");
        var figure = Build(Autoscaled);
        new ResetEvent("c", Convert.ToInt32(reset["axesIndex"]), Num(reset, "xMin"), Num(reset, "xMax"), Num(reset, "yMin"), Num(reset, "yMax"))
            .ApplyTo(figure);
        var after = DrawnPoints(figure.ToSvg(), "load");

        for (int i = 0; i < before.Length; i++)
        {
            AssertNear(before[i].X, after[i].X, 0.01, $"x pixel of point {i}");
            AssertNear(before[i].Y, after[i].Y, 0.01, $"y pixel of point {i}");
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
        var mock = h.WireSignalRMock();
        var p = DrawnPoints(h.Svg, "right")[2];

        Svg(h).Fire(new DomEvent("pointermove") { target = Svg(h), clientX = p.X, clientY = p.Y });

        var hover = Payload(mock, "OnHover");
        Assert.Equal(1, Convert.ToInt32(hover["axesIndex"]));
        AssertNear(300.0, Num(hover, "x"), 300e-6, "x");
        AssertNear(0.75, Num(hover, "y"), 1e-6, "y");
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
        var mock = h.WireSignalRMock();

        Svg(h).Fire(new DomEvent("pointermove") { target = Svg(h), clientX = 55, clientY = 35 });

        var hover = Payload(mock, "OnHover");
        AssertNear(0.25, Num(hover, "x"), 1e-6, "x");
        AssertNear(0.5, Num(hover, "y"), 1e-6, "y");
    }

    [Fact]
    public void WithServerInteraction_EveryAxesWritesItsView_UnderItsIndex_AndOnlyACartesianOneARange()
    {
        var doc = XDocument.Parse(Plt.Create()
            .WithSize(800, 400)
            .WithServerInteraction("c", s => s.EnableZoom())
            .AddSubPlot(1, 2, 1, ax => ax.Plot(X, Y))
            .AddSubPlot(1, 2, 2, ax => ax.PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0]))
            .ToSvg());

        var views = doc.Descendants().Where(e => e.Attribute("data-axes-index") is not null).ToList();
        Assert.Equal(["0", "1"], views.Select(v => v.Attribute("data-axes-index")!.Value));
        Assert.All(views, v => Assert.True(double.Parse(v.Attribute("data-plot-w")!.Value, CultureInfo.InvariantCulture) > 0));
        Assert.NotNull(views[0].Attribute("data-xmin"));
        Assert.Null(views[1].Attribute("data-xmin"));
        Assert.Empty(views[0].Nodes());
    }

    [Fact]
    public void WithoutServerInteraction_NoAxesCarriesAView()
    {
        // The view is weight only the server-interaction script reads: every other figure keeps its bytes.
        var svg = Plt.Create().WithLegendToggle().WithZoomPan().AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => s.Label = "A")).ToSvg();

        Assert.DoesNotContain("data-axes-index", svg, StringComparison.Ordinal);
    }
}
