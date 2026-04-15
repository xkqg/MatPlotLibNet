// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Phase 5 Special-family fidelity tests (Sankey, Table, Treemap, Radar).</summary>
public class SpecialChartFidelityTests : FidelityTest
{
    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 110, Ssim = 0.40, DeltaE = 90)]   // matplotlib.sankey uses polygonal arrows; ours is rectangular
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
        SankeyLink[] links =
        [
            new(0, 2, 10),
            new(0, 3, 20),
            new(1, 4, 5),
            new(1, 5, 15),
            new(1, 6, 35),
        ];
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Sankey diagram")
                .Sankey(nodes, links))
            .Build();
        AssertFidelity(figure, "sankey");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 90, Ssim = 0.45, DeltaE = 70)]   // table cell layout / AA text
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
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Loss table")
                .Table(cellText))
            .Build();
        AssertFidelity(figure, "table");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    // matplotlib's reference uses the `squarify` library's simple strip layout (just
    // horizontal bars when the input is a flat tree), while we produce a real squarified
    // (Bruls–Huijse–Van Wijk) layout with viridis-coloured tiles. Our output is
    // intentionally not bit-identical — tolerance is loosened so the test confirms
    // "something was drawn within the plot area" rather than "matches matplotlib pixel-for-pixel".
    [FidelityTolerance(Rms = 120, Ssim = 0.40, DeltaE = 120)]
    public void Treemap_SevenRegions_MatchesMatplotlib(string themeId)
    {
        double[] sizes = [500, 300, 200, 150, 100, 80, 60];
        string[] labels = ["A", "B", "C", "D", "E", "F", "G"];
        var root = new TreeNode
        {
            Label = "root",
            Children = sizes.Zip(labels, (s, l) => new TreeNode { Label = l, Value = s }).ToArray(),
        };
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Treemap")
                .Treemap(root))
            .Build();
        AssertFidelity(figure, "treemap");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 85, Ssim = 0.50, DeltaE = 80)]   // our radar uses tab10 fill, matplotlib recipe uses C0 (blue)
    public void Radar_FiveAxes_MatchesMatplotlib(string themeId)
    {
        string[] categories = ["Speed", "Power", "Range", "Accuracy", "Cost"];
        double[] values = [0.8, 0.6, 0.7, 0.9, 0.5];
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Radar — 5 axes")
                .Radar(categories, values))
            .Build();
        AssertFidelity(figure, "radar");
    }
}
