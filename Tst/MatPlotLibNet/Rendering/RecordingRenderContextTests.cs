// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The recording surface keeps every call as its own typed leaf of <see cref="CallRecord"/>, so a test reads an argument by
/// name and a renamed argument fails to compile.</summary>
public sealed class RecordingRenderContextTests
{
    private static readonly Font Font = new() { Size = 10 };
    private static readonly StrokeStyle Line = StrokeStyle.Of(Colors.Red, 2, LineStyle.Dashed, JoinStyle.Bevel);
    private static readonly ShapeStyle Shape = new(Colors.Blue, Colors.Green, 3);
    private static readonly PathSegment[] Path = [new MoveToSegment(new(0, 0)), new LineToSegment(new(1, 2)), new CloseSegment()];

    /// <summary>Draws one of every call, in the order of <see cref="IRenderContext"/>.</summary>
    private static RecordingRenderContext EveryCall()
    {
        var ctx = new RecordingRenderContext();
        IRenderContext surface = ctx;
        surface.DrawLine(new(0, 1), new(2, 3), Line);
        surface.DrawLines([new(0, 0), new(1, 1)], Line);
        surface.DrawPolygon([new(0, 0), new(1, 0), new(0, 1)], Shape);
        surface.DrawCircle(new(5, 5), 4, Shape);
        surface.DrawRectangle(new(1, 2, 3, 4), Shape);
        surface.DrawEllipse(new(0, 0, 6, 2), Shape);
        surface.DrawText("plain", new(7, 8), Font, TextAlignment.Left);
        surface.DrawText("turned", new(9, 10), Font, TextAlignment.Right, 90);
        surface.DrawPath(Path, Shape);
        surface.PushClip(new(0, 0, 10, 10));
        ctx.PushClip(Path);
        surface.PopClip();
        surface.SetOpacity(0.5);
        surface.BeginGroup("legend");
        surface.EndGroup();
        surface.SetNextElementData("key", "value");
        surface.DrawRichText(new RichText([new TextSpan("rich")]), new(1, 1), Font, TextAlignment.Center);
        return ctx;
    }

    [Fact]
    public void EveryCall_IsKeptInOrder_AsTheLeafNamedAfterIt_AndIsCountedByItsLeaf()
    {
        var ctx = EveryCall();

        Assert.Equal(
            ["DrawLine", "DrawLines", "DrawPolygon", "DrawCircle", "DrawRectangle", "DrawEllipse", "DrawText", "DrawText", "DrawPath",
                "PushClip", "PushClipOutline", "PopClip", "SetOpacity", "BeginGroup", "EndGroup", "SetNextElementData", "DrawRichText"],
            ctx.Calls.Select(c => c.Kind));
        Assert.Equal((2, 1, 0), (ctx.Count<DrawTextCall>(), ctx.Count<PopClipCall>(), new RecordingRenderContext().Count<DrawLineCall>()));
        Assert.Empty(new RecordingRenderContext().Of<DrawLineCall>());
    }

    [Fact]
    public void EveryLeaf_KeepsWhatItWasHanded_ALineReadOffItsStroke_AShapeOffItsStyle()
    {
        var ctx = EveryCall();
        var (line, lines) = (ctx.Of<DrawLineCall>()[0], ctx.Of<DrawLinesCall>()[0]);
        var (circle, rich) = (ctx.Of<DrawCircleCall>()[0], ctx.Of<DrawRichTextCall>()[0]);

        Assert.Equal((new Point(0, 1), new Point(2, 3)), (line.P1, line.P2));
        Assert.Equal([new(0, 0), new(1, 1)], lines.Points);
        Assert.All(new StrokeCall[] { line, lines }, call =>
            Assert.Equal((Colors.Red, 2.0, LineStyle.Dashed, (JoinStyle?)JoinStyle.Bevel), (call.Color, call.Thickness, call.Style, call.Join)));
        Assert.All(ctx.Calls.OfType<ShapeCall>(), call =>
            Assert.Equal((Shape, (Color?)Colors.Blue, (Color?)Colors.Green, 3.0), (call.Shape, call.Fill, call.Stroke, call.StrokeThickness)));
        Assert.Equal((3, new Point(5, 5), 4.0), (ctx.Of<DrawPolygonCall>()[0].Points.Length, circle.Center, circle.Radius));
        Assert.Equal((new Rect(1, 2, 3, 4), new Rect(0, 0, 6, 2)), (ctx.Of<DrawRectangleCall>()[0].Rect, ctx.Of<DrawEllipseCall>()[0].Bounds));
        Assert.Equal(Path, ctx.Of<DrawPathCall>()[0].Segments);
        Assert.Equal(Path, ctx.Of<PushClipOutlineCall>()[0].Outline);
        Assert.Equal([("plain", new Point(7, 8), TextAlignment.Left, 0.0), ("turned", new Point(9, 10), TextAlignment.Right, 90.0)],
            ctx.Of<DrawTextCall>().Select(t => (t.Text, t.Position, t.Alignment, t.Rotation)));
        Assert.Equal(("rich", TextAlignment.Center, 0.0, 3), (rich.RichText.Spans[0].Text, rich.Alignment, rich.Rotation, ctx.Of<TextCall>().Count));
        Assert.Equal((new Rect(0, 0, 10, 10), 0.5, "legend"), (ctx.Of<PushClipCall>()[0].ClipRect, ctx.Of<SetOpacityCall>()[0].Opacity, ctx.Of<BeginGroupCall>()[0].CssClass));
        Assert.Equal(("key", "value"), (ctx.Of<SetNextElementDataCall>()[0].Key, ctx.Of<SetNextElementDataCall>()[0].Value));
    }

