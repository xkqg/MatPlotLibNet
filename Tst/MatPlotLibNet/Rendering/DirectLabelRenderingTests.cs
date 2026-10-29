// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>End labels as they reach the SVG: in the colour of their line, inside the figure in both layout paths,
/// inside the group of their series, and only for the series that can carry one.</summary>
public class DirectLabelRenderingTests
{
    private const double FigureWidth = 800;
    private static readonly double[] X = [0.0, 1.0, 2.0];

    private static IEnumerable<XElement> Named(XContainer root, string name) =>
        root.Descendants().Where(e => e.Name.LocalName == name);

    private static bool InLegend(XElement e) =>
        e.Ancestors().Any(a => a.Name.LocalName == "g" && (string?)a.Attribute("class") == "legend");

    /// <summary>The drawn end label with this text (never a legend entry).</summary>
    private static XElement Label(XDocument doc, string text) =>
        Assert.Single(Named(doc, "text"), t => t.Value == text && !InLegend(t));

    private static XElement? LabelOrNull(XDocument doc, string text) =>
        Named(doc, "text").SingleOrDefault(t => t.Value == text && !InLegend(t));

    private static List<string> LegendTexts(XDocument doc) =>
        Named(doc, "text").Where(InLegend).Select(t => t.Value).ToList();

    private static double Num(XElement e, string attr) =>
        double.Parse((string)e.Attribute(attr)!, CultureInfo.InvariantCulture);

    /// <summary>The stroke of the polyline drawn inside the same series group as <paramref name="label"/>.</summary>
    private static string GroupStroke(XElement label) =>
        (string)Named(label.Parent!, "polyline").Single().Attribute("stroke")!;

