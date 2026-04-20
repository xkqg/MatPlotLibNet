// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>
/// Direct unit tests for <see cref="CartesianSignalsPart"/> (Phase B.9).
/// Covers Buy/Sell triangle orientation, color fallback, explicit color,
/// size customization, multiple signals.
/// </summary>
public class CartesianSignalsPartUnitTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static DataTransform Transform() => new(0, 10, 0, 10, Plot);

    private static CartesianSignalsPart Build(Axes axes, RecordingRenderContext ctx) =>
        new(axes, Plot, ctx, DefaultTheme, Transform());

    [Fact]
    public void Render_NoSignals_DoesNothing()
    {
        var ctx = new RecordingRenderContext();
        Build(new Axes(), ctx).Render();
        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void Render_BuySignal_DrawsUpwardTriangle()
    {
        var ax = new Axes();
        ax.AddSignal(5, 5, SignalDirection.Buy);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawPolygon"));
        var pts = (Point[])((dynamic)ctx.OfKind("DrawPolygon")[0].Args!).points;
        // Buy: tip is at pt.Y + size, base is below at pt.Y + size*2 (screen-Y-down)
        Assert.Equal(3, pts.Length);
        Assert.Equal(pts[0].X, (pts[1].X + pts[2].X) / 2, precision: 6);
        // Tip above base (smaller Y = above in screen-Y-down)
        Assert.True(pts[0].Y < pts[1].Y);
    }

    [Fact]
    public void Render_SellSignal_DrawsDownwardTriangle()
    {
        var ax = new Axes();
        ax.AddSignal(5, 5, SignalDirection.Sell);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var pts = (Point[])((dynamic)ctx.OfKind("DrawPolygon")[0].Args!).points;
        // Sell: tip is below pt.Y, base is above. Tip has LARGER Y (screen-Y-down).
        Assert.True(pts[0].Y > pts[1].Y);
    }

    [Fact]
    public void Render_BuySignalDefaultColor_IsGreen()
    {
        var ax = new Axes();
        ax.AddSignal(5, 5, SignalDirection.Buy);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var fill = (Color?)((dynamic)ctx.OfKind("DrawPolygon")[0].Args!).fill;
        Assert.Equal(Colors.Green, fill);
    }

    [Fact]
    public void Render_SellSignalDefaultColor_IsRed()
    {
        var ax = new Axes();
        ax.AddSignal(5, 5, SignalDirection.Sell);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var fill = (Color?)((dynamic)ctx.OfKind("DrawPolygon")[0].Args!).fill;
        Assert.Equal(Colors.Red, fill);
    }

    [Fact]
    public void Render_SignalWithExplicitColor_UsesIt()
    {
        var ax = new Axes();
        var m = ax.AddSignal(5, 5, SignalDirection.Buy);
        m.Color = Color.FromHex("#0000FF");
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var fill = (Color?)((dynamic)ctx.OfKind("DrawPolygon")[0].Args!).fill;
        Assert.Equal(Color.FromHex("#0000FF"), fill);
    }

    [Fact]
    public void Render_SignalWithCustomSize_TriangleReflectsSize()
    {
        var ax = new Axes();
        var m = ax.AddSignal(5, 5, SignalDirection.Buy);
        m.Size = 20;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var pts = (Point[])((dynamic)ctx.OfKind("DrawPolygon")[0].Args!).points;
        // Base width equals Size (20)
        Assert.Equal(20.0, pts[2].X - pts[1].X, precision: 6);
    }

    [Fact]
    public void Render_MultipleSignals_AllDrawn()
    {
        var ax = new Axes();
        ax.AddSignal(1, 1, SignalDirection.Buy);
        ax.AddSignal(3, 3, SignalDirection.Sell);
        ax.AddSignal(5, 5, SignalDirection.Buy);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(3, ctx.CountOf("DrawPolygon"));
    }
}
