// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A series that asks for its name at the end of its line leaves the legend ONLY where that end label is
/// actually drawn. Cartesian axes draw it; polar and 3D axes share the legend drawing but draw no end labels, so a
/// flagged series there keeps its legend entry and its name is never lost.
/// <para>Every assertion reads the legend GROUP: the name also appears in the SVG as the end label itself, so
/// "the name is somewhere in the SVG" cannot tell a legend entry from a label.</para></summary>
public class DirectLabelLegendTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Theta = [0.0, 1.0, 2.0];
    private static readonly double[,] Z = { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };

    /// <summary>The text of every entry inside <c>&lt;g class="legend"&gt;</c>; empty when no legend was drawn.</summary>
    private static List<string> LegendTexts(string svg) =>
        XDocument.Parse(svg).Descendants()
            .Where(e => e.Name.LocalName == "g" && (string?)e.Attribute("class") == "legend")
            .SelectMany(g => g.Descendants().Where(t => t.Name.LocalName == "text"))
            .Select(t => t.Value)
            .ToList();

    private static void Flagged(LineSeries s, string label)
    {
        s.Label = label;
        s.LabelPlacement = SeriesLabelPlacement.LineEnd;
    }

    [Fact]
    public void OnCartesianAxes_ADirectlyLabelledLine_IsNotALegendEntry()
    {
        var svg = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "kept")
            .Plot(X, [4.0, 5.0, 6.0], s => Flagged(s, "at the end"))).ToSvg();

        Assert.Equal(["kept"], LegendTexts(svg));
    }

    [Fact]
    public void OnCartesianAxes_WhenEveryLabelIsDirect_NoLegendIsDrawn()
    {
        var svg = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => Flagged(s, "one"))
            .Plot(X, [4.0, 5.0, 6.0], s => Flagged(s, "two"))).ToSvg();

        Assert.DoesNotContain("class=\"legend\"", svg);
    }

    [Fact]
    public void OnCartesianAxes_ADirectlyLabelledStreamingLine_IsNotALegendEntry()
    {
        var svg = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot(X, [2.0, 3.0, 4.0], s => s.Label = "history");
            var live = ax.StreamingPlot(configure: s =>
            {
                s.Label = "live";
                s.LabelPlacement = SeriesLabelPlacement.LineEnd;
            });
            live.AppendPoint(1, 3);
            live.AppendPoint(2, 4);
        }).ToSvg();

        Assert.Equal(["history"], LegendTexts(svg));
    }

    [Fact]
    public void OnPolarAxes_ADirectlyLabelledLine_KeepsItsLegendEntry()
    {
        var builder = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .PolarPlot([1.0, 2.0, 3.0], Theta, s => s.Label = "polar")
            .Plot(X, [2.0, 3.0, 4.0], s => Flagged(s, "flagged")));
        var figure = builder.Build();
        Assert.Equal(MatPlotLibNet.Models.CoordinateSystem.Polar, figure.SubPlots[0].CoordinateSystem);

        Assert.Equal(["polar", "flagged"], LegendTexts(figure.ToSvg()));
    }

    [Fact]
    public void OnThreeDAxes_ADirectlyLabelledLine_KeepsItsLegendEntry()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Surface(X, X, Z, s => s.Label = "surface")
            .Plot(X, [2.0, 3.0, 4.0], s => Flagged(s, "flagged"))).Build();
        Assert.Equal(MatPlotLibNet.Models.CoordinateSystem.ThreeD, figure.SubPlots[0].CoordinateSystem);

        Assert.Contains("flagged", LegendTexts(figure.ToSvg()));
    }

    [Fact]
    public void OnPolarAxes_TheMeasurerCountsTheFlaggedEntry_LikeTheDrawing()
    {
        // The margin reservation asks the SAME renderer rule the drawing uses (through the renderer factory), so a
        // flagged series on polar axes is measured exactly as if it were not flagged.
        static FigureBuilder Polar(bool flag) => Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .PolarPlot([1.0, 2.0, 3.0], Theta, s => s.Label = "polar")
            .Plot(X, [2.0, 3.0, 4.0], s => { s.Label = "a much longer name"; if (flag) s.LabelPlacement = SeriesLabelPlacement.LineEnd; }));

        var flagged = Polar(true).Build();
        var plain = Polar(false).Build();
        var ctx = new SvgRenderContext();

        Assert.Equal(LegendMeasurer.MeasureBox(plain.SubPlots[0], ctx, plain.Theme),
                     LegendMeasurer.MeasureBox(flagged.SubPlots[0], ctx, flagged.Theme));
    }

    [Fact]
    public void OnCartesianAxes_TheMeasurerLeavesTheFlaggedEntryOut_LikeTheDrawing()
    {
        static FigureBuilder Cartesian(bool flag) => Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "short")
            .Plot(X, [4.0, 5.0, 6.0], s => { s.Label = "a much longer name"; if (flag) s.LabelPlacement = SeriesLabelPlacement.LineEnd; }));

        var flagged = Cartesian(true).Build();
        var only = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, [2.0, 3.0, 4.0], s => s.Label = "short")).Build();
        var ctx = new SvgRenderContext();

        Assert.Equal(LegendMeasurer.MeasureBox(only.SubPlots[0], ctx, only.Theme),
                     LegendMeasurer.MeasureBox(flagged.SubPlots[0], ctx, flagged.Theme));
    }
}
