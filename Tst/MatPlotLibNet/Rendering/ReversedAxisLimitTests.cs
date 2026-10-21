// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A reversed limit (<c>SetYLim(5.5, 0.5)</c>, matplotlib's way to put rank 1 at the top) draws the axis upside
/// down and keeps every tick: the ticks of a reversed axis are the ticks of the same interval, drawn where their values
/// are. Before, a reversed limit was handed to the tick locator as (min 5.5, max 0.5) and the locator, which counts from
/// min up to max, returned the single tick 5.5.</summary>
public class ReversedAxisLimitTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0, 4.0, 5.0];
    private static readonly double[] Ranks = [1.0, 3.0, 5.0, 2.0, 4.0];

    /// <summary>A tick label as drawn: its text and where it stands.</summary>
    private readonly record struct DrawnLabel(string Text, double X, double Y);

    /// <summary>The tick labels drawn with <paramref name="anchor"/> (left Y labels end at the axis, X labels are
    /// centred under it, right-hand Y labels start at it). The figures below have no title and no axis labels, so every
    /// text is a tick label.</summary>
    private static List<DrawnLabel> Labels(string svg, string anchor) => XDocument.Parse(svg).Descendants()
        .Where(e => e.Name.LocalName == "text" && (string?)e.Attribute("text-anchor") == anchor)
        .Select(e => new DrawnLabel(e.Value,
            double.Parse((string)e.Attribute("x")!, CultureInfo.InvariantCulture),
            double.Parse((string)e.Attribute("y")!, CultureInfo.InvariantCulture)))
        .ToList();

    private static double Value(DrawnLabel label) => double.Parse(label.Text.Replace('−', '-'), CultureInfo.InvariantCulture);

    private static string Render(Action<Axes> configure)
    {
        var figure = Plt.Create().WithSize(600, 500).AddSubPlot(1, 1, 1, ax => ax.Plot(X, Ranks)).Build();
        configure(figure.SubPlots[0]);
        return figure.ToSvg();
    }

    private static string[] Texts(IEnumerable<DrawnLabel> labels) => labels.Select(l => l.Text).Order(StringComparer.Ordinal).ToArray();

    [Fact]
    public void AReversedYLimit_DrawsTheTicksOfTheSameInterval_UpsideDown()
    {
        var forward = Labels(Render(a => { a.YAxis.Min = 0.5; a.YAxis.Max = 5.5; }), "end");
        var reversed = Labels(Render(a => { a.YAxis.Min = 5.5; a.YAxis.Max = 0.5; }), "end");

        Assert.Equal(11, forward.Count);
        Assert.Equal(Texts(forward), Texts(reversed));
        // Drawn where the values are: forward, a larger value stands higher (smaller pixel y); reversed, lower.
        var byValue = reversed.OrderBy(Value).ToList();
        for (int i = 1; i < byValue.Count; i++)
            Assert.True(byValue[i].Y > byValue[i - 1].Y,
                $"reversed: label {byValue[i].Text} at y {byValue[i].Y} must stand below {byValue[i - 1].Text} at y {byValue[i - 1].Y}");
        // Ordered top to bottom, the reversed labels read the forward labels bottom to top.
        Assert.Equal(forward.OrderByDescending(l => l.Y).Select(l => l.Text), reversed.OrderBy(l => l.Y).Select(l => l.Text));
    }

    [Fact]
    public void AReversedXLimit_DrawsTheTicksOfTheSameInterval_RightToLeft()
    {
        var forward = Labels(Render(a => { a.XAxis.Min = 0; a.XAxis.Max = 6; }), "middle");
        var reversed = Labels(Render(a => { a.XAxis.Min = 6; a.XAxis.Max = 0; }), "middle");

        Assert.True(forward.Count >= 5, $"forward X draws {forward.Count} labels");
        Assert.Equal(Texts(forward), Texts(reversed));
        var byValue = reversed.OrderBy(Value).ToList();
        for (int i = 1; i < byValue.Count; i++)
            Assert.True(byValue[i].X < byValue[i - 1].X,
                $"reversed: label {byValue[i].Text} at x {byValue[i].X} must stand left of {byValue[i - 1].Text} at x {byValue[i - 1].X}");
    }

    [Fact]
    public void AReversedSecondaryYLimit_DrawsTheTicksOfTheSameInterval_UpsideDown()
    {
        static void WithSecondary(Axes a, double min, double max)
        {
            a.PlotSecondary(X, [100.0, 300.0, 500.0, 200.0, 400.0]);
            a.SecondaryYAxis!.Min = min;
            a.SecondaryYAxis.Max = max;
        }
        var forward = Labels(Render(a => WithSecondary(a, 0, 600)), "start");
        var reversed = Labels(Render(a => WithSecondary(a, 600, 0)), "start");

        Assert.True(forward.Count >= 5, $"forward secondary Y draws {forward.Count} labels");
        Assert.Equal(Texts(forward), Texts(reversed));
        var byValue = reversed.OrderBy(Value).ToList();
        for (int i = 1; i < byValue.Count; i++)
            Assert.True(byValue[i].Y > byValue[i - 1].Y,
                $"reversed: label {byValue[i].Text} at y {byValue[i].Y} must stand below {byValue[i - 1].Text} at y {byValue[i - 1].Y}");
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
        var forward = Labels(Render(a => Log(a, 0.1, 1000)), "end");
        var reversed = Labels(Render(a => Log(a, 1000, 0.1)), "end");

        Assert.Equal(5, forward.Count);
        Assert.Equal(Texts(forward), Texts(reversed));
        // Ordered top to bottom, the reversed labels read the forward labels bottom to top.
        Assert.Equal(forward.OrderByDescending(l => l.Y).Select(l => l.Text), reversed.OrderBy(l => l.Y).Select(l => l.Text));
    }

    /// <summary>The coordinates of every polyline and the vertices of every path, as drawn.</summary>
    private static List<XElement> Shapes(string svg, string name) =>
        XDocument.Parse(svg).Descendants().Where(e => e.Name.LocalName == name).ToList();

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
            ((MatPlotLibNet.Models.Series.XYSeries)axes.Series[0]).MaxDisplayPoints = 50;
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
        Assert.Equal(forward, reversed);
    }

    [Fact]
    public void AReversedYLimit_DrawsARugInsideThePlotArea_FromTheBottomEdge()
    {
        string Draw(double min, double max)
        {
            var figure = Plt.Create().WithSize(600, 500).AddSubPlot(1, 1, 1, ax => ax.Plot(X, Ranks)).Build();
            var axes = figure.SubPlots[0];
            axes.Rugplot(new[] { 1.5, 2.5, 3.5 }).Color = MatPlotLibNet.Styling.Color.FromHex("#123456");
            axes.YAxis.Min = min; axes.YAxis.Max = max;
            return figure.ToSvg();
        }
        // The rug ticks are the three lines in the rug's own colour; each runs from the bottom edge upwards.
        static double[] Lengths(string svg) => Shapes(svg, "line")
            .Where(l => string.Equals((string?)l.Attribute("stroke"), "#123456", StringComparison.OrdinalIgnoreCase))
            .Select(l => Math.Round(double.Parse((string)l.Attribute("y1")!, CultureInfo.InvariantCulture)
                                  - double.Parse((string)l.Attribute("y2")!, CultureInfo.InvariantCulture), 3))
            .ToArray();

        var forward = Lengths(Draw(0.5, 5.5));
        var reversed = Lengths(Draw(5.5, 0.5));

        Assert.Equal(3, forward.Length);
        Assert.All(forward, d => Assert.True(d > 0, $"forward rug tick runs {d} px upwards"));
        Assert.Equal(forward, reversed);
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
}
