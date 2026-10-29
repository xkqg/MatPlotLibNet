// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one owner of "the colour of series i": which slot a series holds, what the theme hands out at that
/// slot, and which colour wins when the series names its own. Every site that draws a series or its legend swatch
/// asks this owner, so the line, the swatch and (later) an end label cannot disagree.</summary>
public class SeriesSlotTests
{
    private static readonly PropCycler RedGreenBlue =
        new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green, Colors.Blue).Build();

    private static readonly Theme CyclerTheme =
        Theme.CreateFrom(Theme.Default).WithPropCycler(RedGreenBlue).Build();

    // ── Theme: what a slot is worth ────────────────────────────────────────────

    [Fact]
    public void WithoutACycler_theSlotTakesTheCycleColour_andCarriesNoCycledProperties()
    {
        var slot = Theme.Default.SeriesSlot(1);

        Assert.Equal(Theme.Default.CycleColors[1], slot.CycleColor);
        Assert.Null(slot.CycledProperties);
    }

    [Fact]
    public void WithoutACycler_theCycleColourWrapsAround()
    {
        int n = Theme.Default.CycleColors.Length;

        Assert.Equal(Theme.Default.CycleColors[2], Theme.Default.SeriesSlot(n + 2).CycleColor);
    }

    [Fact]
    public void TheCyclerBeatsTheCycleColour_andHandsOverItsWholeSet()
    {
        var slot = CyclerTheme.SeriesSlot(1);

        Assert.Equal(Colors.Green, slot.CycleColor);
        Assert.Equal(RedGreenBlue[1], slot.CycledProperties);
    }

    // ── Which colour wins for a series ─────────────────────────────────────────

    [Fact]
    public void AnExplicitColourWins_overTheSlot()
    {
        var line = new LineSeries([0.0, 1], [0.0, 1]) { Color = Colors.Blue };

        Assert.Equal(Colors.Blue, CyclerTheme.SeriesSlot(0).ColorOf(line));
    }

    [Fact]
    public void AnUnsetColourTakesTheSlot()
    {
        var line = new LineSeries([0.0, 1], [0.0, 1]);

        Assert.Equal(Colors.Red, CyclerTheme.SeriesSlot(0).ColorOf(line));
    }

    [Fact]
    public void ASeriesWithoutAColourOfItsOwnTakesTheSlot()
    {
        var heatmap = new HeatmapSeries(new double[,] { { 1, 2 }, { 3, 4 } });
        Assert.False(typeof(IHasColor).IsAssignableFrom(heatmap.GetType()));

        Assert.Equal(Colors.Green, CyclerTheme.SeriesSlot(1).ColorOf(heatmap));
    }

    // ── A violin: the colour it names, else its theme's violin colour, else the slot ──

    private static readonly Color ThemeViolinBody = Color.FromHex("#112233");

    private static readonly Theme ViolinTheme = new("violin colours", Theme.Default) { ViolinBodyColor = ThemeViolinBody };

    [Fact]
    public void AViolin_TakesTheThemesViolinColour_OverTheSlot()
    {
        var violin = new ViolinSeries([[1.0, 2, 3]]);

        Assert.Equal(ThemeViolinBody, ViolinTheme.SeriesSlot(2).ColorOf(violin));
    }

    [Fact]
    public void AViolinsExplicitColour_BeatsTheThemesViolinColour()
    {
        var violin = new ViolinSeries([[1.0, 2, 3]]) { Color = Colors.Blue };

        Assert.Equal(Colors.Blue, ViolinTheme.SeriesSlot(2).ColorOf(violin));
    }

    [Fact]
    public void AViolin_OfAThemeWithoutViolinColours_TakesTheSlot()
    {
        var violin = new ViolinSeries([[1.0, 2, 3]]);

        Assert.Equal(Theme.Default.CycleColors[2], Theme.Default.SeriesSlot(2).ColorOf(violin));
    }

    [Fact]
    public void TheThemesViolinColour_IsForViolinsOnly()
    {
        var line = new LineSeries([0.0, 1], [0.0, 1]);

        Assert.Equal(ViolinTheme.CycleColors[2], ViolinTheme.SeriesSlot(2).ColorOf(line));
    }

    private static readonly Color ThemeViolinStats = Color.FromHex("#445566");

    private static readonly Theme ViolinLinesTheme =
        new("violin lines", Theme.Default) { ViolinBodyColor = ThemeViolinBody, ViolinStatsColor = ThemeViolinStats };

    [Fact]
    public void TheLinesOverAViolin_TakeTheThemesLineColour_OverTheSlot() =>
        Assert.Equal(ThemeViolinStats, ViolinLinesTheme.SeriesSlot(2).StatsColorOf(new ViolinSeries([[1.0, 2, 3]])));

    [Fact]
    public void TheLinesOverAViolin_TakeTheColourTheViolinNames_OverTheThemes() =>
        Assert.Equal(Colors.Blue, ViolinLinesTheme.SeriesSlot(2).StatsColorOf(new ViolinSeries([[1.0, 2, 3]]) { Color = Colors.Blue }));

    [Fact]
    public void TheLinesOverAViolin_OfAThemeWithoutLineColours_TakeTheSlot() =>
        Assert.Equal(Theme.Default.CycleColors[2], Theme.Default.SeriesSlot(2).StatsColorOf(new ViolinSeries([[1.0, 2, 3]])));

    /// <summary>The classic style gives every violin a yellow body and red lines, whatever its slot.</summary>
    [Fact]
    public void AClassicSlot_CarriesTheThemesViolinColours()
    {
        var slot = Theme.MatplotlibClassic.SeriesSlot(3);

        Assert.Equal(Color.FromHex("#BFBF00"), slot.ViolinBody);
        Assert.Equal(Color.FromHex("#FF0000"), slot.ViolinStats);
    }

    /// <summary>A slot built from a cycle colour already chosen (a renderer is handed its slot in parts) carries the same
    /// theme colours as one the theme hands out, so the renderer and the legend cannot read different slots.</summary>
    [Fact]
    public void ASlotMadeFromChosenParts_CarriesTheThemesViolinColours_LikeTheSlotTheThemeHandsOut()
    {
        var handedOut = ViolinLinesTheme.SeriesSlot(2);

        Assert.Equal(handedOut, ViolinLinesTheme.ThemedSlot(handedOut.CycleColor, handedOut.CycledProperties));
        Assert.Equal(ThemeViolinBody, ViolinLinesTheme.ThemedSlot(Colors.Red, null).ViolinBody);
        Assert.Equal(Colors.Red, ViolinLinesTheme.ThemedSlot(Colors.Red, null).CycleColor);
    }

    // ── How a series is stroked: the cycler's dash, width and marker ───────────

    private static readonly PropCycler Styled = new PropCyclerBuilder()
        .WithColors(Colors.Red, Colors.Green)
        .WithLineStyles(LineStyle.Solid, LineStyle.Dashed)
        .WithMarkerStyles(MarkerStyle.None, MarkerStyle.Square)
        .WithLineWidths(1.5, 3.0)
        .Build();

    private static readonly Theme StyledTheme = Theme.CreateFrom(Theme.Default).WithPropCycler(Styled).Build();

    [Fact]
    public void ALineStyleAtRest_TakesTheSlotsDashPattern()
    {
        Assert.Equal(LineStyle.Dashed, StyledTheme.SeriesSlot(1).LineStyleOf(LineStyle.Solid));
        Assert.Equal(LineStyle.Solid, StyledTheme.SeriesSlot(0).LineStyleOf(LineStyle.Solid));
    }

    [Fact]
    public void ALineStyleTheSeriesNames_Wins()
    {
        Assert.Equal(LineStyle.Dotted, StyledTheme.SeriesSlot(1).LineStyleOf(LineStyle.Dotted));
        Assert.Equal(LineStyle.Dashed, StyledTheme.SeriesSlot(0).LineStyleOf(LineStyle.Dashed));
    }

    [Fact]
    public void WithoutACycler_TheLineStyleStaysWhatTheSeriesHolds()
    {
        Assert.Equal(LineStyle.Solid, Theme.Default.SeriesSlot(1).LineStyleOf(LineStyle.Solid));
        Assert.Equal(LineStyle.DashDot, Theme.Default.SeriesSlot(1).LineStyleOf(LineStyle.DashDot));
    }

    [Fact]
    public void ALineWidthAtRest_TakesTheSlotsWidth_AndOneTheSeriesNamesWins()
    {
        Assert.Equal(3.0, StyledTheme.SeriesSlot(1).LineWidthOf(1.5));
        Assert.Equal(1.5, StyledTheme.SeriesSlot(0).LineWidthOf(1.5));
        Assert.Equal(0.5, StyledTheme.SeriesSlot(1).LineWidthOf(0.5));
    }

    [Fact]
    public void WithoutACycler_TheLineWidthStaysWhatTheSeriesHolds()
    {
        Assert.Equal(1.5, Theme.Default.SeriesSlot(1).LineWidthOf(1.5));
        Assert.Equal(2.0, Theme.Default.SeriesSlot(1).LineWidthOf(2.0));
    }

    /// <summary>A regression line is 2 wide of its own, not 1.5: "at rest" is the series' own number, and a cycler that
    /// lists no width (it hands out 1.5) names nothing to a series whose own is another.</summary>
    [Fact]
    public void ALineWidthWithItsOwnRest_TakesTheSlotsWidth_OnlyWhenTheCyclerNamesOne()
    {
        Assert.Equal(3.0, StyledTheme.SeriesSlot(1).LineWidthOf(2.0, 2.0));
        Assert.Equal(2.0, StyledTheme.SeriesSlot(0).LineWidthOf(2.0, 2.0));
        Assert.Equal(0.5, StyledTheme.SeriesSlot(1).LineWidthOf(0.5, 2.0));
        Assert.Equal(2.0, Theme.Default.SeriesSlot(1).LineWidthOf(2.0, 2.0));
        // a series whose rest is the common 1.5 is decided as before
        Assert.Equal(1.5, StyledTheme.SeriesSlot(0).LineWidthOf(1.5, 1.5));
        Assert.Equal(3.0, StyledTheme.SeriesSlot(1).LineWidthOf(1.5, 1.5));
    }

    [Fact]
    public void AStroke_TakesTheSlotsDashPatternAndWidth_InTheSeriesColour()
    {
        var stroke = StyledTheme.SeriesSlot(1).StrokeOf(Colors.Blue, 1.5, LineStyle.Solid);

        Assert.Equal(new StrokeStyle(Colors.Blue, 3.0, LineStyle.Dashed), stroke);
    }

    [Fact]
    public void AStrokeTheSeriesNames_IsKept()
    {
        var stroke = StyledTheme.SeriesSlot(1).StrokeOf(Colors.Blue, 0.5, LineStyle.Dotted);

        Assert.Equal(new StrokeStyle(Colors.Blue, 0.5, LineStyle.Dotted), stroke);
    }

    [Fact]
    public void AStrokeOfASeriesWithItsOwnRest_UsesThatRestForTheWidth()
    {
        var stroke = StyledTheme.SeriesSlot(0).StrokeOf(Colors.Blue, 2.0, LineStyle.Solid, restingLineWidth: 2.0);

        Assert.Equal(new StrokeStyle(Colors.Blue, 2.0, LineStyle.Solid), stroke);
    }

    [Fact]
    public void WithoutACycler_AStrokeIsExactlyWhatTheSeriesHolds()
    {
        Assert.Equal(new StrokeStyle(Colors.Blue, 2.0, LineStyle.DashDot),
            Theme.Default.SeriesSlot(1).StrokeOf(Colors.Blue, 2.0, LineStyle.DashDot));
    }

    [Fact]
    public void ALineThatNamesNoMarker_TakesTheSlotsMarker()
    {
        Assert.Equal(MarkerStyle.Square, StyledTheme.SeriesSlot(1).LineMarkerOf(null));
    }

    [Fact]
    public void ACyclerMarkerOfNone_NamesNoMarker()
    {
        Assert.Null(StyledTheme.SeriesSlot(0).LineMarkerOf(null));
    }

    [Fact]
    public void AMarkerTheSeriesNames_Wins_NoneIncluded()
    {
        Assert.Equal(MarkerStyle.Circle, StyledTheme.SeriesSlot(1).LineMarkerOf(MarkerStyle.Circle));
        Assert.Equal(MarkerStyle.None, StyledTheme.SeriesSlot(1).LineMarkerOf(MarkerStyle.None));
    }

    [Fact]
    public void WithoutACycler_ALineNamesItsOwnMarkerOrNone()
    {
        Assert.Null(Theme.Default.SeriesSlot(1).LineMarkerOf(null));
        Assert.Equal(MarkerStyle.Triangle, Theme.Default.SeriesSlot(1).LineMarkerOf(MarkerStyle.Triangle));
    }

    [Fact]
    public void AScatterAtRest_TakesTheSlotsMarker_ButNeverNone()
    {
        Assert.Equal(MarkerStyle.Square, StyledTheme.SeriesSlot(1).ScatterMarkerOf(MarkerStyle.Circle));
        Assert.Equal(MarkerStyle.Circle, StyledTheme.SeriesSlot(0).ScatterMarkerOf(MarkerStyle.Circle));
    }

    [Fact]
    public void AScatterMarkerTheSeriesNames_Wins()
    {
        Assert.Equal(MarkerStyle.Triangle, StyledTheme.SeriesSlot(1).ScatterMarkerOf(MarkerStyle.Triangle));
        Assert.Equal(MarkerStyle.Circle, Theme.Default.SeriesSlot(1).ScatterMarkerOf(MarkerStyle.Circle));
    }

    // The slot reads "at rest" as the value a new series holds, so the two must be one number.

    [Fact]
    public void TheRestingStroke_IsWhatANewLineStepAreaAndErrorBarHold()
    {
        double[] x = [0.0, 1], y = [0.0, 1];

        Assert.Equal(SeriesSlot.RestingLineStyle, new LineSeries(x, y).LineStyle);
        Assert.Equal(SeriesSlot.RestingLineStyle, new StepSeries(x, y).LineStyle);
        Assert.Equal(SeriesSlot.RestingLineStyle, new AreaSeries(x, y).LineStyle);
        Assert.Equal(SeriesSlot.RestingLineWidth, new LineSeries(x, y).LineWidth);
        Assert.Equal(SeriesSlot.RestingLineWidth, new StepSeries(x, y).LineWidth);
        Assert.Equal(SeriesSlot.RestingLineWidth, new AreaSeries(x, y).LineWidth);
        Assert.Equal(SeriesSlot.RestingLineWidth, new ErrorBarSeries(x, y, x, x).LineWidth);
    }

    [Fact]
    public void TheRestingWidthOfARegressionLine_IsWhatANewRegressionHolds()
    {
        double[] x = [0.0, 1], y = [0.0, 1];

        Assert.Equal(RegressionSeries.RestingLineWidth, new RegressionSeries(x, y).LineWidth);
        Assert.Equal(SeriesSlot.RestingLineStyle, new RegressionSeries(x, y).LineStyle);
    }

    [Fact]
    public void TheRestingStroke_IsWhatANewDensityEcdfSignalSparklinePolarAnd3dLineHold()
    {
        double[] x = [0.0, 1], y = [0.0, 1];

        Assert.Equal(SeriesSlot.RestingLineWidth, new KdeSeries(x).LineWidth);
        Assert.Equal(SeriesSlot.RestingLineWidth, new EcdfSeries(x).LineWidth);
        Assert.Equal(SeriesSlot.RestingLineWidth, new SignalSeries(y).LineWidth);
        Assert.Equal(SeriesSlot.RestingLineWidth, new SignalXYSeries(x, y).LineWidth);
        Assert.Equal(SeriesSlot.RestingLineWidth, new SparklineSeries(y).LineWidth);
        Assert.Equal(SeriesSlot.RestingLineWidth, new PolarLineSeries(x, y).LineWidth);
        Assert.Equal(SeriesSlot.RestingLineWidth, new Line3DSeries(x, y, y).LineWidth);
        Assert.Equal(SeriesSlot.RestingLineStyle, new KdeSeries(x).LineStyle);
        Assert.Equal(SeriesSlot.RestingLineStyle, new EcdfSeries(x).LineStyle);
        Assert.Equal(SeriesSlot.RestingLineStyle, new SignalSeries(y).LineStyle);
        Assert.Equal(SeriesSlot.RestingLineStyle, new SignalXYSeries(x, y).LineStyle);
        Assert.Equal(SeriesSlot.RestingLineStyle, new PolarLineSeries(x, y).LineStyle);
        Assert.Equal(SeriesSlot.RestingLineStyle, new Line3DSeries(x, y, y).LineStyle);
    }

    [Fact]
    public void TheRestingMarker_IsWhatANewScatterHolds_AndANewLineOrStepHoldsNone()
    {
        double[] x = [0.0, 1], y = [0.0, 1];

        Assert.Equal(SeriesSlot.RestingScatterMarker, new ScatterSeries(x, y).Marker);
        Assert.Null(new LineSeries(x, y).Marker);
        Assert.Null(new StepSeries(x, y).Marker);
    }

    [Fact]
    public void TheSlotsStrokeIsTheSameWhicheverAxisPairTheSeriesSitsIn()
    {
        var axes = OneTwoThree();

        Assert.Equal(StyledTheme.SeriesSlot(3).LineStyleOf(LineStyle.Solid),
            axes.SeriesSlot(StyledTheme, SeriesAxisPair.SecondaryX, 0).LineStyleOf(LineStyle.Solid));
        Assert.Equal(LineStyle.Dashed, axes.SeriesSlot(StyledTheme, SeriesAxisPair.SecondaryY, 0).LineStyleOf(LineStyle.Solid));
    }

    // ── Axes: which slot a series holds ────────────────────────────────────────

    private static Axes OneTwoThree()
    {
        var axes = new Axes();
        axes.Plot([0.0, 1], [0.0, 1]);
        axes.YAxes.Right.FirstOrAdd().AddSeries(new LineSeries([0.0, 1], [0.0, 1]));
        axes.YAxes.Right.FirstOrAdd().AddSeries(new LineSeries([0.0, 1], [0.0, 1]));
        axes.PlotXSecondary([0.0, 1], [0.0, 1]);
        axes.PlotXSecondary([0.0, 1], [0.0, 1]);
        axes.PlotXSecondary([0.0, 1], [0.0, 1]);
        return axes;
    }

    [Fact]
    public void APrimarySeriesHoldsItsOwnIndex()
    {
        Assert.Equal(0, OneTwoThree().SlotIndex(SeriesAxisPair.Primary, 0));
    }

    [Fact]
    public void ASecondaryYSeriesContinuesAfterThePrimarySeries()
    {
        Assert.Equal(2, OneTwoThree().SlotIndex(SeriesAxisPair.SecondaryY, 1));
    }

    [Fact]
    public void ASecondaryXSeriesContinuesAfterThePrimaryAndTheSecondaryYSeries()
    {
        Assert.Equal(5, OneTwoThree().SlotIndex(SeriesAxisPair.SecondaryX, 2));
    }

    [Fact]
    public void TheSlotsFollowTheOrderOfAllSeries()
    {
        var axes = OneTwoThree();
        var all = axes.AllSeries.ToList();

        Assert.Equal(all.IndexOf(axes.Series[0]), axes.SlotIndex(SeriesAxisPair.Primary, 0));
        Assert.Equal(all.IndexOf(axes.YAxes.Right[0].Series[1]), axes.SlotIndex(SeriesAxisPair.SecondaryY, 1));
        Assert.Equal(all.IndexOf(axes.XSecondarySeries[2]), axes.SlotIndex(SeriesAxisPair.SecondaryX, 2));
    }

    [Fact]
    public void AnUnknownAxisPairIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Axes().SlotIndex((SeriesAxisPair)99, 0));
    }

    [Fact]
    public void TheAxesSlotIsTheThemesSlotAtThatIndex()
    {
        var axes = OneTwoThree();

        Assert.Equal(CyclerTheme.SeriesSlot(4), axes.SeriesSlot(CyclerTheme, SeriesAxisPair.SecondaryX, 1));
    }

    // ── ColorGroup: several series share one colour slot, without sharing identity ─────────────

    [Fact]
    public void TheColorSlotMatchesThePositionalSlot_WhileNothingIsGrouped_AndDivergesOnceSomethingIs()
    {
        var axes = new Axes();
        var a = axes.Plot([0.0, 1], [0.0, 1]);
        var b = axes.Plot([0.0, 1], [0.0, 1]);
        var c = axes.Plot([0.0, 1], [0.0, 1]);

        Assert.Equal(0, axes.ColorSlotIndex(SeriesAxisPair.Primary, 0));
        Assert.Equal(1, axes.ColorSlotIndex(SeriesAxisPair.Primary, 1));
        Assert.Equal(2, axes.ColorSlotIndex(SeriesAxisPair.Primary, 2));

        a.ColorGroup = 0;
        b.ColorGroup = 0;

        Assert.Equal(0, axes.ColorSlotIndex(SeriesAxisPair.Primary, 0));
        Assert.Equal(0, axes.ColorSlotIndex(SeriesAxisPair.Primary, 1));
        Assert.Equal(1, axes.ColorSlotIndex(SeriesAxisPair.Primary, 2));
    }

    [Fact]
    public void TheSeriesAfterAGroup_ContinuesAtTheNextColorSlot_NotAfterEveryMember()
    {
        var axes = new Axes();
        axes.Plot([0.0, 1], [0.0, 1]);                 // slot 0, ungrouped
        var curve = axes.Plot([0.0, 1], [0.0, 1]);     // group 7
        var band = axes.Plot([0.0, 1], [0.0, 1]);      // group 7
        var marks = axes.Plot([0.0, 1], [0.0, 1]);     // group 7
        curve.ColorGroup = band.ColorGroup = marks.ColorGroup = 7;
        axes.Plot([0.0, 1], [0.0, 1]);                 // ungrouped again

        Assert.Equal(0, axes.ColorSlotIndex(SeriesAxisPair.Primary, 0));
        Assert.Equal(1, axes.ColorSlotIndex(SeriesAxisPair.Primary, 1));
        Assert.Equal(1, axes.ColorSlotIndex(SeriesAxisPair.Primary, 2));
        Assert.Equal(1, axes.ColorSlotIndex(SeriesAxisPair.Primary, 3));
        Assert.Equal(2, axes.ColorSlotIndex(SeriesAxisPair.Primary, 4));
    }

    [Fact]
    public void TwoDistinctGroups_EachTakeTheirOwnColorSlot_NotMergedWithEachOther()
    {
        var axes = new Axes();
        var a1 = axes.Plot([0.0, 1], [0.0, 1]);
        var a2 = axes.Plot([0.0, 1], [0.0, 1]);
        var b1 = axes.Plot([0.0, 1], [0.0, 1]);
        var b2 = axes.Plot([0.0, 1], [0.0, 1]);
        a1.ColorGroup = a2.ColorGroup = 0;
        b1.ColorGroup = b2.ColorGroup = 1;

        Assert.Equal(0, axes.ColorSlotIndex(SeriesAxisPair.Primary, 1));
        Assert.Equal(1, axes.ColorSlotIndex(SeriesAxisPair.Primary, 2));
        Assert.Equal(1, axes.ColorSlotIndex(SeriesAxisPair.Primary, 3));
    }

    [Fact]
    public void SlotIndex_StaysPositional_EvenWhenSeriesAreGrouped()
    {
        var axes = new Axes();
        var curve = axes.Plot([0.0, 1], [0.0, 1]);
        var band = axes.Plot([0.0, 1], [0.0, 1]);
        curve.ColorGroup = band.ColorGroup = 0;

        Assert.Equal(0, axes.SlotIndex(SeriesAxisPair.Primary, 0));
        Assert.Equal(1, axes.SlotIndex(SeriesAxisPair.Primary, 1));
    }

    [Fact]
    public void TheLegendSwatch_WearsTheGroupsColorSlot_WhileTheLegendIndexStaysPositional()
    {
        var axes = new Axes();
        var curve = axes.Plot([0.0, 1], [0.0, 1]);
        curve.Label = "Treatment";
        var band = axes.Plot([0.0, 1], [0.0, 1]);
        curve.ColorGroup = band.ColorGroup = 0;

        var entry = Assert.Single(axes.LegendEntries(directLabelsDrawn: false));

        Assert.Equal(0, entry.OriginalIndex);
        Assert.Equal(0, entry.ColorSlotIndex);
    }

    [Fact]
    public void NextColorGroup_IsZero_WhenNoSeriesIsGroupedYet()
    {
        Assert.Equal(0, new Axes().NextColorGroup());
    }

    [Fact]
    public void NextColorGroup_IsOneMoreThanTheHighestGroupAlreadyUsed()
    {
        var axes = new Axes();
        var a = axes.Plot([0.0, 1], [0.0, 1]);
        axes.Plot([0.0, 1], [0.0, 1]);
        a.ColorGroup = 4;

        Assert.Equal(5, axes.NextColorGroup());
    }

    [Fact]
    public void TheGroupsRenderedColor_IsTheSlotsColor_SharedByEveryMember()
    {
        var axes = new Axes();
        var curve = axes.Plot([0.0, 1], [0.0, 1]);
        var band = axes.Plot([0.0, 1], [0.0, 1]);
        curve.ColorGroup = band.ColorGroup = 0;

        Assert.Equal(axes.SeriesSlot(CyclerTheme, SeriesAxisPair.Primary, 0).CycleColor,
            axes.SeriesSlot(CyclerTheme, SeriesAxisPair.Primary, 1).CycleColor);
    }

    // ── ColorSlotSpan: a series that colours its own parts holds a slot for each of them ──────

    private static readonly double[] Xs = [0.0, 1, 2];

    [Fact]
    public void AStackOfThreeLayers_HoldsThreeSlots_AndTheLineAfterItTakesTheFourth()
    {
        var axes = new Axes();
        axes.StackPlot(Xs, [[1.0, 2, 3], [2.0, 3, 4], [3.0, 4, 5]]);
        axes.Plot(Xs, Xs);

        Assert.Equal([0, 3], axes.ColorSlots());
    }

    [Fact]
    public void ALineBeforeAndAfterAStack_TakeTheSlotsOnEitherSideOfItsLayers()
    {
        var axes = new Axes();
        axes.Plot(Xs, Xs);
        axes.StackPlot(Xs, [[1.0, 2, 3], [2.0, 3, 4], [3.0, 4, 5]]);
        axes.Plot(Xs, Xs);

        Assert.Equal([0, 1, 4], axes.ColorSlots());
    }

    [Fact]
    public void APieDonutAndFunnel_HoldOneSlotForEachPart()
    {
        var axes = new Axes();
        axes.Pie([1.0, 2, 3, 4]);
        axes.Donut([1.0, 2, 3]);
        axes.Funnel(["a", "b"], [3.0, 2]);
        axes.Plot(Xs, Xs);

        Assert.Equal([0, 4, 7, 9], axes.ColorSlots());
    }

    [Fact]
    public void ASeriesWithNoPartsToColour_StillHoldsOneSlot()
    {
        var axes = new Axes();
        axes.StackPlot(Xs, []);
        axes.Plot(Xs, Xs);

        Assert.Equal([0, 1], axes.ColorSlots());
    }

    [Fact]
    public void AColorGroup_TakesTheLargestSpanOfItsMembers_AndTheNextSeriesContinuesPastIt()
    {
        var axes = new Axes();
        var stack = axes.StackPlot(Xs, [[1.0, 2, 3], [2.0, 3, 4], [3.0, 4, 5]]);
        var line = axes.Plot(Xs, Xs);
        stack.ColorGroup = line.ColorGroup = 0;
        axes.Plot(Xs, Xs);

        Assert.Equal([0, 0, 3], axes.ColorSlots());
    }

    [Fact]
    public void IdentityStaysPositional_WhateverTheSpans()
    {
        var axes = new Axes();
        axes.StackPlot(Xs, [[1.0, 2, 3], [2.0, 3, 4], [3.0, 4, 5]]);
        axes.Plot(Xs, Xs);

        Assert.Equal(1, axes.SlotIndex(SeriesAxisPair.Primary, 1));
        Assert.Equal(3, axes.ColorSlotIndex(SeriesAxisPair.Primary, 1));
    }
}
