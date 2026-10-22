// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A mark that stands on a base (a bar, a histogram's bin, a count plot's bar, a waterfall step, an area filled
/// down to a floor, a stacked layer) is drawn from its base to the value it reaches, so its range holds both, as far as the
/// axis can place them: matplotlib's data limits are the union of what is drawn. A log axis has no place for 0: the base
/// is drawn at the edge where the axis begins (<see cref="DataTransform.DataToPixelClippedY"/>) and takes no part in the
/// range, which starts at the lowest value the marks reach, matplotlib's floor (the smallest positive value, its
/// <c>minpos</c>). Before, the base 0 was handed to the range as it is, the log axis lifted that floor to three decades under
/// the top, and every mark shorter than that hung below the plot: <c>Bar(["1 KB", "1 MB", "1 GB"], [0.4, 35, 41000])</c>
/// on a log axis wrote <c>&lt;rect y="464.24" height="-152.24"&gt;</c> in a 500 x 350 figure, a bar a browser does not draw
/// and the PNG drew below the plot. On a linear axis the same omission let bars that all fall hang from a zero above the
/// plot (355.91 px past its top edge), a stack with a falling layer reach 128.57 px below it, and a histogram's range ignore
/// its <see cref="HistogramSeries.Density"/>, <see cref="HistogramSeries.Cumulative"/> and
/// <see cref="HistogramSeries.Weights"/> (cumulative bars 244.29 px and weighted ones 7,444.29 px above the plot).</summary>
public class StandingMarkRangeTests
{
    private static readonly string[] Sizes = ["1 KB", "1 MB", "1 GB"];
    private static readonly double[] Small = [0.4, 35, 41000];
    private static readonly double[] X = [0.0, 1, 2];

    private static Axes LogY()
    {
        var axes = new Axes();
        axes.YAxis.Scale = AxisScale.Log;
        return axes;
    }

    private static DataRangeContribution RangeOf(ISeries series, Axes axes) => series.ComputeDataRange(new AxesContextAdapter(axes));

    // ---- the floor of a log axis is the lowest value the marks reach ----------------------------------------------------

    [Fact]
    public void Bars_OnALogAxis_RangeFromTheirLowestValue()
    {
        var axes = LogY();
        var range = RangeOf(axes.Bar(Sizes, Small), axes);

        Assert.Equal(0.4, range.YMin);
        Assert.Equal(41000, range.YMax);
    }

    [Fact]
    public void BarsThatLieDown_OnALogAxis_RangeFromTheirLowestValue()
    {
        var axes = new Axes();
        axes.XAxis.Scale = AxisScale.Log;
        var bars = axes.Bar(Sizes, Small);
        bars.Orientation = BarOrientation.Horizontal;

        var range = RangeOf(bars, axes);

        Assert.Equal(0.4, range.XMin);
        Assert.Equal(41000, range.XMax);
    }

    /// <summary>The lowest layer of a stack reaches 0.4 in its first column: the floor, not the lowest column total.</summary>
    [Fact]
    public void StackedBars_OnALogAxis_RangeFromTheLowestEndOfAnyLayer()
    {
        var axes = LogY();
        axes.BarMode = BarMode.Stacked;
        var lower = axes.Bar(Sizes, Small);
        axes.Bar(Sizes, [1.0, 1, 1]);

        var range = RangeOf(lower, axes);

        Assert.Equal(0.4, range.YMin);
        Assert.Equal(41001, range.YMax);
    }

    /// <summary>A bar on a base of 0, one on a base of 10 and one that falls from 20 to -5: the base 0 takes no part on a
    /// log axis, the base 10 does, and it is the lowest base the axis can place that a margin stops at; the falling bar's
    /// end cannot be placed, so it is not drawn and its base 20 takes no part either.</summary>
    [Fact]
    public void BarsOnBases_OnALogAxis_RangeOverTheEdgesTheAxisCanPlace()
    {
        var axes = LogY();
        var bars = axes.Bar(["a", "b", "c"], [0.5, 5, -25]);
        bars.StackBaseline = [0.0, 10, 20];

        var range = RangeOf(bars, axes);

        Assert.Equal(0.5, range.YMin);
        Assert.Equal(15, range.YMax);
        Assert.Equal(10, range.StickyYMin);
    }

