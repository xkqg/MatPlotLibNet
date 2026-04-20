// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>
/// Direct unit tests for <see cref="CartesianReferenceLinesPart"/> (Phase B.6).
/// Covers horizontal + vertical orientation, color fallback, label anchoring.
/// </summary>
public class CartesianReferenceLinesPartUnitTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static readonly DataRange Range = new(0, 10, 0, 10);
    private static DataTransform Transform() => new(0, 10, 0, 10, Plot);

    private static CartesianReferenceLinesPart Build(Axes axes, RecordingRenderContext ctx) =>
        new(axes, Plot, ctx, DefaultTheme, Transform(), Range);

    [Fact]
    public void Render_NoLines_DoesNothing()
    {
        var ctx = new RecordingRenderContext();
        Build(new Axes(), ctx).Render();
        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void Render_HorizontalLine_DrawsFullWidthLine()
    {
        var ax = new Axes();
        ax.AxHLine(5);
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
    public void Render_VerticalLine_DrawsFullHeightLine()
    {
        var ax = new Axes();
        ax.AxVLine(5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var line = ctx.OfKind("DrawLine")[0];
        var p1 = (Point)((dynamic)line.Args!).p1;
        var p2 = (Point)((dynamic)line.Args!).p2;
        Assert.Equal(p1.X, p2.X);
        Assert.Equal(Plot.Y, p1.Y);
        Assert.Equal(Plot.Y + Plot.Height, p2.Y);
    }

    [Fact]
    public void Render_LineWithColor_UsesIt()
    {
        var ax = new Axes();
        var l = ax.AxHLine(5);
        l.Color = Color.FromHex("#FF0000");
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(Color.FromHex("#FF0000"),
            (Color)((dynamic)ctx.OfKind("DrawLine")[0].Args!).color);
    }

    [Fact]
    public void Render_LineWithoutColor_FallsBackToGray()
    {
        var ax = new Axes();
        ax.AxVLine(5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(Colors.Gray,
            (Color)((dynamic)ctx.OfKind("DrawLine")[0].Args!).color);
    }

    [Fact]
    public void Render_HorizontalLineWithLabel_RightAligned()
    {
        var ax = new Axes();
        var l = ax.AxHLine(5);
        l.Label = "Threshold";
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawText"));
        Assert.Equal(TextAlignment.Right,
            (TextAlignment)((dynamic)ctx.OfKind("DrawText")[0].Args!).alignment);
    }

    [Fact]
    public void Render_VerticalLineWithLabel_LeftAligned()
    {
        var ax = new Axes();
        var l = ax.AxVLine(5);
        l.Label = "Mark";
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(TextAlignment.Left,
            (TextAlignment)((dynamic)ctx.OfKind("DrawText")[0].Args!).alignment);
    }

    [Fact]
    public void Render_HorizontalLine_NoLabel_NoText()
    {
        var ax = new Axes();
        ax.AxHLine(5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void Render_VerticalLine_NoLabel_NoText()
    {
        var ax = new Axes();
        ax.AxVLine(5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void Render_MultipleLines_AllRendered()
    {
        var ax = new Axes();
        ax.AxHLine(1);
        ax.AxVLine(2);
        ax.AxHLine(3);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(3, ctx.CountOf("DrawLine"));
    }
}
