// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A bar registers its foot as a sticky edge and nothing else. matplotlib 3.11.2, <c>Axes.bar</c>: <c>if orientation ==
/// 'vertical': r.sticky_edges.y.append(b) else: r.sticky_edges.x.append(l)</c> (printed: <c>p.sticky_edges</c> is <c>x: [], y: [0]</c>
/// for a vertical bar, <c>x: [0], y: []</c> for a horizontal one), so the margin pads the axis along the categories and stops at the
/// foot along the values. Measured, default style: <c>bar([0, 1, 2], [3, 5.1, 2])</c> has <c>xlim</c> (-0.54, 2.54) and <c>ylim</c> (0,
/// 5.355); <c>barh</c> the same turned; <c>bar(..., [3, -5.1, 2])</c> <c>ylim</c> (-5.505, 3.405); <c>bar(..., bottom=[1, 1, 1])</c>
/// <c>ylim</c> (1, 6.355). The classic style, whose margins are 0 and which rounds out to the ticks: <c>xlim</c> (-0.5, 2.5), <c>ylim</c>
/// (0, 6). The library registered the outer edges of the bars as sticky on the category axis too, so the axis stopped at the bars. A bar
/// on named categories is a slot of width 1 centred on i + 0.5 (its bars span 0.1 .. 2.9 where matplotlib's, centred on 0, 1 and 2, span
/// -0.4 .. 2.4): the same ranges shifted half a category, and an axis of names rounds nothing out (matplotlib's category locator).</summary>
public sealed class BarStickyEdgesTests
{
    private static DataRange Of(Theme theme, BarSeries series)
    {
        var axes = new Axes();
        axes.AddSeries(series);
        return axes.ComputeDataRanges(theme);
    }

    /// <summary>The categories are padded by the margin in the default style (on numbers and on names, upright and lying, on a bottom too)
    /// and rounded out to the ticks in the classic one, where an axis of names keeps its range; the values stop at the lowest foot.</summary>
    public static TheoryData<Theme, BarSeries, Range1D, Range1D> BarsAndTheirRanges => new()
    {
        { Theme.MatplotlibV2, new BarSeries([0.0, 1.0, 2.0], [3.0, 5.1, 2.0]), new(-0.54, 2.54), new(0, 5.355) },
        { Theme.MatplotlibV2, new BarSeries([0.0, 1.0, 2.0], [3.0, 5.1, 2.0]) { Orientation = BarOrientation.Horizontal }, new(0, 5.355), new(-0.54, 2.54) },
        { Theme.MatplotlibV2, new BarSeries([0.0, 1.0, 2.0], [3.0, 5.1, 2.0]) { StackBaseline = [1.0, 1.0, 1.0] }, new(-0.54, 2.54), new(1, 6.355) },
        { Theme.MatplotlibV2, new BarSeries(["a", "b", "c"], [3.0, 5.1, 2.0]), new(-0.04, 3.04), new(0, 5.355) },
        { Theme.MatplotlibClassic, new BarSeries(["a", "b", "c"], [3.0, 5.1, 2.0]), new(0.1, 2.9), new(0, 6) },
        { Theme.MatplotlibClassic, new BarSeries([0.0, 1.0, 2.0], [3.0, 5.1, 2.0]), new(-0.5, 2.5), new(0, 6) },
    };

    [Theory]
    [MemberData(nameof(BarsAndTheirRanges))]
    public void ABar_PadsOrRoundsOutItsCategories_AndStopsAtItsFoot(Theme theme, BarSeries series, Range1D x, Range1D y)
    {
        var range = Of(theme, series);

        x.AssertCloseTo(new Range1D(range.XMin, range.XMax), 1e-6);
        y.AssertCloseTo(new Range1D(range.YMin, range.YMax), 1e-6);
    }

    [Fact]
    public void V2_BarsThatFall_AreNotClampedAtTheirFootWhereTheFootIsInsideTheRange()
    {
        var range = Of(Theme.MatplotlibV2, new BarSeries([0.0, 1.0, 2.0], [3.0, -5.1, 2.0]));

        new Range1D(-5.505, 3.405).AssertCloseTo(new Range1D(range.YMin, range.YMax), 1e-6);
    }

    [Fact]
    public void V2_BarsAlignedToTheirEdge_AreAlsoPaddedAlongTheCategories()
    {
        var range = Of(Theme.MatplotlibV2, new BarSeries(["a", "b", "c"], [3.0, 5.1, 2.0]) { Align = BarAlignment.Edge });

        // The bars span 0 .. 2.8, each from its slot's left edge; matplotlib pads 5 % of 2.8 each side.
        new Range1D(-0.14, 2.94).AssertCloseTo(new Range1D(range.XMin, range.XMax), 1e-6);
    }

    [Fact]
    public void TheLibrarysOwnThemes_PadTheCategoriesByTheirMargin_TooBecauseTheLibrarysEdgesWereNotMatplotlibs()
    {
        // The comment on the range said it matches matplotlib's DataLim; the sticky edges never were matplotlib's.
        var range = Of(Theme.Default, new BarSeries([0.0, 1.0, 2.0], [3.0, 5.1, 2.0]));

        Assert.True(range.XMin < -0.4 - 1e-9, $"X starts at {range.XMin}");
        Assert.True(range.XMax > 2.4 + 1e-9, $"X ends at {range.XMax}");
    }

    // ── a histogram is bars: the same foot, no other sticky edge ────────────────────────────────────────────────

    /// <summary><c>Axes.hist</c> draws its bars through <c>bar</c> and keeps only the foot sticky (printed: <c>sticky_edges</c> <c>x: []</c>,
    /// <c>y: [0]</c>), so the margin pads the data's extent along X: <c>hist([1, 2, 2, 3, 3, 3, 4, 5.5], bins=5)</c> has <c>xlim</c> (0.775,
    /// 5.725) and <c>ylim</c> (0, 3.15) in the default style, (1, 5.5) and (0, 3) in the classic one. The library held X to the data.</summary>
    [Theory]
    [InlineData("v2", 0.775, 5.725, 3.15)]
    [InlineData("classic", 1, 5.5, 3)]
    public void AHistogram_PadsItsExtentByTheMarginAndStopsAtItsFoot_OrKeepsItRoundedOutToTheTicks(string theme, double xMin, double xMax, double yMax)
    {
        var axes = new Axes();
        axes.AddSeries(new HistogramSeries([1.0, 2, 2, 3, 3, 3, 4, 5.5]) { Bins = 5 });

        var range = axes.ComputeDataRanges(theme.AsTheme());

        new Range1D(xMin, xMax).AssertCloseTo(new Range1D(range.XMin, range.XMax), 1e-6);
        new Range1D(0, yMax).AssertCloseTo(new Range1D(range.YMin, range.YMax), 1e-6);
    }

    [Fact]
    public void ABar_RegistersItsFootAsTheOnlyStickyEdge()
    {
        var upright = new BarSeries(["a", "b"], [1.0, 2.0]).ComputeDataRange(new TestAxesContext());
        var lying = new BarSeries(["a", "b"], [1.0, 2.0]) { Orientation = BarOrientation.Horizontal }.ComputeDataRange(new TestAxesContext());

        Assert.Null(upright.StickyXMin);
        Assert.Null(upright.StickyXMax);
        Assert.Equal(0, upright.StickyYMin);
        Assert.Equal(0, upright.StickyYMax);
        Assert.Null(lying.StickyYMin);
        Assert.Null(lying.StickyYMax);
        Assert.Equal(0, lying.StickyXMin);
        Assert.Equal(0, lying.StickyXMax);
    }
}