    [Fact]
    public void AHistogram_OnALogAxis_RangesFromItsLowestBin()
    {
        var axes = LogY();
        var range = RangeOf(axes.Hist([.. Enumerable.Repeat(1.5, 5000), 9.5]), axes);

        Assert.Equal(1, range.YMin);
        Assert.Equal(5000, range.YMax);
    }

    /// <summary>A histogram of no observations stands on a zero the log axis cannot place and reaches nothing: it adds no
    /// height to the range (on a linear axis it adds the zero, as before).</summary>
    [Fact]
    public void AnEmptyHistogram_OnALogAxis_AddsNoHeight()
    {
        var axes = LogY();
        var range = RangeOf(axes.Hist([]), axes);

        Assert.Null(range.YMin);
        Assert.Null(range.YMax);
        Assert.Equal(0, RangeOf(new Axes().Hist([]), new Axes()).YMax);
    }

    /// <summary>A waterfall whose levels never rise above zero (-5, then -8) has nothing a log axis can place.</summary>
    [Fact]
    public void AWaterfallThatStaysBelowZero_OnALogAxis_AddsNoHeight()
    {
        var axes = LogY();
        var range = RangeOf(axes.Waterfall(["a", "b"], [-5.0, -3]), axes);

        Assert.Null(range.YMin);
        Assert.Null(range.YMax);
    }

    [Theory]
    [InlineData(BarOrientation.Vertical)]
    [InlineData(BarOrientation.Horizontal)]
    public void ACountPlot_OnALogAxis_RangesFromItsLowestCount(BarOrientation orientation)
    {
        var axes = new Axes();
        (orientation == BarOrientation.Vertical ? axes.YAxis : axes.XAxis).Scale = AxisScale.Log;
        var counts = axes.Countplot([.. Enumerable.Repeat("a", 5000), "b"]);
        counts.Orientation = orientation;

        var range = RangeOf(counts, axes);

        Assert.Equal(1, orientation == BarOrientation.Vertical ? range.YMin : range.XMin);
        Assert.Equal(5000, orientation == BarOrientation.Vertical ? range.YMax : range.XMax);
    }

    /// <summary>Totals of 41000, 0.4 and 10.4: the first step stands on 0, which a log axis cannot place.</summary>
    [Fact]
    public void AWaterfall_OnALogAxis_RangesFromItsLowestTotal()
    {
        var axes = LogY();
        var range = RangeOf(axes.Waterfall(["a", "b", "c"], [41000, -40999.6, 10]), axes);

        Assert.Equal(0.4, range.YMin!.Value, 9);
        Assert.Equal(41000, range.YMax);
    }

    [Fact]
    public void AnAreaFilledToZero_OnALogAxis_RangesFromItsLowestValue()
    {
        var axes = LogY();
        var range = RangeOf(axes.FillBetween(X, Small), axes);

        Assert.Equal(0.4, range.YMin);
        Assert.Equal(41000, range.YMax);
    }

    /// <summary>An area between two curves ranges over both, as far as the axis can place them: the lower curve's 0 takes no
    /// part.</summary>
    [Fact]
    public void AnAreaBetweenTwoCurves_OnALogAxis_RangesOverTheValuesItCanPlace()
    {
        var axes = LogY();
        var range = RangeOf(axes.FillBetween(X, [3.0, 35, 41000], [0.0, 2, 1]), axes);

        Assert.Equal(1, range.YMin);
        Assert.Equal(41000, range.YMax);
    }

    [Fact]
    public void AStackedArea_OnALogAxis_RangesFromTheLowestEdgeOfAnyLayer()
    {
        var axes = LogY();
        var range = RangeOf(axes.StackPlot(X, [Small, [1.0, 1, 1]]), axes);

        Assert.Equal(0.4, range.YMin);
        Assert.Equal(41001, range.YMax);
    }

