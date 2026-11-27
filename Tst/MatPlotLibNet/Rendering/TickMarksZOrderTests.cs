// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The tick marks of a Cartesian axes stand in the z-order where matplotlib draws its axis: <c>axes.axisbelow</c> puts the
/// axis (its grid and its tick marks, <c>Axis.draw</c> draws both) at 2.5 under the classic style (<c>False</c>) and at 1.5 under the
/// default one (<c>'line'</c>); matplotlib 3.11.2, printed with python: <c>ax.xaxis.get_zorder()</c> 2.5 and 1.5, a <c>Line2D</c> 2, a
/// <c>Rectangle</c> 1. So under the classic style a mark that points into the plot is drawn over a line along the edge, and under the
/// default style over a bar and under a line. The tick labels stand outside the plot and are drawn where they always were, before
/// the data, so the band a Y axis' labels take is known before the series are placed. Under the library's own themes a mark is
/// drawn with its label, under every data layer, as before.</summary>
public class TickMarksZOrderTests
{
    private static readonly Rect Plot = new(40, 20, 400, 300);
    private static readonly Color TickInk = Color.FromHex("#0A0B0C");
    private static readonly Color MinorInk = Color.FromHex("#0C0B0A");
    private static readonly Color LineInk = Color.FromHex("#1100EE");
    private static readonly Color BarInk = Color.FromHex("#B0A001");

    /// <summary>Draws the one subplot <paramref name="build"/> makes under <paramref name="theme"/>, its major tick marks in
    /// <see cref="TickInk"/> and its minor ones, switched on, in <see cref="MinorInk"/>.</summary>
    private static RecordingRenderContext Draw(Theme theme, Func<AxesBuilder, AxesBuilder> build)
    {
        var figure = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => build(ax.WithMinorTicks())).Build();
        var axes = figure.SubPlots[0];
        foreach (var axis in new[] { axes.XAxis, axes.YAxis })
        {
            axis.MajorTicks = axis.MajorTicks with { Color = TickInk };
            axis.MinorTicks = axis.MinorTicks with { Color = MinorInk };
        }

