// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What a drawing target that draws nothing turns each drawing call into: the marks a legend keeps clear of. Every
/// call that stands for a mark adds it where it is drawn, and one that cannot be placed (a NaN) adds nothing, as it
/// draws nothing.</summary>
public class MarkCollectingContextTests
{
    private static readonly SvgRenderContext Inner = new();
    private static readonly StrokeStyle Stroke = new(Colors.Black, 1, LineStyle.Solid);
    private static readonly ShapeStyle Shape = new(Colors.Red, null, 0);
    private static readonly Font Font = ThemedFontProvider.DataTextFont(Theme.Default);

    private static (MarkCollectingContext Context, MarkObstacles.Builder Marks) Collector()
    {
        var marks = new MarkObstacles.Builder();
        return (new MarkCollectingContext(Inner, marks), marks);
    }

    // ---- strokes -----------------------------------------------------------------------------------------------------

    [Fact]
    public void DrawLine_BetweenTwoPlacedPoints_IsOnePathOfTwoVertices()
    {
        var (context, marks) = Collector();

        context.DrawLine(new Point(1, 2), new Point(30, 40), Stroke);

        var path = Assert.Single(marks.Build().Paths);
        Assert.Equal([new Point(1, 2), new Point(30, 40)], path);
    }

    [Fact]
    public void DrawLine_ToAPointThatCannotBePlaced_AddsNothing()
    {
        var (context, marks) = Collector();

        context.DrawLine(new Point(1, 2), new Point(double.NaN, 40), Stroke);

        Assert.Empty(marks.Build().Paths);
    }

    [Fact]
    public void DrawLines_BreaksAtAPointThatCannotBePlaced_AndLeavesALoneVertexOut()
    {
        var (context, marks) = Collector();
        IReadOnlyList<Point> points =
            [new(0, 0), new(5, 5), new(double.NaN, 1), new(9, 9), new(double.NaN, 2), new(20, 20), new(30, 30)];

        context.DrawLines(points, Stroke);

        var paths = marks.Build().Paths;
        Assert.Equal(2, paths.Count);
        Assert.Equal([new Point(0, 0), new Point(5, 5)], paths[0]);
        Assert.Equal([new Point(20, 20), new Point(30, 30)], paths[1]);
    }

    [Fact]
    public void DrawPolygon_IsOnePathOfItsVertices()
    {
        var (context, marks) = Collector();
        IReadOnlyList<Point> triangle = [new(0, 0), new(10, 0), new(5, 8)];

        context.DrawPolygon(triangle, Shape);

        Assert.Equal(triangle, Assert.Single(marks.Build().Paths));
    }

    // ---- fills -------------------------------------------------------------------------------------------------------

    [Fact]
    public void DrawCircle_IsItsCentre_AndNothingWhenTheCentreCannotBePlaced()
    {
        var (context, marks) = Collector();

        context.DrawCircle(new Point(7, 8), 3, Shape);
        context.DrawCircle(new Point(double.NaN, 8), 3, Shape);

        Assert.Equal([new Point(7, 8)], marks.Build().Points);
    }

    [Fact]
    public void DrawRectangle_IsItsBox_AndNothingWhenItCannotBePlaced()
    {
        var (context, marks) = Collector();

        context.DrawRectangle(new Rect(1, 2, 30, 40), Shape);
        context.DrawRectangle(new Rect(double.NaN, 2, 30, 40), Shape);

        Assert.Equal([new Rect(1, 2, 30, 40)], marks.Build().Boxes);
    }

    [Fact]
    public void DrawEllipse_IsItsBoundingBox_AndNothingWhenItCannotBePlaced()
    {
        var (context, marks) = Collector();

        context.DrawEllipse(new Rect(5, 6, 20, 10), Shape);
        context.DrawEllipse(new Rect(5, double.PositiveInfinity, 20, 10), Shape);

        Assert.Equal([new Rect(5, 6, 20, 10)], marks.Build().Boxes);
    }

    // ---- text --------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(TextAlignment.Left)]
    [InlineData(TextAlignment.Center)]
    [InlineData(TextAlignment.Right)]
    public void DrawText_IsTheBoxTheTextStandsIn(TextAlignment alignment)
    {
        var (context, marks) = Collector();
        var position = new Point(100, 80);

        context.DrawText("Peak", position, Font, alignment);

        var expected = new TextBox(position, Inner.MeasureText("Peak", Font), Font.Size, alignment,
            TextVerticalAlignment.Baseline, 0).Bounds();
        Assert.Equal([expected], marks.Build().Boxes);
    }

