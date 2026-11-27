// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The offset of matplotlib's <c>ScalarFormatter</c>: when the visible ticks of an axis share enough leading digits, a
/// round offset is taken off every label and written once at the end of the axis ("+1e2"). matplotlib 3.11.2,
/// <c>ScalarFormatter._compute_offset</c> over the ticks in view, with <c>axes.formatter.offset_threshold</c> 2 in the classic
/// style and 4 in the default one ("only use offset if it saves at least _offset_threshold digits"), the text
/// <c>format_data(offset)</c> with a '+' before a positive one. Measured: classic ticks 95 .. 103 read -5 .. 3 under "+1e2";
/// classic 1000 .. 1004 "+1e3"; default 100000 .. 100004 "+1e5"; default 1000 .. 1004 and -1004 .. -1000 none.</summary>
public sealed class TickOffsetTests
{
    private static double[] Range(double from, double to, double step) =>
        [.. Enumerable.Range(0, (int)Math.Round((to - from) / step) + 1).Select(i => from + i * step)];

    [Theory]
    [InlineData(95, 103, 1, 2, 100)]
    [InlineData(95, 103, 1, 4, 0)]
    [InlineData(1000, 1004, 0.5, 2, 1000)]
    [InlineData(1000, 1004, 0.5, 4, 0)]
    [InlineData(100000, 100004, 0.5, 4, 100000)]
    [InlineData(-1004, -1000, 0.5, 4, 0)]
    [InlineData(-1004, -1000, 0.5, 2, -1000)]
    [InlineData(-2, 2, 1, 2, 0)]
    [InlineData(0.5, 0.5, 1, 2, 0)]
    public void TheOffset_IsMatplotlibs(double from, double to, double step, int threshold, double expected)
    {
        var ticks = Range(from, to, step);

        Assert.Equal(expected, ticks.OffsetIn(new Range1D(from, to), threshold));
    }

    /// <summary>Only the ticks in view count: a locator's tick past the end of the range is left out, as
    /// <c>_compute_offset</c> keeps the locations between the view's ends.</summary>
    [Fact]
    public void OnlyTheTicksInViewCount()
    {
        double[] ticks = [94, 96, 98, 100, 102, 104];

        Assert.Equal(100, ticks.OffsetIn(new Range1D(95, 103), 2));
        Assert.Equal(0, ticks.OffsetIn(new Range1D(200, 300), 2));
    }

    [Theory]
    [InlineData(100, "+1e2")]
    [InlineData(1000, "+1e3")]
    [InlineData(1200, "+1.2e3")]
    [InlineData(-1000, "-1e3")]
    [InlineData(5, "+5")]
    public void TheOffsetText_IsMatplotlibsFormatData(double offset, string expected)
    {
        Assert.Equal(expected, offset.OffsetText());
    }

