// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The head an arrow ends in: one <see cref="ArrowHead"/> for each <see cref="ArrowStyle"/>, chosen in one place, that
/// draws itself (a filled polygon or an open path) at the tip of a line, for annotation arrows and the edges of a network
/// graph.</summary>
public class ArrowHeadTests
{
    private static IReadOnlyList<Point> Polygon(ArrowStyle style, Point tip, double ux, double uy, double size = 8) =>
        Assert.IsType<SolidArrowHead>(ArrowHead.Of(style)).Polygon(tip, ux, uy, size);

    private static IReadOnlyList<PathSegment> Path(ArrowStyle style, Point tip, double ux, double uy, double size = 8) =>
        Assert.IsAssignableFrom<PathArrowHead>(ArrowHead.Of(style)).Path(tip, ux, uy, size);

    private static IEnumerable<Point> Ends(IReadOnlyList<PathSegment> path) =>
        path.Select(segment => segment switch { MoveToSegment m => m.Point, LineToSegment l => l.Point, _ => throw new InvalidOperationException() });

    private static RecordingRenderContext Drawn(ArrowStyle style, Point from, Point tip, double size = 8)
    {
        var ctx = new RecordingRenderContext();
        ArrowHead.Of(style).Draw(ctx, from, tip, size, Colors.Red);
        return ctx;
    }

    // ── The one mapping ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(ArrowStyle.None, typeof(NoArrowHead))]
    [InlineData(ArrowStyle.Simple, typeof(NoArrowHead))]
    [InlineData(ArrowStyle.FancyArrow, typeof(SolidArrowHead))]
    [InlineData(ArrowStyle.Wedge, typeof(SolidArrowHead))]
    [InlineData(ArrowStyle.CurveA, typeof(CurveArrowHead))]
    [InlineData(ArrowStyle.CurveB, typeof(CurveArrowHead))]
    [InlineData(ArrowStyle.CurveAB, typeof(CurveArrowHead))]
    [InlineData(ArrowStyle.BracketA, typeof(BracketArrowHead))]
    [InlineData(ArrowStyle.BracketB, typeof(BracketArrowHead))]
    [InlineData(ArrowStyle.BracketAB, typeof(BracketArrowHead))]
    [InlineData((ArrowStyle)99, typeof(NoArrowHead))]
    public void EveryStyle_HasItsHead(ArrowStyle style, Type head) => Assert.IsType(head, ArrowHead.Of(style));

    // ── Drawing at the end of a line ─────────────────────────────────────────

    /// <summary>A head is drawn for the two ends of the line it finishes, the one way an annotation, a graph edge and an arc
    /// ask: the unit direction is worked out here, once, instead of by each caller.</summary>
    [Fact]
    public void ASolidHead_IsFilledInTheColour_AtThePolygonOfTheUnitDirectionFromTheFirstEndToTheSecond()
    {
        var from = new Point(10, 20);
        var tip = new Point(13, 24);   // 3 across, 4 down: length 5, direction (0.6, 0.8)

        var call = Assert.IsType<DrawPolygonCall>(Assert.Single(Drawn(ArrowStyle.FancyArrow, from, tip).Calls));
        var byDirection = Polygon(ArrowStyle.FancyArrow, tip, 0.6, 0.8);

        Assert.Equal(Colors.Red, call.Fill);
        Assert.Null(call.Stroke);
        var drawn = call.Points;
        Assert.Equal(byDirection.Count, drawn.Length);
        for (int i = 0; i < drawn.Length; i++)
        {
            Assert.Equal(byDirection[i].X, drawn[i].X, 9);
            Assert.Equal(byDirection[i].Y, drawn[i].Y, 9);
        }
    }

    [Fact]
    public void AnOpenHead_IsStrokedOnePixelWideInTheColour_AlongThePathOfTheUnitDirection()
    {
        var call = Assert.IsType<DrawPathCall>(Assert.Single(Drawn(ArrowStyle.CurveB, new Point(0, 0), new Point(0, 50)).Calls));
        var byDirection = Path(ArrowStyle.CurveB, new Point(0, 50), 0, 1);

        Assert.Null(call.Fill);
        Assert.Equal(Colors.Red, call.Stroke);
        Assert.Equal(1.0, call.StrokeThickness);
        Assert.Equal(byDirection.Select(s => s.ToSvgPathData()), call.Segments.Select(s => s.ToSvgPathData()));
    }

