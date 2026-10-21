// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>A legend toggle comes only from a legend entry. In a figure with server interaction the plotted lines are
/// drawn inside groups that carry <c>data-series-index</c> too, so a toggle anchored on that attribute turned a click on
/// a line into a legend click: the server hid the very line the reader had just clicked to read a value from.</summary>
public class LegendToggleAnchorTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];

    private static readonly Action<FigureBuilder> TwinAxes = b => b
        .WithSize(600, 500)
        .WithServerInteraction("c", s => s.EnableLegendToggle().EnableDataCursor())
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "primary")
            .WithSecondaryYAxis(y2 => y2.Plot(X, [100.0, 250.0, 400.0], s => s.Label = "secondary")));

    private static DomElement SeriesGroup(InteractionScriptHarness h, string label) =>
        h.Document.QuerySelectorAllRaw("[data-series-index]").Single(g => g.getAttribute("aria-label") == label);

    private static DomElement LegendEntry(InteractionScriptHarness h, string label) =>
        h.Document.QuerySelectorAllRaw("[data-legend-index]").Single(g => g.getAttribute("aria-label") == label);

    /// <summary>Clicks <paramref name="target"/> the way a browser delivers it: at the svg (where the listeners sit),
    /// with the clicked element as the target, at a point inside the plot.</summary>
    private static List<(string Method, object? Payload)> Click(InteractionScriptHarness h, DomElement target)
    {
        var mock = h.WireSignalRMock();
        h.Document.QuerySelectorAllRaw("svg").Single().Fire(new DomEvent("click") { target = target, clientX = 300, clientY = 250 });
        return mock.Calls;
    }

    [Fact]
    public void AClickOnAPlottedLine_SendsTheDataCursor_AndNoLegendToggle()
    {
        using var h = InteractionScriptHarness.FromBuilder(TwinAxes);
        var polyline = h.Document.Wrap(SeriesGroup(h, "primary").Xml.Descendants().First(e => e.Name.LocalName == "polyline"));

        var calls = Click(h, polyline);

        Assert.Contains(calls, c => c.Method == "OnDataCursor");
        Assert.DoesNotContain(calls, c => c.Method == "OnLegendToggle");
    }

    [Fact]
    public void AClickOnALegendEntry_SendsTheLegendToggle_AndNoDataCursor()
    {
        using var h = InteractionScriptHarness.FromBuilder(TwinAxes);
        var text = h.Document.Wrap(LegendEntry(h, "primary").Xml.Descendants().First(e => e.Name.LocalName == "text"));

        var calls = Click(h, text);

        var toggle = Assert.Single(calls, c => c.Method == "OnLegendToggle");
        Assert.Equal(0, Convert.ToInt32(((IDictionary<string, object?>)toggle.Payload!)["seriesIndex"]));
        Assert.DoesNotContain(calls, c => c.Method == "OnDataCursor");
    }

    [Fact]
    public void AClickOnASecondaryLegendEntry_TogglesThatSecondarySeries()
    {
        using var h = InteractionScriptHarness.FromBuilder(TwinAxes);

        var calls = Click(h, LegendEntry(h, "secondary"));

        var toggle = Assert.Single(calls, c => c.Method == "OnLegendToggle");
        var payload = (IDictionary<string, object?>)toggle.Payload!;
        int seriesIndex = Convert.ToInt32(payload["seriesIndex"]);
        Assert.Equal(SeriesGroup(h, "secondary").getAttribute("data-series-index"), seriesIndex.ToString());

        // The server applies what the page sent to the figure the page shows: the secondary line goes, the primary stays.
        var fb = Plt.Create();
        TwinAxes(fb);
        var figure = fb.Build();
        new LegendToggleEvent("c", Convert.ToInt32(payload["axesIndex"]), seriesIndex).ApplyTo(figure);

        var axes = figure.SubPlots[0];
        Assert.False(((ChartSeries)axes.SecondarySeries[0]).Visible);
        Assert.True(((ChartSeries)axes.Series[0]).Visible);
    }
}
