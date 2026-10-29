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

    [Theory]
    [InlineData("Default")]
    [InlineData("MatplotlibV2")]
    [InlineData("MatplotlibClassic")]
    [InlineData("Poster")]
    [InlineData("Paper")]
    public void Render_AnnotationWithoutFont_UsesThemeDefault(string themeName)
    {
        var theme = themeName switch
        {
            "MatplotlibV2" => Theme.MatplotlibV2,
            "MatplotlibClassic" => Theme.MatplotlibClassic,
            "Poster" => Theme.Poster,
            "Paper" => Theme.Paper,
            _ => Theme.Default,
        };
        var ax = new Axes();
        ax.Annotate("Default", 5, 5);
        var ctx = new RecordingRenderContext();
        new CartesianAnnotationsPart(ax, Plot, ctx, theme, Transform()).Render();
        var f = (Font)((dynamic)ctx.OfKind("DrawText")[0].Args!).font;
        Assert.Equal(theme.DefaultFont.Family, f.Family);
        Assert.Equal(theme.DefaultFont.Size, f.Size);
        Assert.Equal(theme.ForegroundText, f.Color);
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

    [Theory]
    [InlineData(TextAlignment.Left)]
    [InlineData(TextAlignment.Center)]
    [InlineData(TextAlignment.Right)]
    public void Render_ABackground_StandsAroundTheTextWhateverItsAlignment(TextAlignment alignment)
    {
        var ax = new Axes();
        var a = ax.Annotate("Backed", 5, 5);
        a.Alignment = alignment;
        a.BackgroundColor = Color.FromHex("#FFFF00");
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        var text = ctx.OfKind("DrawText")[0].Args!;
        var position = (Point)((dynamic)text).position;
        var width = ctx.MeasureText("Backed", (Font)((dynamic)text).font).Width;
        double left = alignment switch
        {
            TextAlignment.Center => position.X - width / 2,
            TextAlignment.Right => position.X - width,
            _ => position.X,
        };

        var rect = (Rect)((dynamic)ctx.OfKind("DrawRectangle")[0].Args!).rect;
        Assert.Equal(left - 2, rect.X, 9);
        Assert.Equal(width + 4, rect.Width, 9);
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

    // ──────────────────────────────────────────────────────────────────────────
    // Pixel offset and vertical alignment
    // ──────────────────────────────────────────────────────────────────────────

    private static Point TextPosition(RecordingRenderContext ctx) => (Point)((dynamic)ctx.OfKind("DrawText")[0].Args!).position;

    [Fact]
    public void Render_AnOffset_MovesTheTextThatManyPixelsFromItsAnchor()
    {
        var anchored = new Axes();
        anchored.Annotate("Moved", 5, 5);
        var plain = new RecordingRenderContext();
        Build(anchored, plain).Render();

        var moved = new Axes();
        var a = moved.Annotate("Moved", 5, 5);
        a.OffsetX = 12;
        a.OffsetY = -8;
        var ctx = new RecordingRenderContext();
        Build(moved, ctx).Render();

        Assert.Equal(TextPosition(plain).X + 12, TextPosition(ctx).X, 9);
        Assert.Equal(TextPosition(plain).Y - 8, TextPosition(ctx).Y, 9);
    }

    [Fact]
    public void Render_AnOffsetOnAPanelFractionAnnotation_IsAddedToThePanelPosition()
    {
        var ax = new Axes();
        var a = ax.Annotate("Panel", 0.5, 0.5);
        a.Coordinates = AnnotationCoordinates.AxesFraction;
        a.OffsetX = 6;
        a.OffsetY = 4;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        Assert.Equal(Plot.X + 0.5 * Plot.Width + 6, TextPosition(ctx).X, 9);
        Assert.Equal(Plot.Y + 0.5 * Plot.Height + 4, TextPosition(ctx).Y, 9);
    }

    [Fact]
    public void Render_AnOffsetAnnotation_StartsItsArrowAtTheMovedText_AndPointsAtTheSameTarget()
    {
        var ax = new Axes();
        var a = ax.Annotate("Arrowed", 1, 1);
        a.ArrowTargetX = 5;
        a.ArrowTargetY = 5;
        a.ArrowStyle = ArrowStyle.Simple;
        a.OffsetX = 20;
        a.OffsetY = 10;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        var segments = (PathSegment[])((dynamic)ctx.OfKind("DrawPath")[0].Args!).segments;
        var text = TextPosition(ctx);
        var anchor = Transform().DataToPixel(1, 1);
        Assert.Equal(anchor.X + 20, text.X, 9);
        Assert.Equal(anchor.Y + 10, text.Y, 9);
        var start = Assert.IsType<MoveToSegment>(segments[0]).Point;
        Assert.Equal(text.X, start.X, 9);
        Assert.Equal(text.Y, start.Y, 9);
        var target = Transform().DataToPixel(5, 5);
        Assert.Equal(target, Assert.IsType<LineToSegment>(segments[1]).Point);
    }

    [Fact]
    public void Render_AnOffsetAnnotation_MovesItsBackgroundWithIt()
    {
        var ax = new Axes();
        var a = ax.Annotate("Backed", 5, 5);
        a.BackgroundColor = Color.FromHex("#FFFF00");
        a.OffsetX = 30;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        var rect = (Rect)((dynamic)ctx.OfKind("DrawRectangle")[0].Args!).rect;
        Assert.Equal(Transform().DataToPixel(5, 5).X + 30, TextPosition(ctx).X, 9);
        Assert.Equal(TextPosition(ctx).X - 2, rect.X, 9);
    }

    [Fact]
    public void Render_AnAnnotationAtAnAnchorThatCannotBePlaced_IsSkippedWhole_WhateverItsOffset()
    {
        var ax = new Axes();
        var a = ax.Annotate("Lost", double.NaN, 5);
        a.OffsetX = 12;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void Render_ANotAnumberOffset_SkipsTheAnnotationRatherThanDrawingItNowhere()
    {
        var ax = new Axes();
        var a = ax.Annotate("Nowhere", 5, 5);
        a.OffsetY = double.NaN;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void Render_NoOffset_DrawsTheTextAtTheAnchorExactly()
    {
        var ax = new Axes();
        ax.Annotate("Here", 5, 5);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        Assert.Equal(Transform().DataToPixel(5, 5), TextPosition(ctx));
    }

    [Theory]
    [InlineData(TextVerticalAlignment.Baseline, 0.0)]
    [InlineData(TextVerticalAlignment.Center, 0.35 * 13.0)]   // a third of the size and a half point below the anchor
    [InlineData(TextVerticalAlignment.Top, 13.0)]             // the text hangs from the anchor: its baseline an ascent below
    [InlineData(TextVerticalAlignment.Bottom, 13.0 - 13.0)]   // the measured height equals the size here, so no descent
    public void Render_TheVerticalAlignment_MovesTheBaselineFromTheAnchor(TextVerticalAlignment vertical, double drop)
    {
        var ax = new Axes();
        var a = ax.Annotate("Levelled", 5, 5);
        a.VerticalAlignment = vertical;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        Assert.Equal(Transform().DataToPixel(5, 5).Y + drop, TextPosition(ctx).Y, 9);
    }

    [Fact]
    public void Render_ACentredBackground_StandsAroundTheText_NotAroundTheAnchor()
    {
        var ax = new Axes();
        var a = ax.Annotate("Backed", 5, 5);
        a.VerticalAlignment = TextVerticalAlignment.Center;
        a.BackgroundColor = Color.FromHex("#FFFF00");
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();

        var rect = (Rect)((dynamic)ctx.OfKind("DrawRectangle")[0].Args!).rect;
        var text = TextPosition(ctx);
        Assert.Equal(text.Y - 13.0, rect.Y, 9);   // the rule the box has always had: from a text height above the baseline
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

    // ──────────────────────────────────────────────────────────────────────────
    // Annotations that move out of the way
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>A mark exactly where the annotation 'Mover' stands at data (5, 5): its anchor and its text.</summary>
    private static MarkObstacles MarkUnderTheAnchor(RecordingRenderContext measure)
    {
        var anchor = Transform().DataToPixel(5, 5);
        var font = ThemedFontProvider.DataTextFont(DefaultTheme);
        var box = new TextBox(anchor, measure.MeasureText("Mover", font), font.Size, TextAlignment.Left,
            TextVerticalAlignment.Baseline, 0).Bounds();
        return new MarkObstacles.Builder().AddBox(box).Build();
    }

    private static CartesianAnnotationsPart WithObstacles(Axes axes, RecordingRenderContext ctx, MarkObstacles obstacles) =>
        new(axes, Plot, ctx, DefaultTheme, Transform()) { Obstacles = obstacles };

    [Fact]
    public void Render_AnAnnotationThatAvoidsOverlap_StandsOffAMarkThatIsUnderItsAnchor()
    {
        var ax = new Axes();
        ax.Annotate("Mover", 5, 5).AvoidOverlap = true;
        var ctx = new RecordingRenderContext();
        WithObstacles(ax, ctx, MarkUnderTheAnchor(ctx)).Render();

        Assert.NotEqual(Transform().DataToPixel(5, 5), TextPosition(ctx));
    }

    [Fact]
    public void Render_AnAnnotationThatDoesNotAskToMove_StaysOverTheMark()
    {
        var ax = new Axes();
        ax.Annotate("Mover", 5, 5);
        var ctx = new RecordingRenderContext();
        WithObstacles(ax, ctx, MarkUnderTheAnchor(ctx)).Render();

        Assert.Equal(Transform().DataToPixel(5, 5), TextPosition(ctx));
    }

    [Fact]
    public void Render_AnAnnotationThatAvoidsOverlapOverNothing_IsDrawnExactlyAtItsAnchor()
    {
        var ax = new Axes();
        ax.Annotate("Mover", 5, 5).AvoidOverlap = true;
        var ctx = new RecordingRenderContext();
        WithObstacles(ax, ctx, MarkObstacles.None).Render();

        Assert.Equal(Transform().DataToPixel(5, 5), TextPosition(ctx));
        Assert.Equal(0, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void Render_AMovedAnnotation_IsJoinedToItsAnchorByALeaderLine()
    {
        var ax = new Axes();
        ax.Annotate("Mover", 5, 5).AvoidOverlap = true;
        var ctx = new RecordingRenderContext();
        WithObstacles(ax, ctx, MarkUnderTheAnchor(ctx)).Render();

        var leaders = ctx.OfKind("DrawLine");
        Assert.True(leaders.Count == 1 || ctx.CountOf("DrawPath") >= 1, "the moved text has no line back to its anchor");
    }

    [Fact]
    public void Render_AMovedAnnotationWithAnArrow_NeedsNoLeader_ItsArrowStartsAtTheMovedText()
    {
        var ax = new Axes();
        var a = ax.Annotate("Mover", 5, 5);
        a.AvoidOverlap = true;
        a.ArrowTargetX = 8;
        a.ArrowTargetY = 8;
        a.ArrowStyle = ArrowStyle.Simple;
        var ctx = new RecordingRenderContext();
        WithObstacles(ax, ctx, MarkUnderTheAnchor(ctx)).Render();

        Assert.Equal(0, ctx.CountOf("DrawLine"));
        var segments = (PathSegment[])((dynamic)ctx.OfKind("DrawPath")[0].Args!).segments;
        var start = Assert.IsType<MoveToSegment>(segments[0]).Point;
        Assert.Equal(TextPosition(ctx), start);
        Assert.NotEqual(Transform().DataToPixel(5, 5), start);
    }

    [Fact]
    public void Render_ATextThatStays_IsLeftWhereItIs_WhileAnotherMovesOutOfItsWay()
    {
        var ax = new Axes();
        ax.Annotate("Stayer", 2, 2);
        ax.Annotate("Mover", 5, 5).AvoidOverlap = true;
        var ctx = new RecordingRenderContext();
        WithObstacles(ax, ctx, MarkUnderTheAnchor(ctx)).Render();

        var positions = ctx.OfKind("DrawText").ToDictionary(c => (string)((dynamic)c.Args!).text, c => (Point)((dynamic)c.Args!).position);
        Assert.Equal(Transform().DataToPixel(2, 2), positions["Stayer"]);
        Assert.NotEqual(Transform().DataToPixel(5, 5), positions["Mover"]);
    }

    [Fact]
    public void Render_ATextThatMovesOutOfTheWayOfAStayer_KeepsClearOfIt()
    {
        var ax = new Axes();
        ax.Annotate("Mover", 5, 5).AvoidOverlap = true;
        ax.Annotate("Mover", 5, 5);   // the same text, put on the same point, and it stays
        var ctx = new RecordingRenderContext();
        WithObstacles(ax, ctx, MarkObstacles.None).Render();

        var positions = ctx.OfKind("DrawText").Select(c => (Point)((dynamic)c.Args!).position).ToList();
        Assert.Equal(2, positions.Count);
        Assert.NotEqual(positions[0], positions[1]);
    }

    [Fact]
    public void Render_ALeaderLine_IsDrawnInTheAnnotationsColour()
    {
        var ax = new Axes();
        var a = ax.Annotate("Mover", 5, 5);
        a.AvoidOverlap = true;
        a.Color = Color.FromHex("#AA3300");
        var ctx = new RecordingRenderContext();
        WithObstacles(ax, ctx, MarkUnderTheAnchor(ctx)).Render();

        var leader = Assert.Single(ctx.OfKind("DrawLine"));
        Assert.Equal(Color.FromHex("#AA3300"), (Color)((dynamic)leader.Args!).color);
    }

    [Fact]
    public void Render_ALeaderLine_IsDrawnInTheThemesTextColour_WhenTheAnnotationNamesNone()
    {
        var ax = new Axes();
        ax.Annotate("Mover", 5, 5).AvoidOverlap = true;
        var ctx = new RecordingRenderContext();
        WithObstacles(ax, ctx, MarkUnderTheAnchor(ctx)).Render();

        var leader = Assert.Single(ctx.OfKind("DrawLine"));
        Assert.Equal(DefaultTheme.ForegroundText, (Color)((dynamic)leader.Args!).color);
    }
}
