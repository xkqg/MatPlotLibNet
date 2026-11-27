// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// Every text a series draws is set in the family of the theme the figure wears, from the one owner of that font
/// (<see cref="Rendering.ThemeFontExtensions.DataTextFont(Theme)"/>): a renderer that wrote <c>"sans-serif"</c> itself, or
/// built a font that carries the library's default family, drew its table cells, edge weights, key labels and cell names in
/// DejaVu Sans whatever family the theme asked for, beside tick labels that did take it.
/// </summary>
public class SeriesTextFamilyTests
{
    private const string ThemeFamily = "serif";

    private static readonly Theme Serif = Theme.CreateFrom(Theme.Default).WithFont(f => f with { Family = ThemeFamily }).Build();

    /// <summary>One chart and the texts it must draw: each of them is looked up in the picture and read for its family.</summary>
    private readonly record struct Case(Action<AxesBuilder> Draw, string[] Texts);

    private static readonly TreeNode Fleet = new()
    {
        Label = "Svc",
        Headline = "99 %",
        Children =
        [
            new() { Label = "Svc.Ingest", Headline = "13 %", Value = 1 },
            new() { Label = "Svc.Obs", Headline = "11 %", Value = 1 },
        ],
    };

    private static readonly GraphNode[] Nodes = [new("alpha", 0, 0), new("beta", 1, 1)];

    private static double[,] Ramp()
    {
        var z = new double[4, 6];
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 6; c++)
                z[r, c] = 2.0 * c;
        return z;
    }

    private static readonly Dictionary<string, Case> Cases = new()
    {
        ["Table"] = new(ax => ax.Table([["Alice", "90"], ["Bob", "85"]], s => s.ColumnHeaders = ["Name", "Score"]), ["Alice", "Name"]),
        ["NetworkGraphEdgeWeight"] = new(
            ax => ax.NetworkGraph(Nodes, [new GraphEdge("alpha", "beta", 2.5, false)], s => { s.Layout = GraphLayout.Manual; s.ShowEdgeWeights = true; }),
            ["2.5"]),
        ["NetworkGraphSelfLoopWeight"] = new(
            ax => ax.NetworkGraph(Nodes, [new GraphEdge("alpha", "alpha", 7.5, false)], s => { s.Layout = GraphLayout.Manual; s.ShowEdgeWeights = true; }),
            ["7.5"]),
        ["QuiverKey"] = new(ax => ax.QuiverKey(0.8, 0.9, 1.0, "1 m/s"), ["1 m/s"]),
        ["RelativeRotation"] = new(
            ax => ax.RelativeRotation(
                [Enumerable.Range(0, 60).Select(i => 100.0 + i).ToArray(), Enumerable.Repeat(95.0, 60).ToArray()],
                Enumerable.Repeat(100.0, 60).ToArray(), ["ETH", "BNB"]),
            ["ETH", "BNB"]),
        ["TreemapLeafAndParent"] = new(ax => ax.Treemap(Fleet), ["Svc.Ingest", "13 %", "Svc", "99 %"]),
        ["Icicle"] = new(ax => ax.Icicle(new TreeNode { Label = "Root", Children = [new() { Label = "Left", Value = 10 }, new() { Label = "Right", Value = 30 }] }), ["Left"]),
        ["Text3D"] = new(ax => ax.Text3D(1, 2, 3, "hello3d"), ["hello3d"]),
        ["ContourLevel"] = new(ax => ax.Contour([0.0, 1, 2, 3, 4, 5], [0.0, 1, 2, 3], Ramp(), s => { s.LevelValues = [0.37 * 2]; s.ShowLabels = true; }), ["0.74"]),
        ["DonutCentre"] = new(ax => ax.Donut([30.0, 70.0], null, s => s.CenterText = "centre"), ["centre"]),
        ["GaugeValue"] = new(ax => ax.Gauge(0.37), ["0.37"]),
        ["ProgressBarPercent"] = new(ax => ax.ProgressBar(0.37), ["37%"]),
        ["StateTimelineSegment"] = new(ax => ax.StateTimeline([new StateSegment(0, 10, "Running", Colors.Blue)]), ["Running"]),
        ["StatTile"] = new(ax => ax.StatTile(4217, t => { t.Label = "Processes"; t.Caption = "two over"; }), ["Processes", "two over"]),
        ["TreeGrid"] = new(
            ax => ax.TreeGrid(
                [new("Svc", ["24 %"]) { Expanded = true }, new("Svc.Billing", ["10 %"]) { Depth = 1 }],
                s => s.ColumnHeaders = ["CPU"]),
            ["CPU", "Svc", "Svc.Billing", "24 %"]),
    };

    public static TheoryData<string> Names => [.. Cases.Keys];

    private static string Render(Case chart) =>
        Plt.Create().WithSize(800, 500).WithTheme(Serif).AddSubPlot(1, 1, 1, chart.Draw).ToSvg();

    private static string FamilyOf(string svg, string text)
    {
        var match = svg.Elements("text").FirstOrDefault(element => element["font-family"] is not null && element.Text.StartsWith(text, StringComparison.Ordinal));
        Assert.True(match.Attributes is not null, $"no <text> reading '{text}' in the picture");
        return match["font-family"]!;
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void ASeriesText_IsSetInTheFamilyOfTheTheme(string name)
    {
        var chart = Cases[name];
        string svg = Render(chart);

        foreach (string text in chart.Texts)
            Assert.Equal(ThemeFamily, FamilyOf(svg, text));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void EveryTextOfTheChart_IsSetInTheFamilyOfTheTheme(string name)
    {
        string svg = Render(Cases[name]);

        var families = svg.Elements("text").Select(text => text["font-family"]).OfType<string>().Distinct().ToArray();

        Assert.Equal([ThemeFamily], families);
    }
}
