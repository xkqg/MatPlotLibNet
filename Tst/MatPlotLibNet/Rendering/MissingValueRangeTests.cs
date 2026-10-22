// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A missing value (NaN) is not there, so it has no place in the auto range; an infinity cannot be placed on an
/// axis either. matplotlib leaves both out of its data limits. Before, a series took its minimum with LINQ
/// <c>Min()</c>, which answers NaN as soon as it meets one; the fold then dropped that minimum, and a lone line with one
/// gap collapsed the axis to the empty range 0..1, drawing its data off the plot.</summary>
public class MissingValueRangeTests
{
    private static IAxesContext Context(AxisScale yScale = AxisScale.Linear)
    {
        var axes = new Axes();
        axes.YAxis.Scale = yScale;
        return new AxesContextAdapter(axes);
    }

    // ---- the series that range over their own values --------------------------------------------------------------

    /// <summary>The line family (a line, a step line, a scatter) ranges through <see cref="XYSeries"/>.</summary>
    public static TheoryData<string> XYKinds { get; } = new("line", "step", "scatter");

    private static XYSeries XY(string kind, double[] y) => kind switch
    {
        "line" => new LineSeries([0.0, 1, 2, 3, 4], y),
        "step" => new StepSeries([0.0, 1, 2, 3, 4], y),
        _ => new ScatterSeries([0.0, 1, 2, 3, 4], y),
    };

    [Theory]
    [MemberData(nameof(XYKinds))]
    public void AnXYSeries_LeavesAMissingValueOutOfItsRange(string kind)
    {
        var range = XY(kind, [2, 5, double.NaN, 3, 4]).ComputeDataRange(Context());

        Assert.Equal(2.0, range.YMin);
        Assert.Equal(5.0, range.YMax);
    }

    [Theory]
    [MemberData(nameof(XYKinds))]
    public void AnXYSeries_LeavesAnInfinityOutOfItsRange(string kind)
    {
        var range = XY(kind, [2, double.PositiveInfinity, 3, double.NegativeInfinity, 4]).ComputeDataRange(Context());

        Assert.Equal(2.0, range.YMin);
        Assert.Equal(4.0, range.YMax);
    }

    [Fact]
    public void AnXYSeries_OnALogAxis_RangesOverItsPositiveDrawableValuesOnly()
    {
        var range = XY("line", [0, 2, double.NaN, double.PositiveInfinity, 20]).ComputeDataRange(Context(AxisScale.Log));

        Assert.Equal(2.0, range.YMin);
        Assert.Equal(20.0, range.YMax);
    }

    [Fact]
    public void AnXYSeries_WithNoDrawableValue_ContributesNothing()
    {
        var range = XY("line", [double.NaN, double.NaN, double.NaN, double.NaN, double.NaN]).ComputeDataRange(Context());

        Assert.Equal(Range1D.Empty, Range1D.Empty.Merge(range.YMin, range.YMax));
    }

    [Fact]
    public void AnAreaToItsBaseline_LeavesAMissingValueOut_AndKeepsItsFloorAtZero()
    {
        var range = new AreaSeries([0.0, 1, 2], [2, double.NaN, 5]).ComputeDataRange(Context());

        Assert.Equal(0.0, range.YMin);
        Assert.Equal(0.0, range.StickyYMin);
        Assert.Equal(5.0, range.YMax);
    }

    [Fact]
    public void AnAreaBetweenTwoLines_LeavesAMissingValueOutOfBoth()
    {
        var range = new AreaSeries([0.0, 1, 2], [4, double.NaN, 6]) { YData2 = [1, 2, double.NaN] }.ComputeDataRange(Context());

        Assert.Equal(1.0, range.YMin);
        Assert.Equal(6.0, range.YMax);
    }

