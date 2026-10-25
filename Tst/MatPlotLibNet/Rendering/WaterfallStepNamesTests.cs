// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A waterfall names its steps on the X axis, each under its bar. A waterfall is a category chart: its steps have
/// names, and the axis writes a category's name on the centre of the category's slot
/// (<see cref="CategorySlot"/>), as it does for bars, a count plot and a candlestick's dates. Before, the series named
/// no category, so the X axis counted 0, 2, 4 ... under the bars (the cookbook's monthly P&amp;L waterfall showed those
/// numbers under its bars although it sets a tick label rotation for names), and its bars stood on 0, 1, 2 ... while the
/// slot rule puts a category's centre half a slot further on.</summary>
public class WaterfallStepNamesTests
{
    private const double NaN = double.NaN;
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static readonly string[] Steps = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Total"];
    private static readonly double[] Pnl = [120, -45, 80, -30, 95, 60, 280];

    private static WaterfallSeries MonthlySeries() =>
        new(Steps, Pnl) { TotalIndices = [6] };

    private static Figure Monthly(Action<AxesBuilder>? more = null, Theme? theme = null)
    {
        var builder = Plt.Create().WithSize(700, 400);
        if (theme is not null) builder = builder.WithTheme(theme);
        return builder
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Waterfall(Steps, Pnl, s => s.TotalIndices = [6]);
                more?.Invoke(ax);
            })
            .Build();
    }

    private static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    /// <summary>The bars of the waterfall in the order they were drawn: step 0 first. The colour of each is one of the
    /// series' three; nothing else in the figure wears them.</summary>
    private static (double X, double Y, double W, double H)[] Bars(string svg)
    {
        var series = new WaterfallSeries([], []);
        string[] fills = [series.IncreaseColor.ToHex(), series.DecreaseColor.ToHex(), series.TotalColor.ToHex()];
        return [.. Regex
            .Matches(svg, @"<rect x=""([0-9.eE+-]+)"" y=""([0-9.eE+-]+)"" width=""([0-9.eE+-]+)"" height=""([0-9.eE+-]+)"" fill=""(#[0-9A-Fa-f]{6})""")
            .Where(m => fills.Contains(m.Groups[5].Value, StringComparer.OrdinalIgnoreCase))
            .Select(m => (Number(m.Groups[1].Value), Number(m.Groups[2].Value), Number(m.Groups[3].Value), Number(m.Groups[4].Value)))];
    }

    /// <summary>Everything written under the plot's bottom edge and inside its width: the X axis' tick labels, left to
    /// right (the Y axis' labels stand left of the plot).</summary>
    private static string[] LabelsUnderThePlot(string svg)
    {
        var plot = BarSeriesRendererTests.PlotArea(svg);
        return [.. Regex
            .Matches(svg, @"<text x=""([-0-9.eE+]+)"" y=""([-0-9.eE+]+)""[^>]*>([^<]*)</text>")
            .Where(m => Number(m.Groups[2].Value) > plot.Y + plot.H && Number(m.Groups[1].Value) > plot.X)
            .OrderBy(m => Number(m.Groups[1].Value))
            .Select(m => m.Groups[3].Value)];
    }

    // ---- the names -----------------------------------------------------------------------------------------------------

    /// <summary>Each step's name stands under the centre of its bar. Before: no name at all, only the numbers 0, 2, 4, 6.</summary>
    [Fact]
    public void AWaterfall_NamesEveryStepUnderTheCentreOfItsBar()
    {
        string svg = Monthly().ToSvg();

        var bars = Bars(svg);
        Assert.Equal(Steps.Length, bars.Length);
        for (int i = 0; i < Steps.Length; i++)
        {
            var name = Assert.Single(BarSeriesRendererTests.Texts(svg, Steps[i]));
            Assert.Equal(bars[i].X + bars[i].W / 2, name.X, 6);
            Assert.Equal("middle", name.Anchor);
        }
    }

    /// <summary>One name for each bar position and nothing else on the X axis: no number counts the steps.</summary>
    [Fact]
    public void AWaterfall_WritesOneNamePerBarPosition_AndNoNumbersOnTheXAxis() =>
        Assert.Equal(Steps, LabelsUnderThePlot(Monthly().ToSvg()));

    /// <summary>A tick label rotation set for the names applies to them (the cookbook's waterfall sets 30 degrees for its
    /// month names, which were not there to turn).</summary>
    [Fact]
    public void ATickLabelRotation_AppliesToTheStepNames()
    {
        string svg = Monthly(ax => ax.WithXTickLabelRotation(30)).ToSvg();

        Assert.All(Steps, name =>
        {
            Assert.Equal("end", Assert.Single(BarSeriesRendererTests.Texts(svg, name)).Anchor);
            Assert.Matches($@"<text [^>]*transform=""rotate\(-30,[^>]*>{name}</text>", svg);
        });
    }

    /// <summary>The names are tick labels like any other, so hidden X ticks hide them.</summary>
    [Fact]
    public void HiddenXTicks_HideTheStepNames()
    {
        var figure = Monthly();
        Assert.All(Steps, name => Assert.Single(BarSeriesRendererTests.Texts(figure.ToSvg(), name)));
        var axis = figure.SubPlots[0].XAxis;
        axis.MajorTicks = axis.MajorTicks with { Visible = false };

        string svg = figure.ToSvg();

        Assert.All(Steps, name => Assert.Empty(BarSeriesRendererTests.Texts(svg, name)));
    }

    /// <summary>Mirrored X ticks repeat the step names above the top spine, as they repeat numbers.</summary>
    [Fact]
    public void MirroredXTicks_RepeatTheStepNamesAboveTheTopSpine()
    {
        var figure = Monthly();
        var axis = figure.SubPlots[0].XAxis;
        axis.MajorTicks = axis.MajorTicks with { Mirror = true };

        string svg = figure.ToSvg();
        var plot = BarSeriesRendererTests.PlotArea(svg);

        Assert.All(Steps, name =>
        {
            var texts = BarSeriesRendererTests.Texts(svg, name);
            Assert.Equal(2, texts.Length);
            Assert.Contains(texts, t => t.Y > plot.Y + plot.H);
            Assert.Contains(texts, t => t.Y < plot.Y);
        });
    }

    /// <summary>Under a theme that draws a grid, one vertical grid line runs through each bar: the grid follows the step
    /// names, not numeric ticks between the bars.</summary>
    [Fact]
    public void UnderAGridTheme_OneVerticalGridLineRunsThroughEachBar()
    {
        string svg = Monthly(theme: Theme.Ggplot).ToSvg();

        var bars = Bars(svg).Select(b => b.X + b.W / 2).ToArray();
        // A grid line runs the height of the plot (over 100 px here) in ggplot's white; a tick mark is a few pixels long.
        var gridLines = Regex
            .Matches(svg, @"<line x1=""([0-9.eE+-]+)"" y1=""([0-9.eE+-]+)"" x2=""\1"" y2=""([0-9.eE+-]+)""[^>]*stroke=""#FFFFFF""")
            .Where(m => Math.Abs(Number(m.Groups[3].Value) - Number(m.Groups[2].Value)) > 100)
            .Select(m => Number(m.Groups[1].Value))
            .ToArray();

        Assert.Equal(Steps.Length, bars.Length);
        Assert.Equal(bars, gridLines, (a, b) => Math.Abs(a - b) < 1e-6);
    }

    /// <summary>A step whose value is missing is still a step with a name: the axis names the categories, whatever the
    /// data holds.</summary>
    [Fact]
    public void AMissingStep_KeepsItsName()
    {
        string svg = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Waterfall(["a", "b", "c"], [4, NaN, 2]))
            .ToSvg();

        Assert.Equal(["a", "b", "c"], LabelsUnderThePlot(svg));
        Assert.DoesNotContain("NaN", svg);
    }

    /// <summary>The names are not stored in the JSON of the figure: the series carries its categories, and the axis names
    /// them again when it is drawn, so a figure read back names its steps the same way.</summary>
    [Fact]
    public void AFigureReadBackFromJson_NamesItsStepsTheSame()
    {
        var figure = Monthly();

        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(Steps, LabelsUnderThePlot(readBack));
        Assert.Equal(figure.ToSvg(), readBack);
    }

    // ---- the series ----------------------------------------------------------------------------------------------------

    /// <summary>The series names its categories with its steps' names, in order: the one thing the axes need to name them.</summary>
    [Fact]
    public void TheSeries_IsCategoryLabeled_WithItsStepNames()
    {
        var labeled = Assert.IsAssignableFrom<ICategoryLabeled>(MonthlySeries());

        Assert.Equal(Steps, labeled.CategoryLabels);
    }

    // ---- where the bars stand ------------------------------------------------------------------------------------------

    /// <summary>The range spans the slots of the steps, 0 to the number of steps, sticky at both ends: the same range a count
    /// plot has, and the one the bars' places (below) are measured against. The picture keeps its proportions, the
    /// slots only count from 0 instead of from -0.5.</summary>
    [Fact]
    public void TheRange_SpansTheSlotsOfTheSteps()
    {
        var range = MonthlySeries().ComputeDataRange(new AxesContextAdapter(new Axes()));

        Assert.Equal(CategorySlot.Of(0).Left, range.XMin);
        Assert.Equal(CategorySlot.Of(Steps.Length - 1).Right, range.XMax);
        Assert.Equal(range.XMin, range.StickyXMin);
        Assert.Equal(range.XMax, range.StickyXMax);
    }

    /// <summary>A limit on the X axis still wins over the slots, as it did.</summary>
    [Fact]
    public void AnXLimit_StillWinsOverTheSlots()
    {
        var limited = new Axes();
        limited.XAxis.Min = -3;
        limited.XAxis.Max = 9;

        var range = MonthlySeries().ComputeDataRange(new AxesContextAdapter(limited));

        Assert.Equal(-3.0, range.XMin);
        Assert.Equal(9.0, range.XMax);
    }

    private static List<CallRecord> Drawn(WaterfallSeries series, string kind)
    {
        var ctx = new RecordingRenderContext();
        var transform = new DataTransform(0, series.Categories.Length, -10, 10, Bounds);
        new WaterfallSeriesRenderer(new SeriesRenderContext(transform, ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx))).Render(series);
        return [.. ctx.OfKind(kind)];
    }

    private static object? Arg(CallRecord call, string name) => call.Args!.GetType().GetProperty(name)!.GetValue(call.Args);

    /// <summary>Bar i stands centred in the slot of step i, as a bar of a bar chart and a count plot does, <c>BarWidth</c> of
    /// the slot wide: on a 400 px axis of four steps each slot is 100 px and the default bar 60 px.</summary>
    [Fact]
    public void ABar_StandsCentredInTheSlotOfItsStep()
    {
        var series = new WaterfallSeries(["a", "b", "c", "d"], [1, 2, 3, 4]);

        var bars = Drawn(series, "DrawRectangle").Select(c => (Rect)Arg(c, "rect")!).ToList();

        Assert.Equal(4, bars.Count);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal((i + 0.5) * 100 - 30, bars[i].X, 9);
            Assert.Equal(60, bars[i].Width, 9);
        }
    }

    /// <summary>The dashed connector runs from the right edge of a bar to the left edge of the next one, at the level the
    /// bar reaches.</summary>
    [Fact]
    public void TheConnector_RunsFromTheEdgeOfOneBarToTheEdgeOfTheNext()
    {
        var series = new WaterfallSeries(["a", "b", "c"], [1, 2, 3]);

        var lines = Drawn(series, "DrawLine");

        Assert.Equal(2, lines.Count);
        for (int i = 0; i < 2; i++)
        {
            var from = (Point)Arg(lines[i], "p1")!;
            var to = (Point)Arg(lines[i], "p2")!;
            double slot = Bounds.Width / 3;
            Assert.Equal((i + 0.5) * slot + 0.3 * slot, from.X, 9);
            Assert.Equal((i + 1.5) * slot - 0.3 * slot, to.X, 9);
        }
    }
}
