// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.9.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.BarSeriesRenderer"/>. Pre-X.9
/// the renderer was at 71.8%L / 65.4%B because only the simplest path (categorical
/// vertical, no labels, no edges, no stack) was exercised. This file pins:
///   - Vertical vs Horizontal orientation
///   - Categorical (Center vs Edge alignment) vs numeric XCoordinate paths
///   - LineWidth=0+EdgeColor vs LineWidth&gt;0
///   - StackBaseline path
///   - ShowLabels=true with vertical AND horizontal
///   - LabelLayoutEngine batch placement including custom LabelFormat</summary>
public class BarSeriesRendererTests
{
    private static string Render(BarSeries s) =>
        Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Render_VerticalCategorical_BasicPath", () => Render(new BarSeries(["A", "B", "C"], [1.0, 2.0, 3.0])), "<rect"),
        // Horizontal orientation. Bars draw left-to-right; LabelCandidates are positioned right-of-bar.
        new DrawnToken("Render_HorizontalOrientation_WithLabels_DrawsBarsAndLabels", () => Render(new BarSeries(["A", "B"], [1.0, 2.0])
        {
            Orientation = BarOrientation.Horizontal,
            ShowLabels = true,
        }), "<rect", "<text"),
        // Numeric XCoordinate path (useNumericX=true). Used by MACD-style histograms that share a real X
        // axis with a line series.
        new DrawnToken("Render_NumericXCoordinate_PlacesBarAtX", () => Render(new BarSeries([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])), "<rect"),
        // Categorical Edge alignment (Align==Edge on a categorical axis). Bar starts at the slot
        // edge, not center.
        new DrawnToken("Render_CategoricalEdgeAlignment_DrawsBarsAtSlotEdge", () => Render(new BarSeries(["A", "B"], [1.0, 2.0])
        {
            Align = BarAlignment.Edge,
        }), "<rect"),
        // StackBaseline non-null path. Used by stacked-bar plots where each series sits on top of the
        // cumulative sum below.
        new DrawnToken("Render_StackBaseline_OffsetsBarsByBaseline", () => Render(new BarSeries(["A", "B"], [1.0, 2.0])
        {
            StackBaseline = new[] { 5.0, 10.0 },
        }), "<rect"),
        // LineWidth>0 path (EdgeColor falls back to baseColor). Bars get an explicit border at the
        // requested width.
        // EdgeColor null → falls back to baseColor
        new DrawnToken("Render_LineWidthGreaterThanZero_FallsBackToBaseColorForEdge", () => Render(new BarSeries(["A", "B"], [1.0, 2.0])
        {
            LineWidth = 2.0,
            
        }), "<rect"),
        // EdgeColor.HasValue path with LineWidth=0 (EdgeColor.HasValue true → edgeWidth=1;
        // edgeColor=series.EdgeColor).
        new DrawnToken("Render_EdgeColorWithoutLineWidth_DefaultsToWidthOne", () => Render(new BarSeries(["A", "B"], [1.0, 2.0])
        {
            EdgeColor = Colors.Red,
        }), "<rect"),
        // Custom LabelFormat (the labelFormat-not-null arm). Verifies that the formatted text shows
        // up in SVG output.
        // → "1.50", "2.50"
        new DrawnToken("Render_ShowLabels_WithCustomLabelFormat_AppliesFormat", () => Render(new BarSeries(["A", "B"], [1.5, 2.5])
        {
            ShowLabels = true,
            LabelFormat = "F2",   
        }), ">1.50<", ">2.50<"),
        // End to end, through the figure: a bar of 12345 is labelled 12345, not an exponent, in the SVG.
        new DrawnToken("ABarOf12345_IsLabelledWithAllItsDigitsInTheSvg", () => Render(new BarSeries(["A", "B"], [12345.0, 6789.0]) { ShowLabels = true }), ">12345<", ">6789<") { Absent = ["E+04"] },
        // Negative values arm (Math.Min/Max so a negative value renders below baseline). No exception, rect drawn.
        new DrawnToken("Render_NegativeValues_DrawsBarsBelowBaseline", () => Render(new BarSeries(["A", "B"], [-1.0, -2.0])), "<rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ── the default value label: up to six significant digits, as matplotlib's bar_label '%g' ──────────────────────

    /// <summary>The text a bar of <paramref name="value"/> is labelled with, handed to the render context by the bar
    /// renderer itself (the axes' tick labels are not part of it).</summary>
    private static string LabelOf(double value, BarOrientation orientation, string? format)
    {
        var bounds = new Rect(0, 0, 400, 300);
        double low = Math.Min(0, value), high = Math.Max(1, value);
        // A bar that stands up measures its value along Y, one that lies down along X; its category runs 0..1 on the other.
        var transform = orientation == BarOrientation.Vertical
            ? new DataTransform(0, 1, low, high, bounds)
            : new DataTransform(low, high, 0, 1, bounds);
        var ctx = new RecordingRenderContext();
        new BarSeriesRenderer(new SeriesRenderContext(transform, ctx, Colors.Tab10Blue, new RenderArea(bounds, ctx)))
            .Render(new BarSeries(["A"], [value]) { Orientation = orientation, ShowLabels = true, LabelFormat = format });
        return (string)Assert.Single(ctx.Of<DrawTextCall>()).Text;
    }

    /// <summary>A bar without a <c>LabelFormat</c> is labelled as matplotlib's <c>bar_label</c> labels it by default,
    /// <c>'%g'</c>: up to six significant digits, trailing zeros dropped, an exponent only below 1e-4 or from 1e6. The
    /// expectations are Python's <c>'%g' % value</c>, run on each value. Before, the default was <c>G4</c>, so 12500 was
    /// written <c>1.25E+04</c> and 123456 <c>1.235E+05</c>, and a value of five digits lost one. Both orientations
    /// share the rule.</summary>
    [Theory]
    [InlineData(0.00001, "1e-05")]
    [InlineData(0.0001, "0.0001")]
    [InlineData(0.5, "0.5")]
    [InlineData(12, "12")]
    [InlineData(1234.5, "1234.5")]
    [InlineData(12500, "12500")]
    [InlineData(123456, "123456")]
    [InlineData(1234567, "1.23457e+06")]
    [InlineData(1000000, "1e+06")]
    [InlineData(-12500, "-12500")]
    [InlineData(0, "0")]
    [InlineData(0.000123456789, "0.000123457")]
    [InlineData(123456.7, "123457")]
    [InlineData(99999.95, "99999.9")]
    public void TheDefaultBarLabel_IsMatplotlibsPercentG(double value, string expected)
    {
        Assert.Equal(expected, LabelOf(value, BarOrientation.Vertical, format: null));
        Assert.Equal(expected, LabelOf(value, BarOrientation.Horizontal, format: null));
    }

    /// <summary>A format the user names still decides: <c>F0</c> (the cookbook's) and <c>G4</c> are applied as given, the
    /// new default only replaces what a bar had when it named none.</summary>
    [Theory]
    [InlineData("F0", 12500.4, "12500")]
    [InlineData("F1", 12500.44, "12500.4")]
    [InlineData("G4", 12500, "1.25E+04")]
    public void ANamedBarLabelFormat_IsAppliedAsGiven(string format, double value, string expected) =>
        Assert.Equal(expected, LabelOf(value, BarOrientation.Vertical, format));

    /// <summary>A centred bar and its category's label stand on one x: the renderer draws the bar where
    /// <see cref="CategorySlot"/> puts it and the axis writes the label on the same slot's centre, so a mark placed on
    /// <c>CategorySlot.Of(i).Centre</c> (a Pareto chart's running share) sits on the bar.</summary>
    [Fact]
    public void ACentredBar_AndItsCategoryLabel_StandOnOneX()
    {
        string svg = Render(new BarSeries(["A", "B", "C"], [1.0, 2.0, 3.0]));

        var barCentres = svg.RectsFilled("#1F77B4").Select(rect => rect.X + rect.Width / 2).ToArray();
        var labelXs = new[] { "A", "B", "C" }.Select(name => svg.TextReading(name).Number("x")).ToArray();

        Assert.Equal(3, barCentres.Length);
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(labelXs[i], barCentres[i], SvgMarkupExtensions.WrittenPrecision);
        }
    }

