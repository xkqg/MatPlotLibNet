// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>How wide a series' line is drawn when its author gives no width is one rule with one merge
/// (<see cref="LineWidthRule.WidthOf"/>); what differs between kinds is a value: the width the kind rests at, whether the theme's
/// cycler names it, whether the kind's own width beats the theme's, and a factor on the theme's. matplotlib 3.11.2, read with
/// python: a <c>Line2D</c> made by <c>plot</c> takes the prop_cycle's <c>linewidth</c>, else <c>lines.linewidth</c>; every collection
/// of lines takes <c>lines.linewidth</c> and never the cycler (<c>collections.py:1815</c> <c>LineCollection._get_default_linewidth:
/// return mpl.rcParams['lines.linewidth']</c>; <c>streamplot.py:144</c> <c>linewidth = mpl._val_or_rc(linewidth,
/// 'lines.linewidth')</c>; <c>contour.py:1144-1146</c> <c>contour.linewidth</c>, else <c>lines.linewidth</c>); seaborn 0.13.2 draws
/// a point plot's lines <c>mpl.rcParams["lines.linewidth"] * 1.8</c> (<c>categorical.py:1180</c>).</summary>
public class LineWidthRuleTests
{
    [Fact]
    public void ALine_RestsAtTheLibrarysWidth_TakesTheCycler_AndTheThemeBeatsItsOwnWidth()
    {
        var line = LineWidthRule.Line;

        Assert.Equal(1.5, SeriesLineLook.LibraryWidth);
        Assert.Equal(SeriesLineLook.LibraryWidth, line.Resting);
        Assert.True(line.Cycled);
        Assert.False(line.BeatsTheme);
        Assert.Equal(1.0, line.Scale);
    }

    [Fact]
    public void ACollectionOfLines_RestsAsALine_ButNoCyclerNamesItsWidth()
    {
        Assert.Equal(LineWidthRule.Line with { Cycled = false }, LineWidthRule.LineCollection);
    }

    /// <summary>A line of the library's own look (an arrow of a quiver given a scale, a barb, a violin's mean under the library's look):
    /// a line's resting width whatever width the theme names, and no cycler names it.</summary>
    [Fact]
    public void ALineOfTheLibrarysOwnLook_RestsAsALine_UnderEveryTheme_AndNoCyclerNamesIt()
    {
        var own = LineWidthRule.Own;

        Assert.Equal(LineWidthRule.LineCollection with { BeatsTheme = true }, own);
        Assert.Equal(SeriesLineLook.LibraryWidth, own.WidthOf(null, 4.0, 3.0));
        Assert.Equal(SeriesLineLook.LibraryWidth, own.WidthOf(null, null, null));
        Assert.Equal(0.5, own.WidthOf(0.5, 4.0, 3.0));
    }

    [Fact]
    public void AGivenWidth_WinsOverEverything()
    {
        Assert.Equal(0.5, LineWidthRule.Line.WidthOf(0.5, 4.0, 2.0));
        Assert.Equal(0.5, LineWidthRule.LineCollection.WidthOf(0.5, 4.0, 2.0));
        Assert.Equal(0.5, (LineWidthRule.Line with { Resting = 2, BeatsTheme = true }).WidthOf(0.5, 4.0, 2.0));
    }

    [Fact]
    public void TheCyclersWidth_IsTakenOnlyByAKindTheCyclerNames()
    {
        Assert.Equal(4.0, LineWidthRule.Line.WidthOf(null, 4.0, 2.0));
        Assert.Equal(2.0, LineWidthRule.LineCollection.WidthOf(null, 4.0, 2.0));
    }

    [Fact]
    public void TheThemesWidth_BeatsTheKindsOwn_UnlessTheKindBeatsTheTheme()
    {
        var own = LineWidthRule.Line with { Resting = 2, BeatsTheme = true };

        Assert.Equal(3.0, LineWidthRule.Line.WidthOf(null, null, 3.0));
        Assert.Equal(2.0, own.WidthOf(null, null, 3.0));
        Assert.Equal(4.0, own.WidthOf(null, 4.0, 3.0));
    }

    [Fact]
    public void AThemeThatNamesNoWidth_LeavesTheKindsOwn()
    {
        Assert.Equal(1.5, LineWidthRule.Line.WidthOf(null, null, null));
        Assert.Equal(0.5, (LineWidthRule.LineCollection with { Resting = 0.5 }).WidthOf(null, null, null));
    }

    /// <summary>seaborn's point plot: <c>lines.linewidth * 1.8</c>, and 1.8 times the kind's own where the theme names no width.</summary>
    [Fact]
    public void AScale_MultipliesTheThemesWidth_OrTheKindsOwnWhereTheThemeNamesNone()
    {
        var seaborn = LineWidthRule.LineCollection with { Scale = 1.8 };

        Assert.Equal(1.5.PointsInPixels * 1.8, seaborn.WidthOf(null, null, 1.5.PointsInPixels));
        Assert.Equal(1.5 * 1.8, seaborn.WidthOf(null, null, null));
        Assert.Equal(1.0, seaborn.WidthOf(1.0, null, 3.0));
    }

