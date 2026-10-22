// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>A stat tile draws its own label: the name sits under the headline number, in the tile. A legend entry
/// for it would be a second copy of that name, in a swatch colour the tile is not drawn in, and in the corner of
/// the plot area it covers the number (the ops dashboard sample showed a swatch box over "12", "1" and "99.4").
/// So a stat tile is never a legend entry, and the one place that decides it is the entry list the drawing, the
/// hit-test and the margin reservation all read (<c>AxesLegendEntryExtensions.LegendEntries</c>).
/// <para>Every assertion reads the legend GROUP or that list: the tile's name is also in the SVG as the tile's own
/// text, so "the name is somewhere in the SVG" cannot tell a legend entry from the tile.</para></summary>
public class StatTileLegendTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];

    /// <summary>The text of every entry inside <c>&lt;g class="legend"&gt;</c>; empty when no legend was drawn.</summary>
    private static List<string> LegendTexts(string svg) =>
        XDocument.Parse(svg).Descendants()
            .Where(e => e.Name.LocalName == "g" && (string?)e.Attribute("class") == "legend")
            .SelectMany(g => g.Descendants().Where(t => t.Name.LocalName == "text"))
            .Select(t => t.Value)
            .ToList();

    private static FigureBuilder ThreeTiles() => Plt.Create()
        .WithGridSpec(1, 3)
        .AddSubPlot(GridPosition.Single(0, 0), ax => ax.StatTile(12, t => { t.Label = "Participants"; t.Format = "0"; }))
        .AddSubPlot(GridPosition.Single(0, 1), ax => ax.StatTile(1, t => { t.Label = "Alerts"; t.Format = "0"; }))
        .AddSubPlot(GridPosition.Single(0, 2), ax => ax.StatTile(99.4, t => { t.Label = "Uptime %"; t.Format = "0.0"; }));

    [Fact]
    public void AFigureOfOnlyLabelledStatTiles_DrawsNoLegendGroup()
    {
        var svg = ThreeTiles().ToSvg();

        Assert.DoesNotContain("class=\"legend\"", svg);
        Assert.Empty(LegendTexts(svg));
        // The tile's own label is still drawn: the fix removes the copy, not the name.
        Assert.Contains("Participants", svg);
        Assert.Contains("Alerts", svg);
        Assert.Contains("Uptime %", svg);
    }

    [Fact]
    public void TheEntryList_OfAStatTileAxes_IsEmpty_WhetherOrNotEndLabelsAreDrawn()
    {
        var axes = ThreeTiles().Build().SubPlots[0];

        Assert.Empty(axes.LegendEntries(directLabelsDrawn: false));
        Assert.Empty(axes.LegendEntries(directLabelsDrawn: true));
    }

    [Fact]
    public void ATile_BesideALabelledLine_LeavesOnlyTheLine_UnderItsOwnSeriesIndex()
    {
        var figure = Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .StatTile(5, t => t.Label = "Tile")
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "Line")).Build();

        var entry = Assert.Single(figure.SubPlots[0].LegendEntries(directLabelsDrawn: false));
        Assert.Equal("Line", entry.Label);
        Assert.Equal(1, entry.OriginalIndex);   // the tile ahead of it has no entry but still has slot 0

        Assert.Equal(["Line"], LegendTexts(figure.ToSvg()));
    }

    [Fact]
    public void TheHitTest_AndTheMeasurer_AgreeThatAStatTileHasNoEntry()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .StatTile(5, t => t.Label = "Tile")
                .WithLegend(position: LegendPosition.OutsideRight))
            .Build();

        // Sized: an outside legend reserves its margin from the same list, so a tile reserves none.
        var measured = LegendMeasurer.MeasureBox(figure.SubPlots[0], new SvgRenderContext(), figure.Theme);
        Assert.Equal(0, measured.Width);
        Assert.Equal(0, measured.Height);

        // Hit-tested: nothing on screen to click.
        var layout = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext());
        Assert.Empty(Assert.Single(layout.LegendItems));
    }

    [Fact]
    public void ATileOnASeriesWithoutALabel_StillAddsNothing()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StatTile(5)).Build().SubPlots[0];

        Assert.Empty(axes.LegendEntries(directLabelsDrawn: false));
    }
}