    /// <summary>A box stands on its lower statistic, which a log axis clips to its edge when it cannot place it: a sample
    /// holding -1 ranges over the values the axis can place, from 0.001, upright and lying down. Before, the -1 lifted the
    /// floor to three decades under the top and the box of 0.001 to 0.004 hung 307.07 px below the plot.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ABox_OnALogAxis_RangesOverTheValuesItCanPlace(bool upright)
    {
        var axes = new Axes();
        (upright ? axes.YAxis : axes.XAxis).Scale = AxisScale.Log;
        var box = axes.BoxPlot([[-1.0, 0.001, 0.002, 0.003, 0.004, 10000]]);
        box.Vert = upright;

        var range = RangeOf(box, axes);

        Assert.Equal(0.001, upright ? range.YMin : range.XMin);
        Assert.Equal(10000, upright ? range.YMax : range.XMax);
    }

    [Fact]
    public void AViolin_OnALogAxis_RangesOverTheValuesItCanPlace()
    {
        var axes = LogY();
        var range = RangeOf(axes.Violin([[-1.0, 0.001, 0.002, 0.003, 0.004, 10000]]), axes);

        Assert.Equal(0.001, range.YMin);
        Assert.Equal(10000, range.YMax);
    }

    /// <summary>The axis the escalation was measured on: its floor is 0.4 padded by the theme's 5 % margin in log space,
    /// matplotlib's 0.2245 for the same data, where it was about 29.</summary>
    [Fact]
    public void TheLogAxisUnderBars_StartsAMarginBelowTheLowestBar()
    {
        var axes = LogY();
        axes.Bar(Sizes, Small);

        var range = new CartesianAxesRenderer(axes, new Rect(80, 60, 700, 400), new SvgRenderContext(), Theme.MatplotlibV2)
            .ComputeDataRanges();

        double decades = Math.Log10(41000) - Math.Log10(0.4);
        Assert.Equal(Math.Pow(10, Math.Log10(0.4) - 0.05 * decades), range.YMin, 9);
    }

    // ---- on a linear axis the range holds the base as well --------------------------------------------------------------

    /// <summary>Bars that all fall hang from 0: the range reaches up to it, and stops there, as matplotlib's sticky base
    /// does.</summary>
    [Fact]
    public void BarsThatAllFall_RangeUpToTheZeroTheyHangFrom()
    {
        var axes = new Axes();
        var range = RangeOf(axes.Bar(["a", "b", "c"], [-5.0, -3, -4]), axes);

        Assert.Equal(-5, range.YMin);
        Assert.Equal(0, range.YMax);
        Assert.Equal(0, range.StickyYMax);

        var drawn = new CartesianAxesRenderer(axes, new Rect(80, 60, 700, 400), new SvgRenderContext(), Theme.MatplotlibV2)
            .ComputeDataRanges();
        Assert.Equal(0, drawn.YMax);
    }

    /// <summary>A stack whose second layer falls: columns run 0 → 3 → -2, 0 → 3 → 4 and 0 → 3 → -1, so the bars reach from
    /// -2 to 4.</summary>
    [Fact]
    public void AStackWithAFallingLayer_RangesOverEveryEndItReaches()
    {
        var axes = new Axes { BarMode = BarMode.Stacked };
        var lower = axes.Bar(Sizes, [3.0, 3, 3]);
        axes.Bar(Sizes, [-5.0, 1, -4]);

        var range = RangeOf(lower, axes);

        Assert.Equal(-2, range.YMin);
        Assert.Equal(4, range.YMax);
    }

