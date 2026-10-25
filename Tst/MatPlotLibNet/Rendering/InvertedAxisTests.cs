// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary><see cref="Axis.Inverted"/> draws the axis the other way round, by the rule a limit set high to low follows
/// (<see cref="ReversedAxisLimitTests"/>): the ticks of the same interval, each drawn where its value is. It is matplotlib's
/// <c>Axis.set_inverted(True)</c>: a state, not a toggle, so a limit set high to low on an inverted axis does not turn it
/// back. Before, the property was written to JSON and read back, and nothing drew it.</summary>
public class InvertedAxisTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0, 4.0, 5.0];
    private static readonly double[] Ranks = [1.0, 3.0, 5.0, 2.0, 4.0];

    /// <summary>A tick label as drawn: its text and where it stands.</summary>
    private readonly record struct DrawnLabel(string Text, double X, double Y);

    /// <summary>The tick labels drawn with <paramref name="anchor"/>: left Y labels end at the axis, X labels are centred
    /// under it, right-hand Y labels start at it. The figures below have no title and no axis labels.</summary>
    private static List<DrawnLabel> Labels(string svg, string anchor) => XDocument.Parse(svg).Descendants()
        .Where(e => e.Name.LocalName == "text" && (string?)e.Attribute("text-anchor") == anchor)
        .Select(e => new DrawnLabel(e.Value,
            double.Parse((string)e.Attribute("x")!, CultureInfo.InvariantCulture),
            double.Parse((string)e.Attribute("y")!, CultureInfo.InvariantCulture)))
        .ToList();

    private static double Value(DrawnLabel label) => double.Parse(label.Text.Replace('−', '-'), CultureInfo.InvariantCulture);

    private static Figure Build(Action<Axes> configure)
    {
        var figure = Plt.Create().WithSize(600, 500).AddSubPlot(1, 1, 1, ax => ax.Plot(X, Ranks)).Build();
        configure(figure.SubPlots[0]);
        return figure;
    }

    private static string Render(Action<Axes> configure) => Build(configure).ToSvg();

    [Fact]
    public void AnInvertedYAxis_DrawsTheTicksOfTheSameRange_UpsideDown()
    {
        var forward = Labels(Render(_ => { }), "end");
        var inverted = Labels(Render(a => a.YAxis.Inverted = true), "end");

        Assert.True(forward.Count >= 5, $"forward Y draws {forward.Count} labels");
        Assert.Equal(forward.Select(l => l.Text).Order(StringComparer.Ordinal), inverted.Select(l => l.Text).Order(StringComparer.Ordinal));
        var byValue = inverted.OrderBy(Value).ToList();
        for (int i = 1; i < byValue.Count; i++)
            Assert.True(byValue[i].Y > byValue[i - 1].Y,
                $"inverted: label {byValue[i].Text} at y {byValue[i].Y} must stand below {byValue[i - 1].Text} at y {byValue[i - 1].Y}");
    }

    [Fact]
    public void AnInvertedXAxis_DrawsTheTicksOfTheSameRange_RightToLeft()
    {
        var forward = Labels(Render(_ => { }), "middle");
        var inverted = Labels(Render(a => a.XAxis.Inverted = true), "middle");

        Assert.True(forward.Count >= 5, $"forward X draws {forward.Count} labels");
        Assert.Equal(forward.Select(l => l.Text).Order(StringComparer.Ordinal), inverted.Select(l => l.Text).Order(StringComparer.Ordinal));
        var byValue = inverted.OrderBy(Value).ToList();
        for (int i = 1; i < byValue.Count; i++)
            Assert.True(byValue[i].X < byValue[i - 1].X,
                $"inverted: label {byValue[i].Text} at x {byValue[i].X} must stand left of {byValue[i - 1].Text} at x {byValue[i - 1].X}");
    }

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

        var byValue = Labels(figure.ToSvg(), "end").OrderBy(Value).ToList();
        Assert.True(byValue.Count >= 3);
        for (int i = 1; i < byValue.Count; i++)
            Assert.True(byValue[i].Y > byValue[i - 1].Y,
                $"after the zoom: label {byValue[i].Text} at y {byValue[i].Y} must stand below {byValue[i - 1].Text}");
    }

    [Fact]
    public void AnInvertedSecondaryYAxis_DrawsItsTicksUpsideDown()
    {
        var forward = Labels(Render(a => a.PlotSecondary(X, [100.0, 300.0, 500.0, 200.0, 400.0])), "start");
        var inverted = Labels(Render(a =>
        {
            a.PlotSecondary(X, [100.0, 300.0, 500.0, 200.0, 400.0]);
            a.SecondaryYAxis!.Inverted = true;
        }), "start");

        Assert.True(forward.Count >= 3, $"forward secondary Y draws {forward.Count} labels");
        Assert.Equal(forward.Select(l => l.Text).Order(StringComparer.Ordinal), inverted.Select(l => l.Text).Order(StringComparer.Ordinal));
        var byValue = inverted.OrderBy(Value).ToList();
        for (int i = 1; i < byValue.Count; i++)
            Assert.True(byValue[i].Y > byValue[i - 1].Y,
                $"inverted: label {byValue[i].Text} at y {byValue[i].Y} must stand below {byValue[i - 1].Text} at y {byValue[i - 1].Y}");
    }

    [Fact]
    public void AnInvertedSecondaryXAxis_DrawsItsTicksRightToLeft()
    {
        static void Top(Axes a) => a.PlotXSecondary([1_000.0, 3_000.0, 5_000.0], [1.0, 2.0, 3.0]);
        var forward = Labels(Render(Top), "middle").Where(l => l.Y < 100).ToList();
        var inverted = Labels(Render(a => { Top(a); a.SecondaryXAxis!.Inverted = true; }), "middle").Where(l => l.Y < 100).ToList();

        Assert.True(forward.Count >= 3, $"forward secondary X draws {forward.Count} labels");
        var byValue = inverted.OrderBy(Value).ToList();
        for (int i = 1; i < byValue.Count; i++)
            Assert.True(byValue[i].X < byValue[i - 1].X,
                $"inverted: label {byValue[i].Text} at x {byValue[i].X} must stand left of {byValue[i - 1].Text}");
    }

    [Fact]
    public void AnInvertedAxis_DrawsTheSamePictureAfterAJsonRoundTrip()
    {
        var figure = Build(a => { a.YAxis.Inverted = true; a.XAxis.Inverted = true; });

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    [Theory]
    [InlineData(1.0, 5.0, false, 1.0, 5.0)]
    [InlineData(5.0, 1.0, false, 5.0, 1.0)]
    [InlineData(1.0, 5.0, true, 5.0, 1.0)]
    [InlineData(5.0, 1.0, true, 5.0, 1.0)]
    public void OrientedBy_TurnsARangeHighToLow_OnlyForAnInvertedAxis(double lo, double hi, bool inverted, double drawnLo, double drawnHi)
    {
        var drawn = new Range1D(lo, hi).OrientedBy(new Axis { Inverted = inverted });

        Assert.Equal(new Range1D(drawnLo, drawnHi), drawn);
    }
}
