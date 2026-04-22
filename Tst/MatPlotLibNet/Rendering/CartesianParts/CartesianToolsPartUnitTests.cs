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