    private static string Svg(Theme theme) => Plt.Create().WithTheme(theme).WithSize(600, 400)
        .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1, 2, 3, 4, 5, 6, 7, 8, 9], [95.0, 96, 99, 103, 101, 97, 98, 100, 102, 99]))
        .ToSvg();

    private static readonly double[] Near1000 = [1000.2, 1001, 1002, 1003, 1003.7];

    /// <summary>Every edge writes its own offset at its end: an X axis below its labels, a Y axis past the primary on the right
    /// above the plot, the top axis above its labels; each once.</summary>
    [Fact]
    public void EveryEdge_WritesItsOffsetOnce()
    {
        string svg = Plt.Create().WithTheme(Theme.MatplotlibClassic).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(Near1000, [0.0, 1, 2, 3, 4])
                .WithAddRightYAxis(right => right.Plot([0.0, 1, 2, 3, 4], [95.0, 96, 99, 103, 101]))
                .WithSecondaryXAxis(top => top.PlotXSecondary([100000.2, 100001, 100002, 100003, 100003.7], [0.0, 1, 2, 3, 4])))
            .ToSvg();

        Assert.Equal(1, svg.CountOf(">+1e3</text>"));
        Assert.Equal(1, svg.CountOf(">+1e2</text>"));
        Assert.Equal(1, svg.CountOf(">+1e5</text>"));
    }

    private static readonly double[] Near1e9 = [1e9 + 0.1, 1e9 + 0.25, 1e9 + 0.4, 1e9 + 0.6, 1e9 + 0.75, 1e9 + 0.9];
    private static readonly double[] Small = [0.0, 1, 2, 3, 4, 5];
    private static readonly AxisTransform TwiceAndFive = AxisTransform.Linear(2, 5);

    /// <summary>A linked axis (matplotlib's <c>secondary_xaxis</c> / <c>secondary_yaxis</c> with <c>functions</c>) is drawn on a
    /// <c>FuncScale</c>, whose formatter is a <c>ScalarFormatter</c> of its own: it works its offset out over its own ticks and its own
    /// view, in its own unit. A primary over 1e9 + 0.1 .. 1e9 + 0.9 under v2 writes "+1e9"; the axis linked by 2x + 5 shows
    /// 2e9 + 5.2 .. 2e9 + 6.8 and writes "+2e9" with labels of a few digits, on the top, on a bottom axis below the plot and on a Y
    /// axis past the primary.</summary>
    [Fact]
    public void ALinkedAxis_WritesTheOffsetOfItsOwnView()
    {
        string top = Plt.Create().WithTheme(Theme.MatplotlibV2).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(Near1e9, Small).WithSecondaryXAxis(s => s.Link(TwiceAndFive))).ToSvg();
        string bottom = Plt.Create().WithTheme(Theme.MatplotlibV2).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(Near1e9, Small).WithAddBottomXAxis(b => b.Link(TwiceAndFive))).ToSvg();
        string right = Plt.Create().WithTheme(Theme.MatplotlibV2).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(Small, Near1e9).WithAddRightYAxis(r => r.Link(TwiceAndFive))).ToSvg();

        foreach (string svg in new[] { top, bottom, right })
        {
            Assert.Equal(1, svg.CountOf(">+1e9</text>"));
            Assert.Equal(1, svg.CountOf(">+2e9</text>"));
            Assert.DoesNotContain(">200000000", svg, StringComparison.Ordinal);
        }
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // An axis with a break shows its whole range in the unit of its ticks; the offset is worked out over that view, not over the range
        // the break compresses it to on the pixels (which ends 20 below the top here and would leave the tick at 1100 out, so 1000 .. 1080
        // would give "+1e3"). Over the view 1000 .. 1100 matplotlib's classic _compute_offset finds no offset worth two digits, so the
        // labels read 1000 .. 1100 and no "+1e3" is written.
        new DrawnToken("AnAxisWithABreak_WorksItsOffsetOutOverTheRangeItShows", () => Plt.Create().WithTheme(Theme.MatplotlibClassic).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1, 2, 3, 4, 5], [1000.0, 1010, 1030, 1070, 1090, 1100])
                .SetYLim(1000, 1100).WithYBreak(1040, 1060)).ToSvg(), ">1100</text>") { Absent = ["+1e3"] },
        // Under the classic theme the Y labels of a line over 95 .. 103 read -5 .. 3 and "+1e2" stands above the plot's top-left corner;
        // under v2, whose threshold is 4, the labels read 95 .. 103 and no offset is written.
        new DrawnToken("TheClassicTheme_WritesTheOffsetOnce_AndTheLabelsLessIt (classic)", () => Svg(Theme.MatplotlibClassic), ">+1e2</text>", ">−5</text>") { Absent = [">103</text>"] },
        new DrawnToken("TheClassicTheme_WritesTheOffsetOnce_AndTheLabelsLessIt (v2)", () => Svg(Theme.MatplotlibV2), ">103</text>") { Absent = ["+1e"] },
        // No offset is written for hidden ticks, for labels a formatter writes, or on a log axis.
        new DrawnToken("HiddenTicks_AFormatter_AndALogAxis_WriteNoOffset (hidden)", () =>
        {
            var hiddenFigure = Plt.Create().WithTheme(Theme.MatplotlibClassic)
                .AddSubPlot(1, 1, 1, ax => ax.Plot(Near1000, [0.0, 1, 2, 3, 4])).Build();
            hiddenFigure.SubPlots[0].XAxis.MajorTicks = hiddenFigure.SubPlots[0].XAxis.MajorTicks with { Visible = false };
            return hiddenFigure.ToSvg();
        }) { Absent = ["+1e"] },
        new DrawnToken("HiddenTicks_AFormatter_AndALogAxis_WriteNoOffset (formatted)", () =>
        {
            var hiddenFigure = Plt.Create().WithTheme(Theme.MatplotlibClassic)
                .AddSubPlot(1, 1, 1, ax => ax.Plot(Near1000, [0.0, 1, 2, 3, 4])).Build();
            hiddenFigure.SubPlots[0].XAxis.MajorTicks = hiddenFigure.SubPlots[0].XAxis.MajorTicks with { Visible = false };
            return Plt.Create().WithTheme(Theme.MatplotlibClassic)
                .AddSubPlot(1, 1, 1, ax => ax.Plot(Near1000, [0.0, 1, 2, 3, 4])
                    .SetXTickFormatter(new MatPlotLibNet.Rendering.TickFormatters.NumericTickFormatter())).ToSvg();
        }) { Absent = ["+1e"] },
        new DrawnToken("HiddenTicks_AFormatter_AndALogAxis_WriteNoOffset (log)", () =>
        {
            var hiddenFigure = Plt.Create().WithTheme(Theme.MatplotlibClassic)
                .AddSubPlot(1, 1, 1, ax => ax.Plot(Near1000, [0.0, 1, 2, 3, 4])).Build();
            hiddenFigure.SubPlots[0].XAxis.MajorTicks = hiddenFigure.SubPlots[0].XAxis.MajorTicks with { Visible = false };
            return Plt.Create().WithTheme(Theme.MatplotlibClassic)
                .AddSubPlot(1, 1, 1, ax => ax.Plot(Near1000, [0.0, 1, 2, 3, 4]).SetXScale(MatPlotLibNet.Models.AxisScale.Log)).ToSvg();
        }) { Absent = ["+1e"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>The library's own themes write no offset; the threshold survives a derived theme and the theme's JSON.</summary>
    [Fact]
    public void TheThreshold_IsTheThemes()
    {
        Assert.Equal(2, Theme.MatplotlibClassic.Looks.TickLabels.OffsetThreshold);
        Assert.Equal(4, Theme.MatplotlibV2.Looks.TickLabels.OffsetThreshold);
        Assert.Null(Theme.Default.Looks.TickLabels.OffsetThreshold);
        Assert.Equal(2, Theme.MatplotlibClassic.Cloned().Looks.TickLabels.OffsetThreshold);
        Assert.Equal(4, Theme.MatplotlibV2.ThroughDto().Looks.TickLabels.OffsetThreshold);
        Assert.DoesNotContain("+1e", Svg(Theme.Default), StringComparison.Ordinal);
    }
}