    [Fact]
    public void TheHead_StandsBehindTheTip_NotAheadOfIt()
    {
        var polygon = Assert.IsType<DrawPolygonCall>(Drawn(ArrowStyle.FancyArrow, new Point(0, 0), new Point(100, 0)).Calls[0]).Points;

        Assert.Equal(new Point(100, 0), polygon[0]);
        Assert.All(polygon.Skip(1), vertex => Assert.True(vertex.X < 100));
    }

    [Fact]
    public void AnArrowOfNoLength_HasNoHead()
    {
        var spot = new Point(5, 5);
        foreach (ArrowStyle style in Enum.GetValues<ArrowStyle>())
            Assert.Empty(Drawn(style, spot, spot).Calls);
    }

    [Theory]
    [InlineData(ArrowStyle.None)]
    [InlineData(ArrowStyle.Simple)]
    public void ALineWithNoHead_DrawsNothingAtItsTip(ArrowStyle style) =>
        Assert.Empty(Drawn(style, new Point(0, 0), new Point(10, 0)).Calls);

    // ── The end a head stands at ─────────────────────────────────────────────
    // matplotlib 3.11.2 ArrowStyle: CurveA "<-" "an arrow with a head at its start point", CurveB "->" at its end point,
    // BracketA "]-" "an outward square bracket at its start", BracketB "-[" at its end; the start is where the line begins
    // (an annotation's text), the end where it points to (its target).

    [Theory]
    [InlineData(ArrowStyle.CurveA)]
    [InlineData(ArrowStyle.BracketA)]
    public void ASourceHead_StandsAtTheStartOfTheLine_PointingBackAlongIt(ArrowStyle style)
    {
        var from = new Point(10, 20);
        var tip = new Point(10, 70);   // the line runs straight down: direction (0, 1)

        var call = Assert.IsType<DrawPathCall>(Assert.Single(Drawn(style, from, tip).Calls));

        Assert.Equal(Path(style, from, 0, -1).Select(s => s.ToSvgPathData()), call.Segments.Select(s => s.ToSvgPathData()));
    }

    [Theory]
    [InlineData(ArrowStyle.CurveB)]
    [InlineData(ArrowStyle.BracketB)]
    public void ATargetHead_StandsAtTheEndOfTheLine_PointingAlongIt(ArrowStyle style)
    {
        var from = new Point(10, 20);
        var tip = new Point(10, 70);

        var call = Assert.IsType<DrawPathCall>(Assert.Single(Drawn(style, from, tip).Calls));

        Assert.Equal(Path(style, tip, 0, 1).Select(s => s.ToSvgPathData()), call.Segments.Select(s => s.ToSvgPathData()));
    }

    [Theory]
    [InlineData(ArrowStyle.CurveA, ArrowStyle.CurveB)]
    [InlineData(ArrowStyle.BracketA, ArrowStyle.BracketB)]
    public void TheTwoEnds_DrawDifferentPaths(ArrowStyle atSource, ArrowStyle atTarget)
    {
        var from = new Point(0, 0);
        var tip = new Point(40, 30);

        var source = Assert.IsType<DrawPathCall>(Assert.Single(Drawn(atSource, from, tip).Calls)).Segments.Select(s => s.ToSvgPathData());
        var target = Assert.IsType<DrawPathCall>(Assert.Single(Drawn(atTarget, from, tip).Calls)).Segments.Select(s => s.ToSvgPathData());

        Assert.NotEqual(target, source);
    }

    // ── The geometry of each head ────────────────────────────────────────────

    [Fact]
    public void FancyArrow_IsATriangle_TipFirst()
    {
        var tip = new Point(100, 50);

        var polygon = Polygon(ArrowStyle.FancyArrow, tip, 1, 0);

        Assert.Equal(3, polygon.Count);
        Assert.Equal(tip, polygon[0]);
    }