    /// <summary>Ten values in five bins of width 1.6 (3, 5, 1, 0 and 1 of them): the heights the bars are drawn at are the
    /// heights the range takes. Density divides by 10 x 1.6, a weight of 10 multiplies by 10.</summary>
    [Theory]
    [InlineData("counts", 5.0)]
    [InlineData("density", 0.3125)]
    [InlineData("cumulative", 10.0)]
    [InlineData("weights", 50.0)]
    public void AHistogram_RangesOverTheHeightsItDraws(string kind, double top)
    {
        var axes = new Axes();
        var histogram = axes.Hist([1.0, 2, 2, 3, 3, 3, 4, 4, 5, 9], 5);
        switch (kind)
        {
            case "density": histogram.Density = true; break;
            case "cumulative": histogram.Cumulative = true; break;
            case "weights": histogram.Weights = [10.0, 10, 10, 10, 10, 10, 10, 10, 10, 10]; break;
        }

        var range = RangeOf(histogram, axes);

        Assert.Equal(0, range.YMin);
        Assert.Equal(top, range.YMax!.Value, 9);
    }

    [Fact]
    public void AnAreaThatAllFalls_RangesUpToTheZeroItIsFilledTo()
    {
        var axes = new Axes();
        var range = RangeOf(axes.FillBetween(X, [-1.0, -3, -2]), axes);

        Assert.Equal(-3, range.YMin);
        Assert.Equal(0, range.YMax);
    }

    /// <summary>A stacked area whose second layer falls below its baseline: the layer's top edge (-2, 4, -1) is drawn, so
    /// the range reaches it.</summary>
    [Fact]
    public void AStackedAreaWithAFallingLayer_RangesOverEveryEdge()
    {
        var axes = new Axes();
        var range = RangeOf(axes.StackPlot(X, [[3.0, 3, 3], [-5.0, 1, -4]]), axes);

        Assert.Equal(-2, range.YMin);
        Assert.Equal(4, range.YMax);
    }

    // ---- what a figure draws: every mark inside the plot -----------------------------------------------------------------

    private static readonly Regex RectRe = new(
        "<rect x=\"([^\"]+)\" y=\"([^\"]+)\" width=\"([^\"]+)\" height=\"([^\"]+)\"", RegexOptions.Compiled);

    private static readonly Regex PointsRe = new("<(?:polygon|polyline) points=\"([^\"]+)\"", RegexOptions.Compiled);

    private static double N(string s) => double.Parse(s, CultureInfo.InvariantCulture);

    private static void Draw(string kind, AxesBuilder ax)
    {
        double[] many = [.. Enumerable.Repeat(1.5, 5000), 9.5];
        string[] counted = [.. Enumerable.Repeat("a", 5000), "b"];
        switch (kind)
        {
            case "bar": ax.Bar(Sizes, Small).SetYScale(AxisScale.Log); break;
            case "bar lying": ax.Bar(Sizes, Small, s => s.Orientation = BarOrientation.Horizontal).SetXScale(AxisScale.Log); break;
            case "bar stacked": ax.Bar(Sizes, Small).Bar(Sizes, [1.0, 1, 1]).SetBarMode(BarMode.Stacked).SetYScale(AxisScale.Log); break;
            case "histogram": ax.Hist(many).SetYScale(AxisScale.Log); break;
            case "count": ax.Countplot(counted).SetYScale(AxisScale.Log); break;
            case "count lying": ax.Countplot(counted, s => s.Orientation = BarOrientation.Horizontal).SetXScale(AxisScale.Log); break;
            case "waterfall": ax.Waterfall(["a", "b", "c"], [41000, -40999.6, 10]).SetYScale(AxisScale.Log); break;
            case "area": ax.FillBetween(X, Small).SetYScale(AxisScale.Log); break;
            case "stack plot": ax.StackPlot(X, [Small, [1.0, 1, 1]]).SetYScale(AxisScale.Log); break;
            case "box": ax.BoxPlot([[-1.0, 0.001, 0.002, 0.003, 0.004, 10000]]).SetYScale(AxisScale.Log); break;
            case "box lying": ax.BoxPlot([[-1.0, 0.001, 0.002, 0.003, 0.004, 10000]], s => s.Vert = false).SetXScale(AxisScale.Log); break;
            case "bars that fall": ax.Bar(Sizes, [-5.0, -3, -4]); break;
            case "stack with a falling layer": ax.Bar(Sizes, [3.0, 3, 3]).Bar(Sizes, [-5.0, 1, -4]).SetBarMode(BarMode.Stacked); break;
            case "cumulative histogram": ax.Hist([1.0, 2, 2, 3, 3, 3, 4, 4, 5, 9], 5, s => s.Cumulative = true); break;
            case "weighted histogram": ax.Hist([1.0, 2, 3], 3, s => s.Weights = [10.0, 20, 30]); break;
            case "area that falls": ax.FillBetween(X, [-1.0, -3, -2]); break;
            case "stacked area with a falling layer": ax.StackPlot(X, [[3.0, 3, 3], [-5.0, 1, -4]]); break;
        }
    }

