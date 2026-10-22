// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A waterfall's total bar is a level, not a step: it stands on zero, reaches the level it names and wears
/// <see cref="WaterfallSeries.TotalColor"/>, and the steps after it start from that level. Before, nothing read the
/// total colour: a bar the user meant as the total was one more step, floating from the running total up by its own
/// value (a cookbook waterfall whose last bar is the sum of the months drew that bar from the sum to twice the sum, in
/// the increase colour). The series marks its total bars in <see cref="WaterfallSeries.TotalIndices"/>; a series that
/// marks none draws exactly as it always did.</summary>
public class WaterfallTotalTests
{
    private const double NaN = double.NaN;
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    /// <summary>X 0..10 over 400 px, Y -10..10 over 300 px.</summary>
    private static DataTransform Linear() => new(0, 10, -10, 10, Bounds);

    /// <summary>A log Y axis from 1 (the bottom edge, 300 px) to 100 (the top edge, 0 px).</summary>
    private static DataTransform LogY() =>
        new(0, 10, 0, 2, Bounds, null, null, 0, 10, 1, 100, AxisScale.Linear, AxisScale.Log, 1, 1);

    private static double PixelY(double value) => Linear().DataToPixel(0, value).Y;

    private static double LogPixelY(double value) => LogY().DataToPixel(0, value).Y;