    [Fact]
    public void Wedge_IsWiderThanFancyArrow()
    {
        var tip = new Point(100, 50);

        var fancy = Polygon(ArrowStyle.FancyArrow, tip, 1, 0);
        var wedge = Polygon(ArrowStyle.Wedge, tip, 1, 0);

        Assert.Equal(3, wedge.Count);
        double fancyWidth = Math.Abs(fancy[1].Y - fancy[2].Y);
        double wedgeWidth = Math.Abs(wedge[1].Y - wedge[2].Y);
        Assert.Equal(8.0, fancyWidth, 9);
        Assert.Equal(16.0, wedgeWidth, 9);
    }

    [Fact]
    public void ABracket_IsOneLineAcrossTheTip()
    {
        var path = Path(ArrowStyle.BracketA, new Point(100, 50), 1, 0);

        Assert.Equal([new Point(100, 46), new Point(100, 54)], Ends(path));
    }

    [Fact]
    public void ACurveHead_IsTwoCurvesFanningOutFromTheTip()
    {
        var path = Path(ArrowStyle.CurveA, new Point(100, 50), 1, 0);

        Assert.Equal(4, path.Count);
        Assert.IsType<MoveToSegment>(path[0]);
        Assert.Equal(new Point(100, 50), Assert.IsType<BezierSegment>(path[1]).End);
        Assert.Equal(new Point(100, 50), Assert.IsType<MoveToSegment>(path[2]).Point);
    }

    // matplotlib 3.11.2: CurveAB "<->" an arrow with a head at both ends, BracketAB "]-[" a bracket at both ends.
    [Theory]
    [InlineData(ArrowStyle.CurveAB, ArrowStyle.CurveA, ArrowStyle.CurveB)]
    [InlineData(ArrowStyle.BracketAB, ArrowStyle.BracketA, ArrowStyle.BracketB)]
    public void ABothEndsHead_StandsAtTheTargetAndAtTheStartOfTheLine(ArrowStyle both, ArrowStyle atSource, ArrowStyle atTarget)
    {
        var from = new Point(10, 20);
        var tip = new Point(40, 60);

        var calls = Drawn(both, from, tip).Calls.Select(c => Assert.IsType<DrawPathCall>(c)).ToList();
        var source = Assert.IsType<DrawPathCall>(Assert.Single(Drawn(atSource, from, tip).Calls)).Segments.Select(s => s.ToSvgPathData());
        var target = Assert.IsType<DrawPathCall>(Assert.Single(Drawn(atTarget, from, tip).Calls)).Segments.Select(s => s.ToSvgPathData());

        Assert.Equal(2, calls.Count);
        Assert.Equal(target, calls[0].Segments.Select(s => s.ToSvgPathData()));
        Assert.Equal(source, calls[1].Segments.Select(s => s.ToSvgPathData()));
    }

    [Theory]
    [InlineData(ArrowStyle.CurveAB)]
    [InlineData(ArrowStyle.BracketAB)]
    public void ABothEndsHead_OfALineOfNoLength_HasNoHead(ArrowStyle style) =>
        Assert.Empty(Drawn(style, new Point(3, 3), new Point(3, 3)).Calls);

    // ── The open head: two strokes from the tip back along the line ──────────────

    private static RecordingRenderContext Barbs(ArrowHead head, Point from, Point tip, double size, StrokeStyle ink)
    {
        var ctx = new RecordingRenderContext();
        head.Draw(ctx, from, tip, size, ink);
        return ctx;
    }

    /// <summary>The library's open head, as the streamplot drew it: the barbs 6 px long for an arrow size of 1, each 0.4 rad from the
    /// way back along the line, the one at the smaller angle first (<c>head.AtAngle(-length, angle - 0.4)</c>, then <c>angle + 0.4</c>).</summary>
    [Fact]
    public void TheOpenHead_DrawsItsTwoBarbsBackFromTheTip_TheOneAtTheSmallerAngleFirst()
    {
        var from = new Point(10, 40);
        var tip = new Point(50, 70);
        double angle = Math.Atan2(30, 40), length = ArrowHead.OpenHeadLength;
        var ink = StrokeStyle.Solid(Colors.Red, 2.5);

        var calls = Barbs(ArrowHead.OpenHead, from, tip, length, ink).Calls.Select(c => Assert.IsType<DrawLineCall>(c)).ToList();

        Assert.Equal(2, calls.Count);
        foreach (var (call, turn) in calls.Zip(new[] { -0.4, 0.4 }))
        {
            var expected = tip.AtAngle(-length, angle + turn);
            Assert.Equal(tip, call.P1);
            Assert.Equal(expected.X, call.P2.X, 9);
            Assert.Equal(expected.Y, call.P2.Y, 9);
            Assert.Equal(2.5, call.Thickness);
            Assert.Equal(Colors.Red, call.Color);
        }
    }

