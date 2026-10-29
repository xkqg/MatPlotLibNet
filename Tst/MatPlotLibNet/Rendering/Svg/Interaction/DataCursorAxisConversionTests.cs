// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>A click on a line reports the value on the axis that line is drawn against. The data-cursor branch of the
/// SignalR script finds the series group the click landed in and converts the pointer with the mapping that group was
/// drawn with: the plot area and the data range of ITS axes — primary, secondary Y or secondary X — so a click on a
/// secondary line reports a value on the secondary axis, and every click is measured against the plot area, not the
/// whole picture.</summary>
public class DataCursorAxisConversionTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];

    /// <summary>A twin-axes figure with the data cursor on: a primary line, a secondary-Y line on a range two orders
    /// of magnitude away, and a line on a secondary X axis. Every axis is autoscaled, the way a caller writes it.</summary>
    private static InteractionScriptHarness TwinAxes() =>
        InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithServerInteraction("c", s => s.EnableDataCursor())
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "primary")
                .WithAddRightYAxis(y2 => y2.Plot(X, [100.0, 250.0, 400.0], s => s.Label = "secondary"))
                .WithSecondaryXAxis(x2 => x2.PlotXSecondary([1000.0, 2000.0, 3000.0], [3.0, 1.0, 2.0], s => s.Label = "top"))));

    /// <summary>The pixel the renderer drew point <paramref name="index"/> of the series labelled
    /// <paramref name="label"/> at, read from its polyline, and the polyline itself (the click target).</summary>
    private static (DomElement Target, double Px, double Py) DrawnPoint(InteractionScriptHarness h, string label, int index)
    {
        var group = h.Document.QuerySelectorAllRaw("[data-series-index]")
            .Single(g => g.getAttribute("aria-label") == label && !g.hasAttribute("data-legend-index"));
        var polyline = group.Xml.Descendants().Single(e => e.Name.LocalName == "polyline");
        var pair = polyline.Attribute("points")!.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)[index].Split(',');
        return (h.Document.Wrap(polyline),
                double.Parse(pair[0], CultureInfo.InvariantCulture),
                double.Parse(pair[1], CultureInfo.InvariantCulture));
    }

    private static IDictionary<string, object?> ClickAndReadAnnotation(InteractionScriptHarness h, string label, int index)
    {
        var mock = h.WireSignalRMock();
        var (target, px, py) = DrawnPoint(h, label, index);
        var svg = h.Document.QuerySelectorAllRaw("svg").Single();
        // The harness does not bubble, so the click is fired at the svg (where the listener sits) with the line as its
        // target, which is what a browser's bubbling delivers. Its screen CTM is the identity: client = user units.
        svg.Fire(new DomEvent("click") { target = target, clientX = px, clientY = py });
        var call = Assert.Single(mock.Calls, c => c.Method == "OnDataCursor");
        var payload = Assert.IsAssignableFrom<IDictionary<string, object?>>(call.Payload);
        return Assert.IsAssignableFrom<IDictionary<string, object?>>(payload["annotation"]);
    }

    private static void AssertNear(double expected, object? actual, double range, string what)
    {
        double value = Convert.ToDouble(actual, CultureInfo.InvariantCulture);
        Assert.True(Math.Abs(value - expected) <= range * 1e-6,
            $"{what}: expected {expected.ToString(CultureInfo.InvariantCulture)}, the click reported {value.ToString("R", CultureInfo.InvariantCulture)}");
    }

    [Fact]
    public void AClickOnARightYLine_ReportsTheValueOnTheRightYAxis()
    {
        using var h = TwinAxes();

        var annotation = ClickAndReadAnnotation(h, "secondary", 1);

        Assert.Equal("secondary", annotation["seriesLabel"]);
        AssertNear(2.0, annotation["dataX"], 2.0, "dataX");
        AssertNear(250.0, annotation["dataY"], 300.0, "dataY");
    }

    [Fact]
    public void AClickOnThePrimaryLine_InTheSameFigure_StillReportsThePrimaryValue()
    {
        using var h = TwinAxes();

        var annotation = ClickAndReadAnnotation(h, "primary", 1);

        Assert.Equal("primary", annotation["seriesLabel"]);
        AssertNear(2.0, annotation["dataX"], 2.0, "dataX");
        AssertNear(2.0, annotation["dataY"], 2.0, "dataY");
    }

    [Fact]
    public void AClickOnASecondaryXLine_ReportsTheValueOnTheSecondaryXAxis()
    {
        using var h = TwinAxes();

        var annotation = ClickAndReadAnnotation(h, "top", 1);

        Assert.Equal("top", annotation["seriesLabel"]);
        AssertNear(2000.0, annotation["dataX"], 2000.0, "dataX");
        AssertNear(1.0, annotation["dataY"], 2.0, "dataY");
    }

    [Fact]
    public void AClickOnALine_InAFigureWithoutASecondaryAxis_ReportsTheValueOnItsAxes()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithServerInteraction("c", s => s.EnableDataCursor())
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [10.0, 40.0, 20.0], s => s.Label = "load")));

        var annotation = ClickAndReadAnnotation(h, "load", 2);

        AssertNear(3.0, annotation["dataX"], 2.0, "dataX");
        AssertNear(20.0, annotation["dataY"], 30.0, "dataY");
    }

    [Fact]
    public void AClickOnASeriesDrawnWithoutACartesianRange_StillReports_FromTheCachedLimits()
    {
        // A polar line is not drawn through a data range, so its group carries no mapping; the click still reaches the
        // hub, converted from the limits cached on the root as before.
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithServerInteraction("c", s => s.EnableDataCursor())
            .AddSubPlot(1, 1, 1, ax => ax.PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0], s => s.Label = "orbit")));
        var mock = h.WireSignalRMock();
        var group = h.Document.QuerySelectorAllRaw("[data-series-index]")
            .Single(g => g.getAttribute("aria-label") == "orbit" && !g.hasAttribute("data-legend-index"));
        Assert.False(group.hasAttribute("data-plot-w"));

        h.Document.QuerySelectorAllRaw("svg").Single().Fire(new DomEvent("click")
        {
            target = h.Document.Wrap(group.Xml.Elements().First()), clientX = 55, clientY = 35,
        });

        var call = Assert.Single(mock.Calls, c => c.Method == "OnDataCursor");
        var annotation = (IDictionary<string, object?>)((IDictionary<string, object?>)call.Payload!)["annotation"]!;
        // Root limits 0..1 over the harness' synthetic 20 x 10 rect at (50, 30): (55-50)/20 = 0.25, 1 - (35-30)/10 = 0.5.
        AssertNear(0.25, annotation["dataX"], 1.0, "dataX");
        AssertNear(0.5, annotation["dataY"], 1.0, "dataY");
    }

    [Fact]
    public void WithoutTheDataCursor_ASeriesGroup_CarriesNoMapping()
    {
        // The mapping is weight only the data cursor reads: an interactive figure that did not ask for it keeps its bytes.
        var doc = XDocument.Parse(Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "A")
            .WithAddRightYAxis(y2 => y2.Plot(X, [10.0, 20.0, 30.0], s => s.Label = "S"))).ToSvg());

        var groups = doc.Descendants().Where(e => e.Name.LocalName == "g" && e.Attribute("data-series-index") is not null).ToList();
        Assert.NotEmpty(groups);
        Assert.All(groups, g =>
        {
            Assert.Null(g.Attribute("data-plot-x"));
            Assert.Null(g.Attribute("data-ymin"));
        });
    }
}
