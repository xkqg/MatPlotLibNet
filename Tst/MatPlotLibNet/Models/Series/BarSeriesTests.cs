// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="BarSeries"/> default properties and construction.</summary>
public class BarSeriesTests
{
    /// <summary>Verifies that the constructor stores categories and values.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        string[] cats = ["A", "B"];
        double[] vals = [1, 2];
        var series = new BarSeries(cats, vals);
        Assert.Equal(cats, series.Categories);
        Assert.Equal(vals, series.Values);
    }

    /// <summary>Verifies that Orientation defaults to Vertical.</summary>
    [Fact]
    public void DefaultOrientation_IsVertical()
    {
        var series = new BarSeries(["A"], [1.0]);
        Assert.Equal(BarOrientation.Vertical, series.Orientation);
    }

    /// <summary>Verifies that BarWidth defaults to 0.8.</summary>
    [Fact]
    public void DefaultBarWidth_Is0Point8()
    {
        var series = new BarSeries(["A"], [1.0]);
        Assert.Equal(0.8, series.BarWidth);
    }

    /// <summary>Verifies that Alpha defaults to 1.0.</summary>
    [Fact]
    public void DefaultAlpha_Is1()
    {
        var series = new BarSeries(["A"], [1.0]);
        Assert.Equal(1.0, series.Alpha);
    }

    /// <summary>Verifies that LineWidth defaults to 0.0.</summary>
    [Fact]
    public void DefaultLineWidth_Is0()
    {
        var series = new BarSeries(["A"], [1.0]);
        Assert.Equal(0.0, series.LineWidth);
    }

    /// <summary>Verifies that Align defaults to Center.</summary>
    [Fact]
    public void DefaultAlign_IsCenter()
    {
        var series = new BarSeries(["A"], [1.0]);
        Assert.Equal(BarAlignment.Center, series.Align);
    }

    /// <summary>Verifies that Alpha can be set.</summary>
    [Fact]
    public void Alpha_CanBeSet()
    {
        var series = new BarSeries(["A"], [1.0]);
        series.Alpha = 0.5;
        Assert.Equal(0.5, series.Alpha);
    }

    /// <summary>Verifies that Align can be set.</summary>
    [Fact]
    public void Align_CanBeSet()
    {
        var series = new BarSeries(["A"], [1.0]);
        series.Align = BarAlignment.Edge;
        Assert.Equal(BarAlignment.Edge, series.Align);
    }

    /// <summary>Verifies that LineWidth can be set.</summary>
    [Fact]
    public void LineWidth_CanBeSet()
    {
        var series = new BarSeries(["A"], [1.0]);
        series.LineWidth = 1.5;
        Assert.Equal(1.5, series.LineWidth);
    }

    /// <summary>Verifies that multiple BarSeries can share the same categories (prerequisite for grouped layout).</summary>
    [Fact]
    public void MultipleSeries_ShareCategories()
    {
        string[] cats = ["A", "B", "C"];
        var s1 = new BarSeries(cats, [1, 2, 3]);
        var s2 = new BarSeries(cats, [4, 5, 6]);
        Assert.Same(s1.Categories, s2.Categories);
        Assert.Equal(3, s1.Values.Length);
        Assert.Equal(3, s2.Values.Length);
    }

    /// <summary>A bar lifted onto a <see cref="BarSeries.StackBaseline"/> the user sets is drawn from that baseline up;
    /// the data range covers it, so the bars are not cut off at the top of a range that reached their values only. The
    /// lowest foot is the sticky floor, the way every bar's foot is in matplotlib.</summary>
    [Fact]
    public void AStackBaselineTheUserSets_IsPartOfTheDataRange()
    {
        var series = new BarSeries(["A", "B"], [1.0, 2.0]) { StackBaseline = [10.0, 20.0] };

        var range = series.ComputeDataRange(new GroupedContext());

        Assert.Equal(10, range.YMin);
        Assert.Equal(22, range.YMax);
        Assert.Equal(10, range.StickyYMin);
    }

    /// <summary>A negative bar hangs below its foot: the range reaches down to its end, and up to the highest foot.</summary>
    [Fact]
    public void ANegativeBarOnAStackBaseline_HangsBelowItsFoot_InTheDataRange()
    {
        var series = new BarSeries(["A", "B"], [2.0, -3.0]) { StackBaseline = [5.0, 5.0] };

        var range = series.ComputeDataRange(new GroupedContext());

        Assert.Equal(2, range.YMin);
        Assert.Equal(7, range.YMax);
        Assert.Equal(5, range.StickyYMin);
    }

    /// <summary>Bar i of a categorical series stands in category i's slot, centred, as wide as the series says.</summary>
    [Fact]
    public void ACategoricalBar_StandsInItsCategorysSlot()
    {
        var series = new BarSeries(["A", "B", "C"], [1.0, 2.0, 3.0]);

        Assert.Equal(CategorySlot.Of(2).Bar(0.8, 0, BarAlignment.Center), series.SlotOf(2));
        Assert.Equal(2.5, series.SlotOf(2).Centre);
    }

    /// <summary>An edge-aligned bar starts at its slot's left edge.</summary>
    [Fact]
    public void AnEdgeAlignedBar_StandsFromItsSlotsLeftEdge()
    {
        var series = new BarSeries(["A", "B"], [1.0, 2.0]) { Align = BarAlignment.Edge };

        Assert.Equal(CategorySlot.Of(1).Bar(0.8, 0, BarAlignment.Edge), series.SlotOf(1));
        Assert.Equal(1, series.SlotOf(1).Left);
    }

    /// <summary>A bar series makes room for the bars it draws, read from the same rule the renderer draws by: from the first
    /// bar's left edge to the last bar's right edge, both sticky (no margin pads past them). Centred, that is 0.5 - 0.4 to
    /// n - 0.5 + 0.4; edge-aligned it is 0 to n - 1 + 0.8, where the range used to assume centred bars and cut the first
    /// one.</summary>
    [Theory]
    [InlineData(BarAlignment.Center, 0.1, 2.9)]
    [InlineData(BarAlignment.Edge, 0.0, 2.8)]
    public void ABarSeries_RangesFromItsFirstBarsLeftEdgeToItsLastBarsRightEdge(BarAlignment align, double left, double right)
    {
        var series = new BarSeries(["A", "B", "C"], [1.0, 2.0, 3.0]) { Align = align };

        var range = series.ComputeDataRange(new GroupedContext());

        Assert.Equal(series.SlotOf(0).Left, range.XMin);
        Assert.Equal(series.SlotOf(2).Right, range.XMax);
        Assert.Equal(left, range.XMin!.Value, 12);
        Assert.Equal(right, range.XMax!.Value, 12);
        Assert.Equal(range.XMin, range.StickyXMin);
        Assert.Equal(range.XMax, range.StickyXMax);
    }

    /// <summary>A bar on numeric x positions stands around its x, whatever its alignment, as wide as the series says.</summary>
    [Fact]
    public void ABarOnNumericX_StandsAroundItsX()
    {
        var series = new BarSeries([10.0, 20.0], [1.0, 2.0]) { BarWidth = 4, Align = BarAlignment.Edge };

        Assert.Equal(CategorySlot.Around(20, 4), series.SlotOf(1));
    }

    /// <summary>After a render that groups two bar series side by side, each bar stands where its group put it: half the
    /// group's width wide, stepped aside by its offset, the two touching on the slot's centre.</summary>
    [Fact]
    public void AGroupedBar_StandsWhereItsGroupPutsIt()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["A", "B"], [1.0, 2.0]).Bar(["A", "B"], [3.0, 4.0]))
            .Build();
        figure.ToSvg();
        var bars = figure.SubPlots[0].Series.OfType<BarSeries>().ToArray();
        double barWidth = 0.7 / 2;

        Assert.Equal(CategorySlot.Of(1).Bar(barWidth, -0.5 * barWidth, BarAlignment.Center), bars[0].SlotOf(1));
        Assert.Equal(CategorySlot.Of(1).Bar(barWidth, 0.5 * barWidth, BarAlignment.Center), bars[1].SlotOf(1));
        Assert.Equal(1.5, bars[0].SlotOf(1).Right, 12);
        Assert.Equal(1.5, bars[1].SlotOf(1).Left, 12);
    }

    // ── a horizontal bar series stands its categories along Y ───────────────────────────────────────────────────────

    /// <summary>The range a vertical series reports, on the other axes: its categories' extent along Y and its values
    /// along X, with the sticky edges moved with them. Measured before the fix: the category extent stood on X and the
    /// values on Y whatever the orientation, so three horizontal bars of 10, 20 and 30 in a 400 x 300 figure were drawn
    /// 1107, 2214 and 3321 px wide and 5.87 px high. The zero the bars stand on is sticky on both sides, as matplotlib's
    /// <c>sticky_edges.y = [0]</c> is: a margin stops at it below bars that rise and above bars that all fall.</summary>
    [Theory]
    [InlineData(BarAlignment.Center)]
    [InlineData(BarAlignment.Edge)]
    public void AHorizontalBarSeries_RangesItsCategoriesAlongY_AndItsValuesAlongX(BarAlignment align)
    {
        var vertical = new BarSeries(["A", "B", "C"], [10.0, -20.0, 30.0]) { Align = align };
        var horizontal = new BarSeries(["A", "B", "C"], [10.0, -20.0, 30.0]) { Align = align, Orientation = BarOrientation.Horizontal };

        var v = vertical.ComputeDataRange(new GroupedContext());
        var h = horizontal.ComputeDataRange(new GroupedContext());

        Assert.Equal(-20, h.XMin);
        Assert.Equal(30, h.XMax);
        Assert.Equal(0, h.StickyXMin);
        Assert.Equal(0, h.StickyXMax);
        Assert.Equal(horizontal.SlotOf(0).Left, h.YMin);
        Assert.Equal(horizontal.SlotOf(2).Right, h.YMax);
        Assert.Equal(h.YMin, h.StickyYMin);
        Assert.Equal(h.YMax, h.StickyYMax);
        Assert.Equal(v with { XMin = v.YMin, XMax = v.YMax, YMin = v.XMin, YMax = v.XMax,
                              StickyXMin = v.StickyYMin, StickyXMax = v.StickyYMax, StickyYMin = v.StickyXMin, StickyYMax = v.StickyXMax }, h);
    }

    /// <summary>Bars lifted onto a <see cref="BarSeries.StackBaseline"/> run along X when they lie down: from the lowest foot
    /// (the sticky floor) to the furthest end, the categories along Y.</summary>
    [Fact]
    public void AHorizontalBarOnAStackBaseline_RangesItsFeetAndEndsAlongX()
    {
        var series = new BarSeries(["A", "B"], [1.0, 2.0]) { StackBaseline = [10.0, 20.0], Orientation = BarOrientation.Horizontal };

        var range = series.ComputeDataRange(new GroupedContext());

        Assert.Equal(10, range.XMin);
        Assert.Equal(22, range.XMax);
        Assert.Equal(10, range.StickyXMin);
        Assert.Equal(series.SlotOf(0).Left, range.YMin);
        Assert.Equal(series.SlotOf(1).Right, range.YMax);
    }

    /// <summary>Stacked horizontal bars reach as far along X as the sum of the stack in a category.</summary>
    [Fact]
    public void StackedHorizontalBars_ReachTheSumOfTheirStackAlongX()
    {
        var lower = new BarSeries(["A", "B"], [1.0, 2.0]) { Orientation = BarOrientation.Horizontal };
        var upper = new BarSeries(["A", "B"], [3.0, 5.0]) { Orientation = BarOrientation.Horizontal };

        var range = lower.ComputeDataRange(new StackedContext([lower, upper]));

        Assert.Equal(0, range.XMin);
        Assert.Equal(7, range.XMax);
        Assert.Equal(0, range.StickyXMin);
        Assert.Equal(lower.SlotOf(0).Left, range.YMin);
    }

    /// <summary>Horizontal bars on numeric positions stand around those positions along Y.</summary>
    [Fact]
    public void HorizontalBarsOnNumericPositions_StandAroundThemAlongY()
    {
        var series = new BarSeries([10.0, 20.0], [1.0, 2.0]) { BarWidth = 4, Orientation = BarOrientation.Horizontal };

        var range = series.ComputeDataRange(new GroupedContext());

        Assert.Equal(8, range.YMin);
        Assert.Equal(22, range.YMax);
        Assert.Equal(0, range.XMin);
        Assert.Equal(2, range.XMax);
    }

    /// <summary>A limit set on the axis that carries the categories is that axis' limit: Y for horizontal bars, X for
    /// vertical ones. The value axis' limit does not move the category extent.</summary>
    [Theory]
    [InlineData(BarOrientation.Vertical)]
    [InlineData(BarOrientation.Horizontal)]
    public void TheCategoryExtent_TakesTheLimitsOfTheAxisThatCarriesTheCategories(BarOrientation orientation)
    {
        var series = new BarSeries(["A", "B", "C"], [1.0, 2.0, 3.0]) { Orientation = orientation };
        bool vertical = orientation == BarOrientation.Vertical;
        var limits = new LimitedContext(xMin: vertical ? -1 : -100, xMax: vertical ? 5 : 100, yMin: vertical ? -100 : -1, yMax: vertical ? 100 : 5);

        var range = series.ComputeDataRange(limits);

        Assert.Equal(-1, vertical ? range.XMin : range.YMin);
        Assert.Equal(5, vertical ? range.XMax : range.YMax);
        Assert.Equal(3, vertical ? range.YMax : range.XMax);
    }

    private sealed class GroupedContext : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }

    private sealed class StackedContext(IReadOnlyList<ISeries> series) : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Stacked;
        public IReadOnlyList<ISeries> AllSeries => series;
    }

    private sealed class LimitedContext(double xMin, double xMax, double yMin, double yMax) : IAxesContext
    {
        public double? XAxisMin => xMin;
        public double? XAxisMax => xMax;
        public double? YAxisMin => yMin;
        public double? YAxisMax => yMax;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }
}
