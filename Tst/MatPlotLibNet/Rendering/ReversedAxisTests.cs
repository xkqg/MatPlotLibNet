// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An axis drawn the other way round keeps every tick: the ticks of a reversed axis are the ticks of the same interval, drawn
/// where their values are. There are two ways to ask for it and one rule for both. A reversed limit (<c>SetYLim(5.5, 0.5)</c>,
/// matplotlib's way to put rank 1 at the top) draws the axis upside down; before, it was handed to the tick locator as (min 5.5, max
/// 0.5) and the locator, which counts from min up to max, returned the single tick 5.5. <see cref="Axis.Inverted"/> is matplotlib's
/// <c>Axis.set_inverted(True)</c>: a state, not a toggle, so a limit set high to low on an inverted axis does not turn it back. Before,
/// the property was written to JSON and read back, and nothing drew it. The rule every axis and every way shares is a theory over
/// the way and the axis.</summary>
public class ReversedAxisTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0, 4.0, 5.0];
    private static readonly double[] Ranks = [1.0, 3.0, 5.0, 2.0, 4.0];
    private static readonly double[] OnTheRight = [100.0, 300.0, 500.0, 200.0, 400.0];
    private static double Value(DrawnText label) => double.Parse(label.Text.Replace('−', '-'), CultureInfo.InvariantCulture);

    private static Figure Build(Action<Axes> configure)
    {
        var figure = Plt.Create().WithSize(600, 500).AddSubPlot(1, 1, 1, ax => ax.Plot(X, Ranks)).Build();
        configure(figure.SubPlots[0]);
        return figure;
    }

    private static string Render(Action<Axes> configure) => Build(configure).ToSvg();

    private static string[] Texts(IEnumerable<DrawnText> labels) => labels.Select(l => l.Text).Order(StringComparer.Ordinal).ToArray();

    // ── one rule, two ways to ask for it, three axes ────────────────────────────────────────────────────

    /// <summary>The axes a reversal is read on: where its labels stand (their text anchor), the interval a reversed limit is given
    /// over, and whether the axis runs along Y (a larger value stands lower when reversed) or along X (further left).</summary>
    private sealed record Reading(string Anchor, double Low, double High, bool AlongY);

    private static readonly Dictionary<string, Reading> Readings = new()
    {
        ["Y"] = new("end", 0.5, 5.5, true),
        ["X"] = new("middle", 0, 6, false),
        ["RightY"] = new("start", 0, 600, true),
    };

    private static Axis AxisOf(Axes axes, string name) => name switch
    {
        "Y" => axes.YAxis,
        "X" => axes.XAxis,
        _ => axes.YAxes.Right.FirstOrAdd().Axis,
    };

    /// <summary>The picture, with the axis drawn forward or the other way round by the given way: a limit set high to low, or
    /// <see cref="Axis.Inverted"/> (over its automatic range).</summary>
    private static string Draw(string way, string name, bool reversed) => Render(axes =>
    {
        if (name == "RightY") axes.YAxes.Right.FirstOrAdd().AddSeries(new LineSeries(X, OnTheRight));
        var axis = AxisOf(axes, name);
        var reading = Readings[name];
        if (way == "Limit")
        {
            axis.Min = reversed ? reading.High : reading.Low;
            axis.Max = reversed ? reading.Low : reading.High;
        }
        else if (reversed)
        {
            axis.Inverted = true;
        }
    });

    public static TheoryData<string, string> Reversals { get; } = new()
    {
        { "Limit", "Y" }, { "Limit", "X" }, { "Limit", "RightY" },
        { "Inverted", "Y" }, { "Inverted", "X" }, { "Inverted", "RightY" },
    };

    [Theory]
    [MemberData(nameof(Reversals))]
    public void AReversedAxis_DrawsTheTicksOfTheSameRange_TheOtherWayRound(string way, string name)
    {
        var reading = Readings[name];
        var forward = Draw(way, name, reversed: false).TextsAnchored(reading.Anchor);
        var reversed = Draw(way, name, reversed: true).TextsAnchored(reading.Anchor);

        Assert.True(forward.Count >= (way == "Inverted" && name == "RightY" ? 3 : 5), $"forward {name} draws {forward.Count} labels");
        Assert.Equal(Texts(forward), Texts(reversed));
        // Drawn where the values are: forward, a larger value stands higher (smaller pixel y) or further right; reversed, the other way.
        var byValue = reversed.OrderBy(Value).ToList();
        for (int i = 1; i < byValue.Count; i++)
        {
            var (larger, smaller) = (byValue[i], byValue[i - 1]);
            Assert.True(reading.AlongY ? larger.Y > smaller.Y : larger.X < smaller.X,
                $"{way} reversed: label {larger.Text} at ({larger.X}, {larger.Y}) must stand {(reading.AlongY ? "below" : "left of")} {smaller.Text} at ({smaller.X}, {smaller.Y})");
        }
    }

    /// <summary>matplotlib 3.11.2 at 6 x 5 in, <c>ax.set_ylim(0.5, 5.5)</c>: labels 1, 2, 3, 4, 5 (nine bins, the step at or above 0.56
    /// is 1), and the reversed limit reads them bottom to top as the forward one reads them top to bottom.</summary>
    [Fact]
    public void AReversedYLimit_DrawsMatplotlibsFiveLabels_ReadingTheForwardOnesTheOtherWayUp()
    {
        var forward = Draw("Limit", "Y", reversed: false).TextsAnchored("end");
        var reversed = Draw("Limit", "Y", reversed: true).TextsAnchored("end");

        Assert.Equal(5, forward.Count);
        Assert.Equal(forward.OrderByDescending(l => l.Y).Select(l => l.Text), reversed.OrderBy(l => l.Y).Select(l => l.Text));
    }

    [Fact]
    public void AReversedLogYLimit_DrawsTheDecadesOfTheSameInterval_UpsideDown()
    {
        static void Log(Axes a, double min, double max)
        {
            a.YAxis.Scale = AxisScale.Log;
            a.YAxis.Min = min;
            a.YAxis.Max = max;
        }
        var forward = Render(a => Log(a, 0.1, 1000)).TextsAnchored("end");
        var reversed = Render(a => Log(a, 1000, 0.1)).TextsAnchored("end");

        Assert.Equal(5, forward.Count);
        Assert.Equal(Texts(forward), Texts(reversed));
        // Ordered top to bottom, the reversed labels read the forward labels bottom to top.
        Assert.Equal(forward.OrderByDescending(l => l.Y).Select(l => l.Text), reversed.OrderBy(l => l.Y).Select(l => l.Text));
    }

    [Fact]
    public void AnInvertedSecondaryXAxis_DrawsItsTicksRightToLeft()
    {
        static void Top(Axes a) => a.PlotXSecondary([1_000.0, 3_000.0, 5_000.0], [1.0, 2.0, 3.0]);
        var forward = Render(Top).TextsAnchored("middle").Where(l => l.Y < 100).ToList();
        var inverted = Render(a => { Top(a); a.SecondaryXAxis!.Inverted = true; }).TextsAnchored("middle").Where(l => l.Y < 100).ToList();

        Assert.True(forward.Count >= 3, $"forward secondary X draws {forward.Count} labels");
        var byValue = inverted.OrderBy(Value).ToList();
        for (int i = 1; i < byValue.Count; i++)
            Assert.True(byValue[i].X < byValue[i - 1].X,
                $"inverted: label {byValue[i].Text} at x {byValue[i].X} must stand left of {byValue[i - 1].Text}");
    }

    // ── the two ways are one state ──────────────────────────────────────────────────────────────────────

    /// <summary>Inverting an axis whose limits run low to high draws exactly the figure of the same limits set high to low:
    /// one effect, one rule.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InvertedWithLimits_IsTheReversedLimit(bool onY)
    {
        string inverted = Render(a =>
        {
            var axis = onY ? a.YAxis : a.XAxis;
            axis.Min = 0.5; axis.Max = 5.5; axis.Inverted = true;
        });
        string reversed = Render(a =>
        {
            var axis = onY ? a.YAxis : a.XAxis;
            axis.Min = 5.5; axis.Max = 0.5;
        });

        Assert.Equal(reversed, inverted);
    }

    /// <summary>The two switches are one state, as matplotlib's <c>set_inverted(True)</c> is: an inverted axis whose limits
    /// are already set high to low is drawn reversed once, not turned back to forward.</summary>
    [Fact]
    public void InvertedAndReversedLimits_DrawTheAxisReversedOnce()
    {
        string both = Render(a => { a.YAxis.Min = 5.5; a.YAxis.Max = 0.5; a.YAxis.Inverted = true; });
        string reversed = Render(a => { a.YAxis.Min = 5.5; a.YAxis.Max = 0.5; });

        Assert.Equal(reversed, both);
    }

    /// <summary>A zoom reads the new limits off the axes as drawn, so on an inverted axis it writes them high to low; the
    /// axis stays the way round it was. A toggle reading would turn the chart over on the first scroll.</summary>
    [Fact]
    public void AnInvertedAxis_StaysReversed_AfterAScrollZoom()
    {
        var figure = Build(a => a.YAxis.Inverted = true);
        var areas = new ChartRenderer().ComputeLayout(figure, new MatPlotLibNet.Rendering.Svg.SvgRenderContext()).PlotAreas;
        var layout = ChartLayout.Create(figure, areas);
        var events = new List<FigureInteractionEvent>();
        var zoom = new ZoomModifier("c", layout, events.Add);
        var area = areas[0];

        zoom.OnScroll(new ScrollInputArgs(area.X + area.Width / 2, area.Y + area.Height / 2, 0, -1));
        events.Single().ApplyTo(figure);

        var byValue = figure.ToSvg().TextsAnchored("end").OrderBy(Value).ToList();
        Assert.True(byValue.Count >= 3);
        for (int i = 1; i < byValue.Count; i++)
            Assert.True(byValue[i].Y > byValue[i - 1].Y,
                $"after the zoom: label {byValue[i].Text} at y {byValue[i].Y} must stand below {byValue[i - 1].Text}");
    }

    [Fact]
    public void AnInvertedAxis_DrawsTheSamePictureAfterAJsonRoundTrip()
    {
        var figure = Build(a => { a.YAxis.Inverted = true; a.XAxis.Inverted = true; });

        var back = figure.RoundTripped();

        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    [Theory]
    [InlineData(1.0, 5.0, false, 1.0, 5.0)]
    [InlineData(5.0, 1.0, false, 5.0, 1.0)]
    [InlineData(1.0, 5.0, true, 5.0, 1.0)]
    [InlineData(5.0, 1.0, true, 5.0, 1.0)]
    public void OrientedBy_TurnsARangeHighToLow_OnlyForAnInvertedAxis(double lo, double hi, bool inverted, double drawnLo, double drawnHi)
    {
        var drawn = new AxisAutoscale(new Axis { Inverted = inverted }, isX: false, Theme.Default).Oriented(new Range1D(lo, hi));

        Assert.Equal(new Range1D(drawnLo, drawnHi), drawn);
    }

    [Theory]
    [InlineData(5.5, 0.5)]
    [InlineData(0.5, 5.5)]
    public void LocateInterval_IsOrderFree_AndAscending(double from, double to)
    {
        var ticks = new AutoLocator(8).LocateInterval(from, to);

        Assert.Equal(11, ticks.Length);
        Assert.Equal(0.5, ticks[0]);
        Assert.Equal(5.5, ticks[^1]);
    }

    // ── what a reversed limit leaves alone ──────────────────────────────────────────────────────────────

    /// <summary>The coordinates of every polyline and the vertices of every path, as drawn.</summary>
    private static List<XElement> Shapes(string svg, string name) =>
        svg.Each(name).ToList();

    [Fact]
    public void AReversedXLimit_KeepsTheVisiblePointsOfADownsampledLine()
    {
        // A line with MaxDisplayPoints is culled to the X range shown; that range is the interval, whichever way it runs.
        double[] x = Enumerable.Range(0, 200).Select(i => (double)i).ToArray();
        double[] y = x.Select(v => v % 7).ToArray();
        string Draw(double min, double max)
        {
            var figure = Plt.Create().WithSize(600, 500).AddSubPlot(1, 1, 1, ax => ax.Plot(x, y)).Build();
            var axes = figure.SubPlots[0];
            ((XYSeries)axes.Series[0]).MaxDisplayPoints = 50;
            axes.XAxis.Min = min; axes.XAxis.Max = max;
            return figure.ToSvg();
        }
        static int Points(string svg) => Shapes(svg, "polyline")
            .Select(p => ((string)p.Attribute("points")!).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length)
            .DefaultIfEmpty(0).Max();

        int forward = Points(Draw(0, 199));
        int reversed = Points(Draw(199, 0));

        Assert.True(forward >= 40, $"forward draws {forward} points");
        Assert.Equal(forward, reversed);
    }

    [Fact]
    public void AReversedXLimit_BinsAHexbinOverTheSameInterval()
    {
        double[] x = Enumerable.Range(0, 60).Select(i => (double)(i % 10)).ToArray();
        double[] y = Enumerable.Range(0, 60).Select(i => (double)(i / 10)).ToArray();
        string Draw(double min, double max)
        {
            var figure = Plt.Create().WithSize(600, 500).AddSubPlot(1, 1, 1, ax => ax.Hexbin(x, y)).Build();
            var axes = figure.SubPlots[0];
            axes.XAxis.Min = min; axes.XAxis.Max = max;
            return figure.ToSvg();
        }
        static double[] Widths(string svg) => Shapes(svg, "polygon").Select(p =>
        {
            var xs = ((string)p.Attribute("points")!).Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(pt => double.Parse(pt.Split(',')[0], CultureInfo.InvariantCulture)).ToArray();
            return Math.Round(xs.Max() - xs.Min(), 3);
        }).Order().ToArray();

        var forward = Widths(Draw(-1, 10));
        var reversed = Widths(Draw(10, -1));

        Assert.NotEmpty(forward);
        Assert.Equal(forward, reversed, (a, b) => Math.Abs(a - b) <= SvgMarkupExtensions.WrittenPrecision);
    }

    [Fact]
    public void AReversedYLimit_DrawsARugInsideThePlotArea_FromTheBottomEdge()
    {
        string Draw(double min, double max)
        {
            var figure = Plt.Create().WithSize(600, 500).AddSubPlot(1, 1, 1, ax => ax.Plot(X, Ranks)).Build();
            var axes = figure.SubPlots[0];
            axes.Rugplot(new[] { 1.5, 2.5, 3.5 }).Color = Color.FromHex("#123456");
            axes.YAxis.Min = min; axes.YAxis.Max = max;
            return figure.ToSvg();
        }
        // The rug ticks are the three lines in the rug's own colour; each runs from the bottom edge upwards.
        static double[] Lengths(string svg) => Shapes(svg, "line")
            .Where(l => string.Equals((string?)l.Attribute("stroke"), "#123456", StringComparison.OrdinalIgnoreCase))
            .Select(l => Math.Round(l.Number("y1") - l.Number("y2"), 3))
            .ToArray();

        var forward = Lengths(Draw(0.5, 5.5));
        var reversed = Lengths(Draw(5.5, 0.5));

        Assert.Equal(3, forward.Length);
        Assert.All(forward, d => Assert.True(d > 0, $"forward rug tick runs {d} px upwards"));
        Assert.Equal(forward, reversed);
    }
}