    /// <summary>An edge-aligned bar chart is not cut: its first bar starts at the plot's left edge and its last bar ends at
    /// the right edge, where a centred chart's bars start and end. Measured before the fix in a 400 px figure: the first
    /// edge-aligned bar started 11 px left of the plot edge (clipped) and 11 px stood empty after the last one.</summary>
    [Fact]
    public void AnEdgeAlignedBarChart_SpansThePlotAsACentredOneDoes()
    {
        var edge = BarRects(Render(new BarSeries(["A", "B", "C"], [1.0, 2.0, 3.0]) { Align = BarAlignment.Edge }));
        var centred = BarRects(Render(new BarSeries(["A", "B", "C"], [1.0, 2.0, 3.0])));

        Assert.Equal(centred[0].X, edge[0].X, 6);
        Assert.Equal(centred[^1].X + centred[^1].Width, edge[^1].X + edge[^1].Width, 6);
    }

    private static (double X, double Width)[] BarRects(string svg) =>
        [.. svg.RectsFilled("#1F77B4").Select(rect => (rect.X, rect.Width))];

    // ── horizontal bars: values along X, categories along Y ─────────────────────────────────────────────────────────

    private static readonly string[] Abc = ["A", "B", "C"];

    private static BarSeries Horizontal(string[] categories, double[] values) =>
        new(categories, values) { Orientation = BarOrientation.Horizontal };

