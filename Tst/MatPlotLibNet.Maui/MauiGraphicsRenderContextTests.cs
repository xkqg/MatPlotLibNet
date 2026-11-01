// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Maui.Graphics;
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
    [Fact]
    public void DrawLine_CallsCanvasDrawLine()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawLine(new MplPoint(10, 20), new MplPoint(30, 40),
            new StrokeStyle(MplColor.Red, 2.0, Styling.LineStyle.Solid));

        canvas.Received(1).DrawLine(10f, 20f, 30f, 40f);
    }

    [Fact]
    public void DrawLine_SetsStrokeColor()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawLine(new MplPoint(0, 0), new MplPoint(10, 10),
            new StrokeStyle(MplColor.Blue, 1.5, Styling.LineStyle.Solid));

        canvas.Received().StrokeColor = Arg.Is<Microsoft.Maui.Graphics.Color>(c =>
            Math.Abs(c.Red - 0f) < 0.01 && Math.Abs(c.Blue - 1f) < 0.01);
    }

    [Fact]
    public void DrawLine_SetsStrokeSize()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawLine(new MplPoint(0, 0), new MplPoint(10, 10),
            new StrokeStyle(MplColor.Red, 3.0, Styling.LineStyle.Solid));

        canvas.Received().StrokeSize = 3.0f;
    }

    [Fact]
    public void DrawLine_DashedStyle_SetsDashPattern()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawLine(new MplPoint(0, 0), new MplPoint(10, 10),
            new StrokeStyle(MplColor.Black, 1, Styling.LineStyle.Dashed));

        canvas.Received().StrokeDashPattern = Arg.Is<float[]>(p => p.Length > 0);
    }

    [Fact]
    public void DrawRectangle_WithFill_CallsFillRectangle()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawRectangle(new MplRect(10, 20, 100, 50),
            new ShapeStyle(MplColor.Blue, null, 0));

        canvas.Received(1).FillRectangle(10f, 20f, 100f, 50f);
    }

    [Fact]
    public void DrawRectangle_WithStroke_CallsDrawRectangle()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawRectangle(new MplRect(10, 20, 100, 50),
            new ShapeStyle(null, MplColor.Red, 2));

        canvas.Received(1).DrawRectangle(10f, 20f, 100f, 50f);
    }

    [Fact]
    public void DrawRectangle_WithBoth_CallsFillAndDraw()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawRectangle(new MplRect(10, 20, 100, 50),
            new ShapeStyle(MplColor.Blue, MplColor.Red, 2));

        canvas.Received(1).FillRectangle(10f, 20f, 100f, 50f);
        canvas.Received(1).DrawRectangle(10f, 20f, 100f, 50f);
    }

    [Fact]
    public void DrawCircle_WithFill_CallsFillCircle()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawCircle(new MplPoint(50, 50), 10, new ShapeStyle(MplColor.Green, null, 0));

        canvas.Received(1).FillCircle(50f, 50f, 10f);
    }

    [Fact]
    public void DrawText_CallsDrawString()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawText("Hello", new MplPoint(100, 200), new MplFont(), MatPlotLibNet.Rendering.TextAlignment.Left);

        canvas.Received(1).DrawString(
            "Hello",
            Arg.Any<float>(), Arg.Any<float>(),
            Arg.Any<HorizontalAlignment>());
    }

    [Fact]
    public void DrawText_SetsFontSize()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawText("Test", new MplPoint(0, 0), new MplFont { Size = 18 }, MatPlotLibNet.Rendering.TextAlignment.Center);

        canvas.Received().FontSize = 18f;
    }

    [Fact]
    public void MeasureText_ReturnsNonZeroSize()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        var size = ctx.MeasureText("Hello World", new MplFont { Size = 12 });

        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
    }

    [Fact]
    public void DrawLines_CallsDrawPath()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        var points = new List<MplPoint> { new(0, 0), new(10, 10), new(20, 0) };
        ctx.DrawLines(points, new StrokeStyle(MplColor.Red, 1.0, Styling.LineStyle.Solid));

        canvas.Received(1).DrawPath(Arg.Any<PathF>());
    }

    [Fact]
    public void DrawPolygon_WithFill_CallsFillPath()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        var points = new List<MplPoint> { new(0, 0), new(50, 0), new(50, 50), new(0, 50) };
        ctx.DrawPolygon(points, new ShapeStyle(MplColor.Blue, null, 0));

        canvas.Received(1).FillPath(Arg.Any<PathF>());
    }

    [Fact]
    public void DrawPolygon_WithStroke_CallsDrawPath()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        var points = new List<MplPoint> { new(0, 0), new(50, 0), new(50, 50), new(0, 50) };
        ctx.DrawPolygon(points, new ShapeStyle(null, MplColor.Red, 2));

        canvas.Received(1).DrawPath(Arg.Any<PathF>());
    }

    [Fact]
    public void DrawEllipse_WithFill_CallsFillEllipse()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawEllipse(new MplRect(10, 20, 60, 40),
            new ShapeStyle(MplColor.Green, null, 0));

        canvas.Received(1).FillEllipse(10f, 20f, 60f, 40f);
    }

    [Fact]
    public void DrawEllipse_WithStroke_CallsDrawEllipse()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawEllipse(new MplRect(10, 20, 60, 40),
            new ShapeStyle(null, MplColor.Red, 2));

        canvas.Received(1).DrawEllipse(10f, 20f, 60f, 40f);
    }

    [Fact]
    public void DrawPath_MoveToLineTo_CallsDrawPath()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        var segments = new List<MatPlotLibNet.Rendering.PathSegment>
        {
            new MatPlotLibNet.Rendering.MoveToSegment(new MplPoint(0, 0)),
            new MatPlotLibNet.Rendering.LineToSegment(new MplPoint(50, 50)),
            new MatPlotLibNet.Rendering.LineToSegment(new MplPoint(100, 0))
        };
        ctx.DrawPath(segments, new ShapeStyle(null, MplColor.Black, 1));

        canvas.Received(1).DrawPath(Arg.Any<PathF>());
    }

    [Fact]
    public void PushClip_CallsSaveStateAndClipRectangle()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.PushClip(new MplRect(10, 20, 100, 50));

        canvas.Received(1).SaveState();
        canvas.Received(1).ClipRectangle(10f, 20f, 100f, 50f);
    }

    [Fact]
    public void PopClip_CallsRestoreState()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.PopClip();

        canvas.Received(1).RestoreState();
    }

    [Fact]
    public void SetOpacity_SetsCanvasAlpha()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.SetOpacity(0.5);

        canvas.Received().Alpha = 0.5f;
    }

    [Fact]
    public void DrawCircle_WithStroke_CallsDrawCircle()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawCircle(new MplPoint(50, 50), 10, new ShapeStyle(null, MplColor.Red, 2));

        canvas.Received(1).DrawCircle(50f, 50f, 10f);
    }

    [Fact]
    public void DrawText_CenterAlignment_CallsDrawString()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawText("Center", new MplPoint(50, 50), new MplFont { Size = 14 },
            MatPlotLibNet.Rendering.TextAlignment.Center);

        canvas.Received(1).DrawString(
            "Center",
            Arg.Any<float>(), Arg.Any<float>(),
            HorizontalAlignment.Center);
    }

    // ── The line style of a shape's stroke ────────────────────────────────────
    // A shape's stroke is dashed by the rule a line's stroke is dashed by (ToMauiDashPattern: the pattern of the style, set on
    // the canvas before the stroke is drawn). The canvas keeps the pattern between calls, so a shape sets it every time:
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

    /// <summary>A path stroked dashed hands the canvas the pattern of a dashed line before it strokes.</summary>
    [Fact]
    public void ADashedPathStroke_SetsTheDashPatternOfADashedLine()
    {
        var recorder = new StrokeRecorder();
        new MauiGraphicsRenderContext(recorder.Canvas).DrawPath(TrianglePath, Stroked(Styling.LineStyle.Dashed));

        Assert.Equal(new[] { 5f, 2f }, Assert.Single(recorder.PatternsAtStroke));
    }

    /// <summary>The dotted and dash-dot styles set the pattern a line of that style sets.</summary>
    [Theory]
    [InlineData(Styling.LineStyle.Dotted, new[] { 1f, 3f })]
    [InlineData(Styling.LineStyle.DashDot, new[] { 5f, 2f, 1f, 2f })]
    public void ADottedOrDashDotPathStroke_SetsThePatternOfALineOfThatStyle(Styling.LineStyle style, float[] pattern)
    {
        var recorder = new StrokeRecorder();
        new MauiGraphicsRenderContext(recorder.Canvas).DrawPath(TrianglePath, Stroked(style));

        Assert.Equal(pattern, Assert.Single(recorder.PatternsAtStroke));
    }

    /// <summary>A path stroked solid sets an empty pattern, whatever the canvas held before: after a dashed line it must not
    /// inherit the dashes.</summary>
    [Fact]
    public void ASolidPathStroke_ClearsTheDashesALineLeftOnTheCanvas()
    {
        var recorder = new StrokeRecorder();
        var ctx = new MauiGraphicsRenderContext(recorder.Canvas);
        ctx.DrawLine(new MplPoint(0, 0), new MplPoint(10, 10), new StrokeStyle(MplColor.Black, 1, Styling.LineStyle.Dashed));
        Assert.NotEmpty(recorder.Held);

        ctx.DrawPath(TrianglePath, Stroked(Styling.LineStyle.Solid));

        Assert.Empty(Assert.Single(recorder.PatternsAtStroke));
    }

    /// <summary>A path whose stroke has the line style None draws no stroke, and its fill is still filled.</summary>
    [Fact]
    public void ANonePathStroke_DrawsNoStroke()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawPath(TrianglePath, new ShapeStyle(MplColor.Red, MplColor.Black, 2) { StrokeLineStyle = Styling.LineStyle.None });

        canvas.Received(1).FillPath(Arg.Any<PathF>());
        canvas.DidNotReceive().DrawPath(Arg.Any<PathF>());
    }

    /// <summary>A polygon stroked dashed hands the canvas the pattern of a dashed line before it strokes.</summary>
    [Fact]
    public void ADashedPolygonStroke_SetsTheDashPatternOfADashedLine()
    {
        var recorder = new StrokeRecorder();
        new MauiGraphicsRenderContext(recorder.Canvas).DrawPolygon(Triangle, Stroked(Styling.LineStyle.Dashed));

        Assert.Equal(new[] { 5f, 2f }, Assert.Single(recorder.PatternsAtStroke));
    }

    /// <summary>A polygon stroked solid sets an empty pattern, whatever the canvas held before.</summary>
    [Fact]
    public void ASolidPolygonStroke_ClearsTheDashesALineLeftOnTheCanvas()
    {
        var recorder = new StrokeRecorder();
        var ctx = new MauiGraphicsRenderContext(recorder.Canvas);
        ctx.DrawLine(new MplPoint(0, 0), new MplPoint(10, 10), new StrokeStyle(MplColor.Black, 1, Styling.LineStyle.Dashed));

        ctx.DrawPolygon(Triangle, Stroked(Styling.LineStyle.Solid));

        Assert.Empty(Assert.Single(recorder.PatternsAtStroke));
    }

    /// <summary>A polygon whose stroke has the line style None draws no stroke, and its fill is still filled.</summary>
    [Fact]
    public void ANonePolygonStroke_DrawsNoStroke()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawPolygon(Triangle, new ShapeStyle(MplColor.Red, MplColor.Black, 2) { StrokeLineStyle = Styling.LineStyle.None });

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
        Assert.All(recorder.PatternsAtStroke, pattern => Assert.Equal(new[] { 5f, 2f }, pattern));

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
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawPolygon(Triangle, new ShapeStyle(MplColor.Red, null, 2) { StrokeLineStyle = Styling.LineStyle.Dashed });

        canvas.DidNotReceive().StrokeDashPattern = Arg.Any<float[]>();
    }

    [Fact]
    public void DrawRectangle_NullFill_NullStroke_NoCanvasCalls()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawRectangle(new MplRect(10, 20, 100, 50),
            new ShapeStyle(null, null, 0));

        canvas.DidNotReceive().FillRectangle(Arg.Any<float>(), Arg.Any<float>(),
            Arg.Any<float>(), Arg.Any<float>());
        canvas.DidNotReceive().DrawRectangle(Arg.Any<float>(), Arg.Any<float>(),
            Arg.Any<float>(), Arg.Any<float>());
    }
}
