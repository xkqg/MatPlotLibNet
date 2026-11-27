// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NSubstitute;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MplColor = MatPlotLibNet.Styling.Colors;
using MplFont = MatPlotLibNet.Styling.Font;
using MplPoint = MatPlotLibNet.Rendering.Point;
using MplRect = MatPlotLibNet.Rendering.Rect;

namespace MatPlotLibNet.Maui.Tests;

public class MauiGraphicsRenderContextTests
{
    private static readonly ShapeStyle Filled = new(MplColor.Blue, null, 0);

    private static readonly ShapeStyle Stroke = new(null, MplColor.Red, 2);

    private static readonly List<MplPoint> Square = [new(0, 0), new(50, 0), new(50, 50), new(0, 50)];

    /// <summary>A shape handed to the context, and the call the canvas must receive once for it: a fill is the canvas' fill of that shape, a
    /// stroke its draw, both together are both calls, and a path, a polygon or a run of lines is a path of the canvas.</summary>
    public static IEnumerable<object[]> Shapes() =>
    [
        [(Action<MauiGraphicsRenderContext>)(ctx => ctx.DrawRectangle(new MplRect(10, 20, 100, 50), Filled)), (Action<ICanvas>)(canvas => canvas.Received(1).FillRectangle(10f, 20f, 100f, 50f))],
        [(Action<MauiGraphicsRenderContext>)(ctx => ctx.DrawRectangle(new MplRect(10, 20, 100, 50), Stroke)), (Action<ICanvas>)(canvas => canvas.Received(1).DrawRectangle(10f, 20f, 100f, 50f))],
        [(Action<MauiGraphicsRenderContext>)(ctx => ctx.DrawRectangle(new MplRect(10, 20, 100, 50), new ShapeStyle(MplColor.Blue, MplColor.Red, 2))), (Action<ICanvas>)(canvas =>
        {
            canvas.Received(1).FillRectangle(10f, 20f, 100f, 50f);
            canvas.Received(1).DrawRectangle(10f, 20f, 100f, 50f);
        })],
        [(Action<MauiGraphicsRenderContext>)(ctx => ctx.DrawCircle(new MplPoint(50, 50), 10, Filled)), (Action<ICanvas>)(canvas => canvas.Received(1).FillCircle(50f, 50f, 10f))],
        [(Action<MauiGraphicsRenderContext>)(ctx => ctx.DrawCircle(new MplPoint(50, 50), 10, Stroke)), (Action<ICanvas>)(canvas => canvas.Received(1).DrawCircle(50f, 50f, 10f))],
        [(Action<MauiGraphicsRenderContext>)(ctx => ctx.DrawEllipse(new MplRect(10, 20, 60, 40), Filled)), (Action<ICanvas>)(canvas => canvas.Received(1).FillEllipse(10f, 20f, 60f, 40f))],
        [(Action<MauiGraphicsRenderContext>)(ctx => ctx.DrawEllipse(new MplRect(10, 20, 60, 40), Stroke)), (Action<ICanvas>)(canvas => canvas.Received(1).DrawEllipse(10f, 20f, 60f, 40f))],
        [(Action<MauiGraphicsRenderContext>)(ctx => ctx.DrawPolygon(Square, Filled)), (Action<ICanvas>)(canvas => canvas.Received(1).FillPath(Arg.Any<PathF>()))],
        [(Action<MauiGraphicsRenderContext>)(ctx => ctx.DrawPolygon(Square, Stroke)), (Action<ICanvas>)(canvas => canvas.Received(1).DrawPath(Arg.Any<PathF>()))],
        [(Action<MauiGraphicsRenderContext>)(ctx => ctx.DrawLines([new(0, 0), new(10, 10), new(20, 0)], new StrokeStyle(MplColor.Red, 1.0, Styling.LineStyle.Solid))), (Action<ICanvas>)(canvas => canvas.Received(1).DrawPath(Arg.Any<PathF>()))],
        [(Action<MauiGraphicsRenderContext>)(ctx => ctx.DrawPath(
            [new MoveToSegment(new MplPoint(0, 0)), new LineToSegment(new MplPoint(50, 50)), new LineToSegment(new MplPoint(100, 0))],
            new ShapeStyle(null, MplColor.Black, 1))), (Action<ICanvas>)(canvas => canvas.Received(1).DrawPath(Arg.Any<PathF>()))],
    ];

