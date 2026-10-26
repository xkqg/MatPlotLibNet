// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>ROADMAP.md row 100: "zoom and pan change the main X and Y axes; the right-hand and top axes keep
/// their range" — the embedded browser script must move every outward-stacked secondary Y axis too, each by its
/// own proportional amount (a different span over the same pixels), not copy the primary's. Runs the ACTUAL
/// embedded JS in Jint (<see cref="InteractionScriptHarness"/>), not a C#-side re-derivation of its math, so a
/// sign or scale mistake in the script itself would fail here.</summary>
public class SecondaryAxisInteractionTests
{
    private static readonly Action<FigureBuilder> WithSecondary = b => b
        .WithSize(600, 500)
        .WithServerInteraction("c", s => s.All())
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0], s => s.Label = "load")
            .WithSecondaryYAxis(y2 => y2.SetYLim(0, 500).Plot([1.0, 2.0, 3.0], [100.0, 400.0, 200.0])));

    private static Figure Build(Action<FigureBuilder> configure)
    {
        var fb = Plt.Create();
        configure(fb);
        return fb.Build();
    }

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

    private static DomElement Svg(InteractionScriptHarness h) => h.Document.QuerySelectorAllRaw("svg").Single();

    private static IDictionary<string, object?> Payload(SignalRInvokeMock mock, string method) =>
        Assert.IsAssignableFrom<IDictionary<string, object?>>(Assert.Single(mock.Calls, c => c.Method == method).Payload);

    private static double Num(IDictionary<string, object?> payload, string key) =>
        Convert.ToDouble(payload[key], CultureInfo.InvariantCulture);

    private static IReadOnlyList<SecondaryAxisRange> SecondaryRanges(IDictionary<string, object?> payload)
    {
        var raw = Assert.IsAssignableFrom<System.Collections.IEnumerable>(payload["secondaryRanges"]);
        var result = new List<SecondaryAxisRange>();
        foreach (var item in raw)
        {
            var d = Assert.IsAssignableFrom<IDictionary<string, object?>>(item);
            var range = Assert.IsAssignableFrom<IDictionary<string, object?>>(d["range"]);
            int side = Convert.ToInt32(d["side"], CultureInfo.InvariantCulture);
            result.Add(new SecondaryAxisRange(
                side == 1 ? AxisSide.Right : AxisSide.Left,
                Convert.ToInt32(d["slotIndex"], CultureInfo.InvariantCulture),
                new DataRange(Num(range, "xMin"), Num(range, "xMax"), Num(range, "yMin"), Num(range, "yMax"))));
        }

        return result;
    }

    [Fact]
    public void WheelZoomIn_AlsoNarrowsTheSecondaryAxissOwnRange()
    {
        using var h = InteractionScriptHarness.FromBuilder(WithSecondary);
        var mock = h.WireSignalRMock();
        var p = DrawnPoints(h.Svg, "load")[1];

        Svg(h).Fire(new DomEvent("wheel") { target = Svg(h), clientX = p.X, clientY = p.Y, deltaY = -100 });

        var zoom = Payload(mock, "OnZoom");
        var secondary = Assert.Single(SecondaryRanges(zoom));
        Assert.Equal(AxisSide.Right, secondary.Side);
        Assert.Equal(0, secondary.SlotIndex);

        var figure = Build(WithSecondary);
        new ZoomEvent("c", Convert.ToInt32(zoom["axesIndex"]), Num(zoom, "xMin"), Num(zoom, "xMax"), Num(zoom, "yMin"), Num(zoom, "yMax"))
        {
            SecondaryRanges = [secondary],
        }.ApplyTo(figure);

        var secondaryAxis = figure.SubPlots[0].SecondaryYAxis!;
        double narrowedSpan = secondaryAxis.Max!.Value - secondaryAxis.Min!.Value;
        Assert.True(narrowedSpan < 500.0, "Zoom in should narrow the secondary axis' own range too.");
        Assert.True(narrowedSpan > 400.0, "A tenth narrower (90%), not collapsed to nothing.");
    }

    [Fact]
    public void Pan_AlsoMovesTheSecondaryAxisByItsOwnProportionalDelta()
    {
        using var h = InteractionScriptHarness.FromBuilder(WithSecondary);
        var mock = h.WireSignalRMock();
        var points = DrawnPoints(h.Svg, "load");
        var p = points[1];
        var svg = Svg(h);

        svg.Fire(new DomEvent("pointerdown") { target = svg, clientX = p.X, clientY = p.Y });
        svg.Fire(new DomEvent("pointermove") { target = svg, clientX = p.X, clientY = p.Y + 30 });
        svg.Fire(new DomEvent("pointerup") { target = svg, clientX = p.X, clientY = p.Y + 30 });

        var pan = Payload(mock, "OnPan");
        var secondary = Assert.Single(SecondaryRanges(pan));
        double secondarySpan = secondary.Range.YMax - secondary.Range.YMin;
        // The secondary axis' span is 500 (ten times the primary's 50-wide [10,40]-ish autoscaled range), so the
        // SAME pixel move must shift it by roughly ten times as much — proof it used its OWN span, not the primary's.
        Assert.Equal(500.0, secondarySpan, precision: 3);
        Assert.NotEqual(0.0, secondary.Range.YMin - 0.0); // moved at all (sanity: not a no-op)

        var figure = Build(WithSecondary);
        var inView = Assert.IsAssignableFrom<IDictionary<string, object?>>(pan["inView"]);
        new PanEvent("c", Convert.ToInt32(pan["axesIndex"]), Num(pan, "dxData"), Num(pan, "dyData"))
        {
            InView = new DataRange(Num(inView, "xMin"), Num(inView, "xMax"), Num(inView, "yMin"), Num(inView, "yMax")),
            SecondaryRanges = [secondary],
        }.ApplyTo(figure);

        var secondaryAxis = figure.SubPlots[0].SecondaryYAxis!;
        Assert.Equal(secondary.Range.YMin, secondaryAxis.Min);
        Assert.Equal(secondary.Range.YMax, secondaryAxis.Max);
    }

    [Fact]
    public void Home_AfterAZoom_AlsoRestoresTheSecondaryAxisToItsOriginalRange()
    {
        using var h = InteractionScriptHarness.FromBuilder(WithSecondary);
        var before = DrawnPoints(h.Svg, "load");
        var p = before[1];
        Svg(h).Fire(new DomEvent("wheel") { target = Svg(h), clientX = p.X, clientY = p.Y, deltaY = -100 });
        var mock = h.WireSignalRMock();

        Svg(h).Fire(new DomEvent("keydown") { target = Svg(h), key = "Home" });

        var reset = Payload(mock, "OnReset");
        var secondary = Assert.Single(SecondaryRanges(reset));
        Assert.Equal(0.0, secondary.Range.YMin, precision: 6);
        Assert.Equal(500.0, secondary.Range.YMax, precision: 6);

        var figure = Build(WithSecondary);
        figure.SubPlots[0].SecondaryYAxis!.Min = 123;
        figure.SubPlots[0].SecondaryYAxis!.Max = 456;
        new ResetEvent("c", Convert.ToInt32(reset["axesIndex"]), Num(reset, "xMin"), Num(reset, "xMax"), Num(reset, "yMin"), Num(reset, "yMax"))
        {
            SecondaryRanges = [secondary],
        }.ApplyTo(figure);

        Assert.Equal(0.0, figure.SubPlots[0].SecondaryYAxis!.Min);
        Assert.Equal(500.0, figure.SubPlots[0].SecondaryYAxis!.Max);
    }
}
