// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A series on a secondary axis is drawn inside a series group of its own, opened by the same rule the primary
/// series pass uses: with interactive attributes the group carries <c>data-series-index</c> (the series' slot — the
/// index its legend entry carries) and an <c>aria-label</c>; without them a labelled series still gets an
/// <c>aria-label</c>led group. So the legend toggle, the hover highlight and a streaming diff reach a secondary line
/// exactly as they reach a primary one.</summary>
public class SecondarySeriesGroupTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];

    private static IEnumerable<XElement> Groups(XDocument doc) =>
        doc.Descendants().Where(e => e.Name.LocalName == "g");

    private static XElement SeriesGroup(XDocument doc, string ariaLabel) =>
        Assert.Single(Groups(doc), g => (string?)g.Attribute("class") == "series" && (string?)g.Attribute("aria-label") == ariaLabel);

    private static XElement LegendItem(XDocument doc, string ariaLabel) =>
        Assert.Single(Groups(doc), g => g.Attribute("data-legend-index") is not null && (string?)g.Attribute("aria-label") == ariaLabel);

    [Theory]
    [InlineData("S", "2")]
    [InlineData("T", "3")]
    public void WithInteractiveAttributes_ASecondaryYGroup_CarriesTheIndexOfItsLegendEntry_AndItsLabel(string name, string index)
    {
        var doc = XDocument.Parse(Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "A")
            .Plot(X, [3.0, 2.0, 1.0], s => s.Label = "B")
            .WithSecondaryYAxis(y2 => y2
                .Plot(X, [10.0, 20.0, 30.0], s => s.Label = "S")
                .Plot(X, [30.0, 20.0, 10.0], s => s.Label = "T"))).ToSvg());

        var group = SeriesGroup(doc, name);
        Assert.Equal(index, (string?)group.Attribute("data-series-index"));
        Assert.Equal(index, (string?)LegendItem(doc, name).Attribute("data-legend-index"));
        Assert.Single(group.Descendants(), e => e.Name.LocalName == "polyline");
    }

    [Fact]
    public void WithInteractiveAttributes_ASecondaryXGroup_CarriesTheSlotAfterThePrimaryAndSecondaryYSeries()
    {
        var doc = XDocument.Parse(Plt.Create().WithHighlight().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "A")
            .WithSecondaryYAxis(y2 => y2.Plot(X, [10.0, 20.0, 30.0], s => s.Label = "S"))
            .WithSecondaryXAxis(x2 => x2.PlotXSecondary([100.0, 200.0, 300.0], [1.0, 3.0, 2.0], s => s.Label = "top"))).ToSvg());

        var group = SeriesGroup(doc, "top");
        Assert.Equal("2", (string?)group.Attribute("data-series-index"));
        Assert.Single(group.Descendants(), e => e.Name.LocalName == "polyline");
    }

    [Fact]
    public void WithoutInteractiveAttributes_ALabelledSecondaryLine_GetsTheAccessibleGroupAPrimaryLineGets()
    {
        var doc = XDocument.Parse(Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "A")
            .WithSecondaryYAxis(y2 => y2
                .Plot(X, [10.0, 20.0, 30.0], s => s.Label = "S")
                .Plot(X, [30.0, 20.0, 10.0]))
            .WithSecondaryXAxis(x2 => x2.PlotXSecondary([100.0, 200.0, 300.0], [1.0, 3.0, 2.0], s => s.Label = "top"))).ToSvg());

        foreach (var name in new[] { "A", "S", "top" })
        {
            var group = SeriesGroup(doc, name);
            Assert.Null(group.Attribute("data-series-index"));
            Assert.Single(group.Descendants(), e => e.Name.LocalName == "polyline");
        }
        // The unlabelled secondary line has nothing to announce and gets no group, as an unlabelled primary line.
        Assert.Equal(3, Groups(doc).Count(g => (string?)g.Attribute("class") == "series"));
        Assert.Equal(4, doc.Descendants().Count(e => e.Name.LocalName == "polyline"));
    }

    [Fact]
    public void ASecondaryEndLabel_IsDrawnInsideTheGroupOfItsSeries()
    {
        var doc = XDocument.Parse(Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "A")
            .WithSecondaryYAxis(y2 => y2.Plot(X, [10.0, 20.0, 30.0], s => s.Label = "S").WithDirectLabel())).ToSvg());

        var label = Assert.Single(doc.Descendants(), e => e.Name.LocalName == "text" && e.Value == "S");
        Assert.Equal("1", (string?)label.Parent!.Attribute("data-series-index"));
        Assert.Equal((string?)label.Parent!.Descendants().Single(e => e.Name.LocalName == "polyline").Attribute("stroke"),
                     (string?)label.Attribute("fill"));
    }

    [Fact]
    public void AStreamingDiff_SeesAChangeOnTheSecondaryLine_AsAPatchOfThatSeriesAlone()
    {
        static string Render(double last) => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "A")
            .SetYLim(0, 4)
            .WithSecondaryYAxis(y2 => y2.SetYLim(0, 100).Plot(X, [10.0, 20.0, last], s => s.Label = "S"))).ToSvg();

        var patch = SvgDiffEngine.Compute(Render(30), Render(40));

        Assert.False(patch.IsFullReplace);
        var only = Assert.Single(patch.Patches);
        Assert.Equal(1, only.SeriesIndex);
        Assert.Contains("<polyline", only.NewContent);
    }
}
