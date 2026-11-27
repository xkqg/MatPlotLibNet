// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Tests.Rendering;

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
    [Theory]
    [InlineData("secondary", 2.0, 250.0, 300.0)] // AClickOnARightYLine_ReportsTheValueOnTheRightYAxis
    [InlineData("primary", 2.0, 2.0, 2.0)] // AClickOnThePrimaryLine_InTheSameFigure_StillReportsThePrimaryValue
    [InlineData("top", 2000.0, 1.0, 2.0)] // AClickOnASecondaryXLine_ReportsTheValueOnTheSecondaryXAxis
    public void AClickOnARightYLine_ReportsTheValueOnTheRightYAxis_Rows(string text, double value, double value2, double value3)
    {
        using var h = TwinAxes();

        var annotation = h.ClickDataCursor(text, 1);

        Assert.Equal(text, annotation["seriesLabel"]);
        Assert.Equal(value, annotation.Num("dataX"), value * 1e-4);
        Assert.Equal(value2, annotation.Num("dataY"), value3 * 1e-4);
    }

    [Fact]
    public void AClickOnALine_InAFigureWithoutASecondaryAxis_ReportsTheValueOnItsAxes()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithServerInteraction("c", s => s.EnableDataCursor())
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [10.0, 40.0, 20.0], s => s.Label = "load")));

        var annotation = h.ClickDataCursor("load", 2);

        Assert.Equal(3.0, annotation.Num("dataX"), 2.0 * 1e-4);
        Assert.Equal(20.0, annotation.Num("dataY"), 30.0 * 1e-4);
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

        h.Chart().Fire(new DomEvent("click")
        {
            target = h.Document.Wrap(group.Xml.Elements().First()), clientX = 55, clientY = 35,
        });

        var call = Assert.Single(mock.Calls, c => c.Method == "OnDataCursor");
        var annotation = (IDictionary<string, object?>)((IDictionary<string, object?>)call.Payload!)["annotation"]!;
        // Root limits 0..1 over the harness' synthetic 20 x 10 rect at (50, 30): (55-50)/20 = 0.25, 1 - (35-30)/10 = 0.5.
        Assert.Equal(0.25, annotation.Num("dataX"), 1.0 * 1e-4);
        Assert.Equal(0.5, annotation.Num("dataY"), 1.0 * 1e-4);
    }

    [Fact]
    public void WithoutTheDataCursor_ASeriesGroup_CarriesNoMapping()
    {
        // The mapping is weight only the data cursor reads: an interactive figure that did not ask for it keeps its bytes.
        var doc = Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "A")
            .WithAddRightYAxis(y2 => y2.Plot(X, [10.0, 20.0, 30.0], s => s.Label = "S"))).ToSvg().Xml();

        var groups = doc.Each("g").Where(e => e.Attribute("data-series-index") is not null).ToList();
        Assert.NotEmpty(groups);
        Assert.All(groups, g =>
        {
            Assert.Null(g.Attribute("data-plot-x"));
            Assert.Null(g.Attribute("data-ymin"));
        });
    }
}
