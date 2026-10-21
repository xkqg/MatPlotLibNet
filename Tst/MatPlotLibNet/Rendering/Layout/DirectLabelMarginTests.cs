// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The DEFAULT layout path (no <c>TightLayout</c>, no <c>ConstrainedLayout</c>) reserves room at the right
/// edge for what is drawn there: the secondary Y axis' tick labels and the end-of-line labels. Before 1.19.0 it took
/// the spacing as given (MarginRight 20 px), so a seven-character end label (about 50 px) ran off the figure.
/// <para>Every assertion reads the plot-area rectangle and measures the label text with the metrics the renderer
/// uses, not the drawn label, so the reservation is pinned on its own.</para></summary>
public class DirectLabelMarginTests
{
    private const double FigureWidth = 800;
    private static readonly double[] X = [0.0, 1.0, 2.0];

    private static IReadOnlyList<Rect> PlotAreas(Figure figure) =>
        new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas;

    private static double RightMargin(Figure figure)
    {
        var area = PlotAreas(figure)[0];
        return figure.Width - (area.X + area.Width);
    }

    /// <summary>The width of <paramref name="text"/> in the font the end label is drawn in, measured by the metrics
    /// every SVG render uses.</summary>
    private static double LabelWidth(Figure figure, string text, int axes = 0) =>
        ChartServices.FontMetrics.Measure(text,
            new DirectLabelMetrics(figure.SubPlots[axes], figure.Theme, new SvgRenderContext()).Font).Width;

