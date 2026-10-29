// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The legend is described three times: drawn (<c>AxesRenderer.RenderLegend</c>), hit-tested
/// (<c>AxesRenderer.ComputeLegendBounds</c>, which feeds <c>ChartRenderer.ComputeLayout</c>) and sized
/// (<c>LegendMeasurer.MeasureBox</c>, which reserves the margin of an outside legend). The three used to walk
/// different lists — the measurer and the hit-test skipped the secondary-axis series and the legend-values
/// suffix — so a click missed an entry that was on screen and an outside legend was reserved too narrow.
/// These cases pin that all three read ONE list: the same entries, in the same order, with the same labels.</summary>
public class LegendEntryAgreementTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];

    private static double[][] Layers() => [[1.0, 2.0, 3.0], [2.0, 3.0, 4.0], [3.0, 4.0, 5.0]];

    public static TheoryData<string, string[], int[]> Cases => new()
    {
        { "plain",                      ["A", "B"],                          [0, 1] },
        { "secondary series",           ["A", "S"],                          [0, 2] },
        { "legend values suffix",       ["A = 4.00", "B = 6.00"],            [0, 1] },
        { "unlabelled series first",    ["B"],                               [1] },
        { "all of them together",       ["B = 6.00", "S = 60.00"],           [1, 2] },
        { "direct-labelled series",     ["A"],                               [0] },
        { "direct-labelled secondary",  ["A", "T"],                          [0, 2] },
        { "second right axis",          ["A", "S", "S2"],                    [0, 1, 2] },
        { "left axis",                  ["A", "L"],                          [0, 1] },
        { "left and right together",    ["A", "S", "L"],                     [0, 1, 2] },
        { "polar axes",                 ["P"],                               [0] },
        { "3-D axes",                   ["T"],                               [0] },
        { "stackplot 3 labelled",       ["a", "b", "c"],                     [0, 0, 0] },
        { "stackplot then line",        ["a", "b", "c", "line"],             [0, 0, 0, 1] },
        { "stackplot with a blank part", ["a", "c"],                         [0, 0] },
        { "stackplot named as one",     ["Stack"],                           [0] },
    };

    public static TheoryData<string> CaseNames =>
    [
        "plain", "secondary series", "legend values suffix", "unlabelled series first", "all of them together",
        "direct-labelled series", "direct-labelled secondary", "second right axis", "left axis", "left and right together",
        "polar axes", "3-D axes", "stackplot 3 labelled", "stackplot then line", "stackplot with a blank part", "stackplot named as one",
    ];

    private static FigureBuilder Build(string name) => name switch
    {
        "plain" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "A")
            .Plot(X, [4.0, 5.0, 6.0], s => s.Label = "B")),
        "secondary series" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "A")
            .Plot(X, [4.0, 5.0, 6.0])
            .WithAddRightYAxis(y2 => y2.Plot(X, [40.0, 50.0, 60.0], s => s.Label = "S"))),
        "legend values suffix" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "A")
            .Plot(X, [4.0, 5.0, 6.0], s => s.Label = "B")
            .WithLegendValues()),
        "unlabelled series first" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0])
            .Plot(X, [4.0, 5.0, 6.0], s => s.Label = "B")),
        "all of them together" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0])
            .Plot(X, [4.0, 5.0, 6.0], s => s.Label = "B")
            .WithAddRightYAxis(y2 => y2.Plot(X, [40.0, 50.0, 60.0], s => s.Label = "S"))
            .WithLegendValues()),
        // A series whose name is drawn at the end of its line is not a legend entry; the entries around it keep
        // their own series index, so a click still toggles the series it names.
        "direct-labelled series" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "A")
            .Plot(X, [4.0, 5.0, 6.0], s => s.Label = "B")
            .WithDirectLabel()),
        "direct-labelled secondary" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "A")
            .WithAddRightYAxis(y2 => y2
                .Plot(X, [40.0, 50.0, 60.0], s => s.Label = "S")
                .WithDirectLabel()
                .Plot(X, [60.0, 50.0, 40.0], s => s.Label = "T"))),
        // ROADMAP.md row 101 — a 2nd right axis (or a left-side secondary axis) must get legend entries too, not
        // only right slot 0: the legend, the hit-test and the measurer share ONE list, so they share this gap too.
        "second right axis" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "A")
            .WithAddRightYAxis(y2 => y2.Plot(X, [40.0, 50.0, 60.0], s => s.Label = "S"))
            .WithAddRightYAxis(outer => outer.Plot(X, [400.0, 500.0, 600.0], s => s.Label = "S2"))),
        "left axis" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "A")
            .WithAddLeftYAxis(left => left.Plot(X, [0.1, 0.2, 0.3], s => s.Label = "L"))),
        "left and right together" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "A")
            .WithAddRightYAxis(y2 => y2.Plot(X, [40.0, 50.0, 60.0], s => s.Label = "S"))
            .WithAddLeftYAxis(left => left.Plot(X, [0.1, 0.2, 0.3], s => s.Label = "L"))),
        "polar axes" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0], s => s.Label = "P")),
        "3-D axes" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Scatter3D([1.0, 2.0, 3.0], [1.0, 3.0, 2.0], [3.0, 4.0, 2.0], s => s.Label = "T")),
        // A stacked area names its layers: one entry for each, all of them the one series (one index), each in its layer's colour.
        "stackplot 3 labelled" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .StackPlot(X, Layers(), s => s.Labels = ["a", "b", "c"])),
        "stackplot then line" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .StackPlot(X, Layers(), s => s.Labels = ["a", "b", "c"])
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "line")),
        "stackplot with a blank part" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .StackPlot(X, Layers(), s => s.Labels = ["a", "", "c"])),
        // The series' own label is the entry of a stack whose layers are not named; it is not a second row beside them.
        "stackplot named as one" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .StackPlot(X, Layers(), s => s.Label = "Stack")),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    private static List<(int Index, string Label)> DrawnEntries(string svg) =>
        Regex.Matches(svg, "data-legend-index=\"(?<i>\\d+)\"[^>]*aria-label=\"(?<l>[^\"]*)\"")
            .Select(m => (int.Parse(m.Groups["i"].Value, CultureInfo.InvariantCulture), m.Groups["l"].Value))
            .ToList();

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheDrawnLegend_TheHitTest_AndTheMeasurer_ReadOneList(string name, string[] labels, int[] indices)
    {
        var figure = Build(name).Build();
        var svg = figure.ToSvg();

        // Drawn: the entries of the interactive SVG, with the index each one carries.
        var drawn = DrawnEntries(svg);
        Assert.Equal(labels, drawn.Select(d => d.Label));
        Assert.Equal(indices, drawn.Select(d => d.Index));

        // Hit-tested: ComputeLegendBounds through ChartRenderer.ComputeLayout, same count, same series indices.
        var layout = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext());
        var bounds = layout.LegendItems[0];
        Assert.Equal(drawn.Count, bounds.Count);
        Assert.Equal(indices, bounds.Select(b => b.SeriesIndex));

        // Sized: MeasureBox reserves the box the renderer draws, so it counted the same rows and the same labels.
        var measured = LegendMeasurer.MeasureBox(figure.SubPlots[0], new SvgRenderContext(), figure.Theme);
        var frame = Regex.Match(svg,
            "<rect x=\"[0-9.]+\" y=\"[0-9.]+\" width=\"(?<w>[0-9.]+)\" height=\"(?<h>[0-9.]+)\"[^>]*stroke=\"#CCCCCC\"");
        Assert.True(frame.Success, "legend frame rect not found");
        double drawnW = double.Parse(frame.Groups["w"].Value, CultureInfo.InvariantCulture);
        double drawnH = double.Parse(frame.Groups["h"].Value, CultureInfo.InvariantCulture);
        Assert.True(Math.Abs(measured.Width - drawnW) < 0.5,
            $"{name}: measured width {measured.Width:F2} disagrees with drawn width {drawnW:F2}.");
        Assert.True(Math.Abs(measured.Height - drawnH) < 0.5,
            $"{name}: measured height {measured.Height:F2} disagrees with drawn height {drawnH:F2}.");
    }

    /// <summary>A Best legend looks for its place among the marks the chart drew, and the draw pass and the hit-test pass
    /// both ask: the entries a pointer is tested against stand inside the box that is drawn, wherever it stands.</summary>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public void TheDrawnLegend_AndTheHitTest_StandInOnePlace(string name)
    {
        var figure = Build(name).Build();
        var frame = figure.ToSvg().LegendFrame()!.Value;

        var bounds = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).LegendItems[0];

        Assert.NotEmpty(bounds);
        foreach (var item in bounds)
        {
            Assert.True(item.Bounds.X >= frame.X && item.Bounds.Right <= frame.Right + 0.001
                        && item.Bounds.Y >= frame.Y && item.Bounds.Bottom <= frame.Bottom + 0.001,
                $"{name}: the entry {item.SeriesIndex} the pointer is tested against ({item.Bounds}) is not inside the drawn legend ({frame}).");
        }
    }

    /// <summary>Two axes in one figure each find the place of their own legend, in the draw and in the hit-test.</summary>
    [Fact]
    public void TwoSubplots_EachStandItsBestLegendWhereItsOwnMarksAre()
    {
        double[] ramp = [0, 1, 2, 3, 4, 5];
        var figure = Plt.Create().WithSize(800, 350)
            .AddSubPlot(1, 2, 1, ax => ax.Plot(ramp, ramp, s => s.Label = "up").WithLegend())
            .AddSubPlot(1, 2, 2, ax => ax.Plot(ramp, ramp.Select(v => 5 - v).ToArray(), s => s.Label = "down").WithLegend())
            .Build();
        string svg = figure.ToSvg();
        var layout = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext());
        var frames = Regex.Matches(svg, @"<g class=""legend""[^>]*>\s*<rect x=""(?<x>[0-9.]+)"" y=""(?<y>[0-9.]+)"" width=""(?<w>[0-9.]+)"" height=""(?<h>[0-9.]+)""")
            .Select(m => new Rect(double.Parse(m.Groups["x"].Value, CultureInfo.InvariantCulture), double.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups["w"].Value, CultureInfo.InvariantCulture), double.Parse(m.Groups["h"].Value, CultureInfo.InvariantCulture)))
            .ToList();

        Assert.Equal(2, frames.Count);
        for (int axes = 0; axes < 2; axes++)
        {
            var entry = Assert.Single(layout.LegendItems[axes]);
            Assert.True(entry.Bounds.X >= frames[axes].X && entry.Bounds.Right <= frames[axes].Right + 0.001,
                $"subplot {axes}: the hit-test entry {entry.Bounds} is not inside the drawn legend {frames[axes]}.");
        }
        Assert.True(frames[0].X < frames[1].X);   // the sanity of the pair: the first axes is the left one
    }
}