        var ctx = axes.Drawn(Plot, theme);
        return ctx;
    }

    private static List<int> Indices<TCall>(RecordingRenderContext ctx, Color ink) where TCall : StrokeCall =>
        [.. ctx.Calls.Select((call, i) => (call, i)).Where(c => c.call is TCall line && line.Color == ink).Select(c => c.i)];

    private static List<int> Marks(RecordingRenderContext ctx) => Indices<DrawLineCall>(ctx, TickInk);
    private static List<int> MinorMarks(RecordingRenderContext ctx) => Indices<DrawLineCall>(ctx, MinorInk);
    private static int Line(RecordingRenderContext ctx) => Indices<DrawLinesCall>(ctx, LineInk) is [var first, ..] ? first : -1;

    private static int Bar(RecordingRenderContext ctx) =>
        ctx.Calls.FindIndex(c => c is DrawRectangleCall k && Equals((object?)k.Fill, BarInk));

    private static int FirstLabel(RecordingRenderContext ctx) => ctx.Calls.FindIndex(c => c is DrawTextCall);

    /// <summary>A line along the bottom of the plot, where the classic style's marks point in.</summary>
    private static AxesBuilder LineAlongTheBottom(AxesBuilder ax) => ax
        .Plot([0.0, 1.0, 2.0, 3.0], [0.0, 0.0, 0.0, 3.0], l => l.Color = LineInk);

    /// <summary>The repro: under the classic style every major and minor mark, on all four edges, is drawn after the line, over it.</summary>
    [Fact]
    public void UnderClassic_EveryTickMarkIsDrawnAfterTheLine()
    {
        var ctx = Draw(Theme.MatplotlibClassic, LineAlongTheBottom);

        int line = Line(ctx);
        var marks = Marks(ctx);
        var minors = MinorMarks(ctx);
        Assert.True(line >= 0 && marks.Count > 0 && minors.Count > 0, $"line {line}, marks {marks.Count}, minor marks {minors.Count}");
        Assert.All(marks, mark => Assert.True(mark > line, $"mark {mark} before the line {line}"));
        Assert.All(minors, mark => Assert.True(mark > line, $"minor mark {mark} before the line {line}"));
    }

    /// <summary>Under the classic style the tick labels are still drawn before the data, so the marks are drawn apart from them, each
    /// once: as many marks as the axes draws with its labels under the library's way of drawing them together.</summary>
    [Fact]
    public void UnderClassic_TheLabelsAreDrawnBeforeTheLine_AndEachMarkOnce()
    {
        var classic = Draw(Theme.MatplotlibClassic, LineAlongTheBottom);

        Assert.True(FirstLabel(classic) >= 0 && FirstLabel(classic) < Line(classic), $"label {FirstLabel(classic)}, line {Line(classic)}");
        // Bottom and top, left and right: every mark the four edges hold, none twice.
        var marks = Marks(classic).Select(i => ((DrawLineCall)classic.Calls[i]).P1).Cast<Point>().ToList();
        Assert.Equal(marks.Count, marks.Distinct().Count());
    }

    /// <summary>Under the default style the axis stands at 1.5: the marks are drawn after a bar (1) and before a line (2).</summary>
    [Fact]
    public void UnderV2_TheTickMarksAreDrawnAfterABar_AndBeforeALine()
    {
        var ctx = Draw(Theme.MatplotlibV2, ax => ax
            .Bar(["a", "b", "c"], [1.0, 3.0, 2.0], b => b.Color = BarInk)
            .Plot([0.0, 1.0, 2.0], [1.0, 2.0, 3.0], l => l.Color = LineInk));

        int bar = Bar(ctx), line = Line(ctx);
        var marks = Marks(ctx);
        Assert.True(bar >= 0 && line > bar && marks.Count > 0, $"bar {bar}, line {line}, marks {marks.Count}");
        Assert.All(marks, mark => Assert.True(mark > bar && mark < line, $"mark {mark}: bar {bar}, line {line}"));
    }

    /// <summary>A series given a z-order above the axis' is drawn over the marks under the classic style.</summary>
    [Fact]
    public void UnderClassic_ALineGivenZOrder3_IsDrawnAfterTheTickMarks()
    {
        var ctx = Draw(Theme.MatplotlibClassic, ax => ax
            .Plot([0.0, 1.0, 2.0, 3.0], [0.0, 0.0, 0.0, 3.0], l => { l.Color = LineInk; l.ZOrder = 3; }));

        int line = Line(ctx);
        Assert.All(Marks(ctx), mark => Assert.True(mark < line, $"mark {mark} after the line {line}"));
    }

    /// <summary>Under the library's own themes a mark is drawn with its label, before the data, in the order it always was: the
    /// first X mark, then its label.</summary>
    [Theory]
    [InlineData("default")]
    [InlineData("dark")]
    public void UnderALibraryTheme_EachMarkIsDrawnWithItsLabel_BeforeTheLine(string name)
    {
        var ctx = Draw(name.AsTheme(), LineAlongTheBottom);

        var marks = Marks(ctx);
        int line = Line(ctx);
        Assert.True(marks.Count > 0 && line > marks[^1], $"marks ..{(marks.Count > 0 ? marks[^1] : -1)}, line {line}");
        Assert.IsType<DrawTextCall>(ctx.Calls[marks[0] + 1]);
    }

    /// <summary>An axes of series that draw no axes (a pie) draws no tick mark under the classic style either.</summary>
    [Fact]
    public void UnderClassic_AnAxisLessAxesDrawsNoTickMark()
    {
        var ctx = Draw(Theme.MatplotlibClassic, ax => ax.Pie([1.0, 2.0, 3.0]));

        Assert.Empty(Marks(ctx));
        Assert.Empty(MinorMarks(ctx));
    }
}
