// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;

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

    public static TheoryData<string, string[], int[]> Cases => new()
    {
        { "plain",                      ["A", "B"],                          [0, 1] },
        { "secondary series",           ["A", "S"],                          [0, 2] },
        { "legend values suffix",       ["A = 4.00", "B = 6.00"],            [0, 1] },
        { "unlabelled series first",    ["B"],                               [1] },
        { "all of them together",       ["B = 6.00", "S = 60.00"],           [1, 2] },
        { "direct-labelled series",     ["A"],                               [0] },
        { "direct-labelled secondary",  ["A", "T"],                          [0, 2] },
    };

    private static FigureBuilder Build(string name) => name switch
    {
        "plain" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "A")
            .Plot(X, [4.0, 5.0, 6.0], s => s.Label = "B")),
        "secondary series" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "A")
            .Plot(X, [4.0, 5.0, 6.0])
            .WithSecondaryYAxis(y2 => y2.Plot(X, [40.0, 50.0, 60.0], s => s.Label = "S"))),
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
            .WithSecondaryYAxis(y2 => y2.Plot(X, [40.0, 50.0, 60.0], s => s.Label = "S"))
            .WithLegendValues()),
        // A series whose name is drawn at the end of its line is not a legend entry; the entries around it keep
        // their own series index, so a click still toggles the series it names.
        "direct-labelled series" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "A")
            .Plot(X, [4.0, 5.0, 6.0], s => s.Label = "B")
            .WithDirectLabel()),
        "direct-labelled secondary" => Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "A")
            .WithSecondaryYAxis(y2 => y2
                .Plot(X, [40.0, 50.0, 60.0], s => s.Label = "S")
                .WithDirectLabel()
                .Plot(X, [60.0, 50.0, 40.0], s => s.Label = "T"))),
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
}
