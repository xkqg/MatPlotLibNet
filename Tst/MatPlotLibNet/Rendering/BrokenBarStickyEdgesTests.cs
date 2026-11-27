// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A broken-bar chart registers no sticky edge, and a Gantt chart registers its bars' starts and nothing else. matplotlib
/// 3.11.2, <c>Axes.broken_barh</c> builds a <c>PolyCollection</c> and sets no sticky edge (printed: <c>c.sticky_edges</c> is <c>x: [],
/// y: []</c>), so the margin pads both axes: <c>broken_barh([(1, 2), (5, 1)], (0, 1))</c> has <c>xlim</c> (0.75, 6.25) and <c>ylim</c>
/// (-0.05, 1.05), and two rows of 0.8 (<c>(-0.4, 0.8)</c> and <c>(0.6, 0.8)</c>) <c>xlim</c> (-0.3, 6.3), <c>ylim</c> (-0.49, 1.49) in
/// the default style, (0, 6) and (-0.4, 1.4) in the classic one, which pads nothing and rounds out. A Gantt chart is a row of
/// <c>barh(i, d, left=s, height=0.6)</c>: <c>Axes.bar</c> registers each bar's foot (<c>sticky_edges.x</c> is <c>[11]</c> for the bar
/// at 11), so the left end stops at the first start and the rows are padded: <c>xlim</c> (0, 13.65) and <c>ylim</c> (-0.48, 3.48) in
/// the default style, (0, 14) and (-0.5, 3.5) in the classic one. The library registered the extent of the bars on both axes as sticky,
/// so the axes stopped at the bars with no room beside them, and took the rows' slots (-0.5 to the last row + 0.5) for the extent of the
/// bars, which stand a bar's height tall.</summary>
public sealed class BrokenBarStickyEdgesTests
{
    private static DataRange Of(Theme theme, ISeries series)
    {
        var axes = new Axes();
        axes.AddSeries(series);
        return axes.ComputeDataRanges(theme);
    }

    private static BrokenBarSeries TwoRows() => new([[new(1, 2), new(5, 1)], [new(0, 3)]]) { BarHeight = 0.8 };

    /// <summary>Broken bars pad both axes by the margin in the default style; the classic style pads nothing and rounds out; the library's
    /// own theme pads the segments too and widens the rows to its nice bounds, by its own rule for the free ends of an axis, which only a
    /// sticky edge held back (the rows' -0.4 .. 1.4 to -0.5 .. 1.5). A bar of one row is padded like matplotlib's:
    /// <c>broken_barh([(1, 2), (5, 1)], (0, 1))</c> pads x to (0.75, 6.25) and the bar's own extent, 1, by 0.05 on each side; the library's
    /// row stands centred on its index, so a bar of height 1 spans -0.5 .. 0.5 and is padded to -0.55 .. 0.55.</summary>
    public static TheoryData<Theme, BrokenBarSeries, Range1D, Range1D> BrokenBarsAndTheirRanges => new()
    {
        { Theme.MatplotlibV2, TwoRows(), new(-0.3, 6.3), new(-0.49, 1.49) },
        { Theme.MatplotlibV2, new BrokenBarSeries([[new(1, 2), new(5, 1)]]) { BarHeight = 1 }, new(0.75, 6.25), new(-0.55, 0.55) },
        { Theme.MatplotlibClassic, TwoRows(), new(0, 6), new(-0.4, 1.4) },
        { Theme.Default, TwoRows(), new(-0.3, 6.3), new(-0.5, 1.5) },
    };

    [Theory]
    [MemberData(nameof(BrokenBarsAndTheirRanges))]
    public void BrokenBars_ArePaddedAndRoundedOutAsTheThemeSays(Theme theme, BrokenBarSeries series, Range1D x, Range1D y)
    {
        var range = Of(theme, series);

        x.AssertCloseTo(new Range1D(range.XMin, range.XMax), 1e-6);
        y.AssertCloseTo(new Range1D(range.YMin, range.YMax), 1e-6);
    }

    [Fact]
    public void BrokenBars_RegisterNoStickyEdge()
    {
        var contribution = TwoRows().ComputeDataRange(new AxesContextAdapter(new Axes()));

        Assert.Null(contribution.StickyXMin);
        Assert.Null(contribution.StickyXMax);
        Assert.Null(contribution.StickyYMin);
        Assert.Null(contribution.StickyYMax);
    }

    private static GanttSeries FourTasks() => new(["Design", "Build", "Test", "Ship"], [0, 3, 8, 11], [3, 8, 11, 13]);

    [Theory]
    [InlineData("v2", 0, 13.65, 0.02, 3.98)] // the rows stand in the category slots the task names are ticked on, half a slot past matplotlib's row numbers
    [InlineData("classic", 0, 14, 0, 4)] // matplotlib's -0.5 .. 3.5, half a slot up: named rows still round out
    [InlineData("default", 0, 13.65, 0, 4)] // the library's own theme widens the rows of a Gantt to its nice bounds
    public void AGantt_StopsAtItsFirstStart_AndPadsOrRoundsItsRowsAsTheThemeDoes(string theme, double xMin, double xMax, double yMin, double yMax)
    {
        var range = Of(theme.AsTheme(), FourTasks());

        new Range1D(xMin, xMax).AssertCloseTo(new Range1D(range.XMin, range.XMax), 1e-6);
        new Range1D(yMin, yMax).AssertCloseTo(new Range1D(range.YMin, range.YMax), 1e-6);
    }

    [Fact]
    public void AGanttOfBarsThatFall_StopsAtItsLastStart()
    {
        // barh(0, -3, left=5): the foot is 5, the bar stands from 2 to 5, so the right end stops at 5 and the left is padded.
        var range = Of(Theme.MatplotlibV2, new GanttSeries(["a"], [5], [2]));

        new Range1D(1.85, 5).AssertCloseTo(new Range1D(range.XMin, range.XMax), 1e-6);
    }

    [Fact]
    public void AGantt_RegistersTheLowestAndTheHighestStart_AndNoEdgeOnTheRows()
    {
        var contribution = FourTasks().ComputeDataRange(new AxesContextAdapter(new Axes()));

        Assert.Equal(0, contribution.StickyXMin);
        Assert.Equal(11, contribution.StickyXMax);
        Assert.Null(contribution.StickyYMin);
        Assert.Null(contribution.StickyYMax);
    }

    [Fact]
    public void AGantt_WithLimitsSetOnItsRowAxis_ContributesThemAsItsRows()
    {
        var axes = new Axes();
        axes.YAxis.Min = 1;
        axes.YAxis.Max = 2;

        var contribution = FourTasks().ComputeDataRange(new AxesContextAdapter(axes));

        Assert.Equal(1, contribution.YMin);
        Assert.Equal(2, contribution.YMax);
    }

    [Fact]
    public void AGanttWithNoBarThatCanBeDrawn_RegistersNoEdgeOnX()
    {
        var contribution = new GanttSeries(["a"], [double.NaN], [1]).ComputeDataRange(new AxesContextAdapter(new Axes()));

        Assert.Null(contribution.StickyXMin);
        Assert.Null(contribution.StickyXMax);
    }
}