    [Theory]
    [MemberData(nameof(Shapes))]
    public void AShape_IsDrawnByTheCanvasCallsOfItsStyle(Action<MauiGraphicsRenderContext> draw, Action<ICanvas> expected)
    {
        var (canvas, ctx) = MauiRig.Fresh();

        draw(ctx);

        expected(canvas);
    }

    [Fact]
    public void DrawLine_CallsCanvasDrawLine()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.DrawLine(new MplPoint(10, 20), new MplPoint(30, 40),
            new StrokeStyle(MplColor.Red, 2.0, Styling.LineStyle.Solid));

        canvas.Received(1).DrawLine(10f, 20f, 30f, 40f);
    }

    [Fact]
    public void DrawLine_SetsStrokeColor()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.DrawLine(new MplPoint(0, 0), new MplPoint(10, 10),
            new StrokeStyle(MplColor.Blue, 1.5, Styling.LineStyle.Solid));

        canvas.Received().StrokeColor = Arg.Is<Microsoft.Maui.Graphics.Color>(c =>
            Math.Abs(c.Red - 0f) < 0.01 && Math.Abs(c.Blue - 1f) < 0.01);
    }

    [Fact]
    public void DrawLine_SetsStrokeSize()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.DrawLine(new MplPoint(0, 0), new MplPoint(10, 10),
            new StrokeStyle(MplColor.Red, 3.0, Styling.LineStyle.Solid));

        canvas.Received().StrokeSize = 3.0f;
    }

    [Fact]
    public void DrawLine_DashedStyle_SetsDashPattern()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.DrawLine(new MplPoint(0, 0), new MplPoint(10, 10),
            new StrokeStyle(MplColor.Black, 1, Styling.LineStyle.Dashed));

        canvas.Received().StrokeDashPattern = Arg.Is<float[]>(p => p.Length > 0);
    }

    [Fact]
    public void DrawText_CallsDrawString()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.DrawText("Hello", new MplPoint(100, 200), new MplFont(), MatPlotLibNet.Rendering.TextAlignment.Left);

        canvas.Received(1).DrawString(
            "Hello",
            Arg.Any<float>(), Arg.Any<float>(),
            Arg.Any<HorizontalAlignment>());
    }

    [Fact]
    public void DrawText_SetsFontSize()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.DrawText("Test", new MplPoint(0, 0), new MplFont { Size = 18 }, MatPlotLibNet.Rendering.TextAlignment.Center);

        canvas.Received().FontSize = 18f;
    }

    [Fact]
    public void MeasureText_ReturnsNonZeroSize()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        var size = ctx.MeasureText("Hello World", new MplFont { Size = 12 });

        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
    }

    [Fact]
    public void PushClip_CallsSaveStateAndClipRectangle()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.PushClip(new MplRect(10, 20, 100, 50));

        canvas.Received(1).SaveState();
        canvas.Received(1).ClipRectangle(10f, 20f, 100f, 50f);
    }

    /// <summary>A clip to an outline (the disc of a polar axes, the triangle of a ternary one) is the canvas' clip path by the
    /// nonzero rule, not the box around it.</summary>
    [Fact]
    public void AnOutlineClip_IsTheCanvasClipPath_ByTheNonzeroRule()
    {
        var (canvas, ctx) = MauiRig.Fresh();
        PathSegment[] triangle =
            [new MoveToSegment(new MplPoint(60, 20)), new LineToSegment(new MplPoint(10, 70)), new LineToSegment(new MplPoint(110, 70)), new CloseSegment()];

        using var clip = ctx.ClippedTo(new ClipRegion(new MplRect(10, 20, 100, 50), triangle));

        canvas.Received(1).SaveState();
        canvas.Received(1).ClipPath(Arg.Is<PathF>(p => p.Count == 3 && p.Closed && p[0] == new PointF(60, 20) && p[2] == new PointF(110, 70)),
            WindingMode.NonZero);
        canvas.DidNotReceive().ClipRectangle(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>());
    }

    [Fact]
    public void PopClip_CallsRestoreState()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.PushClip(new MplRect(10, 20, 100, 50));
        ctx.PopClip();
        ctx.PopClip();

        canvas.Received(1).RestoreState();
    }

    [Fact]
    public void SetOpacity_SetsCanvasAlpha()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.SetOpacity(0.5);

        canvas.Received().Alpha = 0.5f;
    }

    [Fact]
    public void DrawText_CenterAlignment_CallsDrawStringLeftAlignedFromTheCentredStart()
    {
        var (canvas, ctx) = MauiRig.Fresh();
        var font = new MplFont { Size = 14 };

        ctx.DrawText("Center", new MplPoint(50, 50), font, MatPlotLibNet.Rendering.TextAlignment.Center);

        canvas.Received(1).DrawString(
            "Center",
            (float)(50 - ctx.MeasureText("Center", font).Width / 2), 50f,
            HorizontalAlignment.Left);
    }

    // ── The line style of a shape's stroke ────────────────────────────────────
    // A shape's stroke is dashed by the rule a line's stroke is dashed by (ToMauiDashPattern: the pattern of the style under the
    // theme's look, set on the canvas before the stroke is drawn). A MAUI canvas multiplies every value of the pattern by the stroke
    // size, so what it is given is the lengths of the SVG and the PNG divided by the width: the library's 5 and 2 at a width of 2
    // are 2.5 and 1. The canvas keeps the pattern between calls, so a shape sets it every time:
    // a solid shape drawn after a dashed line must not inherit the line's dashes.

    /// <summary>A canvas that notes, for every stroke it is asked to draw, the dash pattern it held at that moment. The
    /// canvas' <c>StrokeDashPattern</c> can be set and not read, so the recorder keeps the last pattern it was given.</summary>
    private sealed class StrokeRecorder
    {
        public ICanvas Canvas { get; } = Substitute.For<ICanvas>();

        /// <summary>The dash pattern the canvas holds now: the last one it was given, none before the first.</summary>
        public float[] Held { get; private set; } = [];

        /// <summary>The dash pattern of the canvas at each stroke, in the order the strokes were drawn.</summary>
        public List<float[]> PatternsAtStroke { get; } = [];

        public StrokeRecorder()
        {
            Canvas.When(c => c.StrokeDashPattern = Arg.Any<float[]>()).Do(call => Held = call.Arg<float[]>());
            Canvas.When(c => c.DrawPath(Arg.Any<PathF>())).Do(_ => Note());
            Canvas.When(c => c.DrawRectangle(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>())).Do(_ => Note());
            Canvas.When(c => c.DrawEllipse(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>())).Do(_ => Note());
        }

        private void Note() => PatternsAtStroke.Add(Held);
    }

    private static ShapeStyle Stroked(Styling.LineStyle style) =>
        new(null, MplColor.Black, 2) { StrokeLineStyle = style };

    private static readonly List<MplPoint> Triangle = [new(0, 0), new(50, 0), new(25, 40)];

    private static readonly List<MatPlotLibNet.Rendering.PathSegment> TrianglePath =
    [
        new MatPlotLibNet.Rendering.MoveToSegment(new MplPoint(0, 0)),
        new MatPlotLibNet.Rendering.LineToSegment(new MplPoint(50, 0)),
        new MatPlotLibNet.Rendering.LineToSegment(new MplPoint(25, 40)),
        new MatPlotLibNet.Rendering.CloseSegment(),
    ];

    /// <summary>Draws the triangle as the shape named, a path or a polygon, which take a line style by the same rule.</summary>
    private static void DrawTriangle(MauiGraphicsRenderContext ctx, string shape, ShapeStyle style)
    {
        if (shape == "path") ctx.DrawPath(TrianglePath, style);
        else ctx.DrawPolygon(Triangle, style);
    }

    /// <summary>A shape stroked dashed hands the canvas the pattern of a dashed line before it strokes.</summary>
    [Theory]
    [InlineData("path")]
    [InlineData("polygon")]
    public void ADashedStroke_SetsTheDashPatternOfADashedLine(string shape)
    {
        var recorder = new StrokeRecorder();
        DrawTriangle(new MauiGraphicsRenderContext(recorder.Canvas), shape, Stroked(Styling.LineStyle.Dashed));

        Assert.Equal(new[] { 2.5f, 1f }, Assert.Single(recorder.PatternsAtStroke));
    }

    /// <summary>The dotted and dash-dot styles set the pattern a line of that style sets.</summary>
    [Theory]
    [InlineData(Styling.LineStyle.Dotted, new[] { 0.5f, 1.5f })]
    [InlineData(Styling.LineStyle.DashDot, new[] { 2.5f, 1f, 0.5f, 1f })]
    public void ADottedOrDashDotPathStroke_SetsThePatternOfALineOfThatStyle(Styling.LineStyle style, float[] pattern)
    {
        var recorder = new StrokeRecorder();
        new MauiGraphicsRenderContext(recorder.Canvas).DrawPath(TrianglePath, Stroked(style));

        Assert.Equal(pattern, Assert.Single(recorder.PatternsAtStroke));
    }

    /// <summary>A shape stroked solid sets an empty pattern, whatever the canvas held before: after a dashed line it must not
    /// inherit the dashes.</summary>
    [Theory]
    [InlineData("path")]
    [InlineData("polygon")]
    public void ASolidStroke_ClearsTheDashesALineLeftOnTheCanvas(string shape)
    {
        var recorder = new StrokeRecorder();
        var ctx = new MauiGraphicsRenderContext(recorder.Canvas);
        ctx.DrawLine(new MplPoint(0, 0), new MplPoint(10, 10), new StrokeStyle(MplColor.Black, 1, Styling.LineStyle.Dashed));
        Assert.NotEmpty(recorder.Held);

        DrawTriangle(ctx, shape, Stroked(Styling.LineStyle.Solid));

        Assert.Empty(Assert.Single(recorder.PatternsAtStroke));
    }

    /// <summary>A shape whose stroke has the line style None draws no stroke, and its fill is still filled.</summary>
    [Theory]
    [InlineData("path")]
    [InlineData("polygon")]
    public void ANoneStroke_DrawsNoStroke(string shape)
    {
        var (canvas, ctx) = MauiRig.Fresh();

        DrawTriangle(ctx, shape, new ShapeStyle(MplColor.Red, MplColor.Black, 2) { StrokeLineStyle = Styling.LineStyle.None });

        canvas.Received(1).FillPath(Arg.Any<PathF>());
        canvas.DidNotReceive().DrawPath(Arg.Any<PathF>());
    }

    /// <summary>The circle, the rectangle and the ellipse take the line style the same way the path and the polygon do.</summary>
    [Fact]
    public void TheOtherShapes_StrokeByTheSameRule()
    {
        var recorder = new StrokeRecorder();
        var ctx = new MauiGraphicsRenderContext(recorder.Canvas);

        ctx.DrawCircle(new MplPoint(5, 5), 4, Stroked(Styling.LineStyle.Dashed));
        ctx.DrawRectangle(new MplRect(0, 0, 10, 10), Stroked(Styling.LineStyle.Dashed));
        ctx.DrawEllipse(new MplRect(0, 0, 10, 6), Stroked(Styling.LineStyle.Dashed));

        Assert.Equal(3, recorder.PatternsAtStroke.Count);
        Assert.All(recorder.PatternsAtStroke, pattern => Assert.Equal(new[] { 2.5f, 1f }, pattern));

        var none = Substitute.For<ICanvas>();
        var noneCtx = new MauiGraphicsRenderContext(none);
        var unstroked = new ShapeStyle(MplColor.Red, MplColor.Black, 2) { StrokeLineStyle = Styling.LineStyle.None };
        noneCtx.DrawCircle(new MplPoint(5, 5), 4, unstroked);
        noneCtx.DrawRectangle(new MplRect(0, 0, 10, 10), unstroked);
        noneCtx.DrawEllipse(new MplRect(0, 0, 10, 6), unstroked);
        none.DidNotReceive().DrawRectangle(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>());
        // The canvas has no circle of its own: a circle is the ellipse about its centre, so one ellipse call covers both.
        none.DidNotReceive().DrawEllipse(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>());
    }

    /// <summary>A shape with no stroke does not touch the canvas' dash pattern, however its line style is set.</summary>
    [Fact]
    public void AShapeWithoutAStroke_LeavesTheDashPatternAlone()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.DrawPolygon(Triangle, new ShapeStyle(MplColor.Red, null, 2) { StrokeLineStyle = Styling.LineStyle.Dashed });

        canvas.DidNotReceive().StrokeDashPattern = Arg.Any<float[]>();
    }

    [Fact]
    public void DrawRectangle_NullFill_NullStroke_NoCanvasCalls()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.DrawRectangle(new MplRect(10, 20, 100, 50),
            new ShapeStyle(null, null, 0));

        canvas.DidNotReceive().FillRectangle(Arg.Any<float>(), Arg.Any<float>(),
            Arg.Any<float>(), Arg.Any<float>());
        canvas.DidNotReceive().DrawRectangle(Arg.Any<float>(), Arg.Any<float>(),
            Arg.Any<float>(), Arg.Any<float>());
    }

    [Fact]
    public void DrawText_Rotated_TurnsTheCanvasAboutTheAnchor_ForThatTextOnly()
    {
        // The interface's default for a rotated text drops the rotation, and this context had no rotated overload of its
        // own, so a Y axis title and turned tick labels were drawn level on MAUI.
        var canvas = Substitute.For<ICanvas>();
        IRenderContext ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawText("Up", new MplPoint(10, 200), new MplFont(), MatPlotLibNet.Rendering.TextAlignment.Center, 90);

        float start = (float)(10 - ctx.MeasureText("Up", new MplFont()).Width / 2);
        Received.InOrder(() =>
        {
            canvas.SaveState();
            canvas.Rotate(-90f, 10f, 200f);
            canvas.DrawString("Up", start, 200f, HorizontalAlignment.Left);
            canvas.RestoreState();
        });
    }

    [Fact]
    public void DrawText_RotatedByNothing_DrawsAsTheLevelText()
    {
        var canvas = Substitute.For<ICanvas>();
        IRenderContext ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawText("Level", new MplPoint(1, 2), new MplFont(), MatPlotLibNet.Rendering.TextAlignment.Left, 0);

        canvas.DidNotReceive().Rotate(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>());
        canvas.DidNotReceive().SaveState();
        canvas.Received(1).DrawString("Level", 1f, 2f, HorizontalAlignment.Left);
    }

    /// <summary>A rich text is laid out in core and drawn run by run: each run a string of its own, left-aligned where the layout puts
    /// it, the canvas turned about the run's start for that run alone.</summary>
    [Fact]
    public void DrawRichText_Rotated_DrawsEachRunTurned()
    {
        var canvas = Substitute.For<ICanvas>();
        IRenderContext ctx = new MauiGraphicsRenderContext(canvas);
        var rich = new MatPlotLibNet.Rendering.MathText.RichText([new("a"), new("b")]);

        ctx.DrawRichText(rich, new MplPoint(5, 6), new MplFont(), MatPlotLibNet.Rendering.TextAlignment.Right, 45);

        Received.InOrder(() =>
        {
            canvas.SaveState();
            canvas.Rotate(-45f, Arg.Any<float>(), Arg.Any<float>());
            canvas.DrawString("a", Arg.Any<float>(), Arg.Any<float>(), HorizontalAlignment.Left);
            canvas.RestoreState();
            canvas.SaveState();
            canvas.Rotate(-45f, Arg.Any<float>(), Arg.Any<float>());
            canvas.DrawString("b", Arg.Any<float>(), Arg.Any<float>(), HorizontalAlignment.Left);
            canvas.RestoreState();
        });
    }

    /// <summary>A fraction on this canvas is stacked: its rule is a filled rectangle (a named fix: the canvas drew the spans as one
    /// string).</summary>
    [Fact]
    public void DrawRichText_AFraction_FillsItsRule()
    {
        var canvas = Substitute.For<ICanvas>();
        IRenderContext ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawRichText(MatPlotLibNet.Rendering.MathText.MathTextParser.Parse(@"$\frac{a}{b}$"), new MplPoint(5, 6), new MplFont(),
            MatPlotLibNet.Rendering.TextAlignment.Left);

        canvas.Received().DrawString("a", Arg.Any<float>(), Arg.Any<float>(), HorizontalAlignment.Left);
        canvas.Received().DrawString("b", Arg.Any<float>(), Arg.Any<float>(), HorizontalAlignment.Left);
        canvas.Received().FillRectangle(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>());
    }

    /// <summary>The canvas multiplies the pattern by the stroke size, so at any width the library's dashes come out at the lengths the
    /// SVG writes: 5 on and 2 off.</summary>
    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(3.0)]
    public void AtAnyWidth_TheCanvasPatternTimesTheStrokeSize_IsTheLengthsTheSvgWrites(double width)
    {
        var recorder = new StrokeRecorder();
        var ctx = new MauiGraphicsRenderContext(recorder.Canvas);

        ctx.DrawPath(TrianglePath, new ShapeStyle(null, MplColor.Black, width) { StrokeLineStyle = Styling.LineStyle.Dashed });

        var pattern = Assert.Single(recorder.PatternsAtStroke);
        Assert.Equal(5, pattern[0] * width, 4);
        Assert.Equal(2, pattern[1] * width, 4);
    }

    /// <summary>Under the default matplotlib style the dashes grow with the width, as the canvas multiplies the pattern by the stroke
    /// size: it is given matplotlib's [3.7, 1.6] as they are. Under the classic style they are 6 pt, 8.33 px, whatever the width: the
    /// canvas is given that divided by the width.</summary>
    [Fact]
    public void UnderTheMatplotlibThemes_TheCanvasIsGivenThePatternTheCanvasMultipliesToMatplotlibsLengths()
    {
        var v2 = new StrokeRecorder();
        var v2Ctx = new MauiGraphicsRenderContext(v2.Canvas);
        v2Ctx.UseLooksOf(Theme.MatplotlibV2);
        v2Ctx.DrawPath(TrianglePath, Stroked(Styling.LineStyle.Dashed));

        var classic = new StrokeRecorder();
        var classicCtx = new MauiGraphicsRenderContext(classic.Canvas);
        classicCtx.UseLooksOf(Theme.MatplotlibClassic);
        classicCtx.DrawPath(TrianglePath, Stroked(Styling.LineStyle.Dashed));

        Assert.Equal(new[] { 3.7f, 1.6f }, Assert.Single(v2.PatternsAtStroke));
        Assert.Equal(new[] { 4.1666665f, 4.1666665f }, Assert.Single(classic.PatternsAtStroke));
    }

    /// <summary>The join of a stroke is set on the canvas with its colour and size, for a line and for a shape alike, every time: the
    /// canvas keeps its stroke state between calls, so a mitred shape drawn after a rounded line gets its mitre.</summary>
    [Fact]
    public void TheJoinOfAStroke_IsSetOnTheCanvasForALineAndAShape_EveryTime()
    {
        var (canvas, ctx) = MauiRig.Fresh();

        ctx.DrawLines([new MplPoint(0, 0), new MplPoint(5, 5), new MplPoint(10, 0)], new StrokeStyle(MplColor.Black, 2, Styling.LineStyle.Solid) { Join = JoinStyle.Round });
        canvas.Received(1).StrokeLineJoin = LineJoin.Round;

        ctx.DrawPath(TrianglePath, new ShapeStyle(null, MplColor.Black, 2) { StrokeJoin = JoinStyle.Bevel });
        canvas.Received(1).StrokeLineJoin = LineJoin.Bevel;

        ctx.DrawPath(TrianglePath, new ShapeStyle(null, MplColor.Black, 2));
        canvas.Received(1).StrokeLineJoin = LineJoin.Miter;
    }

    /// <summary>A line that names no join is joined as the look the canvas was told says (round under the matplotlib themes, the mitre
    /// under a library theme), and a join it names wins.</summary>
    [Fact]
    public void ALineThatNamesNoJoin_IsJoinedByTheLookTheContextWasTold()
    {
        var corner = new[] { new MplPoint(0, 0), new MplPoint(5, 5), new MplPoint(10, 0) };
        var plain = new StrokeStyle(MplColor.Black, 2, Styling.LineStyle.Solid);

        var v2 = Substitute.For<ICanvas>();
        var v2Ctx = new MauiGraphicsRenderContext(v2);
        v2Ctx.UseLooksOf(Theme.MatplotlibV2);
        v2Ctx.DrawLines(corner, plain);
        v2.Received(1).StrokeLineJoin = LineJoin.Round;
        v2Ctx.DrawLines(corner, plain with { Join = JoinStyle.Miter });
        v2.Received(1).StrokeLineJoin = LineJoin.Miter;

        var library = Substitute.For<ICanvas>();
        var libraryCtx = new MauiGraphicsRenderContext(library);
        libraryCtx.UseLooksOf(Theme.Default);
        libraryCtx.DrawLines(corner, plain);
        library.Received(1).StrokeLineJoin = LineJoin.Miter;
        library.DidNotReceive().StrokeLineJoin = LineJoin.Round;
    }

    /// <summary>The canvas is told the math texts of each stage, as it is told the looks, and measures the box of a text by the installed
    /// metrics, so a label a figure kept is read and laid out once across draws on it too.</summary>
    [Fact]
    public void TheContext_TakesTheMathTextsOfAStage_AndMeasuresByTheInstalledMetrics()
    {
        var ctx = MauiRig.Fresh().Context;
        var texts = new MatPlotLibNet.Rendering.MathText.KeptMathTexts();

        Assert.Null(ctx.MathTextsTold);
        ctx.UseStageOf(texts, Theme.MatplotlibV2);

        Assert.Same(texts, ctx.MathTextsTold);
        Assert.Same(FontMetricsDefault.Current, ctx.BoxMetricsTold);
    }

    /// <summary>A stroke of no width has nothing to divide the lengths by, and draws nothing: the canvas is given no pattern.</summary>
    [Fact]
    public void ADashedStrokeOfNoWidth_IsGivenNoPattern()
    {
        var recorder = new StrokeRecorder();
        var ctx = new MauiGraphicsRenderContext(recorder.Canvas);

        ctx.DrawLine(new MplPoint(0, 0), new MplPoint(10, 10), new StrokeStyle(MplColor.Black, 0, Styling.LineStyle.Dashed));

        Assert.Empty(recorder.Held);
    }
}
