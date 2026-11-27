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
public class CartesianSignalsPartTests
{
    private static RecordingRenderContext Drawn(Axes axes) => PartFactory.Drawn(axes, PartFactory.Signals);

    [Fact]
    public void Render_BuySignal_DrawsUpwardTriangle()
    {
        var ax = new Axes();
        ax.AddSignal(5, 5, SignalDirection.Buy);
        var ctx = Drawn(ax);
        Assert.Equal(1, ctx.Count<DrawPolygonCall>());
        var pts = ctx.Of<DrawPolygonCall>()[0].Points;
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
        var ctx = Drawn(ax);
        var pts = ctx.Of<DrawPolygonCall>()[0].Points;
        // Sell: tip is below pt.Y, base is above. Tip has LARGER Y (screen-Y-down).
        Assert.True(pts[0].Y > pts[1].Y);
    }

    /// <summary>The colour a signal wears when it was given none.</summary>
    public static TheoryData<SignalDirection, Color> DefaultColours { get; } = new()
    {
        { SignalDirection.Buy, Colors.Green },
        { SignalDirection.Sell, Colors.Red },
    };

    [Theory]
    [MemberData(nameof(DefaultColours))]
    public void Render_SignalDefaultColor_IsGreenForABuyAndRedForASell(SignalDirection direction, Color expected)
    {
        var ax = new Axes();
        ax.AddSignal(5, 5, direction);
        var ctx = Drawn(ax);
        var fill = ctx.Of<DrawPolygonCall>()[0].Fill;
        Assert.Equal(expected, fill);
    }

    [Fact]
    public void Render_SignalWithExplicitColor_UsesIt()
    {
        var ax = new Axes();
        var m = ax.AddSignal(5, 5, SignalDirection.Buy);
        m.Color = Color.FromHex("#0000FF");
        var ctx = Drawn(ax);
        var fill = ctx.Of<DrawPolygonCall>()[0].Fill;
        Assert.Equal(Color.FromHex("#0000FF"), fill);
    }

    [Fact]
    public void Render_SignalWithCustomSize_TriangleReflectsSize()
    {
        var ax = new Axes();
        var m = ax.AddSignal(5, 5, SignalDirection.Buy);
        m.Size = 20;
        var ctx = Drawn(ax);
        var pts = ctx.Of<DrawPolygonCall>()[0].Points;
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
        var ctx = Drawn(ax);
        Assert.Equal(3, ctx.Count<DrawPolygonCall>());
    }
}