    [Fact]
    public void TheShapesDrawn_AreReadAsTheirGeometry_TheirFills_TheirFrame_AndTheColoursTheyPaint()
    {
        var ctx = EveryCall();
        ctx.DrawLines([new(5, 5), new(6, 6)], StrokeStyle.Solid(Colors.Red, 1));
        ctx.DrawRectangle(new(4, 4, 1, 1), ShapeStyle.Outlined(Colors.Black, 2));
        ctx.DrawRectangle(new(6, 6, 1, 1), Shape);

        Assert.Equal([new Rect(1, 2, 3, 4), new Rect(4, 4, 1, 1), new Rect(6, 6, 1, 1)], ctx.Rects());
        Assert.Equal([[new(0, 0), new(1, 0), new(0, 1)]], ctx.Polygons());
        Assert.Equal((2, 0), (ctx.Polylines().Count, ctx.Polylines(Colors.Blue).Count));
        Assert.Equal([[new(0, 0), new(1, 1)]], ctx.Polylines(Colors.Red, 2));
        Assert.Equal([[new(5, 5), new(6, 6)]], ctx.Polylines(width: 1));
        Assert.Equal(Enumerable.Repeat(Colors.Blue, 6), ctx.Fills());
        Assert.Equal((new Rect(4, 4, 1, 1), 2.0), (ctx.Calls.Frame().Rect, ctx.Calls.Frame().StrokeThickness));
        Assert.Equal(["DrawLine", "DrawLines", "DrawLines"], ctx.Calls.Where(c => c.Paints(Colors.Red)).Select(c => c.Kind));
        Assert.Equal((6, 6), (ctx.Calls.Count(c => c.Paints(Colors.Blue)), ctx.Calls.Count(c => c.Paints(Colors.Green))));
        Assert.DoesNotContain(ctx.Calls, c => c is TextCall or PopClipCall && c.Paints(Colors.Red));
    }

    [Fact]
    public void ACallHandedANumberThatIsNotFinite_OrATextThatSaysSo_FailsTheCheckThatEveryNumberIsFinite()
    {
        EveryCall().AssertNothingUndrawableReached();
        Action<RecordingRenderContext>[] undrawable =
        [
            c => c.DrawCircle(new(double.NaN, 0), 1, Shape),
            c => c.DrawRectangle(new(0, 0, double.PositiveInfinity, 1), Shape),
            c => c.DrawLines([new(0, 0), new(1, double.NaN)], Line),
            c => c.DrawPath([new MoveToSegment(new(0, 0)), new LineToSegment(new(double.NaN, 1))], Shape),
            c => c.DrawLine(new(0, 0), new(1, 1), Line with { Thickness = double.NaN }),
            c => c.DrawText("Infinity", new(0, 0), Font, TextAlignment.Left),
        ];
        Assert.All(undrawable, draw =>
        {
            var ctx = new RecordingRenderContext();
            draw(ctx);
            Assert.ThrowsAny<Exception>(ctx.AssertNothingUndrawableReached);
        });
    }

    [Fact]
    public void TwoSurfacesHandedTheSameCalls_HaveOneSignature_OneMovedPointTellsThemApart_AndDigitsRoundEveryPoint()
    {
        var moved = new RecordingRenderContext();
        moved.DrawCircle(new(1.0000001, 2), 1, Shape);
        var drawn = new RecordingRenderContext();
        drawn.DrawCircle(new(1, 2), 1, Shape);

        Assert.Equal(EveryCall().Signature(), EveryCall().Signature());
        Assert.NotEqual(drawn.Signature(), moved.Signature());
        Assert.Equal(drawn.Signature(6), moved.Signature(6));
        Assert.Contains(EveryCall().Signature(), call => call.StartsWith("DrawPath(", StringComparison.Ordinal) && call.Contains("L 1 2"));
        Assert.All(EveryCall().Signature<DrawTextCall>(), call => Assert.StartsWith("DrawText(", call));
        Assert.Equal(2, EveryCall().Signature<DrawTextCall>().Count);
    }
}
