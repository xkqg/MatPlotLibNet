// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Phase 5 Special-family fidelity tests (Sankey, Table, Treemap, Radar).</summary>
public class SpecialChartFidelityTests : FidelityTest
{
    [Theory]
    [FidelityRows]
    // matplotlib.sankey draws one junction as an arrowed polygon (flows in and out of a single system, labels beside the arrows) inside a framed
    // axes; the library's Sankey is a flow network (nodes in columns, a band for each link) with no frame: two diagrams that share a name, a
    // title and the first colour of the cycle, so the pixels the check can compare are the title, the colour of the flows and the quiet white
    // of the page. The score is what two such pictures measure, not a tolerance for a missing feature: the library has no arrowed-polygon mode
    // because that is another chart. Measured on Windows and in the noble container: classic RMS 69.90 / 69.86, SSIM 0.5737 / 0.5739, colour
    // distance 8.16 / 8.38; v2 RMS 66.39 / 66.34, SSIM 0.6043 / 0.6046, colour distance 61.88 on both (the v2 pictures have different dominant colours).
    [FidelityTolerance(Rms = 70.5, Ssim = 0.57, DeltaE = 9, V2Rms = 67, V2Ssim = 0.60, V2DeltaE = 62)]
    public void Sankey_Flows_MatchesMatplotlib(string themeId)
    {
        SankeyNode[] nodes =
        [
            new("In1"),
            new("In2"),
            new("First"),
            new("Second"),
            new("Third"),
            new("Fourth"),
            new("Fifth"),
        ];
        // The reference's flows are 25 and 60 in and 10, 20, 5, 15 and 35 out (Sankey(flows=[25, 0, 60, -10, -20, -5, -15, -35])).
        // A flow network names who sends what to whom, matplotlib's diagram does not: these links give each node the
        // reference's total, 25 and 60 on the left and 10, 20, 5, 15 and 35 on the right.
        SankeyLink[] links =
        [
            new(0, 2, 10),
            new(0, 3, 15),
            new(1, 3, 5),
            new(1, 4, 5),
            new(1, 5, 15),
            new(1, 6, 35),
        ];
        AssertFidelity(themeId, "sankey", "Sankey diagram", ax => ax
            .Sankey(nodes, links));
    }

