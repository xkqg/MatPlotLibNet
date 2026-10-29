// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A stacked area draws its layers as one series, and a layer that has a name is a legend entry of its own: one entry
/// for each named layer, in the colour that layer is drawn in, all of them the one series (one index, so a click anywhere on
/// them reaches that series). A stack whose layers are not named has one entry, under the series' name.</summary>
public class StackedAreaLegendTests
{
    private static readonly double[] X = [0.0, 1, 2];

    private static double[][] Layers(int count) =>
        [.. Enumerable.Range(0, count).Select(i => new[] { 1.0 + i, 2.0, 1.0 })];

    private static XDocument Legend(Action<AxesBuilder> axes) =>
        XDocument.Parse(Plt.Create().WithLegendToggle().WithSize(500, 400).AddSubPlot(1, 1, 1, axes).ToSvg());

    /// <summary>The legend entries of the picture: the label of each and the colour of its swatch (the fill of a rectangle, the
    /// stroke of a line).</summary>
    private static List<(string Label, string Fill)> Entries(XDocument doc) =>
        [.. doc.Descendants().Where(e => e.Name.LocalName == "g" && e.Attribute("data-legend-index") is not null)
            .Select(g =>
            {
                var swatch = g.Descendants().First(e => e.Name.LocalName is "rect" or "line");
                return ((string)g.Attribute("aria-label")!, (string)(swatch.Attribute("fill") ?? swatch.Attribute("stroke"))!);
            })];

    private static string Hex(int slot) => Theme.Default.CycleColors[slot % Theme.Default.CycleColors.Length].ToHex();

    [Fact]
    public void EachNamedLayer_IsAnEntry_InTheColourItsLayerIsDrawnIn()
    {
        var entries = Entries(Legend(ax => ax.StackPlot(X, Layers(3), s => s.Labels = ["a", "b", "c"])));

        Assert.Equal(["a", "b", "c"], entries.Select(e => e.Label));
        for (int layer = 0; layer < 3; layer++)
            Assert.Equal(Hex(layer), entries[layer].Fill, ignoreCase: true);
    }

    [Fact]
    public void ALineAfterTheStack_WearsTheColourPastTheLayers_InItsEntry()
    {
        var entries = Entries(Legend(ax => ax
            .StackPlot(X, Layers(3), s => s.Labels = ["a", "b", "c"])
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "line")));

        Assert.Equal(["a", "b", "c", "line"], entries.Select(e => e.Label));
        Assert.Equal(Hex(3), entries[3].Fill, ignoreCase: true);
    }

    [Fact]
    public void TwelveLayers_WrapAroundTheCycle_InTheLegendAsInTheDrawing()
    {
        string[] names = [.. Enumerable.Range(0, 12).Select(i => $"layer {i}")];

        var entries = Entries(Legend(ax => ax.StackPlot(X, Layers(12), s => s.Labels = names)));

        Assert.Equal(12, entries.Count);
        for (int layer = 0; layer < 12; layer++)
            Assert.Equal(Hex(layer), entries[layer].Fill, ignoreCase: true);
    }

    [Fact]
    public void ABlankName_DrawsNoEntry_AndTheNamedLayersAfterItKeepTheirOwnColour()
    {
        var entries = Entries(Legend(ax => ax.StackPlot(X, Layers(3), s => s.Labels = ["a", "", "c"])));

        Assert.Equal(["a", "c"], entries.Select(e => e.Label));
        Assert.Equal(Hex(0), entries[0].Fill, ignoreCase: true);
        Assert.Equal(Hex(2), entries[1].Fill, ignoreCase: true);
    }

    [Fact]
    public void NamesOfLayersThatAreNotThere_AreIgnored()
    {
        var entries = Entries(Legend(ax => ax.StackPlot(X, Layers(2), s => s.Labels = ["a", "b", "ghost"])));

        Assert.Equal(["a", "b"], entries.Select(e => e.Label));
    }

    [Fact]
    public void WhenTheLayersAreNamed_TheSeriesOwnNameIsNotASecondRowBesideThem()
    {
        var entries = Entries(Legend(ax => ax.StackPlot(X, Layers(2), s => { s.Label = "Stack"; s.Labels = ["a", "b"]; })));

        Assert.Equal(["a", "b"], entries.Select(e => e.Label));
    }

    [Fact]
    public void WhenTheLayersAreNotNamed_TheStackIsOneEntry_UnderTheSeriesName()
    {
        var entries = Entries(Legend(ax => ax.StackPlot(X, Layers(3), s => s.Label = "Stack")));

        Assert.Equal("Stack", Assert.Single(entries).Label);
        Assert.Equal(Hex(0), entries[0].Fill, ignoreCase: true);
    }

    // ---- what the series says ---------------------------------------------------------------------------------------

    [Fact]
    public void ASeriesWithNoName_SaysNoParts() =>
        Assert.Empty(new LineSeries([1.0], [2.0]).LegendParts);

    [Fact]
    public void ASeriesWithAName_SaysOnePart_ItsSwatchAndItsOwnColour()
    {
        var part = Assert.Single(new LineSeries([1.0], [2.0]) { Label = "L" }.LegendParts);

        Assert.Equal(new LegendPart("L", new LineSeries([1.0], [2.0]).LegendSwatch), part);
        Assert.Equal(0, part.ColorSlotOffset);
    }

    [Fact]
    public void AStackWithNamedLayers_SaysOnePartForEach_AtTheColourOfItsLayer()
    {
        var stack = new StackedAreaSeries(X, Layers(3)) { Labels = ["a", "", "c"] };

        var parts = stack.LegendParts;

        Assert.Equal(["a", "c"], parts.Select(p => p.Label));
        Assert.Equal([0, 2], parts.Select(p => p.ColorSlotOffset));
        Assert.All(parts, p => Assert.Equal(LegendSwatchKind.Patch, p.Swatch.Kind));
    }
}