    [Fact]
    public void DrawText_Turned_IsTheBoundsOfTheTurnedBox()
    {
        var (context, marks) = Collector();
        var position = new Point(100, 80);

        context.DrawText("Peak", position, Font, TextAlignment.Left, 45);

        var expected = new TextBox(position, Inner.MeasureText("Peak", Font), Font.Size, TextAlignment.Left,
            TextVerticalAlignment.Baseline, 45).Bounds();
        var box = Assert.Single(marks.Build().Boxes);
        Assert.Equal(expected, box);
        Assert.True(box.Width < Inner.MeasureText("Peak", Font).Width + Font.Size && box.Height > Font.Size,
            "a text turned by 45 degrees is taller than it is upright");
    }

    [Fact]
    public void DrawText_AtAPositionThatCannotBePlaced_AddsNothing()
    {
        var (context, marks) = Collector();

        context.DrawText("Lost", new Point(double.NaN, 80), Font, TextAlignment.Left);

        Assert.Empty(marks.Build().Boxes);
    }

    // ---- paths -------------------------------------------------------------------------------------------------------

    [Fact]
    public void DrawPath_TakesTheEndOfEachStep_AndLeavesTheCloseOut()
    {
        var (context, marks) = Collector();
        PathSegment[] segments =
        [
            new MoveToSegment(new Point(0, 0)),
            new LineToSegment(new Point(10, 0)),
            new BezierSegment(new Point(12, 2), new Point(14, 4), new Point(16, 6)),
            new CloseSegment(),
        ];

        context.DrawPath(segments, Shape);

        Assert.Equal([new Point(0, 0), new Point(10, 0), new Point(16, 6)], Assert.Single(marks.Build().Paths));
    }

    [Fact]
    public void DrawPath_SamplesAnArcEverySixDegrees_FromItsStartAngleToItsEnd()
    {
        var (context, marks) = Collector();

        context.DrawPath([new ArcSegment(new Point(100, 100), 50, 30, 0, 90)], Shape);

        var path = Assert.Single(marks.Build().Paths);
        Assert.Equal(16, path.Count);                       // 90 degrees in steps of 6: 15 steps, 16 vertices
        Assert.Equal(150, path[0].X, 9);                    // on the ellipse at 0 degrees
        Assert.Equal(100, path[0].Y, 9);
        Assert.Equal(100, path[^1].X, 9);                   // and at 90 degrees, a quarter turn on
        Assert.Equal(130, path[^1].Y, 9);
    }

    [Fact]
    public void DrawPath_OfAnArcThatDoesNotSweep_IsTwoVerticesOnTheSamePoint()
    {
        var (context, marks) = Collector();

        context.DrawPath([new ArcSegment(new Point(10, 10), 5, 5, 45, 45)], Shape);

        var path = Assert.Single(marks.Build().Paths);
        Assert.Equal(2, path.Count);
        Assert.Equal(path[0].X, path[1].X, 9);
        Assert.Equal(path[0].Y, path[1].Y, 9);
    }

    [Fact]
    public void DrawPath_WithNothingToPlace_AddsNoPath()
    {
        var (context, marks) = Collector();

        context.DrawPath([new CloseSegment()], Shape);
        context.DrawPath([], Shape);

        Assert.Empty(marks.Build().Paths);
    }

    // ---- what is not a mark ------------------------------------------------------------------------------------------

    [Fact]
    public void ClippingAndOpacity_AreNotMarks()
    {
        var (context, marks) = Collector();

        context.PushClip(new Rect(0, 0, 100, 100));
        context.SetOpacity(0.5);
        context.PopClip();

        var collected = marks.Build();
        Assert.Empty(collected.Boxes);
        Assert.Empty(collected.Paths);
        Assert.Empty(collected.Points);
    }

    // ---- measuring ---------------------------------------------------------------------------------------------------

    [Fact]
    public void MeasureText_IsWhatTheContextItWrapsMeasures()
    {
        var (context, _) = Collector();

        Assert.Equal(Inner.MeasureText("Measured", Font), context.MeasureText("Measured", Font));
    }

    [Fact]
    public void MeasureRichText_IsWhatTheContextItWrapsMeasures()
    {
        var (context, _) = Collector();
        var rich = MathTextParser.Parse(@"$\alpha$ rate");

        Assert.Equal(((IRenderContext)Inner).MeasureRichText(rich, Font), context.MeasureRichText(rich, Font));
    }
}