    /// <summary>Every rectangle and every polygon a figure of standing marks draws lies inside its plot, on a log axis whose
    /// marks reach below three decades under the top and on a linear axis whose marks fall below their base.</summary>
    [Theory]
    [InlineData("bar")]
    [InlineData("bar lying")]
    [InlineData("bar stacked")]
    [InlineData("histogram")]
    [InlineData("count")]
    [InlineData("count lying")]
    [InlineData("waterfall")]
    [InlineData("area")]
    [InlineData("stack plot")]
    [InlineData("box")]
    [InlineData("box lying")]
    [InlineData("bars that fall")]
    [InlineData("stack with a falling layer")]
    [InlineData("cumulative histogram")]
    [InlineData("weighted histogram")]
    [InlineData("area that falls")]
    [InlineData("stacked area with a falling layer")]
    public void EveryStandingMark_IsDrawnInsideThePlot(string kind)
    {
        var figure = Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => Draw(kind, ax)).Build();
        var plot = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];
        string svg = figure.ToSvg();
        const double Tolerance = 1e-6;

        var marks = RectRe.Matches(svg).Select(m => new Rect(N(m.Groups[1].Value), N(m.Groups[2].Value), N(m.Groups[3].Value), N(m.Groups[4].Value)))
            .Where(r => r != new Rect(0, 0, 500, 350) && r != plot).ToList();
        Assert.True(marks.Count + PointsRe.Matches(svg).Count > 0, $"{kind}: no mark was drawn");
        foreach (var r in marks)
        {
            Assert.True(r.Width >= 0 && r.Height >= 0, $"{kind}: a rectangle of {r.Width} x {r.Height}");
            Assert.True(r.X >= plot.X - Tolerance && r.Right <= plot.Right + Tolerance
                     && r.Y >= plot.Y - Tolerance && r.Bottom <= plot.Bottom + Tolerance,
                $"{kind}: a rectangle at {r} outside the plot {plot}");
        }
        foreach (Match m in PointsRe.Matches(svg))
        {
            foreach (var pair in m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var xy = pair.Split(',');
                double y = N(xy[1]);
                Assert.True(y >= plot.Y - Tolerance && y <= plot.Bottom + Tolerance, $"{kind}: a point at y = {y} outside the plot {plot}");
            }
        }
    }

    /// <summary>Under a limit set above the bar's value, the bar stands on the lower edge and lies wholly below it, inside the
    /// clip the plot draws a set limit with, so none of it shows, as matplotlib clips it; its rectangle has a positive size.</summary>
    [Fact]
    public void ABarBelowASetLowerLimit_LiesBeyondTheLowerEdge_InsideThePlotsClip()
    {
        var figure = Plt.Create().WithSize(500, 350)
            .AddSubPlot(1, 1, 1, ax => ax.Bar(Sizes, Small).SetYScale(AxisScale.Log).SetYLim(1, 100000)).Build();
        var plot = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];
        string svg = figure.ToSvg();

        var first = RectRe.Matches(svg).Select(m => new Rect(N(m.Groups[1].Value), N(m.Groups[2].Value), N(m.Groups[3].Value), N(m.Groups[4].Value)))
            .First(r => r.Width < plot.Width && r.Height != plot.Height && r != new Rect(0, 0, 500, 350));
        Assert.True(first.Height > 0, $"the bar below the limit is {first.Height} high");
        Assert.Equal(plot.Bottom, first.Y, 9);
        Assert.Contains("clip-path=", svg);
    }
}
