// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
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
    public void AClassicSlot_ColoursAViolinAsTheThemeDoes()
    {
        var slot = Theme.MatplotlibClassic.SeriesSlot(3);
        var violin = new ViolinSeries([[1.0, 2, 3]]);

        Assert.Equal(Color.FromHex("#BFBF00"), slot.ColorOf(violin));
        Assert.Equal(Color.FromHex("#FF0000"), slot.StatsColorOf(violin));
    }

    /// <summary>A slot is the theme's colour at a position and the cycler's set there, with the theme that handed it out: one made from
    /// a colour already chosen (a renderer is handed its slot in parts) reads the same looks as one the theme hands out, so the
    /// renderer and the legend cannot read different slots.</summary>
    [Fact]
    public void ASlotMadeFromChosenParts_ReadsTheSameThemeAsTheSlotTheThemeHandsOut()
    {
        var handedOut = ViolinLinesTheme.SeriesSlot(2);
        var violin = new ViolinSeries([[1.0, 2, 3]]);
        var chosen = new SeriesSlot(Colors.Red, null, ViolinLinesTheme);

        Assert.Equal(handedOut, new SeriesSlot(handedOut.CycleColor, handedOut.CycledProperties, ViolinLinesTheme));
        Assert.Equal(ThemeViolinBody, chosen.ColorOf(violin));
        Assert.Equal(ThemeViolinStats, chosen.StatsColorOf(violin));
        Assert.Equal(Colors.Red, chosen.CycleColor);
    }

    /// <summary>A slot is handed by value to every series renderer and legend swatch on every draw: the colour, the cycler's set and
    /// the theme, nothing copied out of the theme's looks.</summary>
    [Fact]
    public void ASlot_IsAsSmallAsItsThreeParts() =>
        Assert.True(System.Runtime.CompilerServices.Unsafe.SizeOf<SeriesSlot>() <= 56,
            $"a slot is {System.Runtime.CompilerServices.Unsafe.SizeOf<SeriesSlot>()} bytes");

    /// <summary>What a theme names for a kind of series is read through the slot's theme, so a slot made for a theme that names a
    /// scatter look colours a scatter from it, and one made for a theme that names none does not.</summary>
    [Fact]
    public void ASlotReadsTheLooksOfItsTheme()
    {
        var scatter = new ScatterSeries([1.0], [2.0]);

        Assert.Equal(Colors.Blue, Theme.MatplotlibClassic.SeriesSlot(0).ColorOf(scatter));
        Assert.Equal(Theme.Default.CycleColors[0], Theme.Default.SeriesSlot(0).ColorOf(scatter));
    }

    // ── How a series is stroked: the cycler's dash, width and marker ───────────

    private static readonly PropCycler Styled = new PropCyclerBuilder()
        .WithColors(Colors.Red, Colors.Green)
        .WithLineStyles(LineStyle.Solid, LineStyle.Dashed)
        .WithMarkerStyles(MarkerStyle.None, MarkerStyle.Square)
        .WithLineWidths(1.5, 3.0)
        .Build();

    private static readonly Theme StyledTheme = Theme.CreateFrom(Theme.Default).WithPropCycler(Styled).Build();

    public static TheoryData<Func<Theme, object?>, object?> WhatTheStyledSlotHandsBack => new()
    {
        { theme => theme.SeriesSlot(1).LineStyleOf(null), LineStyle.Dashed },             // a line style not given takes the slot's dash pattern
        { theme => theme.SeriesSlot(0).LineStyleOf(null), LineStyle.Solid },
        { theme => theme.SeriesSlot(1).LineMarkerOf(MarkerStyle.Circle), MarkerStyle.Circle }, // a marker the series names wins, none included
        { theme => theme.SeriesSlot(1).LineMarkerOf(MarkerStyle.None), MarkerStyle.None },
        { theme => theme.SeriesSlot(1).ScatterMarkerOf(null), MarkerStyle.Square },       // a scatter given no marker takes the slot's marker, but never none
        { theme => theme.SeriesSlot(0).ScatterMarkerOf(null), MarkerStyle.Circle },
    };

    [Theory]
    [MemberData(nameof(WhatTheStyledSlotHandsBack))]
    public void AStyledSlot_HandsBackTheStyleTheSeriesNamesOrTheSlotsOwn(Func<Theme, object?> ask, object? expected) =>
        Assert.Equal(expected, ask(StyledTheme));

    [Fact]
    public void ALineStyleTheSeriesNames_Wins()
    {
        Assert.Equal(LineStyle.Dotted, StyledTheme.SeriesSlot(1).LineStyleOf(LineStyle.Dotted));
        Assert.Equal(LineStyle.Dashed, StyledTheme.SeriesSlot(0).LineStyleOf(LineStyle.Dashed));
        // solid, given, is kept on a slot that dashes
        Assert.Equal(LineStyle.Solid, StyledTheme.SeriesSlot(1).LineStyleOf(LineStyle.Solid));
    }

    [Fact]
    public void WithoutACycler_TheLineStyleStaysWhatTheSeriesHolds()
    {
        Assert.Equal(LineStyle.Solid, Theme.Default.SeriesSlot(1).LineStyleOf(null));
        Assert.Equal(LineStyle.DashDot, Theme.Default.SeriesSlot(1).LineStyleOf(LineStyle.DashDot));
    }

    [Fact]
    public void ALineWidthNotGiven_TakesTheSlotsWidth_AndOneTheSeriesWasGivenWins()
    {
        Assert.Equal(3.0, StyledTheme.SeriesSlot(1).LineWidthOf(LineWidthRule.Line));
        Assert.Equal(1.5, StyledTheme.SeriesSlot(0).LineWidthOf(LineWidthRule.Line));
        Assert.Equal(0.5, StyledTheme.SeriesSlot(1).LineWidthOf(0.5, LineWidthRule.Line));
        // 1.5, given, is kept on a slot that names 3
        Assert.Equal(1.5, StyledTheme.SeriesSlot(1).LineWidthOf(1.5, LineWidthRule.Line));
    }

    [Fact]
    public void WithoutACycler_TheLineWidthStaysWhatTheSeriesHolds()
    {
        Assert.Equal(1.5, Theme.Default.SeriesSlot(1).LineWidthOf(LineWidthRule.Line));
        Assert.Equal(2.0, Theme.Default.SeriesSlot(1).LineWidthOf(2.0, LineWidthRule.Line));
    }

    /// <summary>A regression line is 2 wide of its own, not 1.5 and not the theme's: a cycler that lists no width names
    /// nothing, so a regression given no width keeps its own 2; a cycler that names 1.5 names a width.</summary>
    [Fact]
    public void ALineWidthOfAKindWithItsOwnWidth_TakesTheSlotsWidth_OnlyWhenTheCyclerNamesOne()
    {
        Assert.Equal(3.0, StyledTheme.SeriesSlot(1).LineWidthOf(null, LineWidthRule.TwoPixelLine));
        Assert.Equal(1.5, StyledTheme.SeriesSlot(0).LineWidthOf(null, LineWidthRule.TwoPixelLine));
        var coloursOnly = Theme.CreateFrom(Theme.Default).WithPropCycler(new PropCyclerBuilder().WithColors(Colors.Red).Build()).Build();
        Assert.Equal(2.0, coloursOnly.SeriesSlot(0).LineWidthOf(null, LineWidthRule.TwoPixelLine));
        Assert.Equal(0.5, StyledTheme.SeriesSlot(1).LineWidthOf(0.5, LineWidthRule.TwoPixelLine));
        Assert.Equal(2.0, Theme.Default.SeriesSlot(1).LineWidthOf(null, LineWidthRule.TwoPixelLine));
        Assert.Equal(2.0, Theme.MatplotlibV2.SeriesSlot(0).LineWidthOf(null, LineWidthRule.TwoPixelLine));
    }

    [Fact]
    public void AStroke_TakesTheSlotsDashPatternAndWidth_InTheSeriesColour()
    {
        var stroke = StyledTheme.SeriesSlot(1).StrokeOf(Colors.Blue, default(GivenStroke), LineWidthRule.Line);

        Assert.Equal(new StrokeStyle(Colors.Blue, 3.0, LineStyle.Dashed), stroke);
    }

    [Fact]
    public void AStrokeTheSeriesNames_IsKept()
    {
        var stroke = StyledTheme.SeriesSlot(1).StrokeOf(Colors.Blue, new GivenStroke(0.5, LineStyle.Dotted), LineWidthRule.Line);

        Assert.Equal(new StrokeStyle(Colors.Blue, 0.5, LineStyle.Dotted), stroke);
    }

    [Fact]
    public void ARenderersOwnLine_IsItsColourAtItsRulesWidth_SolidUnlessItNamesADash()
    {
        var slot = StyledTheme.SeriesSlot(1);

        Assert.Equal(StrokeStyle.Solid(Colors.Blue, slot.LineWidthOf(LineWidthRule.OwnHairline)), slot.StrokeOf(Colors.Blue, LineWidthRule.OwnHairline));
        Assert.Equal(new StrokeStyle(Colors.Blue, 0.5, LineStyle.Dashed), slot.StrokeOf(Colors.Blue, LineWidthRule.OwnHairline, LineStyle.Dashed));
    }

    [Fact]
    public void AStrokeOfAKindWithItsOwnWidth_UsesThatWidth()
    {
        var stroke = Theme.Default.SeriesSlot(0).StrokeOf(Colors.Blue, default, LineWidthRule.TwoPixelLine);

        Assert.Equal(new StrokeStyle(Colors.Blue, 2.0, LineStyle.Solid), stroke);
    }

    [Fact]
    public void WithoutACycler_AStrokeIsExactlyWhatTheSeriesHolds()
    {
        Assert.Equal(new StrokeStyle(Colors.Blue, 2.0, LineStyle.DashDot),
            Theme.Default.SeriesSlot(1).StrokeOf(Colors.Blue, new GivenStroke(2.0, LineStyle.DashDot), LineWidthRule.Line));
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
    public void WithoutACycler_ALineNamesItsOwnMarkerOrNone()
    {
        Assert.Null(Theme.Default.SeriesSlot(1).LineMarkerOf(null));
        Assert.Equal(MarkerStyle.Triangle, Theme.Default.SeriesSlot(1).LineMarkerOf(MarkerStyle.Triangle));
    }

    [Fact]
    public void AScatterMarkerTheSeriesNames_Wins()
    {
        Assert.Equal(MarkerStyle.Triangle, StyledTheme.SeriesSlot(1).ScatterMarkerOf(MarkerStyle.Triangle));
        Assert.Equal(MarkerStyle.Circle, Theme.Default.SeriesSlot(1).ScatterMarkerOf(MarkerStyle.Circle));
        // a circle, given, is kept on a slot that hands out squares
        Assert.Equal(MarkerStyle.Circle, StyledTheme.SeriesSlot(1).ScatterMarkerOf(MarkerStyle.Circle));
    }

    // The slot reads "at rest" as the value a new series holds, so the two must be one number. The kinds that hold a given stroke
    // report it through GivenStroke (GivenStrokeHolderTests); the kinds whose line takes a width alone are held here.

    [Fact]
    public void TheRestingWidth_IsWhatANewErrorBarAndSparklineHold()
    {
        double[] x = [0.0, 1], y = [0.0, 1];

        Assert.Equal(LineWidthRule.Line.Resting, new ErrorBarSeries(x, y, x, x).LineWidth);
        Assert.Equal(LineWidthRule.Line.Resting, new SparklineSeries(y).LineWidth);
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

        Assert.Equal(StyledTheme.SeriesSlot(3).LineStyleOf(null),
            StyledTheme.SeriesSlot(axes.ColorSlots(StyledTheme)[axes.SlotIndex(SeriesAxisPair.SecondaryX, 0)]).LineStyleOf(null));
        Assert.Equal(LineStyle.Dashed, StyledTheme.SeriesSlot(axes.ColorSlots(StyledTheme)[axes.SlotIndex(SeriesAxisPair.SecondaryY, 0)]).LineStyleOf(null));
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

    [Theory]
    [InlineData("Primary", 0, 0)] // a primary series holds its own index
    [InlineData("SecondaryY", 1, 2)] // a secondary Y series continues after the primary series
    [InlineData("SecondaryX", 2, 5)] // a secondary X series continues after the primary and the secondary Y series
    public void ASeriesSlot_CountsTheSeriesOfEveryPairBeforeIt(string pair, int index, int slot)
    {
        Assert.Equal(slot, OneTwoThree().SlotIndex(Enum.Parse<SeriesAxisPair>(pair), index));
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

        Assert.Equal(CyclerTheme.SeriesSlot(4), CyclerTheme.SeriesSlot(axes.ColorSlots(CyclerTheme)[axes.SlotIndex(SeriesAxisPair.SecondaryX, 1)]));
    }

    // ── ColorGroup: several series share one colour slot, without sharing identity ─────────────

    [Fact]
    public void TheColorSlotMatchesThePositionalSlot_WhileNothingIsGrouped_AndDivergesOnceSomethingIs()
    {
        var axes = new Axes();
        var a = axes.Plot([0.0, 1], [0.0, 1]);
        var b = axes.Plot([0.0, 1], [0.0, 1]);
        axes.Plot([0.0, 1], [0.0, 1]);

        Assert.Equal(0, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 0)]);
        Assert.Equal(1, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 1)]);
        Assert.Equal(2, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 2)]);

        a.ColorGroup = 0;
        b.ColorGroup = 0;

        Assert.Equal(0, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 0)]);
        Assert.Equal(0, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 1)]);
        Assert.Equal(1, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 2)]);
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

        Assert.Equal(0, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 0)]);
        Assert.Equal(1, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 1)]);
        Assert.Equal(1, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 2)]);
        Assert.Equal(1, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 3)]);
        Assert.Equal(2, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 4)]);
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

        Assert.Equal(0, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 1)]);
        Assert.Equal(1, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 2)]);
        Assert.Equal(1, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 3)]);
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

        var entry = Assert.Single(axes.LegendEntries(Theme.Default, directLabelsDrawn: false));

        Assert.Equal(0, entry.OriginalIndex);
        Assert.Equal(0, entry.ColorSlotIndex);
    }

    [Fact]
    public void InColorGroup_StampsEveryMemberWithGroupZero_WhenNoSeriesIsGroupedYet()
    {
        var axes = new Axes();
        var a = axes.Plot([0.0, 1], [0.0, 1]);
        var b = new LineSeries([0.0, 1], [0.0, 1]);

        axes.InColorGroup(a, b);

        Assert.Equal((0, 0), (a.ColorGroup, b.ColorGroup));
    }

    [Fact]
    public void InColorGroup_StampsOneMoreThanTheHighestGroupAlreadyUsed_AddedOrNot()
    {
        var axes = new Axes();
        var a = axes.Plot([0.0, 1], [0.0, 1]);
        var b = axes.Plot([0.0, 1], [0.0, 1]);
        a.ColorGroup = 4;
        var c = new ScatterSeries([0.0, 1], [0.0, 1]);

        axes.InColorGroup(b, c);

        Assert.Equal((4, 5, 5), (a.ColorGroup, b.ColorGroup, c.ColorGroup));
    }

    [Fact]
    public void InColorGroup_DrawsANewGroupEachCall()
    {
        var axes = new Axes();
        var a = axes.Plot([0.0, 1], [0.0, 1]);
        var b = axes.Plot([0.0, 1], [0.0, 1]);

        axes.InColorGroup(a);
        axes.InColorGroup(b);

        Assert.Equal((0, 1), (a.ColorGroup, b.ColorGroup));
    }

    [Fact]
    public void TheGroupsRenderedColor_IsTheSlotsColor_SharedByEveryMember()
    {
        var axes = new Axes();
        var curve = axes.Plot([0.0, 1], [0.0, 1]);
        var band = axes.Plot([0.0, 1], [0.0, 1]);
        curve.ColorGroup = band.ColorGroup = 0;

        Assert.Equal(CyclerTheme.SeriesSlot(axes.ColorSlots(CyclerTheme)[axes.SlotIndex(SeriesAxisPair.Primary, 0)]).CycleColor,
            CyclerTheme.SeriesSlot(axes.ColorSlots(CyclerTheme)[axes.SlotIndex(SeriesAxisPair.Primary, 1)]).CycleColor);
    }

    // ── ColorSlotSpan: a series that colours its own parts holds a slot for each of them ──────

    private static readonly double[] Xs = [0.0, 1, 2];

    [Fact]
    public void AStackOfThreeLayers_HoldsThreeSlots_AndTheLineAfterItTakesTheFourth()
    {
        var axes = new Axes();
        axes.StackPlot(Xs, [[1.0, 2, 3], [2.0, 3, 4], [3.0, 4, 5]]);
        axes.Plot(Xs, Xs);

        Assert.Equal([0, 3], axes.ColorSlots(Theme.Default));
    }

    [Fact]
    public void ALineBeforeAndAfterAStack_TakeTheSlotsOnEitherSideOfItsLayers()
    {
        var axes = new Axes();
        axes.Plot(Xs, Xs);
        axes.StackPlot(Xs, [[1.0, 2, 3], [2.0, 3, 4], [3.0, 4, 5]]);
        axes.Plot(Xs, Xs);

        Assert.Equal([0, 1, 4], axes.ColorSlots(Theme.Default));
    }

    [Fact]
    public void APieDonutAndFunnel_HoldOneSlotForEachPart()
    {
        var axes = new Axes();
        axes.Pie([1.0, 2, 3, 4]);
        axes.Donut([1.0, 2, 3]);
        axes.Funnel(["a", "b"], [3.0, 2]);
        axes.Plot(Xs, Xs);

        Assert.Equal([0, 4, 7, 9], axes.ColorSlots(Theme.Default));
    }

    [Fact]
    public void ASeriesWithNoPartsToColour_StillHoldsOneSlot()
    {
        var axes = new Axes();
        axes.StackPlot(Xs, []);
        axes.Plot(Xs, Xs);

        Assert.Equal([0, 1], axes.ColorSlots(Theme.Default));
    }

    [Fact]
    public void AColorGroup_TakesTheLargestSpanOfItsMembers_AndTheNextSeriesContinuesPastIt()
    {
        var axes = new Axes();
        var stack = axes.StackPlot(Xs, [[1.0, 2, 3], [2.0, 3, 4], [3.0, 4, 5]]);
        var line = axes.Plot(Xs, Xs);
        stack.ColorGroup = line.ColorGroup = 0;
        axes.Plot(Xs, Xs);

        Assert.Equal([0, 0, 3], axes.ColorSlots(Theme.Default));
    }

    [Fact]
    public void IdentityStaysPositional_WhateverTheSpans()
    {
        var axes = new Axes();
        axes.StackPlot(Xs, [[1.0, 2, 3], [2.0, 3, 4], [3.0, 4, 5]]);
        axes.Plot(Xs, Xs);

        Assert.Equal(1, axes.SlotIndex(SeriesAxisPair.Primary, 1));
        Assert.Equal(3, axes.ColorSlots(Theme.Default)[axes.SlotIndex(SeriesAxisPair.Primary, 1)]);
    }

    // ── the table is built on every draw, so it allocates nothing but itself ──────

    private static Axes WithEveryTwin() =>
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(Xs, Xs).Scatter(Xs, Xs)
            .WithAddRightYAxis(right => right.Plot(Xs, Xs))
            .WithAddLeftYAxis(left => left.Plot(Xs, Xs))
            .WithSecondaryXAxis(top => top.PlotXSecondary(Xs, Xs))).Build().SubPlots[0];

    /// <summary>Bytes the current thread allocated while <paramref name="work"/> ran.</summary>
    private static long Allocated(Action work)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        work();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    /// <summary>A Cartesian draw asks for the table for its series and again for its legend, so building it allocates the table it
    /// returns and nothing else: no list of the series, no flags for where a twin axis begins.</summary>
    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    [InlineData("default")]
    public void TheTableOfAnAxesWithEveryTwin_AllocatesOnlyItself(string name)
    {
        var theme = name.AsTheme();
        var axes = WithEveryTwin();
        int[] slots = axes.ColorSlots(theme);
        int[]? table = null;
        _ = Allocated(() => table = new int[slots.Length]);

        long tableBytes = Allocated(() => table = new int[slots.Length]);
        long allocated = Allocated(() => slots = axes.ColorSlots(theme));

        Assert.Equal(name == "default" ? new[] { 0, 1, 2, 3, 4 } : new[] { 0, 0, 0, 0, 0 }, slots);
        Assert.Equal(tableBytes, allocated);
        GC.KeepAlive(table);
    }

    [Fact]
    public void TheTableOfAnAxesWithNoSeries_IsEmpty_AndAllocatesNothing()
    {
        var axes = new Axes();
        _ = axes.ColorSlots(Theme.MatplotlibV2);
        int[] slots = [1];

        long allocated = Allocated(() => slots = axes.ColorSlots(Theme.MatplotlibV2));

        Assert.Empty(slots);
        Assert.Equal(0, allocated);
    }

    /// <summary>A colour group that runs from the primary axis into a twin is one run that started on the primary, so the twin's
    /// fresh cycles do not start under it, and the twin's next series continues the cycle the run moved.</summary>
    [Fact]
    public void AGroupRunningIntoATwin_KeepsTheCycleItStartedIn()
    {
        var axes = new Axes();
        var primary = axes.Plot(Xs, Xs);
        var right = axes.YAxes.Right.Add();
        var twin = right.AddSeries(new LineSeries(Xs, Xs));
        right.AddSeries(new LineSeries(Xs, Xs));
        primary.ColorGroup = twin.ColorGroup = 0;

        Assert.Equal([0, 0, 1], axes.ColorSlots(Theme.MatplotlibV2));
        Assert.Equal([0, 0, 1], axes.ColorSlots(Theme.Default));
    }

    [Fact]
    public void AGroupOnATwin_AfterTheTwinsFirstSeries_StartsNoCycleAgain()
    {
        var axes = new Axes();
        axes.Plot(Xs, Xs);
        var right = axes.YAxes.Right.Add();
        right.AddSeries(new LineSeries(Xs, Xs));
        var a = right.AddSeries(new LineSeries(Xs, Xs));
        var b = right.AddSeries(new ScatterSeries(Xs, Xs));
        a.ColorGroup = b.ColorGroup = 4;
        axes.YAxes.Left.Add().AddSeries(new ScatterSeries(Xs, Xs));

        Assert.Equal([0, 0, 1, 1, 0], axes.ColorSlots(Theme.MatplotlibV2));
        Assert.Equal([0, 1, 2, 2, 3], axes.ColorSlots(Theme.Default));
    }
}