    [Fact]
    public void EveryKind_StatesItsRuleAsAValue()
    {
        Assert.Equal(LineWidthRule.Line with { Resting = 2, BeatsTheme = true }, LineWidthRule.TwoPixelLine);
        Assert.Equal(LineWidthRule.LineCollection with { Resting = 1 }, LineWidthRule.OnePixelCollection);
        Assert.Equal(LineWidthRule.LineCollection with { Resting = 1 }, LineWidthRule.OnePixelCollection);
        Assert.Equal(LineWidthRule.LineCollection with { Resting = 0.5 }, LineWidthRule.HalfPixelCollection);
        Assert.Equal(LineWidthRule.LineCollection with { Resting = 1 }, LineWidthRule.OnePixelCollection);
        Assert.Equal(LineWidthRule.LineCollection with { Resting = 1 }, LineWidthRule.OnePixelCollection);
        Assert.Equal(LineWidthRule.LineCollection with { Resting = 2.5 }, LineWidthRule.WideCollection);
        Assert.Equal(LineWidthRule.Line with { Resting = 2.5 }, LineWidthRule.WideLine);
        Assert.Equal(LineWidthRule.LineCollection, LineWidthRule.LineCollection);
        Assert.Equal(LineWidthRule.LineCollection, LineWidthRule.LineCollection);
    }

    /// <summary>A point plot's lines: the series' own 1.5 under the library's look whatever width the theme names, seaborn's
    /// <c>lines.linewidth * 1.8</c> under seaborn's.</summary>
    [Fact]
    public void APointPlotsLine_IsALookValue_TheLibrarysOwnOrSeaborns()
    {
        Assert.Same(LineWidthRule.Own, DotLook.Library.PointplotLine);
        Assert.Equal(LineWidthRule.LineCollection with { Scale = 1.8 }, DotLook.Seaborn.PointplotLine);
        Assert.True(DotLook.Seaborn.LikeSeaborn);
        Assert.Same(DotLook.Seaborn, Theme.MatplotlibV2.Looks.Dots);
        Assert.Same(DotLook.Seaborn, Theme.MatplotlibClassic.Looks.Dots);
    }

    /// <summary>The width a series of each kind reads at rest is the resting width of its kind's rule: one number per kind, not a copy
    /// per class.</summary>
    [Fact]
    public void TheWidthANewSeriesReads_IsItsKindsRestingWidth()
    {
        double[] x = [0.0, 1], y = [0.0, 1];
        double line = LineWidthRule.Line.Resting;

        Assert.All(new double[]
        {
            new TernaryLineSeries(x, y, y).LineWidth, new StreamingLineSeries(4).LineWidth, new PointAndFigureSeries(y, 1).LineWidth,
            new Sma(y, 2).LineWidth,
        }, width => Assert.Equal(line, width));

        Assert.Equal(DotLook.Library.PointplotLine.Resting, new PointplotSeries([[1.0]]).LineWidth);
        Assert.Equal(LineWidthRule.LineCollection.Resting, new OhlcBarSeries(y, y, y, y).LineWidth);
        Assert.Equal(LineWidthRule.OnePixelCollection.Resting, new EventplotSeries([[1.0]]).LineWidth);
        Assert.Equal(LineWidthRule.OnePixelCollection.Resting, new StreamplotSeries(x, y, new double[2, 2], new double[2, 2]).LineWidth);
        Assert.Equal(LineWidthRule.HalfPixelCollection.Resting, new WireframeSeries(x, y, new double[2, 2]).LineWidth);
    }

    [Fact]
    public void ACyclerThatNamesNoWidth_HandsOutTheRestingWidthOfALine()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red).Build();

        Assert.Equal(LineWidthRule.Line.Resting, cycler[0].LineWidth);
    }

    /// <summary>A line drawn with no slot (the lines of a contour's colour bar) asks the theme's line look, by the same rule.</summary>
    [Fact]
    public void TheThemesLineLook_AnswersForALineWithNoSlot()
    {
        Assert.Equal(1.5.PointsInPixels, Theme.MatplotlibV2.Looks.Lines.WidthOf(null, LineWidthRule.LineCollection));
        Assert.Equal(1.0, Theme.Default.Looks.Lines.WidthOf(null, LineWidthRule.OnePixelCollection));
    }

    /// <summary>The slot hands the rule its cycler's width and its theme's width; a kind that names no rule draws as a line.</summary>
    [Fact]
    public void TheSlot_HandsTheRuleItsCyclersAndItsThemesWidth()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red).WithLineWidths(4.0).Build();
        var theme = Theme.CreateFrom(Theme.MatplotlibV2).WithPropCycler(cycler).Build();
        var slot = theme.SeriesSlot(0);

        Assert.Equal(4.0, slot.LineWidthOf(LineWidthRule.Line));
        Assert.Equal(4.0, slot.LineWidthOf(null, LineWidthRule.Line));
        Assert.Equal(1.5.PointsInPixels, slot.LineWidthOf(null, LineWidthRule.OnePixelCollection));
        Assert.Equal(new StrokeStyle(Colors.Blue, 1.5.PointsInPixels, LineStyle.Solid), slot.StrokeOf(Colors.Blue, default, LineWidthRule.LineCollection));
        Assert.Equal(new StrokeStyle(Colors.Blue, 4.0, LineStyle.Solid), slot.StrokeOf(Colors.Blue, default(GivenStroke), LineWidthRule.Line));
    }

    /// <summary>A legend swatch that carries no rule is a line's; one that carries a rule is drawn by it.</summary>
    [Fact]
    public void ASwatch_DrawsByItsKindsRule_ElseByALines()
    {
        Assert.Equal(LineWidthRule.Line, new LegendSwatch(LegendSwatchKind.Line).LineRule);
        Assert.Equal(LineWidthRule.TwoPixelLine, new RegressionSeries([0.0, 1], [0.0, 1]).LegendSwatch.LineRule);
    }
}