    private static RecordingRenderContext Render(WaterfallSeries series, DataTransform? transform = null)
    {
        var ctx = new RecordingRenderContext();
        new WaterfallSeriesRenderer(new SeriesRenderContext(transform ?? Linear(), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx))).Render(series);
        return ctx;
    }

    private static object? Arg(CallRecord call, string name) => call.Args!.GetType().GetProperty(name)!.GetValue(call.Args);

    private static List<Rect> Rects(RecordingRenderContext ctx) => [.. ctx.OfKind("DrawRectangle").Select(c => (Rect)Arg(c, "rect")!)];

    private static List<Color?> Fills(RecordingRenderContext ctx) => [.. ctx.OfKind("DrawRectangle").Select(c => (Color?)Arg(c, "fill"))];

    private static List<double> ConnectorLevels(RecordingRenderContext ctx) => [.. ctx.OfKind("DrawLine").Select(c => ((Point)Arg(c, "p1")!).Y)];

    /// <summary>The pixel row of the bar's lower edge (the larger Y).</summary>
    private static double Low(Rect r) => Math.Max(r.Y, r.Y + r.Height);

    /// <summary>The pixel row of the bar's upper edge (the smaller Y).</summary>
    private static double High(Rect r) => Math.Min(r.Y, r.Y + r.Height);

    private static WaterfallSeries Series(double[] values, params int[] totals)
    {
        var steps = new WaterfallSeries([.. values.Select((_, i) => $"s{i}")], values);
        if (totals.Length > 0) steps.TotalIndices = totals;
        return steps;
    }

    // ---- the total bar -----------------------------------------------------------------------------------------------

    [Fact]
    public void ATotalBar_StandsOnZeroAndReachesItsLevel_InTheTotalColour()
    {
        var series = Series([4, 3, 7], 2);

        var ctx = Render(series);

        var bars = Rects(ctx);
        Assert.Equal(3, bars.Count);
        Assert.Equal(PixelY(0), Low(bars[2]), 9);
        Assert.Equal(PixelY(7), High(bars[2]), 9);
        Assert.Equal(series.TotalColor, Fills(ctx)[2]);
    }

    [Fact]
    public void TheStepsBeforeATotal_AreDrawnAsSteps_InTheIncreaseAndDecreaseColours()
    {
        var series = Series([4, -1, 3], 2);

        var ctx = Render(series);

        var bars = Rects(ctx);
        Assert.Equal(PixelY(0), Low(bars[0]), 9);
        Assert.Equal(PixelY(4), High(bars[0]), 9);
        Assert.Equal(PixelY(3), Low(bars[1]), 9);
        Assert.Equal(PixelY(4), High(bars[1]), 9);
        Assert.Equal([series.IncreaseColor, series.DecreaseColor, series.TotalColor], Fills(ctx));
    }

    /// <summary>The level a total names is where the steps after it start, whatever the steps before it added up to: the
    /// total is stated, not summed. Here 4 and 3 run to 7, the total says 5, and the step of 2 after it runs from 5 to 7.</summary>
    [Fact]
    public void TheStepsAfterATotal_ContinueFromItsLevel_NotFromTheRunningTotalBeforeIt()
    {
        var series = Series([4, 3, 5, 2], 2);

        var ctx = Render(series);

        var bars = Rects(ctx);
        Assert.Equal(4, bars.Count);
        Assert.Equal(PixelY(5), Low(bars[3]), 9);
        Assert.Equal(PixelY(7), High(bars[3]), 9);
        Assert.Equal(series.IncreaseColor, Fills(ctx)[3]);
    }

    [Fact]
    public void ATotalBelowZero_RunsDownFromZero_InTheTotalColour()
    {
        var series = Series([-3, -2, -5], 2);

        var ctx = Render(series);

        var bars = Rects(ctx);
        Assert.Equal(PixelY(0), High(bars[2]), 9);
        Assert.Equal(PixelY(-5), Low(bars[2]), 9);
        Assert.Equal(series.TotalColor, Fills(ctx)[2]);
    }

    [Fact]
    public void ATotalOfZero_IsAnEmptyBarOnZero_InTheTotalColour()
    {
        var series = Series([4, -4, 0], 2);

        var ctx = Render(series);

        var bars = Rects(ctx);
        Assert.Equal(0.0, bars[2].Height, 9);
        Assert.Equal(PixelY(0), bars[2].Y, 9);
        Assert.Equal(series.TotalColor, Fills(ctx)[2]);
    }

    /// <summary>Totals at 0, 2 and 4. The second states 3 where the steps before it add up to 6, so the step after it runs
    /// from 3 to 2, and the last total stands on zero again.</summary>
    [Fact]
    public void SeveralTotals_EachStandOnZero_AndEachRestartsTheRunningTotal()
    {
        var series = Series([4, 2, 3, -1, 5], 0, 2, 4);

        var ctx = Render(series);

        var bars = Rects(ctx);
        Assert.Equal(5, bars.Count);
        Assert.Equal([series.TotalColor, series.IncreaseColor, series.TotalColor, series.DecreaseColor, series.TotalColor], Fills(ctx));
        Assert.Equal(PixelY(0), Low(bars[0]), 9);
        Assert.Equal(PixelY(4), High(bars[0]), 9);
        Assert.Equal(PixelY(4), Low(bars[1]), 9);
        Assert.Equal(PixelY(6), High(bars[1]), 9);
        Assert.Equal(PixelY(0), Low(bars[2]), 9);
        Assert.Equal(PixelY(3), High(bars[2]), 9);
        Assert.Equal(PixelY(3), High(bars[3]), 9);
        Assert.Equal(PixelY(2), Low(bars[3]), 9);
        Assert.Equal(PixelY(0), Low(bars[4]), 9);
        Assert.Equal(PixelY(5), High(bars[4]), 9);
    }

    // ---- the connectors ----------------------------------------------------------------------------------------------

    /// <summary>A connector leaves each bar at the level the bar ends on, and for a total that is the level it names (5), not
    /// the running total before it (7).</summary>
    [Fact]
    public void ADashedConnector_LeavesEveryBarAtItsEndLevel_ATotalBarsIncluded()
    {
        var ctx = Render(Series([4, 3, 5, 2], 2));

        Assert.Equal([PixelY(4), PixelY(7), PixelY(5)], ConnectorLevels(ctx));
    }

    // ---- nothing marked: as before -----------------------------------------------------------------------------------

    [Fact]
    public void ASeriesThatMarksNoTotal_DrawsExactlyAsOneWithAnEmptyList_AndNeverWearsTheTotalColour()
    {
        var plain = Series([4, 3, 7]);
        var empty = Series([4, 3, 7]);
        empty.TotalIndices = [];

        var drawn = Render(plain);
        var drawnEmpty = Render(empty);

        Assert.DoesNotContain((Color?)plain.TotalColor, Fills(drawn));
        Assert.Equal(Rects(drawn), Rects(drawnEmpty));
        Assert.Equal(Fills(drawn), Fills(drawnEmpty));
        Assert.Equal(ConnectorLevels(drawn), ConnectorLevels(drawnEmpty));
    }

    [Fact]
    public void ALastBarThatLooksLikeATotal_IsAStepUntilTheSeriesSaysItIsATotal()
    {
        var series = Series([4, 3, 7]);

        var ctx = Render(series);

        var bars = Rects(ctx);
        Assert.Equal(PixelY(7), Low(bars[2]), 9);
        Assert.Equal(PixelY(14), High(bars[2]), 9);
        Assert.Equal(series.IncreaseColor, Fills(ctx)[2]);
    }

    // ---- a missing value ---------------------------------------------------------------------------------------------

    /// <summary>A total is a stated level, so it is known whatever the steps before it were: the missing step is left out,
    /// the total stands on zero, and the step after it starts from the total.</summary>
    [Fact]
    public void ATotalAfterAMissingStep_IsKnownAgain_AndTheStepsAfterItAreDrawn()
    {
        var series = Series([4, NaN, 6, 2], 2);

        var ctx = Render(series);

        var bars = Rects(ctx);
        Assert.Equal(3, bars.Count);   // the missing step is the one bar not drawn
        Assert.Equal(PixelY(0), Low(bars[1]), 9);
        Assert.Equal(PixelY(6), High(bars[1]), 9);
        Assert.Equal(series.TotalColor, Fills(ctx)[1]);
        Assert.Equal(PixelY(6), Low(bars[2]), 9);
        Assert.Equal(PixelY(8), High(bars[2]), 9);
    }

    [Fact]
    public void AMissingTotal_IsNotDrawn_AndNeitherIsAnyStepUntilTheNextTotal()
    {
        var series = Series([4, 3, NaN, 2, 5, 1], 2, 4);

        var ctx = Render(series);

        var bars = Rects(ctx);
        Assert.Equal(4, bars.Count);   // steps 0 and 1, the second total, and the step after it; step 3 is unknown
        Assert.Equal(series.TotalColor, Fills(ctx)[2]);
        Assert.Equal(PixelY(5), High(bars[2]), 9);
        Assert.Equal(PixelY(5), Low(bars[3]), 9);
        Assert.Equal(PixelY(6), High(bars[3]), 9);
    }

    [Fact]
    public void ATotalAtInfinity_IsNotDrawn()
    {
        var ctx = Render(Series([4, double.PositiveInfinity], 1));

        Assert.Single(Rects(ctx));
    }

    // ---- a log axis --------------------------------------------------------------------------------------------------

    /// <summary>The total stands on 0, which a log axis cannot place: it is clipped to the edge where the axis begins, as
    /// the first step of a waterfall is.</summary>
    [Fact]
    public void ATotalOnALogAxis_RunsFromTheBottomEdgeToItsLevel()
    {
        var series = Series([10, 5, 15], 2);

        var ctx = Render(series, LogY());

        var bars = Rects(ctx);
        Assert.Equal(3, bars.Count);
        Assert.Equal(300.0, Low(bars[2]), 9);
        Assert.Equal(LogPixelY(15), High(bars[2]), 9);
        Assert.Equal(series.TotalColor, Fills(ctx)[2]);
    }

    // ---- the data range ----------------------------------------------------------------------------------------------

    private static DataRangeContribution Range(WaterfallSeries series) => series.ComputeDataRange(new AxesContextAdapter(new Axes()));

    /// <summary>5 and 3 run to 8, then a total of 2 stands on zero: the range reaches 8 (the running total), not the 10 the
    /// last bar would reach as a step.</summary>
    [Fact]
    public void TheRange_ReadsATotalAsALevel_NotAsAStep()
    {
        var range = Range(Series([5, 3, 2], 2));

        Assert.Equal(0.0, range.YMin);
        Assert.Equal(8.0, range.YMax);
    }

    [Fact]
    public void TheRange_ReachesATotalBelowEveryStep()
    {
        var range = Range(Series([5, 1, -20], 2));

        Assert.Equal(-20.0, range.YMin);
        Assert.Equal(6.0, range.YMax);
    }

    [Fact]
    public void TheRange_ReachesATotalAboveEveryStep_AndKeepsZeroAsTheStickyFloor()
    {
        var range = Range(Series([2, 1, 30], 2));

        Assert.Equal(0.0, range.YMin);
        Assert.Equal(30.0, range.YMax);
        Assert.Equal(0.0, range.StickyYMin);
    }

    [Fact]
    public void TheRange_StepsAfterATotalCountFromItsLevel()
    {
        var range = Range(Series([5, 3, 20, -4], 2));

        Assert.Equal(0.0, range.YMin);
        Assert.Equal(20.0, range.YMax);
    }

    [Fact]
    public void TheRange_LeavesAMissingTotalOutAndKnowsALaterOne()
    {
        var range = Range(Series([4, NaN, 6, 1], 1, 2));

        Assert.Equal(0.0, range.YMin);
        Assert.Equal(7.0, range.YMax);
    }

    /// <summary>The X range is the slots of the steps, 0 to the number of steps, unless the axis carries limits, which win.</summary>
    [Fact]
    public void TheRange_TakesItsXExtentFromTheSlots_UnlessTheAxisHasLimits()
    {
        var series = Series([5, 3, 2], 2);
        var limited = new Axes();
        limited.XAxis.Min = -3;
        limited.XAxis.Max = 9;

        var own = Range(series);
        var set = series.ComputeDataRange(new AxesContextAdapter(limited));

        Assert.Equal(0.0, own.XMin);
        Assert.Equal(3.0, own.XMax);
        Assert.Equal(-3.0, set.XMin);
        Assert.Equal(9.0, set.XMax);
    }

    [Fact]
    public void TheRange_OfASeriesThatMarksNoTotal_IsTheRunningTotal()
    {
        var range = Range(Series([5, 3, 2]));

        Assert.Equal(0.0, range.YMin);
        Assert.Equal(10.0, range.YMax);
    }

    // ---- the property ------------------------------------------------------------------------------------------------

    [Fact]
    public void TotalIndices_AreNullUntilTheSeriesMarksOne()
    {
        Assert.Null(new WaterfallSeries(["a"], [1.0]).TotalIndices);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(40)]
    public void ATotalThatNamesNoStep_IsRefused_WithTheIndexAndTheNumberOfSteps(int index)
    {
        var series = Series([1, 2, 3]);

        var refusal = Assert.Throws<ArgumentOutOfRangeException>(() => series.TotalIndices = [0, index]);

        Assert.Equal("value", refusal.ParamName);
        Assert.Contains($"{index}", refusal.Message);
        Assert.Contains("the series has 3", refusal.Message);
        Assert.Null(series.TotalIndices);   // a refused list is not kept
    }

    [Fact]
    public void TotalIndices_MayBeClearedAgain()
    {
        var series = Series([1, 2, 3], 2);

        series.TotalIndices = null;

        Assert.Null(series.TotalIndices);
        Assert.DoesNotContain((Color?)series.TotalColor, Fills(Render(series)));
    }

    [Fact]
    public void ATotalIndexNamedTwice_IsOneTotal()
    {
        var once = Render(Series([4, 3, 7], 2));
        var twice = Render(Series([4, 3, 7], 2, 2));

        Assert.Equal(Rects(once), Rects(twice));
        Assert.Equal(Fills(once), Fills(twice));
    }

    // ---- JSON --------------------------------------------------------------------------------------------------------

    private static readonly ChartSerializer Serializer = new();

    private static Figure FigureOf(WaterfallSeries series)
    {
        var figure = new Figure();
        figure.AddSubPlot().AddSeries(series);
        return figure;
    }

    [Fact]
    public void TheTotalsTravelThroughJson_AndTheSameBarsAreTotalsAfterwards()
    {
        var series = Series([4, 3, 7, -2, 5], 2, 4);

        var read = Assert.IsType<WaterfallSeries>(Serializer.FromJson(Serializer.ToJson(FigureOf(series))).SubPlots[0].Series.Single());

        Assert.NotNull(read.TotalIndices);
        Assert.Equal([2, 4], read.TotalIndices);
        Assert.Equal(Rects(Render(series)), Rects(Render(read)));
        Assert.Equal(Fills(Render(series)), Fills(Render(read)));
    }

    /// <summary>A figure that marks no total writes no field for it, so every JSON written before the field existed is what
    /// is written now.</summary>
    [Fact]
    public void ASeriesThatMarksNoTotal_WritesNoTotalIndices()
    {
        string json = Serializer.ToJson(FigureOf(Series([4, 3, 7])));

        Assert.DoesNotContain("totalIndices", json);
    }

    [Fact]
    public void ASeriesWithAnEmptyTotalList_WritesNoTotalIndices_ItMarksNone()
    {
        var series = Series([4, 3, 7]);
        series.TotalIndices = [];

        string json = Serializer.ToJson(FigureOf(series));

        Assert.DoesNotContain("totalIndices", json);
    }

    [Fact]
    public void JsonThatMarksATotalOutsideTheSteps_IsRefusedByTheNameOfTheField()
    {
        const string json = """{"width":400,"height":300,"subPlots":[{"series":[{"type":"waterfall","categories":["a","b"],"values":[1,2],"totalIndices":[5]}]}]}""";

        var refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Serializer.FromJson(json));

        Assert.Contains("totalIndices", refusal.Message);
    }
}