    [Fact]
    public void AStem_LeavesAMissingValueOut_AndKeepsItsBaseline()
    {
        var range = new StemSeries([0.0, 1, 2], [2, double.NaN, 5]).ComputeDataRange(Context());

        Assert.Equal(0.0, range.YMin);
        Assert.Equal(0.0, range.StickyYMin);
        Assert.Equal(5.0, range.YMax);
    }

    [Fact]
    public void ABoxPlot_LeavesAMissingValueOutOfItsRange() =>
        AssertYRange(new BoxSeries([[1, double.NaN, 5]]).ComputeDataRange(Context()), 1, 5);

    [Fact]
    public void AViolin_LeavesAMissingValueOutOfItsRange() =>
        AssertYRange(new ViolinSeries([[1, double.NaN, 5]]).ComputeDataRange(Context()), 1, 5);

    [Fact]
    public void AStripPlot_LeavesAMissingValueOutOfItsRange() =>
        AssertYRange(new StripplotSeries([[1, double.NaN, 5], [2]]).ComputeDataRange(Context()), 1, 5);

    private static void AssertYRange(DataRangeContribution range, double min, double max)
    {
        Assert.Equal(min, range.YMin);
        Assert.Equal(max, range.YMax);
    }

    // ---- a series with nothing drawable contributes nothing on that axis ------------------------------------------

    [Fact]
    public void AnXYSeriesWithNoDrawableX_ContributesNoXRange()
    {
        var range = new LineSeries([double.NaN, double.NaN], [1, 2]).ComputeDataRange(Context());

        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
        Assert.Equal(1.0, range.YMin);
    }

    [Fact]
    public void AnXYSeriesRangedOutsideAnyAxes_ReadsAsLinear()
    {
        var range = new LineSeries([0.0, 1], [-2, 3]).ComputeDataRange(null!);

        Assert.Equal(-2.0, range.YMin);
    }

    [Fact]
    public void AnAreaWithNoDrawableValue_ContributesNoRangeAndNoFloor()
    {
        var range = new AreaSeries([double.NaN, double.NaN], [double.NaN, double.NaN]).ComputeDataRange(Context());

        Assert.Equal(new DataRangeContribution(null, null, null, null), range);
    }

    [Fact]
    public void AStemWithNoDrawableValue_ContributesItsXOnly()
    {
        var range = new StemSeries([0.0, 1], [double.NaN, double.NaN]).ComputeDataRange(Context());

        Assert.Equal(0.0, range.XMin);
        Assert.Null(range.YMin);
        Assert.Null(range.YMax);
        Assert.Null(range.StickyYMin);
    }

    [Fact]
    public void AStemWithNoDrawableX_ContributesItsYOnly()
    {
        var range = new StemSeries([double.NaN, double.NaN], [1, 2]).ComputeDataRange(Context());

        Assert.Null(range.XMin);
        Assert.Null(range.StickyXMax);
        Assert.Equal(2.0, range.YMax);
    }

    [Fact]
    public void ABoxPlotWithNoDrawableSample_ContributesNoYRange()
    {
        var range = new BoxSeries([[double.NaN], []]).ComputeDataRange(Context());

        Assert.Null(range.YMin);
        Assert.Null(range.YMax);
    }

    [Fact]
    public void AViolinWithNoDrawableSample_ContributesNoYRange()
    {
        var range = new ViolinSeries([[double.NaN]]).ComputeDataRange(Context());

        Assert.Null(range.YMin);
        Assert.Null(range.YMax);
    }

    [Fact]
    public void AStripPlotWithNoDrawableSample_KeepsItsDefaultRange() =>
        AssertYRange(new StripplotSeries([[double.NaN]]).ComputeDataRange(Context()), 0, 1);

    // ---- the series whose marks a missing value skips: their range leaves it out by the same rule ------------------

    [Fact]
    public void ABar_LeavesAMissingValueOut_AndKeepsANegativeBar()
    {
        var range = new BarSeries(["a", "b", "c"], [-5, double.NaN, 3]).ComputeDataRange(Context());

        AssertYRange(range, -5, 3);
    }

