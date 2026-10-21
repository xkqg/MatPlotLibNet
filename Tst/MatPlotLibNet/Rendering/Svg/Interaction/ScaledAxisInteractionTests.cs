// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>On a log or symlog axis, hover, brush select, the data cursor, wheel zoom, pan and Home work in the units of
/// the data, not in the scaled (log10 / symlog) space the axis is drawn in. The view and every series group carry the
/// axis' scale; the script interpolates the pointer in the drawn space and inverts the scale, the same rule
/// <see cref="DataTransform.PixelToData"/> follows. The pointer is placed on the pixel the renderer drew a known point at,
/// so the expected value is the data itself. Round trips apply what the page sends and render again.</summary>
public class ScaledAxisInteractionTests
{
    private static Figure Build(Action<FigureBuilder> configure)
    {
        var fb = Plt.Create();
        configure(fb);
        return fb.Build();
    }

    private static Action<FigureBuilder> Figure(double[] x, double[] y, Func<AxesBuilder, AxesBuilder> scale) => b => b
        .WithSize(600, 500)
        .WithServerInteraction("c", s => s.All())
        .AddSubPlot(1, 1, 1, ax => scale(ax.Plot(x, y, s => s.Label = "data")));

    private static Point[] DrawnPoints(string svg)
    {
        var group = XDocument.Parse(svg).Descendants().Single(e => e.Name.LocalName == "g"
            && (string?)e.Attribute("aria-label") == "data" && e.Attribute("data-series-index") is not null);
        return group.Descendants().Single(e => e.Name.LocalName == "polyline").Attribute("points")!.Value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(','))
            .Select(p => new Point(double.Parse(p[0], CultureInfo.InvariantCulture), double.Parse(p[1], CultureInfo.InvariantCulture)))
            .ToArray();
    }

    private static DomElement Svg(InteractionScriptHarness h) => h.Document.QuerySelectorAllRaw("svg").Single();

    private static IDictionary<string, object?> Payload(SignalRInvokeMock mock, string method) =>
        Assert.IsAssignableFrom<IDictionary<string, object?>>(Assert.Single(mock.Calls, c => c.Method == method).Payload);

    private static double Num(IDictionary<string, object?> payload, string key) =>
        Convert.ToDouble(payload[key], CultureInfo.InvariantCulture);

    /// <summary>Relative closeness: the drawn pixels are written with a few decimals, so the value read back is the data
    /// to about 1e-4 of its size; a log10 value instead of the data is off by orders of magnitude.</summary>
    private static void AssertClose(double expected, double actual, string what, double relative = 1e-3)
    {
        double tolerance = Math.Max(Math.Abs(expected), 1e-3) * relative;
        Assert.True(Math.Abs(actual - expected) <= tolerance,
            $"{what}: expected {expected.ToString(CultureInfo.InvariantCulture)}, got {actual.ToString("R", CultureInfo.InvariantCulture)}");
    }

    private static IDictionary<string, object?> HoverAt(InteractionScriptHarness h, Point p)
    {
        var mock = h.WireSignalRMock();
        Svg(h).Fire(new DomEvent("pointermove") { target = Svg(h), clientX = p.X, clientY = p.Y });
        return Payload(mock, "OnHover");
    }

    // ── hover: log X, log Y, symlog, sign-mixed symlog ──

    [Fact]
    public void Hover_OnALogXAxis_ReportsTheDataValue()
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure([1.0, 10.0, 1000.0], [1.0, 2.0, 3.0], ax => ax.SetXScale(AxisScale.Log)));

        var hover = HoverAt(h, DrawnPoints(h.Svg)[1]);

        AssertClose(10.0, Num(hover, "x"), "x");
        AssertClose(2.0, Num(hover, "y"), "y");
    }

    [Fact]
    public void Hover_OnALogYAxis_ReportsTheDataValue()
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure([1.0, 2.0, 3.0], [1.0, 500.0, 20.0], ax => ax.SetYScale(AxisScale.Log)));

        var hover = HoverAt(h, DrawnPoints(h.Svg)[1]);

        AssertClose(2.0, Num(hover, "x"), "x");
        AssertClose(500.0, Num(hover, "y"), "y");
    }

    [Fact]
    public void Hover_OnASymlogYAxis_ReportsTheDataValue()
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure([1.0, 2.0, 3.0], [0.5, 300.0, 20.0], ax => ax.WithSymlogYScale(2.0)));

        var points = DrawnPoints(h.Svg);

        AssertClose(300.0, Num(HoverAt(h, points[1]), "y"), "y of the point past the threshold");
    }

    [Fact]
    public void Hover_OnASignMixedSymlogAxis_ReportsEveryPointOnBothSidesOfZero()
    {
        double[] y = [-400.0, 0.5, 60.0];
        using var h = InteractionScriptHarness.FromBuilder(Figure([1.0, 2.0, 3.0], y, ax => ax.WithSymlogYScale()));
        var points = DrawnPoints(h.Svg);

        for (int i = 0; i < y.Length; i++)
        {
            using var hi = InteractionScriptHarness.FromBuilder(Figure([1.0, 2.0, 3.0], y, ax => ax.WithSymlogYScale()));
            AssertClose(y[i], Num(HoverAt(hi, points[i]), "y"), $"y of point {i}");
        }
    }

    [Fact]
    public void Hover_OnASymlogXAxis_ReportsTheDataValue()
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure([-300.0, 0.25, 80.0], [1.0, 2.0, 3.0], ax => ax.WithSymlogXScale()));

        AssertClose(-300.0, Num(HoverAt(h, DrawnPoints(h.Svg)[0]), "x"), "x");
    }

    // ── brush select and the data cursor ──

    [Fact]
    public void BrushSelect_OnALogYAxis_ReportsTheDataBounds()
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure([1.0, 2.0, 3.0], [1.0, 500.0, 20.0], ax => ax.SetYScale(AxisScale.Log)));
        var mock = h.WireSignalRMock();
        var points = DrawnPoints(h.Svg);
        var svg = Svg(h);

        svg.Fire(new DomEvent("pointerdown") { target = svg, clientX = points[0].X, clientY = points[0].Y, shiftKey = true });
        svg.Fire(new DomEvent("pointerup") { target = svg, clientX = points[2].X, clientY = points[2].Y, shiftKey = true });

        var brush = Payload(mock, "OnBrushSelect");
        AssertClose(1.0, Num(brush, "x1"), "x1");
        AssertClose(3.0, Num(brush, "x2"), "x2");
        AssertClose(1.0, Num(brush, "y1"), "y1");
        AssertClose(20.0, Num(brush, "y2"), "y2");
    }

    [Fact]
    public void DataCursor_OnALogXAxis_ReportsTheClickedPointsDataValue()
    {
        using var h = InteractionScriptHarness.FromBuilder(Figure([1.0, 10.0, 1000.0], [1.0, 2.0, 3.0], ax => ax.SetXScale(AxisScale.Log)));
        var mock = h.WireSignalRMock();
        var group = h.Document.QuerySelectorAllRaw("[data-series-index]")
            .Single(g => g.getAttribute("aria-label") == "data" && !g.hasAttribute("data-legend-index"));
        var polyline = h.Document.Wrap(group.Xml.Descendants().Single(e => e.Name.LocalName == "polyline"));
        var p = DrawnPoints(h.Svg)[2];

        Svg(h).Fire(new DomEvent("click") { target = polyline, clientX = p.X, clientY = p.Y });

        var annotation = Assert.IsAssignableFrom<IDictionary<string, object?>>(Payload(mock, "OnDataCursor")["annotation"]);
        AssertClose(1000.0, Num(annotation, "dataX"), "dataX");
        AssertClose(3.0, Num(annotation, "dataY"), "dataY");
    }

    // ── wheel, pan and Home round trips ──

    [Fact]
    public void WheelZoomIn_OnALogXAxis_AppliedToTheFigure_KeepsThePointUnderThePointer_AndSpreadsTheDecadesEvenly()
    {
        var configure = Figure([1.0, 10.0, 1000.0], [1.0, 2.0, 3.0], ax => ax.SetXScale(AxisScale.Log));
        using var h = InteractionScriptHarness.FromBuilder(configure);
        var mock = h.WireSignalRMock();
        var before = DrawnPoints(h.Svg);
        var p = before[1];

        Svg(h).Fire(new DomEvent("wheel") { target = Svg(h), clientX = p.X, clientY = p.Y, deltaY = -100 });

        var zoom = Payload(mock, "OnZoom");
        var figure = Build(configure);
        new ZoomEvent("c", 0, Num(zoom, "xMin"), Num(zoom, "xMax"), Num(zoom, "yMin"), Num(zoom, "yMax")).ApplyTo(figure);
        var after = DrawnPoints(figure.ToSvg());

        Assert.True(Num(zoom, "xMin") > 0, $"a log axis cannot start at {Num(zoom, "xMin")}");
        Assert.InRange(after[1].X, p.X - 0.02, p.X + 0.02);
        Assert.InRange(after[1].Y, p.Y - 0.02, p.Y + 0.02);
        // Zooming in by 0.9 spreads the points by 1 / 0.9, and on a log axis the same for every decade.
        Assert.InRange(after[2].X - after[1].X, (before[2].X - before[1].X) / 0.9 - 0.05, (before[2].X - before[1].X) / 0.9 + 0.05);
        Assert.InRange(after[1].X - after[0].X, (before[1].X - before[0].X) / 0.9 - 0.05, (before[1].X - before[0].X) / 0.9 + 0.05);
    }

    [Fact]
    public void Pan_OnALogXAxis_AppliedToTheFigure_MovesEveryPointByTheDraggedPixels()
    {
        var configure = Figure([1.0, 10.0, 1000.0], [1.0, 2.0, 3.0], ax => ax.SetXScale(AxisScale.Log));
        using var h = InteractionScriptHarness.FromBuilder(configure);
        var mock = h.WireSignalRMock();
        var before = DrawnPoints(h.Svg);
        var p = before[1];
        var svg = Svg(h);

        svg.Fire(new DomEvent("pointerdown") { target = svg, clientX = p.X, clientY = p.Y });
        svg.Fire(new DomEvent("pointermove") { target = svg, clientX = p.X + 50, clientY = p.Y + 30 });
        svg.Fire(new DomEvent("pointerup") { target = svg, clientX = p.X + 50, clientY = p.Y + 30 });

        var pan = Payload(mock, "OnPan");
        var inView = Assert.IsAssignableFrom<IDictionary<string, object?>>(pan["inView"]);
        var figure = Build(configure);
        new PanEvent("c", 0, Num(pan, "dxData"), Num(pan, "dyData"))
        {
            InView = new DataRange(Num(inView, "xMin"), Num(inView, "xMax"), Num(inView, "yMin"), Num(inView, "yMax")),
        }.ApplyTo(figure);
        var after = DrawnPoints(figure.ToSvg());

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
        using var h = InteractionScriptHarness.FromBuilder(configure);
        var before = DrawnPoints(h.Svg);
        Svg(h).Fire(new DomEvent("wheel") { target = Svg(h), clientX = before[1].X, clientY = before[1].Y, deltaY = -100 });
        var mock = h.WireSignalRMock();

        Svg(h).Fire(new DomEvent("keydown") { target = Svg(h), key = "Home" });

        var reset = Payload(mock, "OnReset");
        var figure = Build(configure);
        new ResetEvent("c", 0, Num(reset, "xMin"), Num(reset, "xMax"), Num(reset, "yMin"), Num(reset, "yMax")).ApplyTo(figure);
        var after = DrawnPoints(figure.ToSvg());

        for (int i = 0; i < before.Length; i++)
        {
            Assert.InRange(after[i].X, before[i].X - 0.02, before[i].X + 0.02);
            Assert.InRange(after[i].Y, before[i].Y - 0.02, before[i].Y + 0.02);
        }
    }

    // ── the view carries the scale; a linear one carries nothing new ──

    [Fact]
    public void TheView_OfALinearAxes_CarriesNoScale()
    {
        var svg = Plt.Create().WithServerInteraction("c", s => s.All())
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0])).ToSvg();

        var mapped = XDocument.Parse(svg).Descendants().Where(e => e.Attribute("data-xmin") is not null).ToList();
        Assert.NotEmpty(mapped);
        Assert.All(mapped, e => Assert.DoesNotContain(e.Attributes(),
            a => a.Name.LocalName is "data-xscale" or "data-yscale" or "data-xlinthresh" or "data-ylinthresh"));
    }

    [Fact]
    public void TheView_OfASymlogAxes_NamesTheScaleAndItsThreshold_AndALogAxisNamesItsScale()
    {
        var svg = Plt.Create().WithServerInteraction("c", s => s.All())
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 10.0], [-5.0, 5.0]).SetXScale(AxisScale.Log).WithSymlogYScale(0.5)).ToSvg();

        var view = XDocument.Parse(svg).Descendants().Single(e => e.Attribute("data-axes-index") is not null);
        Assert.Equal("log", (string?)view.Attribute("data-xscale"));
        Assert.Null(view.Attribute("data-xlinthresh"));
        Assert.Equal("symlog", (string?)view.Attribute("data-yscale"));
        Assert.Equal("0.5", (string?)view.Attribute("data-ylinthresh"));
    }
}
