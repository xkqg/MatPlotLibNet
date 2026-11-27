// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>Unit tests for <see cref="CartesianToolsPart"/>.</summary>
public class CartesianToolsPartTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = PartFactory.Plot;

    private static RecordingRenderContext Drawn(Axes axes) => PartFactory.Drawn(axes, PartFactory.Tools);

    // ── Trendline ─────────────────────────────────────────────────────────────

    /// <summary>What the drawing tools of an axes draw: how many calls of a kind the part makes for the axes the row arranges.</summary>
    public static TheoryData<Action<Axes>, Type, int> Counts { get; } = new()
    {
        { ax => ax.AddTrendline(2, 2, 8, 8), typeof(DrawLineCall), 1 }, // Render_Trendline_DrawsOneLine
        { ax =>
        {
            var t = ax.AddTrendline(0, 0, 5, 5);
            t.Label = "Trend";
        }, typeof(DrawTextCall), 1 }, // Render_Trendline_WithLabel_DrawsText
        { ax =>
        {
            ax.AddTrendline(0, 0, 5, 5);
            ax.AddTrendline(0, 5, 5, 0);
        }, typeof(DrawLineCall), 2 }, // Render_MultipleTrendlines_AllDrawn
        { ax => ax.AddFibonacci(priceHigh: 8.0, priceLow: 2.0), typeof(DrawLineCall), 7 }, // Render_FibonacciRetracement_DrawsSevenLines
        { ax => ax.AddFibonacci(priceHigh: 8.0, priceLow: 2.0), typeof(DrawTextCall), 7 }, // Render_FibonacciRetracement_ShowLabels_DrawsSevenTexts
        { ax =>
        {
            var fib = ax.AddFibonacci(priceHigh: 8.0, priceLow: 2.0);
            fib.ShowLabels = false;
        }, typeof(DrawTextCall), 0 }, // Render_FibonacciRetracement_HideLabels_NoText
        { ax =>
        {
            var t = ax.AddTrendline(2, 2, 8, 8);
            t.IsExtended = true;
        }, typeof(DrawLineCall), 1 }, // Render_Trendline_Extended_DrawsLine
        //  1 line
        //  1 line
        //  7 lines
        { ax =>
        {
            ax.AddTrendline(0, 0, 5, 5);
            ax.AddLevel(5.0);
            ax.AddFibonacci(8.0, 2.0);
        }, typeof(DrawLineCall), 9 }, // Render_MixedTools_AllRendered
    };

    [Theory]
    [MemberData(nameof(Counts))]
    public void Render_DrawsAsManyCallsOfAKindAsTheAxesAsks(Action<Axes> arrange, Type call, int expected) =>
        PartFactory.AssertCallsDrawn(arrange, Drawn, call, expected);

    [Fact]
    public void Render_Trendline_WithColor_UsesIt()
    {
        var ax = new Axes();
        var t = ax.AddTrendline(0, 0, 5, 5);
        t.Color = Color.FromHex("#0000FF");
        var ctx = Drawn(ax);
        Assert.Equal(Color.FromHex("#0000FF"),
            ctx.Of<DrawLineCall>()[0].Color);
    }

    [Fact]
    public void Render_Trendline_NoColor_FallsBackToTheThemesNeutral()
    {
        var ax = new Axes();
        ax.AddTrendline(0, 0, 5, 5);
        var ctx = Drawn(ax);
        Assert.Equal(DefaultTheme.Neutral,
            ctx.Of<DrawLineCall>()[0].Color);
        Assert.Equal(Colors.Gray, DefaultTheme.Neutral);
    }

    /// <summary>A trendline, a level and a Fibonacci retracement without a colour of their own all wear Nord's neutral.</summary>
    [Fact]
    public void Render_ToolsWithoutColour_UnderNord_WearNordsNeutral()
    {
        var ax = new Axes();
        ax.AddTrendline(0, 0, 5, 5);
        ax.AddLevel(2);
        ax.AddFibonacci(5, 1);
        var ctx = CartesianParts.PartFactory.Drawn(c => CartesianParts.PartFactory.Tools(ax, Plot, c, Theme.Nord, Plot.Linear()));
        Assert.All(ctx.Of<DrawLineCall>(), call => Assert.Equal(Color.FromHex("#8D8D8D"), call.Color));
        Assert.True(ctx.Count<DrawLineCall>() >= 3);
    }

    // ── HorizontalLevel ───────────────────────────────────────────────────────

    // ── Trendline: kept inside the plot area ─────────────────────────────────────

    /// <summary>The line the context was asked to draw.</summary>
    private static LineSegment LineOf(RecordingRenderContext ctx)
    {
        var args = ctx.Of<DrawLineCall>()[0];
        return new LineSegment(args.P1, args.P2);
    }

    /// <summary>A trendline inside the plot area is drawn between its two points, exactly: no end is recomputed.</summary>
    [Fact]
    public void Render_TrendlineInside_IsDrawnBetweenItsOwnPoints()
    {
        var ax = new Axes();
        ax.AddTrendline(2, 3, 8, 7);
        var ctx = Drawn(ax);

        var (p1, p2) = LineOf(ctx);
        Assert.Equal(Plot.Linear().DataToPixel(2, 3), p1);
        Assert.Equal(Plot.Linear().DataToPixel(8, 7), p2);
    }

    /// <summary>A trendline that runs out of the range the axes draw is cut at the plot edge, as matplotlib clips a line to
    /// the axes: before, it was drawn on over the margin, the tick labels and the title. Its label stands at the end that is
    /// drawn.</summary>
    [Fact]
    public void Render_TrendlinePastTheTop_IsCutAtThePlotEdge_ItsLabelAtTheDrawnEnd()
    {
        var ax = new Axes();
        var t = ax.AddTrendline(0, 0, 10, 20);   // y = 2x: it leaves the top (y = 10) at x = 5
        t.Label = "Trend";
        var ctx = Drawn(ax);

        var (p1, p2) = LineOf(ctx);
        Assert.Equal(new Point(0, 300), p1);
        Assert.Equal(200, p2.X, 9);
        Assert.Equal(0, p2.Y, 9);
        var text = ctx.Of<DrawTextCall>()[0];
        Assert.Equal(p2, text.Position);
    }

    /// <summary>A trendline wholly outside the plot area draws nothing, its label included.</summary>
    [Fact]
    public void Render_TrendlineOutsideThePlot_DrawsNothing()
    {
        var ax = new Axes();
        var t = ax.AddTrendline(0, 12, 10, 15);
        t.Label = "Above";
        var ctx = Drawn(ax);

        Assert.Equal(0, ctx.Count<DrawLineCall>());
        Assert.Equal(0, ctx.Count<DrawTextCall>());
    }

    /// <summary>An extended trendline steeper than the range runs across the plot from the bottom edge to the top edge, not
    /// from the left edge to the right edge far above and below the plot.</summary>
    [Fact]
    public void Render_ExtendedTrendline_SteeperThanTheRange_RunsFromTheBottomEdgeToTheTopEdge()
    {
        var ax = new Axes();
        var t = ax.AddTrendline(2, 2, 3, 4);     // y = 2x - 2: y = 0 at x = 1, y = 10 at x = 6
        t.IsExtended = true;
        var ctx = Drawn(ax);

        var (p1, p2) = LineOf(ctx);
        Assert.Equal(40, p1.X, 9);
        Assert.Equal(300, p1.Y, 9);
        Assert.Equal(240, p2.X, 9);
        Assert.Equal(0, p2.Y, 9);
    }

    // ── Trendline: a value a log axis cannot place, and the extended line as matplotlib's axline ─────────────────────

    /// <summary>A trendline to 0 on a log Y axis dives to the bottom edge, as matplotlib draws <c>ax.plot([1, 3], [10, 0])</c>:
    /// its log scale clips 0 (<c>nonpositive='clip'</c>) a thousand decades below the axis and the axes cut the line there.
    /// matplotlib 3.11.2, 400x300 px axes, y 1..100 log, x 0..10, pixel y down: vertices [[40, 150], [120, 150300]]; the cut on
    /// the bottom edge is x = 40 + 80 * 150 / 150150 = 40.0799. The library skipped the line and its label.</summary>
    [Fact]
    public void Render_TrendlineToZero_OnALogAxis_DivesToTheBottomEdge()
    {
        var ax = new Axes();
        var t = ax.AddTrendline(1, 10, 3, 0);
        t.Label = "Trend";
        var ctx = CartesianParts.PartFactory.Drawn(c => CartesianParts.PartFactory.Tools(ax, Plot, c, DefaultTheme, Plot.LogY()));

        var (p1, p2) = LineOf(ctx);
        (new Point(40, 150)).AssertCloseTo(p1, 1e-3);
        (new Point(40.0799, 300)).AssertCloseTo(p2, 1e-3);
        Assert.Equal(p2, ctx.Of<DrawTextCall>()[0].Position);
    }

    /// <summary>An extended trendline is matplotlib's <c>axline</c>: the straight line through its two points as the axes draw them, run
    /// across the view. Through a point at 0 on a log Y axis it stands almost upright (matplotlib 3.11.2, <c>ax.axline((1, 10), (3, 0))</c>
    /// on a log Y axis, drawn path in pixels with y down: [[39.9201, 0], [40.0799, 300]]; the library extended the line in data units, met a
    /// value below 0 at the left edge and skipped it). On a log axis it is straight through its own two points as drawn, as <c>axline</c>
    /// runs it in the scaled space (<c>ax.axline((1, 10), (3, 1000))</c>: [[0, 300], [80, 0]]; extended in data units it missed both). Through
    /// two points of one X it is upright across the whole plot, as <c>axline</c> draws one of infinite slope (<c>ax.axline((5, 1), (5, 4))</c>
    /// on axes 0..10 by 0..10, 400x300 px: [[200, 300], [200, 0]]; the library divided by the zero run and skipped it).</summary>
    [Theory]
    [InlineData(1, 10, 3, 0, true, 39.9201, 0, 40.0799, 300)]
    [InlineData(1, 10, 3, 1000, true, 0, 300, 80, 0)]
    [InlineData(5, 1, 5, 4, false, 200, 300, 200, 0)]
    public void Render_ExtendedTrendline_IsMatplotlibsAxline(double x1, double y1, double x2, double y2, bool logY,
        double fromX, double fromY, double toX, double toY)
    {
        var ax = new Axes();
        ax.AddTrendline(x1, y1, x2, y2).IsExtended = true;
        var ctx = logY ? CartesianParts.PartFactory.Drawn(c => CartesianParts.PartFactory.Tools(ax, Plot, c, DefaultTheme, Plot.LogY())) : Drawn(ax);

        var (p1, p2) = LineOf(ctx);
        (new Point(fromX, fromY)).AssertCloseTo(p1, 1e-3);
        (new Point(toX, toY)).AssertCloseTo(p2, 1e-3);
    }

    /// <summary>An extended trendline runs from its left end whichever point was given first, as <c>axline</c> starts it there,
    /// so its label stands at the right end; an upright one from its lower end.</summary>
    [Theory]
    [InlineData(8, 8, 2, 2, 0, 300, 400, 0)]
    [InlineData(5, 4, 5, 1, 200, 300, 200, 0)]
    public void Render_ExtendedTrendline_RunsFromItsLeftOrLowerEnd(double x1, double y1, double x2, double y2,
        double fromX, double fromY, double toX, double toY)
    {
        var ax = new Axes();
        var t = ax.AddTrendline(x1, y1, x2, y2);
        t.IsExtended = true;
        t.Label = "Trend";
        var ctx = Drawn(ax);

        var (p1, p2) = LineOf(ctx);
        (new Point(fromX, fromY)).AssertCloseTo(p1, 1e-3);
        (new Point(toX, toY)).AssertCloseTo(p2, 1e-3);
        Assert.Equal(p2, ctx.Of<DrawTextCall>()[0].Position);
    }

    /// <summary>An extended trendline through one point twice has no direction and draws nothing (matplotlib's <c>axline</c>
    /// refuses it: "Cannot draw a line through two identical points").</summary>
    [Fact]
    public void Render_ExtendedTrendline_ThroughOnePointTwice_DrawsNothing()
    {
        var ax = new Axes();
        var t = ax.AddTrendline(5, 5, 5, 5);
        t.IsExtended = true;
        t.Label = "None";
        var ctx = Drawn(ax);

        Assert.Equal(0, ctx.Count<DrawLineCall>());
        Assert.Equal(0, ctx.Count<DrawTextCall>());
    }
}