    private static FigureBuilder Labelled(string label, Func<FigureBuilder, FigureBuilder>? figure = null)
    {
        var builder = Plt.Create().WithSize(FigureWidth, 400);
        builder = figure?.Invoke(builder) ?? builder;
        return builder.AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0], s => s.Label = label).WithDirectLabel());
    }

    // Wider than the 80 px the default theme's fractional spacing (right = 0.1) leaves on an 800 px figure.
    private const string LongName = "Revenue (EUR millions)";

    [Fact]
    public void ADirectlyLabelledLine_ReservesTheLabelAndItsGapAtTheRightEdge()
    {
        var figure = Labelled(LongName).Build();

        double needed = DirectLabelMetrics.Gap + LabelWidth(figure, LongName);
        Assert.True(needed > 80, $"precondition: the label band ({needed:F2}) must exceed the theme's 80 px");
        Assert.True(RightMargin(figure) >= needed,
            $"MarginRight {RightMargin(figure):F2} < gap + label {needed:F2}");
    }

    [Fact]
    public void TheSvgOutput_DrawsThePlotAreaClearOfTheLabelBand()
    {
        var figure = Labelled(LongName).Build();
        string svg = new MatPlotLibNet.Transforms.SvgTransform().Render(figure);

        // The first rect off the origin is the axes background, i.e. the plot area.
        var rect = Regex.Matches(svg,
                @"<rect x=""(?<x>[0-9.]+)"" y=""(?<y>[0-9.]+)"" width=""(?<w>[0-9.]+)""")
            .Select(m => (X: double.Parse(m.Groups["x"].Value, CultureInfo.InvariantCulture),
                          Y: double.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture),
                          W: double.Parse(m.Groups["w"].Value, CultureInfo.InvariantCulture)))
            .First(r => r.X > 0 && r.Y > 0);

        double needed = DirectLabelMetrics.Gap + LabelWidth(figure, LongName);
        Assert.True(FigureWidth - (rect.X + rect.W) >= needed,
            $"SVG plot area leaves {FigureWidth - (rect.X + rect.W):F2} px, the label band needs {needed:F2}");
    }

    [Fact]
    public void AUserMarginRightSmallerThanTheLabel_IsWidened()
    {
        var figure = Labelled("Revenue", f => f.WithSubPlotSpacing(s => s with { MarginRight = 5 })).Build();

        Assert.True(RightMargin(figure) >= DirectLabelMetrics.Gap + LabelWidth(figure, "Revenue"));
    }

    [Fact]
    public void AUserMarginRightWiderThanTheLabel_IsKept()
    {
        var figure = Labelled("Revenue", f => f.WithSubPlotSpacing(s => s with { MarginRight = 300 })).Build();

        Assert.Equal(300, RightMargin(figure), 6);
    }

    [Fact]
    public void FractionalSpacing_IsWidenedAfterItIsResolvedToPixels()
    {
        // 0.02 x 800 = 16 px once resolved, too narrow for the label.
        var figure = Labelled("Revenue",
            f => f.WithSubPlotSpacing(_ => SubPlotSpacing.FromFractions(0.1, 0.02, 0.1, 0.12))).Build();

        Assert.True(RightMargin(figure) >= DirectLabelMetrics.Gap + LabelWidth(figure, "Revenue"),
            $"fractional MarginRight {RightMargin(figure):F2} was not widened");
    }

    [Fact]
    public void InAOneByTwoGrid_TheGutterGrowsByTheLeftSubplotsLabelBand()
    {
        static Figure Grid(bool label) => Plt.Create().WithSize(FigureWidth, 400)
            .AddSubPlot(1, 2, 1, ax =>
            {
                ax.Plot(X, [1.0, 2.0, 3.0], s => s.Label = "Left series");
                if (label) ax.WithDirectLabel();
            })
            .AddSubPlot(1, 2, 2, ax => ax.Plot(X, [1.0, 2.0, 3.0]))
            .Build();

        var labelled = Grid(true);
        var plain = Grid(false);
        static double Gutter(IReadOnlyList<Rect> a) => a[1].X - (a[0].X + a[0].Width);

        // The plain gutter already holds the right subplot's Y tick labels; the label band comes on top of it, so
        // the left subplot's labels cannot cross into the right subplot's Y axis.
        double growth = Gutter(PlotAreas(labelled)) - Gutter(PlotAreas(plain));
        Assert.True(growth >= DirectLabelMetrics.Gap + LabelWidth(labelled, "Left series"),
            $"gutter grew {growth:F2} px, the label band needs {DirectLabelMetrics.Gap + LabelWidth(labelled, "Left series"):F2}");
        // The right subplot carries no label, so the figure's right margin is untouched.
        static double FigureRightMargin(Figure f) => f.Width - (PlotAreas(f)[1].X + PlotAreas(f)[1].Width);
        Assert.Equal(FigureRightMargin(plain), FigureRightMargin(labelled), 6);
    }

    [Fact]
    public void ASecondaryYAxis_ReservesItsTickLabelsInsideTheFigure()
    {
        var figure = Plt.Create().WithSize(FigureWidth, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [1.0, 2.0, 3.0])
                .WithSecondaryYAxis(y2 => y2.Plot(X, [10_000.0, 20_000.0, 30_000.0])))
            .Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);

        var area = PlotAreas(figure)[0];
        double plotRight = area.X + area.Width;
        var tickLabels = ctx.OfKind("DrawText").Select(c => (dynamic)c.Args!)
            .Where(a => (double)a.position.X > plotRight).ToList();
        Assert.NotEmpty(tickLabels);
        foreach (var a in tickLabels)
        {
            double rightEdge = (double)a.position.X + ctx.MeasureText((string)a.text, (MatPlotLibNet.Styling.Font)a.font).Width;
            Assert.True(rightEdge <= FigureWidth, $"secondary tick label '{a.text}' ends at {rightEdge:F2} > {FigureWidth}");
        }
    }

    // ── (f) nothing at the right edge: the default path keeps the spacing exactly as given ──

    public static TheoryData<string> PlainFigures => ["plain", "legend label", "grid", "fractional", "polar line end", "no label", "hidden"];

    private static Figure Plain(string kind) => kind switch
    {
        "plain" => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0])).Build(),
        "legend label" => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0], s => s.Label = "in the legend")).Build(),
        "grid" => Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0], s => s.Label = "a"))
            .AddSubPlot(1, 2, 2, ax => ax.Plot(X, [1.0, 2.0, 3.0], s => s.Label = "b")).Build(),
        "fractional" => Plt.Create().WithSubPlotSpacing(_ => SubPlotSpacing.FromFractions(0.1, 0.02, 0.1, 0.12))
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0])).Build(),
        // Polar axes draw no end labels (the series keeps its legend entry), so they reserve nothing for one.
        "polar line end" => Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0])
            .Plot(X, [1.0, 2.0, 3.0], s => { s.Label = "flagged"; s.LabelPlacement = SeriesLabelPlacement.LineEnd; })).Build(),
        // A line-end series without a label has no text to draw.
        "no label" => Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.LabelPlacement = SeriesLabelPlacement.LineEnd)).Build(),
        // A hidden series draws neither its line nor its label.
        "hidden" => Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => { s.Label = "hidden"; s.Visible = false; }).WithDirectLabel()).Build(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    [Theory]
    [MemberData(nameof(PlainFigures))]
    public void WithNothingAtTheRightEdge_TheDefaultPathKeepsTheSpacingAsGiven(string kind)
    {
        var figure = Plain(kind);

        var spacing = new ChartRenderer().PrepareSpacing(figure, new SvgRenderContext());

        // What the default path returned before 1.19.0: the figure's own spacing, or the theme's when the figure kept
        // the library default, resolved to pixels.
        var given = figure.Spacing == new SubPlotSpacing() && figure.Theme.DefaultSpacing is { } themed
            ? themed
            : figure.Spacing;
        Assert.Equal(given.Resolve(figure.Width, figure.Height), spacing);
    }
}