    [Theory]
    [FidelityRows]
    // The reference is ax.table where it stands by default (loc='bottom'): below the axes, the data columns sharing the axes' width, the
    // row labels in a column hung left of the axes and fitted to their text, no cell at the corner above them, the axes above drawn as any
    // axes is, its tick labels over the table; the subplot parameters keep room below the axes for the six rows and left of it for the row
    // labels (subplots_adjust(left=0.2, bottom=0.3)). The check is the same figure with the same margins, its table placed by the
    // matplotlib themes, unasked. Measured on Windows: classic RMS 14.31, SSIM 0.9935, colour distance 0.36; v2 RMS 10.70, SSIM 0.9952, colour
    // distance 1.43 (what is left is the antialiasing of the glyphs, and the row labels' column, fitted to text another rasterizer measures).
    [FidelityTolerance(Rms = 15, Ssim = 0.98, DeltaE = 3, V2Rms = 11, V2Ssim = 0.98, V2DeltaE = 2)]
    public void Table_LossTable_MatchesMatplotlib(string themeId)
    {
        string[][] cellText =
        [
            ["30",  "12",  "4",  "2",  "1"],
            ["80",  "28",  "11", "5",  "2"],
            ["150", "45",  "22", "12", "5"],
            ["210", "90",  "50", "22", "11"],
            ["280", "120", "80", "45", "20"],
        ];
        // The style's own margins on the right and at the top (subplot parameters right 0.9 under both, top 0.88 under v2 and 0.9 under
        // classic), the reference's on the left and at the bottom.
        var figure = FidelityFigure(themeId)
            .WithSubPlotSpacing(spacing => spacing with
            {
                MarginLeft = 0.2 * FigWidth, MarginBottom = 0.3 * FigHeight, MarginRight = 0.1 * FigWidth,
                MarginTop = (themeId == "classic" ? 0.1 : 0.12) * FigHeight,
            })
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Loss table")
                .Table(cellText, s =>
                {
                    s.ColumnHeaders = ["Freeze", "Wind", "Flood", "Quake", "Hail"];
                    s.RowHeaders = ["100 year", "50 year", "20 year", "10 year", "5 year"];
                }))
            .Build();
        AssertFidelity(figure, "table");
    }

    [Theory]
    [FidelityRows]
    // The reference is drawn by the squarify package (squarify.plot on the same seven sizes), laid out in a rectangle of the axes' own pixels, as
    // the library lays out its own (squarify's default is a 100 x 100 square stretched to the axes, which is another layout), and without names
    // (squarify centres one, the library writes it in a corner). squarify lays its tiles out from y = 0 in a Y that runs up and draws them as
    // bars, clipped to the axes, so the largest tile stands at the bottom left and each tile has the edge of a bar (black 1 pt under classic,
    // none under v2, both at the tile's alpha): the library's treemap does the same under the matplotlib themes (TreemapLook.LikeSquarify,
    // PatchLook.EdgeOver). The check asks the library for the same picture: no names, no gap between tiles, no header (a named root draws
    // one) and a clear root, so the tiles at alpha 0.8 stand on the page as squarify's bars do, not on a root coloured from the map. squarify
    // colours each tile from a seeded random draw of the style's colour map; the check draws the same colours (the draws are the
    // reference's data). What is left: the title's glyphs, antialiased by another rasterizer.
    [FidelityTolerance(Rms = 4, Ssim = 0.98, DeltaE = 3, V2Rms = 5, V2Ssim = 0.98)]
    public void Treemap_SevenRegions_MatchesMatplotlib(string themeId)
    {
        double[] sizes = [500, 300, 200, 150, 100, 80, 60];
        string[] labels = ["A", "B", "C", "D", "E", "F", "G"];
        // squarify's colours: tile i is the style's image.cmap (jet under classic, viridis under v2) at the i-th draw of its seeded
        // random generator, at alpha 0.8; the generator writes the draws, as they do not depend on the style.
        double[] draws = ReferenceData.Load("treemap").Vector("draws");
        var map = themeId == "classic" ? ColorMaps.Jet : ColorMaps.Viridis;
        // The root is the frame the tiles stand in: a tile at alpha 0.8 over a root in a map colour would show that colour through, where
        // squarify's bars stand straight on the page, so the root is left clear and has no name (a named root draws a header strip).
        var root = new TreeNode
        {
            Color = Colors.Transparent,
            Children = [.. sizes.Select((s, i) => new TreeNode
            {
                Label = labels[i], Value = s, Color = map.GetColor(draws[i]).WithAlpha((byte)Math.Round(0.8 * 255)),
            })],
        };
        AssertFidelity(themeId, "treemap", "Treemap", ax => ax
            .Treemap(root, s => { s.Padding = 0; s.ShowLabels = false; })
            .HideAllAxes());
    }

    [Theory]
    [FidelityRows]
    // matplotlib has no radar axes; the reference draws the recipe of its gallery (a polar axes with a regular polygon for its patch and
    // spine and polygon rings, theta zero at north, turned clockwise as the library's categories run), on the pixels the library gives the
    // web (RADAR_WEB in tools/mpl_reference/generate.py), the outer ring at the largest value as MaxValue is. So the web, its rings
    // at the locator's ticks, their labels, the frame and the patch are compared, not two shapes. The row read RMS 38.93 / SSIM 0.7390
    // (classic) and 26.13 / 0.7630 (v2) against the polar disc this check once drew; against this reference the web as it was before
    // its rings followed the locator reads 17.08 / 0.9267 and 16.42 / 0.9103, and now 14.00 / 0.9723 and 13.14 / 0.9758. What is left
    // of the v2 colour distance (5.50 on Windows and on Linux) is the antialiasing of the grid over the fill.
    [FidelityTolerance(Rms = 15, Ssim = 0.96, DeltaE = 3, V2DeltaE = 8)]
    public void Radar_FiveAxes_MatchesMatplotlib(string themeId)
    {
        string[] categories = ["Speed", "Power", "Range", "Accuracy", "Cost"];
        double[] values = [0.8, 0.6, 0.7, 0.9, 0.5];
        AssertFidelity(themeId, "radar", "Radar — 5 axes", ax => ax
            .Radar(categories, values));
    }
}
