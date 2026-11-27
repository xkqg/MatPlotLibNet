// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>Under the matplotlib themes a multiple of a power of ten that a log axis names is one of its minor ticks, labelled, and a
/// logit axis' named minors are too. Printed with matplotlib 3.11.2 (ax.set_yscale('log'); ax.set_ylim(2, 80); fig.canvas.draw()):
/// the major ticks in view are [10.0] alone, tick1line markersize 3.5 and get_pad() 3.5; the minor ticks 2, 3, 4, 6, 20, 30, 40 and 60
/// carry the labels '2\times10^{0}' .. '6\times10^{1}' with markersize 2.0 and get_pad() 3.4. Under the classic style a view of
/// 2.1 .. 2.9 has no major tick and labels its nine minors, markersize 2.0 and pad 4.0. ax.set_yscale('logit'); ylim (0.05, 0.95):
/// majors 0.1, 1/2, 0.9 (3.5 / 3.5), labelled minors 0.2, 0.3, 0.4, 0.6, 0.7, 0.8 (2.0 / 3.4); over (0.3, 0.7) MaxNLocator lays every
/// tick as a major. ax.grid(True) draws the major grid at 10 alone (gridline visible on the major tick only). minorticks_off() sets a
/// NullLocator: no minor tick, so no minor label. The library's own themes keep their look: a named multiple is drawn as the others.
/// </summary>
public sealed class LogNamedMultiplesAreMinorTicksTests
{
    private const double Pt = 100.0 / 72;