    private static Theme CyclerTheme() => Theme.CreateFrom(Theme.Default)
        .WithPropCycler(new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green, Colors.Blue).Build()).Build();

    // â”€â”€ colour: the label wears the stroke of its line â”€â”€

    [Fact]
    public void APrimaryLabel_IsFilledWithItsLinesStroke()
    {
        var doc = XDocument.Parse(Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "first").WithDirectLabel()
            .Plot(X, [3.0, 2.0, 1.0], s => s.Label = "second").WithDirectLabel()).ToSvg());

        foreach (var name in new[] { "first", "second" })
        {
            var label = Label(doc, name);
            Assert.Equal(GroupStroke(label), (string)label.Attribute("fill")!);
        }
        Assert.NotEqual((string)Label(doc, "first").Attribute("fill")!, (string)Label(doc, "second").Attribute("fill")!);
    }

    [Fact]
    public void UnderAPropCyclerTheme_PrimaryAndRightLabels_WearTheirLinesStroke()
    {
        var doc = XDocument.Parse(Plt.Create().WithTheme(CyclerTheme()).AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "primary").WithDirectLabel()
            .WithAddRightYAxis(y2 => y2.Plot(X, [10.0, 20.0, 30.0], s => s.Label = "secondary").WithDirectLabel())).ToSvg());

        var primary = Label(doc, "primary");
        Assert.Equal("#FF0000", ((string)primary.Attribute("fill")!).ToUpperInvariant());
        Assert.Equal(GroupStroke(primary), (string)primary.Attribute("fill")!);

        // The secondary label sits inside its own series group, beside the line in the cycler's second colour.
        var secondary = Label(doc, "secondary");
        string secondaryFill = (string)secondary.Attribute("fill")!;
        Assert.Equal("#008000", secondaryFill.ToUpperInvariant());
        Assert.Equal(GroupStroke(secondary), secondaryFill);
    }

    [Fact]
    public void ARightLabel_UnderTheDefaultTheme_WearsItsLinesStroke()
    {
        var doc = XDocument.Parse(Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0])
            .WithAddRightYAxis(y2 => y2.Plot(X, [10.0, 20.0, 30.0], s => s.Label = "secondary").WithDirectLabel())).ToSvg());

        var polylines = Named(doc, "polyline").ToList();
        Assert.Equal(2, polylines.Count);
        Assert.Equal((string)polylines[1].Attribute("stroke")!, (string)Label(doc, "secondary").Attribute("fill")!);
    }

    // â”€â”€ the label is inside the figure, in both layout paths â”€â”€

    public static TheoryData<bool> LayoutPaths => [false, true];

    [Theory]
    [MemberData(nameof(LayoutPaths))]
    public void TheDrawnLabel_EndsInsideTheFigure(bool tight)
    {
        const string name = "Revenue (EUR millions)";
        var builder = Plt.Create().WithSize(FigureWidth, 400);
        if (tight) builder = builder.TightLayout();
        var figure = builder.AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = name).WithDirectLabel()
            .WithAddRightYAxis(y2 => y2.Plot(X, [10_000.0, 20_000.0, 30_000.0], s => s.Label = "secondary").WithDirectLabel()))
            .Build();

        var doc = XDocument.Parse(new MatPlotLibNet.Transforms.SvgTransform().Render(figure));
        var font = new DirectLabelMetrics(figure.SubPlots[0], figure.Theme, new MatPlotLibNet.Rendering.Svg.SvgRenderContext()).Font;
        foreach (var text in new[] { name, "secondary" })
        {
            var label = Label(doc, text);
            double right = Num(label, "x") + FontMetricsDefault.Current.Measure(text, font).Width;
            Assert.True(right <= FigureWidth, $"'{text}' ends at {right:F2} > {FigureWidth}");
        }
    }

    [Fact]
    public void WithARightAxis_TheLabelColumnStartsPastTheRightTickLabels()
    {
        var figure = Plt.Create().WithSize(FigureWidth, 400).AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "primary").WithDirectLabel()
            .WithAddRightYAxis(y2 => y2.Plot(X, [10_000.0, 20_000.0, 30_000.0]))).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);

        var texts = ctx.OfKind("DrawText").Select(c => (dynamic)c.Args!).ToList();
        var label = texts.Single(t => (string)t.text == "primary");
        double labelX = (double)label.position.X;
        // Every right-axis tick label (drawn right of the plot) ends before the label column starts.
        var area = new ChartRenderer().ComputeLayout(figure, new MatPlotLibNet.Rendering.Svg.SvgRenderContext()).PlotAreas[0];
        var ticks = texts.Where(t => (double)t.position.X > area.X + area.Width && (string)t.text != "primary").ToList();
        Assert.NotEmpty(ticks);
        foreach (var t in ticks)
        {
            double end = (double)t.position.X + ctx.MeasureText((string)t.text, (Font)t.font).Width;
            Assert.True(end < labelX, $"tick label '{t.text}' ends at {end:F2}, the label column starts at {labelX:F2}");
        }
    }

    // â”€â”€ the label lives inside its series' group â”€â”€

    [Fact]
    public void WithInteractiveAttributes_TheLabel_IsInsideTheGroupCarryingItsSeriesIndex()
    {
        var doc = XDocument.Parse(Plt.Create().WithLegendToggle().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "legend")
            .Plot(X, [3.0, 2.0, 1.0], s => s.Label = "direct").WithDirectLabel()).ToSvg());

        var group = Label(doc, "direct").Parent!;
        Assert.Equal("g", group.Name.LocalName);
        Assert.Equal("1", (string?)group.Attribute("data-series-index"));
        Assert.Single(Named(group, "polyline"));
    }

    [Fact]
    public void ALabelWithXmlSpecialCharacters_IsEscaped()
    {
        string svg = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "R&D <net>").WithDirectLabel()).ToSvg();

        Assert.Contains("R&amp;D &lt;net&gt;</text>", svg);
        Assert.Equal("R&D <net>", Label(XDocument.Parse(svg), "R&D <net>").Value);
    }

    [Fact]
    public void ALegendToggleEvent_OnTheSeries_RemovesItsLabelOnTheNextRender()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "kept").WithDirectLabel()
            .Plot(X, [3.0, 2.0, 1.0], s => s.Label = "toggled").WithDirectLabel()).Build();
        Assert.NotNull(LabelOrNull(XDocument.Parse(figure.ToSvg()), "toggled"));

        new LegendToggleEvent("chart", 0, 1).ApplyTo(figure);

        var doc = XDocument.Parse(figure.ToSvg());
        Assert.Null(LabelOrNull(doc, "toggled"));
        Assert.NotNull(LabelOrNull(doc, "kept"));
    }

    // â”€â”€ an inset has no right margin of its own: its flagged line keeps the legend entry â”€â”€

    /// <summary>Inset axes sit inside the parent's plot area, so right of an inset is the PARENT's data, and no margin
    /// is reserved for a label column there. An inset therefore draws no end labels; its flagged line keeps its legend
    /// entry, so the name is neither lost nor drawn over the parent plot.</summary>
    [Fact]
    public void InAnInset_ADirectlyLabelledLine_DrawsNoEndLabel_AndKeepsItsLegendEntry()
    {
        var figure = Plt.Create().WithSize(FigureWidth, 500).AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "parent")
            .AddInset(0.5, 0.1, 0.35, 0.35, inset => inset
                .Plot(X, [3.0, 1.0, 2.0], s => s.Label = "zoomed").WithDirectLabel())).Build();

        var doc = XDocument.Parse(figure.ToSvg());

        var parent = new ChartRenderer().ComputeLayout(figure, new MatPlotLibNet.Rendering.Svg.SvgRenderContext()).PlotAreas[0];
        var label = LabelOrNull(doc, "zoomed");
        Assert.True(label is null,
            $"the inset drew its end label at x = {(label is null ? 0 : Num(label, "x")):F2}; the inset's plot ends at " +
            $"{parent.X + 0.85 * parent.Width:F2}, inside the parent plot ({parent.X:F2} .. {parent.X + parent.Width:F2})");
        Assert.Contains("zoomed", LegendTexts(doc));
    }

    [Fact]
    public void InAnInset_TheEndLabelOwner_HasNoLabels_SoNoMarginIsReservedForThem()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .AddInset(0.5, 0.1, 0.35, 0.35, inset => inset
                .Plot(X, [3.0, 1.0, 2.0], s => s.Label = "zoomed").WithDirectLabel())).Build();
        var inset = figure.SubPlots[0].Insets[0];

        Assert.Empty(new DirectLabelMetrics(inset, figure.Theme, new MatPlotLibNet.Rendering.Svg.SvgRenderContext()).Labels);
        Assert.False(AxesRenderer.Create(inset, default, new MatPlotLibNet.Rendering.Svg.SvgRenderContext(), figure.Theme).DrawsDirectLabels);
        Assert.True(AxesRenderer.Create(figure.SubPlots[0], default, new MatPlotLibNet.Rendering.Svg.SvgRenderContext(), figure.Theme).DrawsDirectLabels);
    }

    // â”€â”€ the hollow-property theory: the setting reaches the output on every carrier, and nowhere else â”€â”€

    public static TheoryData<string> Carriers => [nameof(LineSeries), nameof(StreamingLineSeries)];

    private static IHasLabelPlacement Carrier(Axes ax, string kind)
    {
        if (kind == nameof(LineSeries)) return ax.Plot([0.0, 1, 2], [1.0, 3, 2]);
        var live = ax.AddSeries(new StreamingLineSeries(capacity: 8));
        live.AppendPoint(0, 1);
        live.AppendPoint(1, 3);
        live.AppendPoint(2, 2);
        return live;
    }

    [Theory]
    [MemberData(nameof(Carriers))]
    public void OnEveryCarrier_ALineEndPlacement_DrawsTheLabelInTheLinesColour_AndLeavesTheLegend(string kind)
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([0.0, 1, 2], [2.0, 2, 2]).Label = "other";
        var carrier = Carrier(ax, kind);
        ((ISeries)carrier).Label = "named";
        carrier.LabelPlacement = SeriesLabelPlacement.LineEnd;

        var doc = XDocument.Parse(figure.ToSvg());

        var label = Label(doc, "named");
        Assert.Equal(GroupStroke(label), (string)label.Attribute("fill")!);
        Assert.Equal(["other"], LegendTexts(doc));
    }

    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void OnEveryNonCarrier_ThePlacementCannotBeSet_AndTheVerbChangesNoByte(ISeries series, string name)
    {
        if (name is nameof(LineSeries) or nameof(StreamingLineSeries))
        {
            Assert.IsAssignableFrom<IHasLabelPlacement>(series);
            return;
        }
        Assert.False(series is IHasLabelPlacement, $"{name} carries a label placement nothing draws");

        series.Label = "named";
        var plain = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddSeries(series)).Build();
        var asked = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddSeries(series).WithDirectLabel()).Build();

        plain.ToSvg(); // a render may fill in defaults on the series (pie wedge colours); both compare after it
        Assert.Equal(plain.ToSvg(), asked.ToSvg());
    }
}