    [Fact]
    public void ABar_LeavesAnInfinityOut() =>
        AssertYRange(new BarSeries(["a", "b", "c"], [1, double.PositiveInfinity, 3]).ComputeDataRange(Context()), 0, 3);

    [Fact]
    public void ABarWithNoDrawableValue_ContributesNoYRange()
    {
        var range = new BarSeries(["a", "b"], [double.NaN, double.NaN]).ComputeDataRange(Context());

        Assert.Null(range.YMin);
        Assert.Null(range.YMax);
    }

    [Fact]
    public void ABarAtAMissingX_LeavesItOutOfTheXRange()
    {
        var bars = new BarSeries([1.0, double.NaN, 3], [1, 2, 3]);

        var range = bars.ComputeDataRange(Context());

        Assert.Equal(1 - bars.BarWidth / 2, range.XMin);
        Assert.Equal(3 + bars.BarWidth / 2, range.XMax);
    }

    [Fact]
    public void BarsWithNoDrawableX_ContributeNoXRange()
    {
        var range = new BarSeries([double.NaN], [1]).ComputeDataRange(Context());

        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
    }

    [Fact]
    public void ABarOnAMissingFoot_IsLeftOutOfTheRangeAndTheFloor()
    {
        var range = new BarSeries(["a", "b"], [2, 3]) { StackBaseline = [double.NaN, 1] }.ComputeDataRange(Context());

        AssertYRange(range, 1, 4);
        Assert.Equal(1.0, range.StickyYMin);
    }

    [Fact]
    public void BarsOnFeetThatCannotBeDrawn_ContributeNoYRange()
    {
        var range = new BarSeries(["a"], [2]) { StackBaseline = [double.NaN] }.ComputeDataRange(Context());

        Assert.Null(range.YMin);
        Assert.Null(range.StickyYMin);
    }

    [Fact]
    public void StackedBars_LeaveAColumnThatCannotBeDrawnOutOfTheirTop()
    {
        var axes = new Axes { BarMode = BarMode.Stacked };
        var first = axes.Bar(["a", "b"], [1, double.PositiveInfinity]);
        axes.Bar(["a", "b"], [1, 1]);

        AssertYRange(first.ComputeDataRange(new AxesContextAdapter(axes)), 0, 2);
    }

    [Fact]
    public void BarsAtNumericX_KeepTheXLimitsTheAxesSet()
    {
        var axes = new Axes();
        axes.XAxis.Min = -4;
        axes.XAxis.Max = 9;

        var range = new BarSeries([1.0, double.NaN, 3], [1, 2, 3]).ComputeDataRange(new AxesContextAdapter(axes));

        Assert.Equal(-4.0, range.XMin);
        Assert.Equal(9.0, range.XMax);
    }

    /// <summary>A stacked series shorter than the first adds nothing to the columns it does not reach.</summary>
    [Fact]
    public void StackedBars_AShorterSeriesAddsNothingPastItsEnd()
    {
        var axes = new Axes { BarMode = BarMode.Stacked };
        var first = axes.Bar(["a", "b", "c"], [1, 2, 3]);
        axes.Bar(["a"], [5]);

        AssertYRange(first.ComputeDataRange(new AxesContextAdapter(axes)), 0, 6);
    }

    [Fact]
    public void StackedBarsTheAxesDoesNotHold_ContributeNoYRange()
    {
        var range = new BarSeries(["a"], [1]).ComputeDataRange(new AxesContextAdapter(new Axes { BarMode = BarMode.Stacked }));

        Assert.Null(range.YMin);
        Assert.Null(range.YMax);
    }

    [Fact]
    public void AnErrorBarSeries_LeavesAMissingXOutOfItsXRange()
    {
        var range = new ErrorBarSeries([0.0, double.NaN, 2], [1, 2, 3], [0.5, 0.5, 0.5], [0.5, 0.5, 0.5]).ComputeDataRange(Context());

        Assert.Equal(0.0, range.XMin);
        Assert.Equal(2.0, range.XMax);
    }

