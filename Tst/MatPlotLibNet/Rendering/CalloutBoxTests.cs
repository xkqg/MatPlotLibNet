// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The box drawn behind an annotation's text (<see cref="CalloutBox"/>), and the leader line that joins a text that was
/// moved to the point it was put at. The pictures the figure draws with them are in <see cref="CalloutBoxRendererTests"/>.</summary>
public class CalloutBoxTests
{
    private static readonly Rect Text = new(10, 20, 30, 40);

    private static CalloutBox BoxOf(BoxStyle style, double padding = 4, double cornerRadius = 5) =>
        new(Text, style, padding, cornerRadius, Colors.Yellow, Colors.Black, 1.5);

    private static PathSegment[] PathDrawnBy(CalloutBox box)
    {
        var ctx = new RecordingRenderContext();
        box.Draw(ctx);
        var call = Assert.IsType<DrawPathCall>(Assert.Single(ctx.Calls));
        Assert.Equal(Colors.Yellow, call.Fill);
        Assert.Equal(Colors.Black, call.Stroke);
        Assert.Equal(1.5, call.StrokeThickness);
        return call.Segments;
    }

    // ── Box ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Box_IsTheTextBoundsPaddedOnEverySide() =>
        Assert.Equal(new Rect(6, 16, 38, 48), BoxOf(BoxStyle.Square).Box);

    // ── Draw ─────────────────────────────────────────────────────────────────