    private static Figure LogY(Theme theme, double lo, double hi, Action<AxesBuilder>? more = null, AxisScale scale = AxisScale.Log) => Plt.Create()
        .WithSize(640, 480)
        .WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax =>
        {
            // X over 10 .. 20, so no X label reads as a Y label.
            ax.Plot([10.0, 20.0], [lo, hi]).SetYScale(scale).SetYLim(lo, hi);
            more?.Invoke(ax);
        })
        .Build();

    private static List<(string Text, Point At)> Texts(RecordingRenderContext ctx) =>
        [.. ctx.Of<DrawTextCall>().Select(a => (a.Text, a.Position))];

    private static double LabelX(RecordingRenderContext ctx, string text) => Texts(ctx).Single(t => t.Text == text).At.X;

    /// <summary>The marks drawn across a left or right edge: short horizontal lines, by their length.</summary>
    private static List<(double Length, double Width, Color Color)> HorizontalMarks(RecordingRenderContext ctx) =>
        [.. ctx.Of<DrawLineCall>()
            .Select(a => (a.P1, a.P2, a.Thickness, a.Color))
            .Where(l => Math.Abs(l.Item1.Y - l.Item2.Y) < 1e-9 && Math.Abs(l.Item2.X - l.Item1.X) < 10)
            .Select(l => (Math.Abs(l.Item2.X - l.Item1.X), l.Item3, l.Item4))];

    /// <summary>The marks <paramref name="points"/> long: the end that meets the spine reaches half the spine's width (under a point)
    /// into it on a surface without a pixel grid.</summary>
    private static int MarksOfLength(RecordingRenderContext ctx, double points) =>
        HorizontalMarks(ctx).Count(m => m.Length - points * Pt is >= 0 and < 0.75);

    [Fact]
    public void UnderV2_ANamedMultiple_StandsItsLabelFromAMinorMark()
    {
        var ctx = LogY(Theme.MatplotlibV2, 2, 80).Drawn();

        // Major: size 3.5 + pad 3.5 points from the edge; labelled minor: size 2.0 + pad 3.4.
        double gain = (3.5 + 3.5 - (2.0 + 3.4)) * Pt;
        foreach (var label in new[] { "2×10⁰", "3×10⁰", "4×10⁰", "6×10⁰", "2×10¹", "3×10¹", "4×10¹", "6×10¹" })
            Assert.Equal(gain, LabelX(ctx, label) - LabelX(ctx, "10¹"), 9);
    }

    /// <summary>Under the default style a view of 2 .. 80 has 10 as its one major (3.5 points long) and 2 .. 9 and 20 .. 80 as fifteen
    /// minors (2 points long), eight of them labelled. Under the classic style a view of 2.1 .. 2.9 has no power: its nine labelled ticks
    /// are minors, marked on the left and, as ytick.right says, on the right (18 marks of 2 points, none of 4).</summary>
    [Theory]
    [InlineData("v2", 2, 80, 3.5, 1, 15)]
    [InlineData("classic", 2.1, 2.9, 4.0, 0, 18)]
    public void APowerIsMarkedAsAMajor_AndEveryLabelledMultipleAsAMinor(string theme, double low, double high, double majorPoints, int majors, int minors)
    {
        var ctx = LogY(theme.AsTheme(), low, high).Drawn();

        Assert.Equal(majors, MarksOfLength(ctx, majorPoints));
        Assert.Equal(minors, MarksOfLength(ctx, 2.0));
    }

    [Fact]
    public void UnderV2_ALogitAxis_StandsItsNamedMinorsFromMinorMarks()
    {
        var ctx = LogY(Theme.MatplotlibV2, 0.05, 0.95, scale: AxisScale.Logit).Drawn();

        double gain = (3.5 + 3.5 - (2.0 + 3.4)) * Pt;
        foreach (var label in new[] { "0.2", "0.3", "0.4", "0.6", "0.7", "0.8" })
            Assert.Equal(gain, LabelX(ctx, label) - LabelX(ctx, "½"), 9);
        Assert.Equal(3, MarksOfLength(ctx, 3.5));
    }

    [Fact]
    public void UnderV2_ALogitAxisZoomedPastItsIdealTicks_HasOnlyMajors()
    {
        var ctx = LogY(Theme.MatplotlibV2, 0.3, 0.7, scale: AxisScale.Logit).Drawn();

        var xs = Texts(ctx).Where(t => t.Text is "0.30" or "0.40" or "½" or "0.60" or "0.70" or "0.3" or "0.4" or "0.6" or "0.7")
            .Select(t => t.At.X).Distinct().ToList();
        Assert.Single(xs);
        Assert.Equal(0, MarksOfLength(ctx, 2.0));
    }

    [Fact]
    public void UnderTheLibrarysTheme_ANamedMultiple_KeepsItsMajorMarkAndPad()
    {
        var ctx = LogY(Theme.Default, 2, 80).Drawn();

        Assert.Equal(LabelX(ctx, "10¹"), LabelX(ctx, "2×10⁰"), 9);
    }

    [Fact]
    public void UnderV2_TheMajorGrid_StandsOnThePowerAlone_AndTheMinorGridOnEveryMultiple()
    {
        int GridLines(GridWhich which) => LogY(Theme.MatplotlibV2, 2, 80,
                ax => ax.WithGrid(g => g with { Visible = true, Axis = GridAxis.Y, Which = which, Color = Colors.Blue })).Drawn()
            .Of<DrawLineCall>().Count(a => a.Color == Colors.Blue);

        Assert.Equal(1, GridLines(GridWhich.Major));
        Assert.Equal(15, GridLines(GridWhich.Minor));
    }

    [Fact]
    public void UnderV2_ALogAxisWhoseMinorTicksAreSwitchedOff_NamesTheMajorsAlone()
    {
        var texts = Texts(LogY(Theme.MatplotlibV2, 2, 80, ax => ax.WithMinorTicks(false)).Drawn()).Select(t => t.Text).ToList();

        Assert.Contains("10¹", texts);
        Assert.DoesNotContain("2×10⁰", texts);
        Assert.DoesNotContain("6×10¹", texts);
    }

    [Fact]
    public void UnderV2_TheBandBelowAnXAxisOfLabelledMinors_IsTheMinorMarkAndPadLess()
    {
        // The X axis title stands below the band the labels take; the layout reserves the band the drawing places them in, so with
        // every label a minor one the title rises by the 1.6 points the labels rise by.
        static double TitleBelowEdge(Figure figure)
        {
            var ctx = figure.Drawn();
            double edge = ctx.Of<DrawLineCall>()
                .Select(a => (a.P1, a.P2))
                .Where(l => Math.Abs(l.Item1.X - l.Item2.X) < 1e-9 && Math.Abs(l.Item2.Y - l.Item1.Y) < 10)
                .Min(l => Math.Min(l.Item1.Y, l.Item2.Y));
            return Texts(ctx).Single(t => t.Text == "x").At.Y - edge;
        }

        // Wide enough that the nine labels stand upright.
        Figure Along(AxisScale scale, double lo, double hi) => Plt.Create().WithSize(1600, 480).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([lo, hi], [0.0, 1.0]).SetXScale(scale).SetXLim(lo, hi).SetXLabel("x")).Build();

        Assert.Equal(TitleBelowEdge(Along(AxisScale.Linear, 0, 1)) - 1.6 * Pt, TitleBelowEdge(Along(AxisScale.Log, 2.1, 2.9)), 9);
    }
}
