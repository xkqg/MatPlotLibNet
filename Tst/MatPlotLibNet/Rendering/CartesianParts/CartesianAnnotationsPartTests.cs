// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>
/// Direct unit tests for <see cref="CartesianAnnotationsPart"/> extracted in
/// Phase B.4. Exercises every arrow/box/background/arrowhead branch by
/// capturing the calls against a <see cref="RecordingRenderContext"/>.
/// </summary>
public class CartesianAnnotationsPartTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static CartesianAnnotationsPart Build(Axes axes, RecordingRenderContext ctx) =>
        CartesianParts.PartFactory.Annotations(axes, Plot, ctx, DefaultTheme, Plot.Linear());

    private static RecordingRenderContext Drawn(Axes axes) => CartesianParts.PartFactory.Drawn(ctx => Build(axes, ctx));

    // ──────────────────────────────────────────────────────────────────────────
    // Empty + text-only
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_SingleTextAnnotation_DrawsTextOnly()
    {
        var ax = new Axes();
        ax.Annotate("Hello", 5, 5);
        var ctx = Drawn(ax);
        Assert.Equal(1, ctx.Count<DrawTextCall>());
        Assert.Equal(0, ctx.Count<DrawRectangleCall>());
        Assert.Equal(0, ctx.Count<DrawPathCall>());
    }

    [Fact]
    public void Render_TextAnnotation_UsesAnnotationTextAndAlignment()
    {
        var ax = new Axes();
        var a = ax.Annotate("Label", 1, 2);
        a.Alignment = TextAlignment.Right;
        var ctx = Drawn(ax);
        var call = ctx.Of<DrawTextCall>()[0];
        Assert.Equal("Label", call.Text);
        Assert.Equal(TextAlignment.Right, call.Alignment);
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
        var ctx = Drawn(ax);
        var f = ctx.Of<DrawTextCall>()[0].Font;
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
        var theme = themeName.AsTheme();
        var ax = new Axes();
        ax.Annotate("Default", 5, 5);
        var ctx = CartesianParts.PartFactory.Drawn(c => CartesianParts.PartFactory.Annotations(ax, Plot, c, theme, Plot.Linear()));
        var f = ctx.Of<DrawTextCall>()[0].Font;
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
        var ctx = Drawn(ax);
        var f = ctx.Of<DrawTextCall>()[0].Font;
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
        var ctx = Drawn(ax);
        // Callout box draws via DrawPath (rounded rect)
        Assert.True(ctx.Count<DrawPathCall>() > 0 || ctx.Count<DrawRectangleCall>() > 0);
    }

    [Fact]
    public void Render_BoxStyleNone_WithBackground_DrawsBackgroundRect()
    {
        var ax = new Axes();
        var a = ax.Annotate("Bg", 5, 5);
        a.BoxStyle = BoxStyle.None;
        a.BackgroundColor = Color.FromHex("#FFFF00");
        var ctx = Drawn(ax);
        var rects = ctx.Of<DrawRectangleCall>();
        Assert.Single(rects);
        Assert.Equal(Color.FromHex("#FFFF00"), rects[0].Fill);
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
        var ctx = Drawn(ax);

        var text = ctx.Of<DrawTextCall>()[0];
        var position = text.Position;
        var width = ctx.MeasureText("Backed", text.Font).Width;
        double left = alignment switch
        {
            TextAlignment.Center => position.X - width / 2,
            TextAlignment.Right => position.X - width,
            _ => position.X,
        };

        var rect = ctx.Of<DrawRectangleCall>()[0].Rect;
        Assert.Equal(left - 2, rect.X, 9);
        Assert.Equal(width + 4, rect.Width, 9);
    }

    /// <summary>What the annotations of an axes draw: how many calls of a kind the part makes for the axes the row arranges.</summary>
    public static TheoryData<Action<Axes>, Type, int> Counts { get; } = new()
    {
        { ax => ax.Annotate("Plain", 5, 5), typeof(DrawRectangleCall), 0 }, // Render_BoxStyleNone_NoBackground_NoRectangle
        //  CurveA produces a path head (no polygon) — exercises the `headPath is { Count: > 0 }` TRUE branch.
        //  Both connection path AND arrowhead path → 2 DrawPath calls.
        { ax =>
        {
            var a = ax.Annotate("CurveHead", 1, 1);
            a.ArrowTargetX = 5;
            a.ArrowTargetY = 5;
            a.ArrowStyle = ArrowStyle.CurveA;
        }, typeof(DrawPathCall), 2 }, // Render_ArrowStyleCurveA_EmitsHeadPath
        { ax =>
        {
            var a = ax.Annotate("NoArrow", 1, 1);
            a.ArrowTargetX = 5;
            a.ArrowTargetY = 5;
            a.ArrowStyle = ArrowStyle.None;
        }, typeof(DrawPathCall), 0 }, // Render_ArrowStyleNone_NoConnectionPath
        { ax =>
        {
            var a = ax.Annotate("NoX", 1, 1);
            a.ArrowTargetY = 5;
            a.ArrowStyle = ArrowStyle.Simple;
        }, typeof(DrawPathCall), 0 }, // Render_ArrowMissingTargetX_NoPath
        { ax =>
        {
            var a = ax.Annotate("NoY", 1, 1);
            a.ArrowTargetX = 5;
            a.ArrowStyle = ArrowStyle.Simple;
        }, typeof(DrawPathCall), 0 }, // Render_ArrowMissingTargetY_NoPath
        { ax =>
        {
            ax.Annotate("A", 1, 1);
            ax.Annotate("B", 5, 5);
            ax.Annotate("C", 8, 8);
        }, typeof(DrawTextCall), 3 }, // Render_MultipleAnnotations_AllRendered
    };

    [Theory]
    [MemberData(nameof(Counts))]
    public void Render_DrawsAsManyCallsOfAKindAsTheAxesAsks(Action<Axes> arrange, Type call, int expected)
    {
        var ax = new Axes();
        arrange(ax);

        Assert.Equal(expected, Drawn(ax).Calls.Count(call.IsInstanceOfType));
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
        var ctx = Drawn(ax);
        Assert.True(ctx.Count<DrawPathCall>() >= 1);
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
        var ctx = Drawn(ax);
        Assert.True(ctx.Count<DrawPolygonCall>() >= 1);
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
        var ctx = Drawn(ax);
        Assert.Equal(1, ctx.Count<DrawPathCall>());    // connection path only
        Assert.Equal(0, ctx.Count<DrawPolygonCall>()); // no polygon head
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
        var ctx = Drawn(ax);
        // Connection path drawn, but no polygon arrowhead at target (because len = 0)
        Assert.Equal(1, ctx.Count<DrawPathCall>());
        Assert.Equal(0, ctx.Count<DrawPolygonCall>());
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
        var ctx = Drawn(ax);
        var path = ctx.Of<DrawPathCall>()[0];
        Assert.Equal(Color.FromHex("#FF0000"), path.Stroke);
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
        var ctx = Drawn(ax);
        var path = ctx.Of<DrawPathCall>()[0];
        Assert.Equal(Color.FromHex("#0000FF"), path.Stroke);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Pixel offset and vertical alignment
    // ──────────────────────────────────────────────────────────────────────────

    private static Point TextPosition(RecordingRenderContext ctx) => ctx.Of<DrawTextCall>()[0].Position;

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
        var ctx = Drawn(moved);

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
        var ctx = Drawn(ax);

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
        var ctx = Drawn(ax);

        var segments = ctx.Of<DrawPathCall>()[0].Segments;
        var text = TextPosition(ctx);
        var anchor = Plot.Linear().DataToPixel(1, 1);
        Assert.Equal(anchor.X + 20, text.X, 9);
        Assert.Equal(anchor.Y + 10, text.Y, 9);
        var start = Assert.IsType<MoveToSegment>(segments[0]).Point;
        Assert.Equal(text.X, start.X, 9);
        Assert.Equal(text.Y, start.Y, 9);
        var target = Plot.Linear().DataToPixel(5, 5);
        Assert.Equal(target, Assert.IsType<LineToSegment>(segments[1]).Point);
    }

    [Fact]
    public void Render_AnOffsetAnnotation_MovesItsBackgroundWithIt()
    {
        var ax = new Axes();
        var a = ax.Annotate("Backed", 5, 5);
        a.BackgroundColor = Color.FromHex("#FFFF00");
        a.OffsetX = 30;
        var ctx = Drawn(ax);

        var rect = ctx.Of<DrawRectangleCall>()[0].Rect;
        Assert.Equal(Plot.Linear().DataToPixel(5, 5).X + 30, TextPosition(ctx).X, 9);
        Assert.Equal(TextPosition(ctx).X - 2, rect.X, 9);
    }

    [Fact]
    public void Render_AnAnnotationAtAnAnchorThatCannotBePlaced_IsSkippedWhole_WhateverItsOffset()
    {
        var ax = new Axes();
        var a = ax.Annotate("Lost", double.NaN, 5);
        a.OffsetX = 12;
        var ctx = Drawn(ax);

        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void Render_ANotAnumberOffset_SkipsTheAnnotationRatherThanDrawingItNowhere()
    {
        var ax = new Axes();
        var a = ax.Annotate("Nowhere", 5, 5);
        a.OffsetY = double.NaN;
        var ctx = Drawn(ax);

        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void Render_NoOffset_DrawsTheTextAtTheAnchorExactly()
    {
        var ax = new Axes();
        ax.Annotate("Here", 5, 5);
        var ctx = Drawn(ax);

        Assert.Equal(Plot.Linear().DataToPixel(5, 5), TextPosition(ctx));
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
        var ctx = Drawn(ax);

        Assert.Equal(Plot.Linear().DataToPixel(5, 5).Y + drop, TextPosition(ctx).Y, 9);
    }

    [Fact]
    public void Render_ACentredBackground_StandsAroundTheText_NotAroundTheAnchor()
    {
        var ax = new Axes();
        var a = ax.Annotate("Backed", 5, 5);
        a.VerticalAlignment = TextVerticalAlignment.Center;
        a.BackgroundColor = Color.FromHex("#FFFF00");
        var ctx = Drawn(ax);

        var rect = ctx.Of<DrawRectangleCall>()[0].Rect;
        var text = TextPosition(ctx);
        Assert.Equal(text.Y - 13.0, rect.Y, 9);   // the rule the box has always had: from a text height above the baseline
    }

    [Fact]
    public void Render_AnnotationWithRotation_PassedThrough()
    {
        var ax = new Axes();
        var a = ax.Annotate("Rotated", 5, 5);
        a.Rotation = 45;
        var ctx = Drawn(ax);
        var call = ctx.Of<DrawTextCall>()[0];
        Assert.Equal(45.0, call.Rotation);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Annotations that move out of the way
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>A mark exactly where the annotation 'Mover' stands at data (5, 5): its anchor and its text.</summary>
    private static MarkObstacles MarkUnderTheAnchor(RecordingRenderContext measure)
    {
        var anchor = Plot.Linear().DataToPixel(5, 5);
        var font = DefaultTheme.DataTextFont();
        var box = new TextBox(new TextPlacement(anchor, TextAlignment.Left), TextExtent.Of(measure.MeasureText("Mover", font), font.Size)).Bounds();
        return new MarkObstacles.Builder().AddBox(box).Build();
    }

    private static CartesianAnnotationsPart WithObstacles(Axes axes, RecordingRenderContext ctx, MarkObstacles obstacles) =>
        new(PartFactory.Context(axes, Plot, ctx, DefaultTheme, Plot.Linear())) { Obstacles = obstacles };

    [Fact]
    public void Render_AnAnnotationThatAvoidsOverlap_StandsOffAMarkThatIsUnderItsAnchor()
    {
        var ax = new Axes();
        ax.Annotate("Mover", 5, 5).AvoidOverlap = true;
        var ctx = CartesianParts.PartFactory.Drawn(c => WithObstacles(ax, c, MarkUnderTheAnchor(c)));

        Assert.NotEqual(Plot.Linear().DataToPixel(5, 5), TextPosition(ctx));
    }

    [Fact]
    public void Render_AnAnnotationThatDoesNotAskToMove_StaysOverTheMark()
    {
        var ax = new Axes();
        ax.Annotate("Mover", 5, 5);
        var ctx = CartesianParts.PartFactory.Drawn(c => WithObstacles(ax, c, MarkUnderTheAnchor(c)));

        Assert.Equal(Plot.Linear().DataToPixel(5, 5), TextPosition(ctx));
    }

    [Fact]
    public void Render_AnAnnotationThatAvoidsOverlapOverNothing_IsDrawnExactlyAtItsAnchor()
    {
        var ax = new Axes();
        ax.Annotate("Mover", 5, 5).AvoidOverlap = true;
        var ctx = CartesianParts.PartFactory.Drawn(c => WithObstacles(ax, c, MarkObstacles.None));

        Assert.Equal(Plot.Linear().DataToPixel(5, 5), TextPosition(ctx));
        Assert.Equal(0, ctx.Count<DrawLineCall>());
    }

    [Fact]
    public void Render_AMovedAnnotation_IsJoinedToItsAnchorByALeaderLine()
    {
        var ax = new Axes();
        ax.Annotate("Mover", 5, 5).AvoidOverlap = true;
        var ctx = CartesianParts.PartFactory.Drawn(c => WithObstacles(ax, c, MarkUnderTheAnchor(c)));

        var leaders = ctx.Of<DrawLineCall>();
        Assert.True(leaders.Count == 1 || ctx.Count<DrawPathCall>() >= 1, "the moved text has no line back to its anchor");
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
        var ctx = CartesianParts.PartFactory.Drawn(c => WithObstacles(ax, c, MarkUnderTheAnchor(c)));

        Assert.Equal(0, ctx.Count<DrawLineCall>());
        var segments = ctx.Of<DrawPathCall>()[0].Segments;
        var start = Assert.IsType<MoveToSegment>(segments[0]).Point;
        Assert.Equal(TextPosition(ctx), start);
        Assert.NotEqual(Plot.Linear().DataToPixel(5, 5), start);
    }

    [Fact]
    public void Render_ATextThatStays_IsLeftWhereItIs_WhileAnotherMovesOutOfItsWay()
    {
        var ax = new Axes();
        ax.Annotate("Stayer", 2, 2);
        ax.Annotate("Mover", 5, 5).AvoidOverlap = true;
        var ctx = CartesianParts.PartFactory.Drawn(c => WithObstacles(ax, c, MarkUnderTheAnchor(c)));

        var positions = ctx.Of<DrawTextCall>().ToDictionary(c => c.Text, c => c.Position);
        Assert.Equal(Plot.Linear().DataToPixel(2, 2), positions["Stayer"]);
        Assert.NotEqual(Plot.Linear().DataToPixel(5, 5), positions["Mover"]);
    }

    [Fact]
    public void Render_ATextThatMovesOutOfTheWayOfAStayer_KeepsClearOfIt()
    {
        var ax = new Axes();
        ax.Annotate("Mover", 5, 5).AvoidOverlap = true;
        ax.Annotate("Mover", 5, 5);   // the same text, put on the same point, and it stays
        var ctx = CartesianParts.PartFactory.Drawn(c => WithObstacles(ax, c, MarkObstacles.None));

        var positions = ctx.Of<DrawTextCall>().Select(c => c.Position).ToList();
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
        var ctx = CartesianParts.PartFactory.Drawn(c => WithObstacles(ax, c, MarkUnderTheAnchor(c)));

        var leader = Assert.Single(ctx.Of<DrawLineCall>());
        Assert.Equal(Color.FromHex("#AA3300"), leader.Color);
    }

    [Fact]
    public void Render_ALeaderLine_IsDrawnInTheThemesTextColour_WhenTheAnnotationNamesNone()
    {
        var ax = new Axes();
        ax.Annotate("Mover", 5, 5).AvoidOverlap = true;
        var ctx = CartesianParts.PartFactory.Drawn(c => WithObstacles(ax, c, MarkUnderTheAnchor(c)));

        var leader = Assert.Single(ctx.Of<DrawLineCall>());
        Assert.Equal(DefaultTheme.ForegroundText, leader.Color);
    }

    // ---- the place of a text that moves out of the way (PlaceText) ----
    // An annotation that asked to move out of the way stands at its own place when nothing is there, and otherwise at the first
    // of a fixed list of places, in a fixed order, that the fewest marks cover: the same selector a Best legend uses. Every place
    // but the anchor lies inside the plot area. No random number takes part, so the result is the same on every platform and
    // every run.

    private static readonly Size Text = new(60, 12);
    private static readonly Point Anchor = new(200, 150);

    private static TextBox Box(Point at, double size = 10) =>
        new(new TextPlacement(at, TextAlignment.Left), TextExtent.Of(Text, size));

    /// <summary>The box of the text standing at <paramref name="at"/>, for a mark placed exactly over it.</summary>
    private static Rect Over(Point at) => Box(at).Bounds();

    private static MarkObstacles Marks(params Rect[] boxes)
    {
        var builder = new MarkObstacles.Builder();
        foreach (var box in boxes) builder.AddBox(box);
        return builder.Build();
    }

    /// <summary>A direction the layout tries, as a multiple of the text's height.</summary>
    private readonly record struct Direction(int X, int Y);

    /// <summary>The directions the layout tries, in order, at each step out from the anchor.</summary>
    private static readonly Direction[] Directions =
        [new(0, -1), new(0, 1), new(1, 0), new(-1, 0), new(1, -1), new(-1, -1), new(1, 1), new(-1, 1)];

    private static Point Candidate(int step, int direction) =>
        new(Anchor.X + Directions[direction].X * step * Text.Height, Anchor.Y + Directions[direction].Y * step * Text.Height);

    /// <summary>The places in the order the layout tries them: the anchor, then four steps of the eight directions.</summary>
    private static IEnumerable<Point> Places(Point anchor)
    {
        yield return anchor;
        for (int step = 1; step <= 4; step++)
            foreach (var d in Directions)
                yield return new Point(anchor.X + d.X * step * Text.Height, anchor.Y + d.Y * step * Text.Height);
    }

    [Fact]
    public void ATextWithNothingUnderIt_StaysAtItsAnchor()
    {
        var obstacles = MarkObstacles.None;

        Assert.Equal(Anchor, CartesianAnnotationsPart.PlaceText(Box(Anchor), Plot, ref obstacles, moves: true));
    }

    [Fact]
    public void ATextThatDoesNotMove_StaysWhateverIsUnderIt()
    {
        var obstacles = Marks(Over(Anchor));

        Assert.Equal(Anchor, CartesianAnnotationsPart.PlaceText(Box(Anchor), Plot, ref obstacles, moves: false));
    }

    [Fact]
    public void ATextOverAMark_MovesOneTextHeightUp_WhenThatPlaceIsFree()
    {
        var obstacles = Marks(Over(Anchor));

        var placed = CartesianAnnotationsPart.PlaceText(Box(Anchor), Plot, ref obstacles, moves: true);

        Assert.Equal(Candidate(1, 0), placed);
    }

    [Fact]
    public void ATextOverAMarkThatReachesAbove_MovesBelow()
    {
        // The mark covers the anchor's box and the place above it; the place below only touches its lower edge.
        var obstacles = Marks(new Rect(Anchor.X - 5, Anchor.Y - 22, 70, 24));

        Assert.Equal(Candidate(1, 1), CartesianAnnotationsPart.PlaceText(Box(Anchor), Plot, ref obstacles, moves: true));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ATextStandsAtTheFirstLeastCoveredPlace_OfTheFixedList(int pattern)
    {
        Rect[][] patterns =
        [
            [new Rect(190, 100, 120, 70)],                                                      // a block over the anchor and its upper right
            [new Rect(150, 120, 100, 20), new Rect(190, 160, 120, 30)],                          // a band above, a band below
            [new Rect(100, 100, 100, 100), new Rect(205, 130, 40, 30)],                         // the left, and a patch to the right
            [new Rect(0, 0, 400, 135), new Rect(0, 165, 400, 135), new Rect(0, 135, 190, 30)],   // everything but a gap to the right
        ];
        var marks = Marks(patterns[pattern]);
        var obstacles = marks;

        var placed = CartesianAnnotationsPart.PlaceText(Box(Anchor), Plot, ref obstacles, moves: true);

        // Reference: the same list, scored by the one rule a legend uses.
        var places = Places(Anchor).Where((p, i) => i == 0 || Plot.Contains(Box(p).Bounds())).ToList();
        var expected = places[marks.FirstLeastCovered([.. places.Select(p => Box(p).Bounds())])];
        Assert.Equal(expected, placed);
    }

    [Fact]
    public void APlaceOutsideThePlotArea_IsNeverChosen_ButTheAnchorAlwaysIs()
    {
        // The text stands at the very top: a place above it would leave the plot area.
        var top = new Point(200, 14);
        var obstacles = Marks(Over(top));

        var placed = CartesianAnnotationsPart.PlaceText(Box(top), Plot, ref obstacles, moves: true);

        Assert.Equal(Candidate(1, 1).Y - Anchor.Y + top.Y, placed.Y, 9);   // the place below, a text height down
        Assert.True(Plot.Y <= Box(placed).Bounds().Y, "the moved text left the plot area through the top");
    }

    [Fact]
    public void WhenEveryPlaceIsCovered_TheTextStaysAtItsAnchor_ThatTiesWithTheRest()
    {
        var everything = new Rect(-1000, -1000, 3000, 3000);
        var obstacles = Marks(everything);

        Assert.Equal(Anchor, CartesianAnnotationsPart.PlaceText(Box(Anchor), Plot, ref obstacles, moves: true));
    }

    [Fact]
    public void ATextThatWasPlaced_StandsAmongTheMarksForTheTextsAfterIt()
    {
        var obstacles = MarkObstacles.None;

        var first = CartesianAnnotationsPart.PlaceText(Box(Anchor), Plot, ref obstacles, moves: true);
        var second = CartesianAnnotationsPart.PlaceText(Box(Anchor), Plot, ref obstacles, moves: true);

        Assert.Equal(Anchor, first);
        Assert.NotEqual(Anchor, second);
        Assert.False(Box(first).Bounds().Overlaps(Box(second).Bounds()), "the second text was set on top of the first");
    }

    [Fact]
    public void FiftyTexts_AreAllPlaced_WithNoNotANumber_AndTheSameWayTwice()
    {
        List<Point> Run()
        {
            var obstacles = MarkObstacles.None;
            return [.. Enumerable.Range(0, 50).Select(i => CartesianAnnotationsPart.PlaceText(Box(new Point(150 + i % 5, 140 + i % 3)), Plot, ref obstacles, moves: true))];
        }

        var one = Run();
        var two = Run();

        Assert.Equal(50, one.Count);
        Assert.All(one, p => Assert.True(p.IsDrawable()));
        Assert.Equal(one, two);
    }

    [Fact]
    public void ATurnedText_IsPlacedByItsTurnedBox()
    {
        // A text turned a quarter stands upright in a narrow column; a mark beside it that the unturned box would cover does not matter.
        var turned = new TextBox(new TextPlacement(Anchor, TextAlignment.Left) { Rotation = 90 }, TextExtent.Of(Text, 10));
        var beside = Marks(new Rect(Anchor.X + 30, Anchor.Y - 5, 20, 10));   // right of the column, inside the unturned box
        var obstacles = beside;

        Assert.Equal(Anchor, CartesianAnnotationsPart.PlaceText(turned, Plot, ref obstacles, moves: true));
    }
}