    [Fact]
    public void None_DrawsNothing()
    {
        var ctx = new RecordingRenderContext();

        BoxOf(BoxStyle.None).Draw(ctx);

        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void AnUndefinedStyle_DrawsNothing()
    {
        var ctx = new RecordingRenderContext();

        BoxOf((BoxStyle)99).Draw(ctx);

        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void Square_DrawsOneRectangle_TheTextBoundsPadded()
    {
        var ctx = new RecordingRenderContext();

        BoxOf(BoxStyle.Square).Draw(ctx);

        var call = Assert.IsType<DrawRectangleCall>(Assert.Single(ctx.Calls));
        Assert.Equal(new Rect(6, 16, 38, 48), call.Rect);
        Assert.Equal(Colors.Yellow, call.Fill);
        Assert.Equal(Colors.Black, call.Stroke);
        Assert.Equal(1.5, call.StrokeThickness);
    }

    [Fact]
    public void Round_DrawsTenSegments_FourBezierCornersOfTheRadius()
    {
        var path = PathDrawnBy(BoxOf(BoxStyle.Round, padding: 4, cornerRadius: 5));

        Assert.Equal(10, path.Length);
        Assert.Equal(new Point(11, 16), Assert.IsType<MoveToSegment>(path[0]).Point);   // box left + radius, box top
        Assert.Equal(4, path.OfType<BezierSegment>().Count());
        Assert.IsType<CloseSegment>(path[^1]);
    }

    [Fact]
    public void Round_ARadiusLargerThanHalfTheBox_IsHeldToHalfTheShorterSide()
    {
        // The box is 38 wide and 48 tall: the radius is held to 19, so the top edge runs from left + 19 to right - 19, a point.
        var path = PathDrawnBy(BoxOf(BoxStyle.Round, padding: 4, cornerRadius: 500));

        Assert.Equal(new Point(25, 16), Assert.IsType<MoveToSegment>(path[0]).Point);
        Assert.Equal(new Point(25, 16), Assert.IsType<LineToSegment>(path[1]).Point);
    }

    [Fact]
    public void RoundTooth_ZigzagsItsBottomEdge_FourPixelsUpOnEveryOtherTooth()
    {
        // Box 38 wide, radius 5: the zigzag runs from right - 5 = 39 down to left + 5 = 11 in steps of 6 while x > 17.
        var path = PathDrawnBy(BoxOf(BoxStyle.RoundTooth, padding: 4, cornerRadius: 5));

        var teeth = path.OfType<LineToSegment>().Where(l => l.Point.Y is 60 or 64).ToList();   // box bottom is 16 + 48 = 64
        Assert.Equal(new[] { new Point(33, 60), new Point(27, 64), new Point(21, 60) }, teeth.Take(3).Select(t => t.Point));
        Assert.Equal(new Point(11, 64), teeth.Last().Point);
        Assert.IsType<CloseSegment>(path[^1]);
    }

    [Fact]
    public void RoundTooth_ARadiusLargerThanHalfTheBox_IsHeldToHalfTheShorterSide()
    {
        var path = PathDrawnBy(BoxOf(BoxStyle.RoundTooth, padding: 4, cornerRadius: 500));

        Assert.Equal(new Point(25, 16), Assert.IsType<MoveToSegment>(path[0]).Point);
    }

    [Fact]
    public void Sawtooth_StartsAtTheTopLeftCorner_AndHasTwoSegmentsForEachTooth()
    {
        var path = PathDrawnBy(BoxOf(BoxStyle.Sawtooth, padding: 4));

        Assert.Equal(new Point(6, 16), Assert.IsType<MoveToSegment>(path[0]).Point);
        // Teeth are 5 wide: the 38 wide edges hold 7 each, the 48 tall ones 9 each; a tooth is a peak and a foot.
        int teeth = 7 + 9 + 7 + 9;
        Assert.Equal(1 + 2 * teeth + 1, path.Length);
        Assert.IsType<CloseSegment>(path[^1]);
    }

    [Fact]
    public void Sawtooth_TheTeethOfTheTopEdgeStandOutwards_ThoseOfTheBottomEdgeInwards()
    {
        var path = PathDrawnBy(BoxOf(BoxStyle.Sawtooth, padding: 4));

        var topPeak = Assert.IsType<LineToSegment>(path[1]).Point;                 // first peak of the top edge
        Assert.Equal(16 - 3, topPeak.Y);                                            // outwards: above the top, 3 px
        var bottomPeak = Assert.IsType<LineToSegment>(path[1 + 2 * (7 + 9)]).Point; // first peak of the bottom edge
        Assert.Equal(64 + 3, bottomPeak.Y);                                         // inwards for this sign: 3 px past the bottom
    }

    [Fact]
    public void ABoxTooSmallForATooth_StillHasOne()
    {
        var path = PathDrawnBy(new CalloutBox(new Rect(0, 0, 1, 1), BoxStyle.Sawtooth, 0, 0, Colors.Yellow, Colors.Black, 1.5));

        Assert.Equal(1 + 2 * 4 + 1, path.Length);
    }

    // ── From the annotation ──────────────────────────────────────────────────

    [Fact]
    public void BoxAround_TakesTheBoxFieldsOfTheAnnotation()
    {
        var annotation = new Annotation("note", 1, 2)
        {
            BoxStyle = BoxStyle.Round, BoxPadding = 7, BoxCornerRadius = 3,
            BoxFaceColor = Colors.Red, BoxEdgeColor = Colors.Blue, BoxLineWidth = 2.5,
        };

        var box = annotation.BoxAround(Text);

        Assert.Equal(new CalloutBox(Text, BoxStyle.Round, 7, 3, Colors.Red, Colors.Blue, 2.5), box);
    }

    // ── Leader line ──────────────────────────────────────────────────────────

    [Fact]
    public void ALeaderLine_IsASolidStrokeOfHalfAPixelByDefault()
    {
        var ctx = new RecordingRenderContext();

        ctx.DrawLeaderLine(new Point(1, 2), new Point(3, 4), Colors.Red);

        var call = Assert.IsType<DrawLineCall>(Assert.Single(ctx.Calls));
        Assert.Equal(new Point(1, 2), call.P1);
        Assert.Equal(new Point(3, 4), call.P2);
        Assert.Equal(Colors.Red, call.Color);
        Assert.Equal(0.5, call.Thickness);
        Assert.Equal(LineStyle.Solid, call.Style);
    }

    [Fact]
    public void ALeaderLine_TakesTheWidthItIsGiven()
    {
        var ctx = new RecordingRenderContext();

        ctx.DrawLeaderLine(new Point(0, 0), new Point(1, 1), Colors.Red, 2);

        Assert.Equal(2, Assert.IsType<DrawLineCall>(Assert.Single(ctx.Calls)).Thickness);
    }
}
