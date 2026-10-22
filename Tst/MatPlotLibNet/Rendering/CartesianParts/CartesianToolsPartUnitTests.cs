// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>Unit tests for <see cref="CartesianToolsPart"/>.</summary>
public class CartesianToolsPartUnitTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static DataTransform Transform() => new(0, 10, 0, 10, Plot);

    private static CartesianToolsPart Build(Axes axes, RecordingRenderContext ctx) =>
        new(axes, Plot, ctx, DefaultTheme, Transform());

    // ── Empty axes ────────────────────────────────────────────────────────────

    [Fact]
    public void Render_NoTools_DoesNothing()
    {
        var ctx = new RecordingRenderContext();
        Build(new Axes(), ctx).Render();
        Assert.Empty(ctx.Calls);
    }

    // ── Trendline ─────────────────────────────────────────────────────────────

    [Fact]
    public void Render_Trendline_DrawsOneLine()
    {
        var ax = new Axes();
        ax.AddTrendline(2, 2, 8, 8);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Render_Trendline_WithColor_UsesIt()
    {
        var ax = new Axes();
        var t = ax.AddTrendline(0, 0, 5, 5);
        t.Color = Color.FromHex("#0000FF");
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(Color.FromHex("#0000FF"),
            (Color)((dynamic)ctx.OfKind("DrawLine")[0].Args!).color);
    }

    [Fact]
    public void Render_Trendline_NoColor_FallsBackToGray()
    {
        var ax = new Axes();
        ax.AddTrendline(0, 0, 5, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(Colors.Gray,
            (Color)((dynamic)ctx.OfKind("DrawLine")[0].Args!).color);
    }

    [Fact]
    public void Render_Trendline_WithLabel_DrawsText()
    {
        var ax = new Axes();
        var t = ax.AddTrendline(0, 0, 5, 5);
        t.Label = "Trend";
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void Render_MultipleTrendlines_AllDrawn()
    {
        var ax = new Axes();
        ax.AddTrendline(0, 0, 5, 5);
        ax.AddTrendline(0, 5, 5, 0);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(2, ctx.CountOf("DrawLine"));
    }

    // ── HorizontalLevel ───────────────────────────────────────────────────────

    [Fact]
    public void Render_HorizontalLevel_DrawsFullWidthLine()
    {
        var ax = new Axes();
        ax.AddLevel(5.0);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawLine"));
        var line = ctx.OfKind("DrawLine")[0];
        var p1 = (Point)((dynamic)line.Args!).p1;
        var p2 = (Point)((dynamic)line.Args!).p2;
        Assert.Equal(Plot.X, p1.X);
        Assert.Equal(Plot.X + Plot.Width, p2.X);
        Assert.Equal(p1.Y, p2.Y);
    }

    [Fact]
    public void Render_HorizontalLevel_WithLabel_RightAligned()
    {
        var ax = new Axes();
        var l = ax.AddLevel(5.0);
        l.Label = "Support";
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawText"));
        Assert.Equal(TextAlignment.Right,
            (TextAlignment)((dynamic)ctx.OfKind("DrawText")[0].Args!).alignment);
    }

    // ── FibonacciRetracement ──────────────────────────────────────────────────

    [Fact]
    public void Render_FibonacciRetracement_DrawsSevenLines()
    {
        var ax = new Axes();
        ax.AddFibonacci(priceHigh: 8.0, priceLow: 2.0);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(7, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Render_FibonacciRetracement_ShowLabels_DrawsSevenTexts()
    {
        var ax = new Axes();
        ax.AddFibonacci(priceHigh: 8.0, priceLow: 2.0);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(7, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void Render_FibonacciRetracement_HideLabels_NoText()
    {
        var ax = new Axes();
        var fib = ax.AddFibonacci(priceHigh: 8.0, priceLow: 2.0);
        fib.ShowLabels = false;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void Render_Trendline_Extended_DrawsLine()
    {
        var ax = new Axes();
        var t = ax.AddTrendline(2, 2, 8, 8);
        t.IsExtended = true;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawLine"));
    }

    // ── Trendline: kept inside the plot area ─────────────────────────────────────

    /// <summary>The line the context was asked to draw.</summary>
    private static LineSegment LineOf(RecordingRenderContext ctx)
    {
        var args = (dynamic)ctx.OfKind("DrawLine")[0].Args!;
        return new LineSegment((Point)args.p1, (Point)args.p2);
    }

    /// <summary>A trendline inside the plot area is drawn between its two points, exactly: no end is recomputed.</summary>
    [Fact]
    public void Render_TrendlineInside_IsDrawnBetweenItsOwnPoints()
    {
        var ax = new Axes();
        ax.AddTrendline(2, 3, 8, 7);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        var (p1, p2) = LineOf(ctx);
        Assert.Equal(Transform().DataToPixel(2, 3), p1);
        Assert.Equal(Transform().DataToPixel(8, 7), p2);
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
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        var (p1, p2) = LineOf(ctx);
        Assert.Equal(new Point(0, 300), p1);
        Assert.Equal(200, p2.X, 9);
        Assert.Equal(0, p2.Y, 9);
        var text = (dynamic)ctx.OfKind("DrawText")[0].Args!;
        Assert.Equal(p2, (Point)text.position);
    }

    /// <summary>A trendline wholly outside the plot area draws nothing, its label included.</summary>
    [Fact]
    public void Render_TrendlineOutsideThePlot_DrawsNothing()
    {
        var ax = new Axes();
        var t = ax.AddTrendline(0, 12, 10, 15);
        t.Label = "Above";
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        Assert.Equal(0, ctx.CountOf("DrawLine"));
        Assert.Equal(0, ctx.CountOf("DrawText"));
    }

    /// <summary>An extended trendline steeper than the range runs across the plot from the bottom edge to the top edge, not
    /// from the left edge to the right edge far above and below the plot.</summary>
    [Fact]
    public void Render_ExtendedTrendline_SteeperThanTheRange_RunsFromTheBottomEdgeToTheTopEdge()
    {
        var ax = new Axes();
        var t = ax.AddTrendline(2, 2, 3, 4);     // y = 2x - 2: y = 0 at x = 1, y = 10 at x = 6
        t.IsExtended = true;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        var (p1, p2) = LineOf(ctx);
        Assert.Equal(40, p1.X, 9);
        Assert.Equal(300, p1.Y, 9);
        Assert.Equal(240, p2.X, 9);
        Assert.Equal(0, p2.Y, 9);
    }

    [Fact]
    public void Render_MixedTools_AllRendered()
    {
        var ax = new Axes();
        ax.AddTrendline(0, 0, 5, 5);  // 1 line
        ax.AddLevel(5.0);              // 1 line
        ax.AddFibonacci(8.0, 2.0);    // 7 lines
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(9, ctx.CountOf("DrawLine"));
    }
}