    [Fact]
    public void AnErrorBarSeries_LeavesAnInfiniteErrorOutOfItsYRange() =>
        AssertYRange(new ErrorBarSeries([0.0, 1, 2], [1, 2, 3], [0.5, 0.5, 0.5], [0.5, double.PositiveInfinity, 0.5]).ComputeDataRange(Context()), 0.5, 3.5);

    [Fact]
    public void AnErrorBarSeries_SpansItsXErrorBars()
    {
        var range = new ErrorBarSeries([1.0, 2], [1, 2], [0, 0], [0, 0]) { XErrorLow = [0.5, double.NaN], XErrorHigh = [0.5, 1] }.ComputeDataRange(Context());

        Assert.Equal(0.5, range.XMin);
        Assert.Equal(3.0, range.XMax);
    }

    [Fact]
    public void AnErrorBarSeriesWithNothingDrawable_ContributesNothing() =>
        Assert.Equal(new DataRangeContribution(null, null, null, null),
            new ErrorBarSeries([double.NaN], [double.NaN], [0], [0]).ComputeDataRange(Context()));

    [Fact]
    public void AStackedArea_LeavesAMissingXOutOfItsXRange()
    {
        var range = new StackedAreaSeries([0.0, double.NaN, 2, 3], [[1, 1, 1, 1]]).ComputeDataRange(Context());

        Assert.Equal(0.0, range.XMin);
        Assert.Equal(3.0, range.XMax);
    }

    /// <summary>An infinite layer took the whole range with it, so every layer was drawn on one line at the centre.</summary>
    [Fact]
    public void AStackedArea_LeavesAnInfinityOutOfItsYRange() =>
        AssertYRange(new StackedAreaSeries([0.0, 1, 2], [[1, double.PositiveInfinity, 1], [2, 2, 2]]).ComputeDataRange(Context()), 0, 3);

    /// <summary>As for a line: an axis with nothing drawable contributes nothing, the other axis still does.</summary>
    [Fact]
    public void AStackedAreaWithNoDrawableX_ContributesNoXRange()
    {
        var range = new StackedAreaSeries([double.NaN, double.NaN], [[1, 1]]).ComputeDataRange(Context());

        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
        AssertYRange(range, 0, 1);
    }

    [Fact]
    public void AStackedAreaWithNoDrawableLayerValue_ContributesNoYRangeAndNoFloor()
    {
        var range = new StackedAreaSeries([0.0, 1], [[double.NaN, double.PositiveInfinity]]) { Baseline = StackedBaseline.Symmetric }.ComputeDataRange(Context());

        Assert.Null(range.YMin);
        Assert.Null(range.YMax);
        Assert.Null(range.StickyYMin);
    }

    [Fact]
    public void AWaterfall_LeavesARunningTotalThatCannotBeDrawnOutOfItsRange() =>
        AssertYRange(new WaterfallSeries(["a", "b", "c"], [10, double.PositiveInfinity, 5]).ComputeDataRange(Context()), 0, 10);

    // ---- every other series ranges over its drawable values, through the same owner -------------------------------

    private const double N = double.NaN;
    private const double Inf = double.PositiveInfinity;

    private static double[,] Grid3(double centre) => new double[,] { { 1, 2, 3 }, { 2, centre, 4 }, { 3, 4, 5 } };

    private static StreamingLineSeries StreamingLine(params double[] y)
    {
        var s = new StreamingLineSeries();
        for (int i = 0; i < y.Length; i++) s.AppendPoint(i, y[i]);
        return s;
    }

    private static StreamingSignalSeries StreamingSignal(params double[] y)
    {
        var s = new StreamingSignalSeries();
        foreach (double v in y) s.AppendSample(v);
        return s;
    }

