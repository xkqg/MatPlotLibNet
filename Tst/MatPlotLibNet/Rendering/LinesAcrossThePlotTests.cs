// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A line drawn across the plot at a data value: a reference line (<c>AxHLine</c>, <c>AxVLine</c>), a horizontal
/// level and the levels of a Fibonacci retracement. Two rules, each in one place:
/// <list type="number">
/// <item>the automatic range takes the line in as it takes a data value there, as matplotlib's <c>axhline</c> and
/// <c>axvline</c> update the data limits, so a line beyond the data is drawn inside the plot. Before, the range was the
/// series' alone: in a 600 x 400 figure of data from 2 to 8, <c>AxHLine(20)</c> was drawn at y = -498, above the figure,
/// and <c>AxVLine(15)</c> at x = 730, right of it;</item>
/// <item>a line beyond a limit the caller set is not drawn, nor its label, as matplotlib clips every line of an axes to the
/// axes; before, it was drawn over the margins, the tick labels and the title, or off the figure.</item>
/// </list></summary>
public class LinesAcrossThePlotTests
{
    private static readonly double[] X = [0, 2, 4, 6, 8, 10];
    private static readonly double[] Y = [2, 5, 3, 8, 4, 6];

    private static DataRange RangeOf(Action<Axes> add, AxisScale yScale = AxisScale.Linear)
    {
        var axes = new Axes();
        axes.YAxis.Scale = yScale;
        axes.Plot(X, Y);
        add(axes);
        return new CartesianAxesRenderer(axes, new Rect(80, 60, 700, 400), new SvgRenderContext(), Theme.MatplotlibV2)
            .ComputeDataRanges();
    }

    // ---- the range takes the line in ------------------------------------------------------------------------------------

    /// <summary>A horizontal reference line counts in the Y range exactly as a data value at its height: the range equals
    /// that of the same data with one more point there, above, below and inside the data.</summary>
    [Theory]
    [InlineData(20.0)]
    [InlineData(-5.0)]
    [InlineData(5.0)]
    public void AHorizontalReferenceLine_CountsInTheRange_AsADataValueAtItsHeight(double y) =>
        Assert.Equal(RangeOf(a => a.Scatter([X[0]], [y])), RangeOf(a => a.AxHLine(y)));

    /// <summary>A vertical reference line counts in the X range exactly as a data value at its place.</summary>
    [Theory]
    [InlineData(15.0)]
    [InlineData(-3.0)]
    [InlineData(5.0)]
    public void AVerticalReferenceLine_CountsInTheRange_AsADataValueAtItsPlace(double x) =>
        Assert.Equal(RangeOf(a => a.Scatter([x], [Y[0]])), RangeOf(a => a.AxVLine(x)));

    /// <summary>A line inside the data leaves the range as the data make it.</summary>
    [Fact]
    public void ALineInsideTheData_LeavesTheRangeAsItWas()
    {
        var plain = RangeOf(_ => { });

        Assert.Equal(plain, RangeOf(a => a.AxHLine(5)));
        Assert.Equal(plain, RangeOf(a => a.AxVLine(5)));
        Assert.Equal(plain, RangeOf(a => a.AddLevel(3)));
    }

    /// <summary>A horizontal level counts like a horizontal reference line.</summary>
    [Fact]
    public void AHorizontalLevel_CountsInTheRange_AsADataValueAtItsHeight() =>
        Assert.Equal(RangeOf(a => a.Scatter([X[0]], [20.0])), RangeOf(a => a.AddLevel(20)));

    /// <summary>A Fibonacci retracement counts with all its levels, from the high to the low.</summary>
    [Fact]
    public void AFibonacciRetracement_CountsInTheRange_FromItsHighToItsLow() =>
        Assert.Equal(RangeOf(a => a.Scatter([X[0], X[0]], [30.0, -10.0])), RangeOf(a => a.AddFibonacci(30, -10)));

    /// <summary>A line at a value its axis cannot place adds nothing: a missing value, an infinity, and 0 or less on a log
    /// axis; a positive value on a log axis counts.</summary>
    [Fact]
    public void ALineAtAValueTheAxisCannotPlace_AddsNothing()
    {
        var plain = RangeOf(_ => { });
        var plainLog = RangeOf(_ => { }, AxisScale.Log);

        Assert.Equal(plain, RangeOf(a => a.AxHLine(double.NaN)));
        Assert.Equal(plain, RangeOf(a => a.AxHLine(double.PositiveInfinity)));
        Assert.Equal(plain, RangeOf(a => a.AxVLine(double.NegativeInfinity)));
        Assert.Equal(plainLog, RangeOf(a => a.AxHLine(0), AxisScale.Log));
        Assert.Equal(plainLog, RangeOf(a => a.AxHLine(-4), AxisScale.Log));
        Assert.Equal(RangeOf(a => a.Scatter([X[0]], [1000.0]), AxisScale.Log), RangeOf(a => a.AxHLine(1000), AxisScale.Log));
    }

    /// <summary>A limit the caller set is the range drawn, line or no line.</summary>
    [Fact]
    public void ALimitTheCallerSet_StaysTheRange()
    {
        var range = RangeOf(a => { a.YAxis.Min = 0; a.YAxis.Max = 10; a.AxHLine(20); });

        Assert.Equal(0, range.YMin);
        Assert.Equal(10, range.YMax);
    }

    // ---- the picture ----------------------------------------------------------------------------------------------------