    /// <summary>A 3-D quiver's head is the same head with its barbs in the other order: the one at the larger angle first.</summary>
    [Fact]
    public void TheMirroredOpenHead_DrawsTheSameTwoBarbsTheOtherWayRound()
    {
        var from = new Point(10, 40);
        var tip = new Point(50, 70);
        var ink = StrokeStyle.Solid(Colors.Red, 1);

        var open = Barbs(ArrowHead.OpenHead, from, tip, 6, ink).Of<DrawLineCall>();
        var mirrored = Barbs(ArrowHead.OpenHeadMirrored, from, tip, 6, ink).Of<DrawLineCall>();

        Assert.Equal(open[0].P2.X, mirrored[1].P2.X, 12);
        Assert.Equal(open[0].P2.Y, mirrored[1].P2.Y, 12);
        Assert.Equal(open[1].P2.X, mirrored[0].P2.X, 12);
        Assert.Equal(open[1].P2.Y, mirrored[0].P2.Y, 12);
    }

    /// <summary>The head of a scaled quiver arrow, as the quiver drew it: the barbs 2.5 rad from the direction the arrow points in
    /// (<c>end.AtAngle(length, angle + 2.5)</c>, then <c>angle - 2.5</c>), as long as the series' share of the shaft.</summary>
    [Fact]
    public void TheQuiverHead_DrawsItsBarbsAtTwoAndAHalfRadiansFromTheDirectionOfTheArrow()
    {
        var from = new Point(10, 10);
        var tip = new Point(40, 50);
        double angle = Math.Atan2(40, 30), length = 50 * 0.3;

        var calls = Barbs(ArrowHead.QuiverHead, from, tip, length, StrokeStyle.Solid(Colors.Blue, 1.5)).Calls.Select(c => Assert.IsType<DrawLineCall>(c)).ToList();

        Assert.Equal(2, calls.Count);
        foreach (var (call, turn) in calls.Zip(new[] { 2.5, -2.5 }))
        {
            var expected = tip.AtAngle(length, angle + turn);
            Assert.Equal(expected.X, call.P2.X, 9);
            Assert.Equal(expected.Y, call.P2.Y, 9);
        }
    }

    [Fact]
    public void AnOpenHead_OfALineOfNoLength_HasNoBarbs() =>
        Assert.Empty(Barbs(ArrowHead.OpenHead, new Point(3, 3), new Point(3, 3), 6, StrokeStyle.Solid(Colors.Red, 1)).Calls);

    [Fact]
    public void TheNamedOpenHeads_AreOneInstanceEach()
    {
        Assert.Same(ArrowHead.OpenHead, ArrowHead.OpenHead);
        Assert.NotSame(ArrowHead.OpenHead, ArrowHead.OpenHeadMirrored);
    }

    /// <summary>A head drawn in a stroke takes the stroke's colour and width, a filled head its colour and an open path head its width:
    /// the one-pixel head an annotation draws is the stroke of one pixel.</summary>
    [Fact]
    public void APathHeadDrawnInAStroke_TakesTheStrokesColourAndWidth()
    {
        var ctx = new RecordingRenderContext();
        ArrowHead.Of(ArrowStyle.CurveB).Draw(ctx, new Point(0, 0), new Point(30, 0), 8, StrokeStyle.Solid(Colors.Green, 3));

        var call = Assert.IsType<DrawPathCall>(Assert.Single(ctx.Calls));
        Assert.Equal(Colors.Green, call.Stroke);
        Assert.Equal(3, call.StrokeThickness);
    }
}