    private static StreamingCandlestickSeries StreamingCandles(double[] high, double[] low)
    {
        var s = new StreamingCandlestickSeries();
        for (int i = 0; i < high.Length; i++) s.AppendBar(10, high[i], low[i], 12);
        return s;
    }

    /// <summary>A series holding a missing value or an infinity, and its finite twin: the same series with every value
    /// that cannot be drawn taken out (or, where the shape needs the slot, replaced by a value inside the finite range).
    /// The two report the same range.</summary>
    private readonly record struct FinitePartCase(Func<ISeries> WithMissing, Func<ISeries> FiniteTwin);

    private static readonly Dictionary<string, FinitePartCase> FinitePartCases = new()
    {
        ["eventplot"] = new(() => new EventplotSeries([[1, N, 3], [2, Inf]]), () => new EventplotSeries([[1, 3], [2]])),
        // a task bar that cannot be placed is not drawn, so neither of its ends counts
        ["gantt"] = new(() => new GanttSeries(["a", "b"], [0, N], [1, 4]), () => new GanttSeries(["a", "b"], [0, 0], [1, 1])),
        ["rugplot"] = new(() => new RugplotSeries(new double[] { 1, N, 5, Inf }), () => new RugplotSeries(new double[] { 1, 5 })),
        ["barbs"] = new(() => new BarbsSeries(new double[] { 1, N, 3 }, new double[] { 1, 2, Inf }, new double[] { 5, 5, 5 }, new double[] { 0, 0, 0 }),
            () => new BarbsSeries(new double[] { 1, 3 }, new double[] { 1, 2 }, new double[] { 5, 5 }, new double[] { 0, 0 })),
        ["streamplot"] = new(() => new StreamplotSeries([0, N, 2], [0, 1, Inf], Grid3(1), Grid3(1)), () => new StreamplotSeries([0, 1, 2], [0, 0.5, 1], Grid3(1), Grid3(1))),
        ["candlestick"] = new(() => new CandlestickSeries([10, 11], [15, N], [N, 9], [12, 12]), () => new CandlestickSeries([10, 11], [15, 15], [9, 9], [12, 12])),
        ["ohlc-bar"] = new(() => new OhlcBarSeries([10, 11], [15, Inf], [N, 9], [12, 12]), () => new OhlcBarSeries([10, 11], [15, 15], [9, 9], [12, 12])),
        ["contour"] = new(() => new ContourSeries([0, N, 2], [0, 1, Inf], Grid3(1)), () => new ContourSeries([0, 1, 2], [0, 0.5, 1], Grid3(1))),
        ["contourf"] = new(() => new ContourfSeries([0, N, 2], [0, 1, Inf], Grid3(1)), () => new ContourfSeries([0, 1, 2], [0, 0.5, 1], Grid3(1))),
        ["pcolormesh"] = new(() => new PcolormeshSeries(new double[] { 0, N, 2 }, new double[] { 0, 1, 2 }, new double[2, 2]),
            () => new PcolormeshSeries(new double[] { 0, 1, 2 }, new double[] { 0, 1, 2 }, new double[2, 2])),
        ["tricontour"] = new(() => new TricontourSeries(new double[] { 0, 1, N, 0.5 }, new double[] { 0, 0, 1, 1 }, new double[] { 1, 2, 3, 4 }),
            () => new TricontourSeries(new double[] { 0, 1, 0.5, 0.5 }, new double[] { 0, 0, 1, 1 }, new double[] { 1, 2, 3, 4 })),
        ["tripcolor"] = new(() => new TripcolorSeries(new double[] { 0, 1, N, 0.5 }, new double[] { 0, 0, 1, Inf }, new double[] { 1, 2, 3, 4 }),
            () => new TripcolorSeries(new double[] { 0, 1, 0.5, 0.5 }, new double[] { 0, 0, 1, 1 }, new double[] { 1, 2, 3, 4 })),
        ["hexbin"] = new(() => new HexbinSeries([1, N, 3], [1, 2, 3]), () => new HexbinSeries([1, 2, 3], [1, 2, 3])),
        ["histogram2d"] = new(() => new Histogram2DSeries([1, N, 3], [1, 2, -Inf]), () => new Histogram2DSeries([1, 2, 3], [1, 2, 2])),
        ["scatter3d"] = new(() => new Scatter3DSeries(new double[] { 1, N, 3 }, new double[] { 1, 2, 3 }, new double[] { 1, 2, Inf }),
            () => new Scatter3DSeries(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 }, new double[] { 1, 2, 2 })),
        ["line3d"] = new(() => new Line3DSeries(new double[] { 1, N, 3 }, new double[] { 1, 2, 3 }, new double[] { N, 2, 1 }),
            () => new Line3DSeries(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 }, new double[] { 2, 2, 1 })),
        // a regression is fitted over the pairs that can be drawn, as seaborn drops a pair with a missing value
        ["regression"] = new(() => new RegressionSeries([1, 2, N, 4], [1, 2, 3, 5]), () => new RegressionSeries([1, 2, 4], [1, 2, 5])),
        ["residual"] = new(() => new ResidualSeries(new double[] { 1, 2, N, 4, 5 }, new double[] { 1, 2, 3, 5, 4 }),
            () => new ResidualSeries(new double[] { 1, 2, 4, 5 }, new double[] { 1, 2, 5, 4 })),
        ["sparkline"] = new(() => new SparklineSeries([1, N, 3]), () => new SparklineSeries([1, 2, 3])),
        ["signal"] = new(() => new SignalSeries([1, N, 3]), () => new SignalSeries([1, 2, 3])),
        // a sample's missing observation is not there: the estimate is made from the others, as for a box or a violin
        ["ecdf"] = new(() => new EcdfSeries([3, N, 1, Inf]), () => new EcdfSeries([3, 1])),
        ["histogram"] = new(() => new HistogramSeries([1, N, 3, 4]), () => new HistogramSeries([1, 3, 4])),
        ["kde"] = new(() => new KdeSeries([1, N, 3, 4]), () => new KdeSeries([1, 3, 4])),
        // an arrow that cannot be placed (its head here) is not drawn, so neither of its ends counts
        ["quiver"] = new(() => new QuiverSeries([1, 2, 3], [1, 2, 3], [1, N, 1], [0, 0, 0]), () => new QuiverSeries([1, 3], [1, 3], [1, 1], [0, 0])),
        ["quiver3d"] = new(() => new Quiver3DSeries(new double[] { 1, 5 }, new double[] { 1, 5 }, new double[] { 1, 5 }, new double[] { 1, N }, new double[] { 0, 0 }, new double[] { 0, 0 }),
            () => new Quiver3DSeries(new double[] { 1 }, new double[] { 1 }, new double[] { 1 }, new double[] { 1 }, new double[] { 0 }, new double[] { 0 })),
        ["bar3d"] = new(() => new Bar3DSeries(new double[] { 1, N, 3 }, new double[] { 1, 2, 3 }, new double[] { 1, 2, N }),
            () => new Bar3DSeries(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 }, new double[] { 1, 2, 2 })),
        ["planar-bar3d"] = new(() => new PlanarBar3DSeries(new double[] { 1, N, 3 }, new double[] { 1, 2, 3 }, new double[] { 1, 2, -Inf }),
            () => new PlanarBar3DSeries(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 }, new double[] { 1, 2, 2 })),
        // a segment that cannot be placed is not drawn, so neither of its ends counts
        ["broken-bar"] = new(() => new BrokenBarSeries([[new(1, 2), new(N, 1), new(5, Inf)]]), () => new BrokenBarSeries([[new(1, 2)]])),
        ["state-timeline"] = new(() => new StateTimelineSeries([new(0, 1, "a", Colors.Red), new(N, 3, "b", Colors.Blue)]),
            () => new StateTimelineSeries([new(0, 1, "a", Colors.Red)])),
        ["text3d"] = new(() => new Text3DSeries([new(1, 1, 1, "a"), new(N, 2, Inf, "b"), new(3, 3, 3, "c")]),
            () => new Text3DSeries([new(1, 1, 1, "a"), new(2, 2, 2, "b"), new(3, 3, 3, "c")])),
        ["streaming-line"] = new(() => StreamingLine(1, N, 3, Inf), () => StreamingLine(1, 2, 3, 3)),
        ["streaming-signal"] = new(() => StreamingSignal(1, N, 3), () => StreamingSignal(1, 2, 3)),
        ["streaming-candlestick"] = new(() => StreamingCandles([15, N], [N, 9]), () => StreamingCandles([15, 15], [9, 9])),
    };

    public static TheoryData<string> FinitePartKinds { get; } = new(FinitePartCases.Keys);

    [Theory]
    [MemberData(nameof(FinitePartKinds))]
    public void ASeriesWithAMissingValue_RangesOverItsFinitePart(string kind)
    {
        var expected = FinitePartCases[kind].FiniteTwin().ComputeDataRange(Context());
        Assert.DoesNotContain(Ends(expected), v => !double.IsFinite(v));   // the twin itself is finite

        Assert.Equal(expected, FinitePartCases[kind].WithMissing().ComputeDataRange(Context()));
    }

    /// <summary>A series none of whose values can be drawn reports no range on the axis those values span; an axis its
    /// shape fixes (event rows, candle slots, a rug's height, an ECDF's 0..1) keeps what it reports.</summary>
    private readonly record struct AllMissingCase(Func<ISeries> Series, DataRangeContribution Expected);

    private static readonly Dictionary<string, AllMissingCase> AllMissingCases = new()
    {
        ["eventplot"] = new(() => new EventplotSeries([[N]]), new(null, null, -0.5, 0.5)),
        ["gantt"] = new(() => new GanttSeries(["a"], [N], [1]), new(null, null, -0.5, 0.5, StickyYMin: -0.5, StickyYMax: 0.5)),
        ["rugplot"] = new(() => new RugplotSeries(new double[] { N }), new(null, null, 0, 1)),
        ["barbs"] = new(() => new BarbsSeries(new double[] { N }, new double[] { Inf }, new double[] { 5 }, new double[] { 0 }), new(null, null, null, null)),
        ["streamplot"] = new(() => new StreamplotSeries([N, N], [N, N], new double[2, 2], new double[2, 2]), new(null, null, null, null)),
        ["candlestick"] = new(() => new CandlestickSeries([10], [N], [N], [12]), new(0, 1, null, null)),
        ["ohlc-bar"] = new(() => new OhlcBarSeries([10], [N], [N], [12]), new(-0.5, 0.5, null, null, StickyXMin: -0.5, StickyXMax: 0.5)),
        ["contour"] = new(() => new ContourSeries([N, N], [N, N], new double[2, 2]), new(null, null, null, null)),
        ["contourf"] = new(() => new ContourfSeries([N, N], [N, N], new double[2, 2]), new(null, null, null, null)),
        ["pcolormesh"] = new(() => new PcolormeshSeries(new double[] { N, N }, new double[] { N, N }, new double[1, 1]), new(null, null, null, null)),
        ["tricontour"] = new(() => new TricontourSeries(new double[] { N, N, N }, new double[] { N, N, N }, new double[] { 1, 2, 3 }), new(null, null, null, null)),
        ["tripcolor"] = new(() => new TripcolorSeries(new double[] { N, N, N }, new double[] { N, N, N }, new double[] { 1, 2, 3 }), new(null, null, null, null)),
        ["hexbin"] = new(() => new HexbinSeries([N], [N]), new(null, null, null, null)),
        ["histogram2d"] = new(() => new Histogram2DSeries([N], [N]), new(null, null, null, null)),
        ["scatter3d"] = new(() => new Scatter3DSeries(new double[] { N }, new double[] { N }, new double[] { N }), new(null, null, null, null)),
        ["regression"] = new(() => new RegressionSeries([N, 1], [1, N]), new(null, null, null, null)),
        ["residual"] = new(() => new ResidualSeries(new double[] { N, 1 }, new double[] { 1, N }), new(null, null, null, null)),
        ["sparkline"] = new(() => new SparklineSeries([N, N]), new(0, 1, null, null)),
        ["signal"] = new(() => new SignalSeries([N, N]), new(0, 1, null, null)),
        ["ecdf"] = new(() => new EcdfSeries([N]), new(null, null, 0, 1)),
        ["histogram"] = new(() => new HistogramSeries([N]), new(null, null, 0, 0, StickyYMin: 0)),
        ["kde"] = new(() => new KdeSeries([N]), new(null, null, null, null)),
        ["quiver"] = new(() => new QuiverSeries([N], [1], [1], [0]), new(null, null, null, null)),
        ["quiver3d"] = new(() => new Quiver3DSeries(new double[] { 1 }, new double[] { N }, new double[] { 1 }, new double[] { 1 }, new double[] { 0 }, new double[] { 0 }), new(null, null, null, null)),
        ["bar3d"] = new(() => new Bar3DSeries(new double[] { N }, new double[] { N }, new double[] { N }), new(null, null, null, null, StickyZMin: 0)),
        ["planar-bar3d"] = new(() => new PlanarBar3DSeries(new double[] { N }, new double[] { N }, new double[] { N }), new(null, null, null, null, StickyZMin: 0)),
        ["broken-bar"] = new(() => new BrokenBarSeries([[new(N, 1)]]), new(null, null, -0.5, 0.5, StickyYMin: -0.5, StickyYMax: 0.5)),
        ["state-timeline"] = new(() => new StateTimelineSeries([new(N, 1, "a", Colors.Red)]), new(null, null, 0, 1)),
        ["text3d"] = new(() => new Text3DSeries([new(N, N, N, "a")]), new(null, null, null, null)),
        ["streaming-line"] = new(() => StreamingLine(N, N), new(0, 1, null, null)),
        ["streaming-signal"] = new(() => StreamingSignal(N, N), new(0, 1, null, null)),
        ["streaming-candlestick"] = new(() => StreamingCandles([N, N], [N, N]), new(0, 1, null, null)),
    };

    public static TheoryData<string> AllMissingKinds { get; } = new(AllMissingCases.Keys);

    [Theory]
    [MemberData(nameof(AllMissingKinds))]
    public void ASeriesWithNoDrawableValue_ReportsNoRangeOnThatAxis(string kind) =>
        Assert.Equal(AllMissingCases[kind].Expected, AllMissingCases[kind].Series().ComputeDataRange(Context()));

    private static IEnumerable<double> Ends(DataRangeContribution c) =>
        new[] { c.XMin, c.XMax, c.YMin, c.YMax, c.ZMin, c.ZMax }.Where(v => v.HasValue).Select(v => v!.Value);

    // ---- the axes the series are folded into ----------------------------------------------------------------------

    /// <summary>The bump chart's case (W20): one line with a missing rank must not collapse the axis to 0..1.</summary>
    [Fact]
    public void ALineWithAGap_DoesNotCollapseTheAutoRange()
    {
        var axes = new Axes();
        axes.Plot([1.0, 2, 3, 4], [10, 20, double.NaN, 30]);

        var range = new CartesianAxesRenderer(axes, new Rect(80, 60, 700, 400), new SvgRenderContext(), Theme.MatplotlibV2).ComputeDataRanges();

        Assert.InRange(range.YMin, 8.0, 10.0);
        Assert.InRange(range.YMax, 30.0, 32.0);
    }
}
