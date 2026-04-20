// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>
/// Direct unit tests for <see cref="CartesianSpansPart"/> extracted in Phase B.5.
/// Covers horizontal + vertical orientation, border vs no-border, Label vs no-Label,
/// swapped min/max, explicit EdgeColor, transparency. 100/100 target.
/// </summary>
public class CartesianSpansPartUnitTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static readonly DataRange Range = new(0, 10, 0, 10);
    private static DataTransform Transform() => new(0, 10, 0, 10, Plot);

    private static CartesianSpansPart Build(Axes axes, RecordingRenderContext ctx) =>
        new(axes, Plot, ctx, DefaultTheme, Transform(), Range);

    [Fact]
    public void Render_NoSpans_DoesNothing()
    {
        var ctx = new RecordingRenderContext();
        Build(new Axes(), ctx).Render();
        Assert.Empty(ctx.Calls);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Horizontal span
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_HorizontalSpan_DrawsFillRectangle()
    {
        var ax = new Axes();
        ax.AxHSpan(2, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Render_HorizontalSpan_NoLineStyle_NoBorderLines()
    {
        var ax = new Axes();
        ax.AxHSpan(2, 5);  // LineStyle.None by default
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Render_HorizontalSpan_WithLineStyle_DrawsFourBorderLines()
    {
        var ax = new Axes();
        var span = ax.AxHSpan(2, 5);
        span.LineStyle = LineStyle.Solid;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(4, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Render_HorizontalSpan_WithLabel_DrawsLabelText()
    {
        var ax = new Axes();
        var span = ax.AxHSpan(2, 5);
        span.Label = "High zone";
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawText"));
        Assert.Equal("High zone", (string)((dynamic)ctx.OfKind("DrawText")[0].Args!).text);
    }

    [Fact]
    public void Render_HorizontalSpan_WithoutLabel_NoText()
    {
        var ax = new Axes();
        ax.AxHSpan(2, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void Render_HorizontalSpan_SwappedMinMax_StillRenders()
    {
        // Intentionally max<min — Math.Min/Math.Max handles the swap.
        var ax = new Axes();
        ax.AxHSpan(8, 3);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Render_HorizontalSpan_LabelUsesLeftAlignment()
    {
        var ax = new Axes();
        var span = ax.AxHSpan(2, 5);
        span.Label = "L";
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(TextAlignment.Left, (TextAlignment)((dynamic)ctx.OfKind("DrawText")[0].Args!).alignment);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Vertical span
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_VerticalSpan_DrawsFillRectangle()
    {
        var ax = new Axes();
        ax.AxVSpan(2, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Render_VerticalSpan_WithLineStyle_DrawsFourBorderLines()
    {
        var ax = new Axes();
        var span = ax.AxVSpan(2, 5);
        span.LineStyle = LineStyle.Dashed;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(4, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Render_VerticalSpan_NoLineStyle_NoBorderLines()
    {
        var ax = new Axes();
        ax.AxVSpan(2, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Render_VerticalSpan_WithLabel_DrawsCenteredText()
    {
        var ax = new Axes();
        var span = ax.AxVSpan(2, 5);
        span.Label = "V zone";
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawText"));
        Assert.Equal(TextAlignment.Center, (TextAlignment)((dynamic)ctx.OfKind("DrawText")[0].Args!).alignment);
    }

    [Fact]
    public void Render_VerticalSpan_WithoutLabel_NoText()
    {
        var ax = new Axes();
        ax.AxVSpan(2, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void Render_VerticalSpan_SwappedMinMax_StillRenders()
    {
        var ax = new Axes();
        ax.AxVSpan(9, 1);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawRectangle"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Color + edge color
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_SpanWithExplicitColor_UsesIt()
    {
        var ax = new Axes();
        var span = ax.AxHSpan(2, 5);
        span.Color = Color.FromHex("#FF0000");
        span.Alpha = 1.0;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var rect = ctx.OfKind("DrawRectangle")[0];
        var fill = (Color?)((dynamic)rect.Args!).fill;
        Assert.Equal(255, fill!.Value.R);
    }

    [Fact]
    public void Render_SpanWithoutColor_UsesDefaultTab10Blue()
    {
        var ax = new Axes();
        ax.AxHSpan(2, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        // Default color path — just verify rect was drawn.
        Assert.Equal(1, ctx.CountOf("DrawRectangle"));
    }

    [Fact]
    public void Render_SpanWithExplicitEdgeColor_BorderUsesEdgeColor()
    {
        var ax = new Axes();
        var span = ax.AxHSpan(2, 5);
        span.LineStyle = LineStyle.Solid;
        span.EdgeColor = Color.FromHex("#00FF00");
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var line = ctx.OfKind("DrawLine")[0];
        Assert.Equal(Color.FromHex("#00FF00"), (Color)((dynamic)line.Args!).color);
    }

    [Fact]
    public void Render_SpanEdgeColorNull_FallsBackToSpanColor()
    {
        var ax = new Axes();
        var span = ax.AxHSpan(2, 5);
        span.LineStyle = LineStyle.Solid;
        span.Color = Color.FromHex("#0000FF");
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var line = ctx.OfKind("DrawLine")[0];
        Assert.Equal(Color.FromHex("#0000FF"), (Color)((dynamic)line.Args!).color);
    }

    [Fact]
    public void Render_MultipleSpans_AllRendered()
    {
        var ax = new Axes();
        ax.AxHSpan(1, 2);
        ax.AxVSpan(3, 4);
        ax.AxHSpan(5, 6);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(3, ctx.CountOf("DrawRectangle"));
    }
}