    private static readonly Color Magenta = Color.FromHex("#FF00FF");
    private static readonly Color Cyan = Color.FromHex("#00FFFF");
    private static readonly Color Olive = Color.FromHex("#808000");
    private static readonly Color Teal = Color.FromHex("#008080");
    private static readonly Color Navy = Color.FromHex("#000080");

    private static string Svg(Action<AxesBuilder> add) =>
        Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => { ax.Plot(X, Y); add(ax); }).ToSvg();

    /// <summary>A reference line, a level and a Fibonacci retracement beyond the data are drawn inside the plot, across its
    /// whole width or height.</summary>
    [Fact]
    public void LinesBeyondTheData_AreDrawnInsideThePlot()
    {
        string svg = Svg(ax => ax
            .AxHLine(20, r => r.Color = Magenta)
            .AxHLine(-5, r => r.Color = Cyan)
            .AxVLine(15, r => r.Color = Olive)
            .AddLevel(25, l => l.Color = Teal)
            .AddFibonacci(30, -10, f => f.Color = Navy));

        var plot = PlotArea(svg);
        var lines = Lines(svg);
        foreach (var colour in new[] { Magenta, Cyan, Teal })
        {
            var line = Assert.Single(lines, l => l.Stroke == colour.ToHex());
            AssertInside(plot, line);
            Assert.Equal(plot.X, line.X1, 9);
            Assert.Equal(plot.Right, line.X2, 9);
        }
        var vertical = Assert.Single(lines, l => l.Stroke == Olive.ToHex());
        AssertInside(plot, vertical);
        Assert.Equal(plot.Y, vertical.Y1, 9);
        Assert.Equal(plot.Bottom, vertical.Y2, 9);
        var fibonacci = lines.Where(l => l.Stroke == Navy.ToHex()).ToList();
        Assert.Equal(7, fibonacci.Count);
        Assert.All(fibonacci, l => AssertInside(plot, l));
    }

    /// <summary>Under limits the caller set, a line beyond them is not drawn, and neither is its label; a line inside them is
    /// drawn across the plot, and one standing on a limit is drawn on that edge.</summary>
    [Fact]
    public void UnderLimitsTheCallerSet_ALineBeyondThem_IsNotDrawn_NorItsLabel()
    {
        string svg = Svg(ax => ax
            .SetXLim(0, 10).SetYLim(0, 12)
            .AxHLine(20, r => { r.Color = Magenta; r.Label = "high"; })
            .AxHLine(-5, r => { r.Color = Cyan; r.Label = "low"; })
            .AxVLine(15, r => { r.Color = Olive; r.Label = "late"; })
            .AxHLine(12, r => r.Color = Teal)
            .AddLevel(25, l => { l.Color = Navy; l.Label = "resistance"; })
            .AddFibonacci(30, -10, f => f.Color = Color.FromHex("#800000")));

        var plot = PlotArea(svg);
        var lines = Lines(svg);
        Assert.DoesNotContain(lines, l => l.Stroke == Magenta.ToHex() || l.Stroke == Cyan.ToHex() || l.Stroke == Olive.ToHex() || l.Stroke == Navy.ToHex());
        Assert.DoesNotContain(">high<", svg);
        Assert.DoesNotContain(">low<", svg);
        Assert.DoesNotContain(">late<", svg);
        Assert.DoesNotContain(">resistance<", svg);

        var onTheLimit = Assert.Single(lines, l => l.Stroke == Teal.ToHex());
        Assert.Equal(plot.Y, onTheLimit.Y1, 9);

        // Of the seven levels from 30 down to -10 the two inside 0..12 are drawn (10 and 5.28), with their labels only.
        var fibonacci = lines.Where(l => l.Stroke == "#800000").ToList();
        Assert.Equal(2, fibonacci.Count);
        Assert.All(fibonacci, l => AssertInside(plot, l));
        Assert.Equal(2, XDocument.Parse(svg).Descendants().Count(e => e.Name.LocalName == "text" && (string?)e.Attribute("fill") == "#800000"));
    }

    // ---- reading the picture --------------------------------------------------------------------------------------------

    private readonly record struct SvgLine(double X1, double Y1, double X2, double Y2, string Stroke);

    private static double Num(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    private static List<SvgLine> Lines(string svg) =>
        [.. XDocument.Parse(svg).Descendants().Where(e => e.Name.LocalName == "line").Select(e => new SvgLine(
            Num((string)e.Attribute("x1")!), Num((string)e.Attribute("y1")!), Num((string)e.Attribute("x2")!), Num((string)e.Attribute("y2")!),
            (string?)e.Attribute("stroke") ?? ""))];

    /// <summary>The plot area: the second rectangle drawn into the figure, after the figure's own background.</summary>
    private static Rect PlotArea(string svg)
    {
        var rect = XDocument.Parse(svg).Root!.Elements().Where(e => e.Name.LocalName == "rect").Skip(1).First();
        return new(Num((string)rect.Attribute("x")!), Num((string)rect.Attribute("y")!),
                   Num((string)rect.Attribute("width")!), Num((string)rect.Attribute("height")!));
    }

    private static void AssertInside(Rect plot, SvgLine line)
    {
        foreach (var (x, y) in new[] { (line.X1, line.Y1), (line.X2, line.Y2) })
        {
            Assert.InRange(x, plot.X - 1e-9, plot.Right + 1e-9);
            Assert.InRange(y, plot.Y - 1e-9, plot.Bottom + 1e-9);
        }
    }
}