    private static string Render(Action<AxesBuilder> axes) =>
        Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, axes).Build().ToSvg();

    /// <summary>The coloured bars, in the order they were drawn (category 0 first).</summary>
    internal static (double X, double Y, double W, double H)[] Bars(string svg) =>
        [.. svg.RectsFilled("#1F77B4").Select(rect => (rect.X, rect.Y, rect.Width, rect.Height))];

    /// <summary>Every text element that reads <paramref name="text"/>: where it stands, how it is anchored, its size.</summary>
    internal static (double X, double Y, string Anchor, double Size)[] Texts(string svg, string text) =>
        [.. svg.Elements("text").Where(element => element.Text == text && element["font-size"] is not null && element["text-anchor"] is not null)
            .Select(element => (element.Number("x"), element.Number("y"), element["text-anchor"]!, element.Number("font-size")))];

    /// <summary>Three horizontal bars of 10, 20 and 30 lie along X, each as long as its value on the X scale and all from
    /// the same x (the value 0); along Y each stands in its category's slot, 0.8 of the slot thick. Measured before the
    /// fix in this 400 x 300 figure: 1107, 2214 and 3321 px long and 5.87 px thick, far past the plot.</summary>
    [Fact]
    public void HorizontalBars_ScaleTheirValuesAlongX_AndStandInTheirSlotsAlongY()
    {
        string svg = Render(Horizontal(Abc, [10.0, 20.0, 30.0]));
        var bars = Bars(svg);
        var plot = svg.PlotArea();

        Assert.Equal(3, bars.Length);
        Assert.Single(bars.Select(b => Math.Round(b.X, 6)).Distinct());
        Assert.Equal(2 * bars[0].W, bars[1].W, SvgMarkupExtensions.WrittenPrecision);
        Assert.Equal(3 * bars[0].W, bars[2].W, SvgMarkupExtensions.WrittenPrecision);
        Assert.Single(bars.Select(b => Math.Round(b.H, 6)).Distinct());
        double pitch = bars[0].Y - bars[1].Y;
        Assert.Equal(0.8 * pitch, bars[0].H, SvgMarkupExtensions.WrittenPrecision);
        Assert.All(bars, b =>
        {
            Assert.InRange(b.X, plot.X - 1e-6, plot.X + plot.Width);
            Assert.InRange(b.X + b.W, plot.X, plot.X + plot.Width + 1e-6);
            Assert.InRange(b.Y, plot.Y - 1e-6, plot.Y + plot.Height);
            Assert.InRange(b.Y + b.H, plot.Y, plot.Y + plot.Height + 1e-6);
        });
    }

    /// <summary>The names stand on the Y axis, each level with its bar, right-aligned left of the spine with a tick mark
    /// at its bar's centre; the first category at the bottom, as matplotlib's <c>barh</c> draws it. The X axis carries
    /// the values. Before the fix the names stood under the X axis.</summary>
    [Fact]
    public void HorizontalBars_NameTheirCategoriesAlongY_TheFirstAtTheBottom()
    {
        string svg = Render(Horizontal(Abc, [10.0, 20.0, 30.0]));
        var bars = Bars(svg);
        var plot = svg.PlotArea();

        Assert.True(bars[0].Y > bars[1].Y && bars[1].Y > bars[2].Y, "the first category stands at the bottom");
        for (int i = 0; i < 3; i++)
        {
            var name = Assert.Single(Texts(svg, Abc[i]));
            double centre = bars[i].Y + bars[i].H / 2;
            Assert.Equal(centre + 0.35 * Theme.MatplotlibV2.TickFont().Size, name.Y, SvgMarkupExtensions.WrittenPrecision);   // centred on its tick: 0.35 of the ascent
            Assert.True(name.X < plot.X, $"{Abc[i]} stands left of the Y spine");
            Assert.Equal("end", name.Anchor);
            Assert.Contains(svg.Elements("line"), line => line["y1"] == Written(centre) && line["y2"] == Written(centre));
        }
        Assert.Contains(Texts(svg, "30"), t => t.Y > plot.Y + plot.Height);
    }

    /// <summary>A range set high to low on the category axis turns it over: the first category stands at the top, its
    /// name with it. This is how a chart reads its categories from the top down.</summary>
    [Fact]
    public void HorizontalBars_OnAReversedCategoryAxis_PutTheFirstCategoryAtTheTop()
    {
        string svg = Render(ax => ax.Bar(Abc, [10.0, 20.0, 30.0], s => s.Orientation = BarOrientation.Horizontal).SetYLim(3, 0));
        var bars = Bars(svg);

        Assert.True(bars[0].Y < bars[1].Y && bars[1].Y < bars[2].Y, "the first category stands at the top");
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(bars[i].Y + bars[i].H / 2 + 0.35 * Theme.MatplotlibV2.TickFont().Size, Assert.Single(Texts(svg, Abc[i])).Y, SvgMarkupExtensions.WrittenPrecision);
        }
    }

    /// <summary>The names on the Y axis honour the axis' tick settings: hidden ticks draw no name, a label size is the
    /// names' size.</summary>
    [Fact]
    public void TheNamesOnTheYAxis_HonourItsTickSettings()
    {
        static string WithYTicks(Func<MatPlotLibNet.Models.TickConfig, MatPlotLibNet.Models.TickConfig> ticks)
        {
            var figure = Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => ax.AddSeries(Horizontal(Abc, [1.0, 2.0, 3.0]))).Build();
            var axis = figure.SubPlots[0].YAxis;
            axis.MajorTicks = ticks(axis.MajorTicks);
            return figure.ToSvg();
        }

        string hidden = WithYTicks(t => t with { Visible = false });
        string sized = WithYTicks(t => t with { LabelSize = 20 });

        Assert.All(Abc, name => Assert.Empty(Texts(hidden, name)));
        Assert.All(Abc, name => Assert.Equal(20, Assert.Single(Texts(sized, name)).Size));
    }

    /// <summary>The Y axis title stands clear of the longest name, by the rule a numeric Y axis follows (the names end at
    /// the tick mark and the pad left of the spine): the title's ink that faces the names, its baseline (the title being
    /// turned a quarter about its anchor), stands the container's gap (4 px, or the matplotlib theme's labelpad) past the far side of the widest name.</summary>
    [Fact]
    public void TheYAxisTitle_StandsClearOfTheLongestName()
    {
        string[] names = ["North-west region", "South", "East"];
        var figure = Plt.Create().WithSize(600, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(Horizontal(names, [1.0, 2.0, 3.0])).SetYLabel("Region"))
            .Build();
        string svg = figure.ToSvg();
        double widest = new MatPlotLibNet.Rendering.Svg.SvgRenderContext()
            .MeasureText(names[0], figure.Theme.TickFont()).Width;
        double namesEnd = Assert.Single(Texts(svg, names[0])).X;

        var title = Assert.Single(Texts(svg, "Region"));

        Assert.Equal(namesEnd - widest - figure.SubPlots[0].YAxes.TitleGapUnder(figure.Theme), title.X + MatPlotLibNet.Rendering.FacingInk.TurnedSink * title.Size, SvgMarkupExtensions.WrittenPrecision);
        Assert.All(names, name => Assert.Equal(namesEnd, Assert.Single(Texts(svg, name)).X, SvgMarkupExtensions.WrittenPrecision));
    }

    // ── upright bars: the names on the X axis are its tick labels ───────────────────────────────────────────────────

    private static readonly string[] Quarters = ["Q1", "Q2", "Q3"];

    /// <summary>A figure of three upright bars named Q1..Q3, its X ticks changed by <paramref name="ticks"/>.</summary>
    private static string WithXTicks(Func<MatPlotLibNet.Models.TickConfig, MatPlotLibNet.Models.TickConfig> ticks,
        Action<AxesBuilder>? more = null, MatPlotLibNet.Styling.Theme? theme = null)
    {
        var builder = Plt.Create().WithSize(500, 350);
        if (theme is not null) builder = builder.WithTheme(theme);
        var figure = builder.AddSubPlot(1, 1, 1, ax => { ax.Bar(Quarters, [1.0, 2.0, 3.0]); more?.Invoke(ax); }).Build();
        var axis = figure.SubPlots[0].XAxis;
        axis.MajorTicks = ticks(axis.MajorTicks);
        return figure.ToSvg();
    }

    /// <summary>The names of upright bars stand where every X tick label stands: centred on the bar, the tick mark, the
    /// pad and the font size below the spine (matplotlib's <c>xtick.major.size</c> + <c>xtick.major.pad</c>), with a tick
    /// mark at the bar's centre. Measured before: 15 px below the spine whatever the tick settings, 8.6 px above the
    /// numeric labels of the same figure.</summary>
    [Fact]
    public void UprightBars_NameTheirCategoriesAlongX_WhereEveryTickLabelStands()
    {
        string svg = WithXTicks(t => t);
        var bars = Bars(svg);
        var plot = svg.PlotArea();
        var ticks = new MatPlotLibNet.Models.TickConfig();
        // The tick marks under the bottom spine: short vertical lines that start at it.
        var marks = svg.Elements("line")
            .Where(line => line["x1"] is not null && line["x1"] == line["x2"] && line.Number("y1") < plot.Y + plot.Height + 1 && line.Number("y2") > plot.Y + plot.Height + 1
                           && line.Number("y2") - line.Number("y1") < 20)
            .Select(line => line.Number("x1"))
            .ToArray();

        Assert.Equal(3, marks.Length);
        for (int i = 0; i < 3; i++)
        {
            var name = Assert.Single(Texts(svg, Quarters[i]));
            double centre = bars[i].X + bars[i].W / 2;
            Assert.Equal(centre, name.X, SvgMarkupExtensions.WrittenPrecision);
            Assert.Equal(centre, marks[i], SvgMarkupExtensions.WrittenPrecision);
            Assert.Equal("middle", name.Anchor);
            Assert.Equal(plot.Y + plot.Height + ticks.Length + ticks.Pad + name.Size, name.Y, SvgMarkupExtensions.WrittenPrecision);
        }
    }

    /// <summary>Hidden X ticks hide the names: <c>HideAllAxes</c> and <c>TickConfig.Visible = false</c> draw no tick mark
    /// and no name, as they draw no number on a numeric axis. Measured before: Q1, Q2 and Q3 at y = 315 under a hidden
    /// axis.</summary>
    [Fact]
    public void HiddenXTicks_HideTheNames()
    {
        string hidden = WithXTicks(t => t with { Visible = false });
        string hideAll = Render(ax => ax.Bar(Quarters, [1.0, 2.0, 3.0]).HideAllAxes());

        Assert.All(Quarters, name => Assert.Empty(Texts(hidden, name)));
        Assert.All(Quarters, name => Assert.Empty(Texts(hideAll, name)));
    }

    /// <summary>The names honour the X axis' tick settings like any tick label: the label size, the label colour, the
    /// rotation (anchored at the label's end, as a rotated number is), and the mark length and pad that place them.</summary>
    [Fact]
    public void TheNamesOnTheXAxis_HonourItsTickSettings()
    {
        var red = MatPlotLibNet.Styling.Colors.Red;
        string sized = WithXTicks(t => t with { LabelSize = 20, LabelColor = red });
        string rotated = WithXTicks(t => t with { LabelRotation = 45 });
        string spaced = WithXTicks(t => t with { Length = 12, Pad = 9 });
        var plot = spaced.PlotArea();

        Assert.All(Quarters, name => Assert.Equal(20, Assert.Single(Texts(sized, name)).Size));
        Assert.All(Quarters, name => Assert.Contains(sized.Elements("text"), text => text.Text == name && text["fill"] == red.ToHex()));
        Assert.All(Quarters, name =>
        {
            Assert.Equal("end", Assert.Single(Texts(rotated, name)).Anchor);
            Assert.Contains(rotated.Elements("text"), text => text.Text == name && text["transform"]?.StartsWith("rotate(-45,", StringComparison.Ordinal) == true);
        });
        Assert.All(Quarters, name =>
        {
            var text = Assert.Single(Texts(spaced, name));
            Assert.Equal(plot.Y + plot.Height + 12 + 9 + text.Size, text.Y, 6);
        });
    }

    /// <summary>Mirrored X ticks repeat the names above the top spine, as they repeat numbers. Before, the names were
    /// never mirrored.</summary>
    [Fact]
    public void MirroredXTicks_RepeatTheNamesAboveTheTopSpine()
    {
        string svg = WithXTicks(t => t with { Mirror = true });
        var plot = svg.PlotArea();

        Assert.All(Quarters, name =>
        {
            var texts = Texts(svg, name);
            Assert.Equal(2, texts.Length);
            Assert.Contains(texts, t => t.Y > plot.Y + plot.Height);
            Assert.Contains(texts, t => t.Y < plot.Y);
        });
    }

    /// <summary>Under a theme that draws a grid, the vertical grid lines fall on the categories, one through each bar, as
    /// matplotlib's grid follows the category ticks. Measured before under ggplot: 11 vertical lines 37.5 px apart, on
    /// numeric ticks, for 3 categories 150 px apart.</summary>
    [Fact]
    public void UnderAGridTheme_TheVerticalGridLinesFallOnTheCategories()
    {
        string svg = WithXTicks(t => t, theme: MatPlotLibNet.Styling.Theme.Ggplot);
        var bars = svg.RectsFilled("#E24A33").Select(rect => rect.X + rect.Width / 2).ToArray();

        // A vertical grid line runs the height of the plot (over 100 px here) in ggplot's white; a tick mark is a few
        // pixels long and a spine is drawn in the text colour.
        var verticalGridLines = svg.VerticalLineXs("#FFFFFF", 100);

        Assert.Equal(3, bars.Length);
        Assert.Equal(bars, verticalGridLines, (a, b) => Math.Abs(a - b) <= SvgMarkupExtensions.WrittenPrecision);
    }

    /// <summary>The X axis title stands clear of the names by the rule a numeric X axis follows: past the mark, the pad
    /// and the measured name height, a 6 px gap and the title's ascent. Before, the names were not measured and the title
    /// fell back to a fixed 35 px below the spine.</summary>
    [Fact]
    public void TheXAxisTitle_StandsClearOfTheNames()
    {
        var figure = Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax.Bar(Quarters, [1.0, 2.0, 3.0]).SetXLabel("Quarter")).Build();
        string svg = figure.ToSvg();
        var plot = svg.PlotArea();
        var ticks = figure.SubPlots[0].XAxis.MajorTicks;
        var name = Assert.Single(Texts(svg, "Q1"));
        double nameHeight = new MatPlotLibNet.Rendering.Svg.SvgRenderContext()
            .MeasureText("Q1", figure.Theme.TickFont()).Height;

        var title = Assert.Single(Texts(svg, "Quarter"));

        Assert.Equal(plot.Y + plot.Height + ticks.Length + ticks.Pad + nameHeight + figure.Theme.XLabelGapUnder + 0.8 * title.Size, title.Y, SvgMarkupExtensions.WrittenPrecision);
        Assert.True(title.Y - 0.8 * title.Size > name.Y, "the title's top stands below the names' baseline");
    }

    /// <summary>A formatter set on the X axis names the category ticks, the rule the Y axis follows for bars that lie
    /// down; a locator set on it takes the place of the names, as before.</summary>
    [Fact]
    public void AFormatterOnTheXAxis_NamesTheCategoryTicks_ALocatorTakesTheirPlace()
    {
        string formatted = Render(ax => ax.Bar(Quarters, [1.0, 2.0, 3.0])
            .SetXTickFormatter(new MatPlotLibNet.Rendering.TickFormatters.CategoryFormatter(["first", "second", "third"])));
        string located = Render(ax => ax.Bar(Quarters, [1.0, 2.0, 3.0])
            .SetXTickLocator(new MatPlotLibNet.Rendering.TickLocators.MultipleLocator(1)));

        Assert.All(["first", "second", "third"], name => Assert.Single(Texts(formatted, name)));
        Assert.All(Quarters, name => Assert.Empty(Texts(formatted, name)));
        Assert.All(Quarters, name => Assert.Empty(Texts(located, name)));
    }

    /// <summary>A candlestick names its dates on the X axis the same way: hidden X ticks hide them, and they stand where
    /// every X tick label stands.</summary>
    [Fact]
    public void ACandlesticksDates_AreTheXTickLabels()
    {
        string[] dates = ["Mon", "Tue", "Wed"];
        static string Candles(string[] dates, bool hide) => Render(ax =>
        {
            ax.Candlestick([10.0, 11, 12], [15.0, 14, 16], [8.0, 9, 10], [12.0, 10, 15], dates);
            if (hide) ax.HideAllAxes();
        });
        string shown = Candles(dates, hide: false);
        string hidden = Candles(dates, hide: true);
        var plot = shown.PlotArea();
        var ticks = new MatPlotLibNet.Models.TickConfig();

        Assert.All(dates, date => Assert.Empty(Texts(hidden, date)));
        Assert.All(dates, date =>
        {
            var text = Assert.Single(Texts(shown, date));
            Assert.Equal(plot.Y + plot.Height + ticks.Length + ticks.Pad + text.Size, text.Y, SvgMarkupExtensions.WrittenPrecision);
        });
    }

    private static string Written(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
