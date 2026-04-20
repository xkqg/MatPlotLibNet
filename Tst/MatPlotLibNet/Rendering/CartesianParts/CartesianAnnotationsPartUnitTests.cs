// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>
/// Direct unit tests for <see cref="CartesianAnnotationsPart"/> extracted in
/// Phase B.4. Exercises every arrow/box/background/arrowhead branch by
/// capturing the calls against a <see cref="RecordingRenderContext"/>.
/// </summary>
public class CartesianAnnotationsPartUnitTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static DataTransform Transform() => new(0, 10, 0, 10, Plot);

    private static CartesianAnnotationsPart Build(Axes axes, RecordingRenderContext ctx) =>
        new(axes, Plot, ctx, DefaultTheme, Transform());

    // ──────────────────────────────────────────────────────────────────────────
    // Empty + text-only
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_NoAnnotations_DoesNothing()
    {
        var ctx = new RecordingRenderContext();
        Build(new Axes(), ctx).Render();
        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void Render_SingleTextAnnotation_DrawsTextOnly()
    {
        var ax = new Axes();
        ax.Annotate("Hello", 5, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawText"));
        Assert.Equal(0, ctx.CountOf("DrawRectangle"));
        Assert.Equal(0, ctx.CountOf("DrawPath"));
    }

    [Fact]
    public void Render_TextAnnotation_UsesAnnotationTextAndAlignment()
    {
        var ax = new Axes();
        var a = ax.Annotate("Label", 1, 2);
        a.Alignment = TextAlignment.Right;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var call = ctx.OfKind("DrawText")[0];
        Assert.Equal("Label", (string)((dynamic)call.Args!).text);
        Assert.Equal(TextAlignment.Right, (TextAlignment)((dynamic)call.Args!).alignment);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Font fallback
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_AnnotationWithExplicitFont_UsesIt()
    {
        var ax = new Axes();
        var a = ax.Annotate("Big", 5, 5);
        var customFont = new Font { Family = "Arial", Size = 30, Color = Color.FromHex("#123456") };
        a.Font = customFont;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var f = (Font)((dynamic)ctx.OfKind("DrawText")[0].Args!).font;
        Assert.Equal(30.0, f.Size);
    }

    [Fact]
    public void Render_AnnotationWithoutFont_UsesThemeDefault()
    {
        var ax = new Axes();
        ax.Annotate("Default", 5, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var f = (Font)((dynamic)ctx.OfKind("DrawText")[0].Args!).font;
        Assert.Equal(DefaultTheme.DefaultFont.Family, f.Family);
        Assert.Equal(10.0, f.Size);
    }

    [Fact]
    public void Render_AnnotationWithColor_FontUsesIt()
    {
        var ax = new Axes();
        var a = ax.Annotate("Colored", 5, 5);
        a.Color = Color.FromHex("#AABBCC");
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var f = (Font)((dynamic)ctx.OfKind("DrawText")[0].Args!).font;
        Assert.Equal(Color.FromHex("#AABBCC"), f.Color);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Box / background branches
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_BoxStyleRound_EmitsCalloutBoxPath()
    {
        var ax = new Axes();
        var a = ax.Annotate("Boxed", 5, 5);
        a.BoxStyle = BoxStyle.Round;
        a.BoxFaceColor = Color.FromHex("#112233");
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        // Callout box draws via DrawPath (rounded rect)
        Assert.True(ctx.CountOf("DrawPath") > 0 || ctx.CountOf("DrawRectangle") > 0);
    }

    [Fact]
    public void Render_BoxStyleNone_WithBackground_DrawsBackgroundRect()
    {
        var ax = new Axes();
        var a = ax.Annotate("Bg", 5, 5);
        a.BoxStyle = BoxStyle.None;
        a.BackgroundColor = Color.FromHex("#FFFF00");
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var rects = ctx.OfKind("DrawRectangle");
        Assert.Single(rects);
        Assert.Equal(Color.FromHex("#FFFF00"), (Color?)((dynamic)rects[0].Args!).fill);
    }

    [Fact]
    public void Render_BoxStyleNone_NoBackground_NoRectangle()
    {
        var ax = new Axes();
        ax.Annotate("Plain", 5, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawRectangle"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Arrow branches
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_ArrowWithTarget_EmitsConnectionPath()
    {
        var ax = new Axes();
        var a = ax.Annotate("WithArrow", 1, 1);
        a.ArrowTargetX = 5;
        a.ArrowTargetY = 5;
        a.ArrowStyle = ArrowStyle.Simple;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.True(ctx.CountOf("DrawPath") >= 1);
    }

    [Fact]
    public void Render_ArrowStyleFancyArrow_EmitsHeadPolygon()
    {
        // FancyArrow produces a polygon head — exercises the `headPolygon.Count > 0` TRUE branch.
        var ax = new Axes();
        var a = ax.Annotate("ArrowHead", 1, 1);
        a.ArrowTargetX = 5;
        a.ArrowTargetY = 5;
        a.ArrowStyle = ArrowStyle.FancyArrow;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.True(ctx.CountOf("DrawPolygon") >= 1);
    }

    [Fact]
    public void Render_ArrowStyleCurveA_EmitsHeadPath()
    {
        // CurveA produces a path head (no polygon) — exercises the `headPath is { Count: > 0 }` TRUE branch.
        var ax = new Axes();
        var a = ax.Annotate("CurveHead", 1, 1);
        a.ArrowTargetX = 5;
        a.ArrowTargetY = 5;
        a.ArrowStyle = ArrowStyle.CurveA;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        // Both connection path AND arrowhead path → 2 DrawPath calls.
        Assert.Equal(2, ctx.CountOf("DrawPath"));
    }

    [Fact]
    public void Render_ArrowStyleSimple_NoArrowheadShape()
    {
        // ArrowStyle.Simple → empty polygon AND null path (both FALSE branches of the shape-emit guards).
        var ax = new Axes();
        var a = ax.Annotate("Simple", 1, 1);
        a.ArrowTargetX = 5;
        a.ArrowTargetY = 5;
        a.ArrowStyle = ArrowStyle.Simple;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(1, ctx.CountOf("DrawPath"));    // connection path only
        Assert.Equal(0, ctx.CountOf("DrawPolygon")); // no polygon head
    }

    [Fact]
    public void Render_ArrowStyleNone_NoConnectionPath()
    {
        var ax = new Axes();
        var a = ax.Annotate("NoArrow", 1, 1);
        a.ArrowTargetX = 5;
        a.ArrowTargetY = 5;
        a.ArrowStyle = ArrowStyle.None;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawPath"));
    }

    [Fact]
    public void Render_ArrowMissingTargetX_NoPath()
    {
        var ax = new Axes();
        var a = ax.Annotate("NoX", 1, 1);
        a.ArrowTargetY = 5;
        a.ArrowStyle = ArrowStyle.Simple;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawPath"));
    }

    [Fact]
    public void Render_ArrowMissingTargetY_NoPath()
    {
        var ax = new Axes();
        var a = ax.Annotate("NoY", 1, 1);
        a.ArrowTargetX = 5;
        a.ArrowStyle = ArrowStyle.Simple;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawPath"));
    }

    [Fact]
    public void Render_ArrowTargetAtSameSpot_SkipsArrowhead()
    {
        // dx=0, dy=0 → len=0 → arrowhead skipped (the `if (len > 0)` branch false).
        var ax = new Axes();
        var a = ax.Annotate("SamePoint", 5, 5);
        a.ArrowTargetX = 5;
        a.ArrowTargetY = 5;
        a.ArrowStyle = ArrowStyle.Simple;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        // Connection path drawn, but no polygon arrowhead at target (because len = 0)
        Assert.Equal(1, ctx.CountOf("DrawPath"));
        Assert.Equal(0, ctx.CountOf("DrawPolygon"));
    }

    [Fact]
    public void Render_ArrowWithExplicitArrowColor_UsesIt()
    {
        var ax = new Axes();
        var a = ax.Annotate("Red arrow", 1, 1);
        a.ArrowTargetX = 5;
        a.ArrowTargetY = 5;
        a.ArrowStyle = ArrowStyle.Simple;
        a.ArrowColor = Color.FromHex("#FF0000");
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var path = ctx.OfKind("DrawPath")[0];
        Assert.Equal(Color.FromHex("#FF0000"), (Color?)((dynamic)path.Args!).stroke);
    }

    [Fact]
    public void Render_ArrowFallsBackToAnnotationColor()
    {
        var ax = new Axes();
        var a = ax.Annotate("Blue", 1, 1);
        a.ArrowTargetX = 5;
        a.ArrowTargetY = 5;
        a.ArrowStyle = ArrowStyle.Simple;
        a.Color = Color.FromHex("#0000FF");
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var path = ctx.OfKind("DrawPath")[0];
        Assert.Equal(Color.FromHex("#0000FF"), (Color?)((dynamic)path.Args!).stroke);
    }

    [Fact]
    public void Render_MultipleAnnotations_AllRendered()
    {
        var ax = new Axes();
        ax.Annotate("A", 1, 1);
        ax.Annotate("B", 5, 5);
        ax.Annotate("C", 8, 8);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(3, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void Render_AnnotationWithRotation_PassedThrough()
    {
        var ax = new Axes();
        var a = ax.Annotate("Rotated", 5, 5);
        a.Rotation = 45;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var call = ctx.OfKind("DrawText")[0];
        Assert.Equal(45.0, (double)((dynamic)call.Args!).rotation);
    }
}
