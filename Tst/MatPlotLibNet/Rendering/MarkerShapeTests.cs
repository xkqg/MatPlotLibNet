// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The shape a marker is drawn in: one <see cref="MarkerShape"/> for each <see cref="MarkerStyle"/>, chosen in one place
/// and resolved once for a series, a polygon holding its corners for a radius of one so a point only scales and moves them.</summary>
public class MarkerShapeTests
{
    private static readonly ShapeStyle Paint = new(Colors.Blue, Colors.Black, 1);

    private static RecordingRenderContext Drawn(MarkerStyle style, Point center, double size, ShapeStyle? shape = null)
    {
        var ctx = new RecordingRenderContext();
        MarkerShape.Of(style).Draw(ctx, center, size, shape ?? Paint);
        return ctx;
    }

    // ── The one mapping ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(MarkerStyle.None, typeof(NoMarker))]
    [InlineData(MarkerStyle.Circle, typeof(CircleMarker))]
    [InlineData(MarkerStyle.Square, typeof(SquareMarker))]
    [InlineData(MarkerStyle.Cross, typeof(CrossMarker))]
    [InlineData(MarkerStyle.Plus, typeof(PlusMarker))]
    [InlineData(MarkerStyle.Triangle, typeof(PolygonMarker))]
    [InlineData(MarkerStyle.TriangleDown, typeof(PolygonMarker))]
    [InlineData(MarkerStyle.TriangleLeft, typeof(PolygonMarker))]
    [InlineData(MarkerStyle.TriangleRight, typeof(PolygonMarker))]
    [InlineData(MarkerStyle.Diamond, typeof(PolygonMarker))]
    [InlineData(MarkerStyle.Pentagon, typeof(PolygonMarker))]
    [InlineData(MarkerStyle.Hexagon, typeof(PolygonMarker))]
    [InlineData(MarkerStyle.Star, typeof(PolygonMarker))]
    public void EveryStyle_HasItsShape(MarkerStyle style, Type shape) => Assert.IsType(shape, MarkerShape.Of(style));

    [Fact]
    public void AStyleTheEnumDoesNotDeclare_Throws_NamingIt()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => MarkerShape.Of((MarkerStyle)999));
        Assert.Contains("999", ex.Message);
    }

    // ── What each shape draws ────────────────────────────────────────────────

    [Fact]
    public void NoMarker_DrawsNothing() => Assert.Empty(Drawn(MarkerStyle.None, new Point(5, 5), 10).Calls);

    [Theory]
    [InlineData(0.0)]
    [InlineData(-3.0)]
    public void AMarkerOfNoSize_DrawsNothing(double size)
    {
        foreach (MarkerStyle style in Enum.GetValues<MarkerStyle>())
            Assert.Empty(Drawn(style, new Point(5, 5), size).Calls);
    }

    [Fact]
    public void ACircle_IsACircleOfHalfTheSize()
    {
        var call = Assert.IsType<DrawCircleCall>(Assert.Single(Drawn(MarkerStyle.Circle, new Point(10, 20), 8).Calls));
        Assert.Equal(new Point(10, 20), call.Center);
        Assert.Equal(4.0, call.Radius);
    }

    [Fact]
    public void ASquare_IsTheSquareOfTheSizeAroundTheCentre()
    {
        var call = Assert.IsType<DrawRectangleCall>(Assert.Single(Drawn(MarkerStyle.Square, new Point(10, 20), 8).Calls));
        Assert.Equal(new Rect(6, 16, 8, 8), call.Rect);
    }

    [Fact]
    public void ACross_IsTwoDiagonals_AndAPlus_TheHorizontalAndTheVertical()
    {
        var cross = Drawn(MarkerStyle.Cross, new Point(10, 20), 8).Of<DrawLineCall>();
        Assert.Equal([new Point(6, 16), new Point(6, 24)], cross.Select(c => c.P1));
        Assert.Equal([new Point(14, 24), new Point(14, 16)], cross.Select(c => c.P2));

        var plus = Drawn(MarkerStyle.Plus, new Point(10, 20), 8).Of<DrawLineCall>();
        Assert.Equal([new Point(6, 20), new Point(10, 16)], plus.Select(c => c.P1));
        Assert.Equal([new Point(14, 20), new Point(10, 24)], plus.Select(c => c.P2));
    }

    [Fact]
    public void AStrokedMarker_StrokesInTheFill_AndAtAnEighthOfItsSizeWhenItHasNoWidth()
    {
        var line = Drawn(MarkerStyle.Cross, new Point(0, 0), 16, new ShapeStyle(Colors.Red, null, 0)).Of<DrawLineCall>()[0];
        Assert.Equal(Colors.Red, line.Color);
        Assert.Equal(2.0, line.Thickness);

        var thin = Drawn(MarkerStyle.Plus, new Point(0, 0), 4, new ShapeStyle(null, Colors.Green, 0)).Of<DrawLineCall>()[0];
        Assert.Equal(Colors.Green, thin.Color);
        Assert.Equal(1.0, thin.Thickness);

        var bare = Drawn(MarkerStyle.Plus, new Point(0, 0), 4, new ShapeStyle(null, null, 3)).Of<DrawLineCall>()[0];
        Assert.Equal(new Color(0, 0, 0), bare.Color);
        Assert.Equal(3.0, bare.Thickness);
    }

    /// <summary>A marker kind makes its own style: a filled one is its face edged by its edge; one made of strokes is its edge as the ink
    /// it strokes in (the fill colour its drawing reads), at the edge's width, so no caller branches on the kind.</summary>
    [Fact]
    public void AMarkerKind_MakesItsOwnStyle_AStrokedOneInksWithItsEdge()
    {
        Assert.Equal(ShapeStyle.Edged(Colors.Red, Colors.Blue, 1.5), MarkerShape.Of(MarkerStyle.Circle).StyleOf(Colors.Red, Colors.Blue, 1.5));
        Assert.Equal(ShapeStyle.Edged(Colors.Blue, null, 1.5), MarkerShape.Of(MarkerStyle.Plus).StyleOf(Colors.Red, Colors.Blue, 1.5));
        Assert.Equal(ShapeStyle.Edged(Colors.Blue, null, 2), MarkerShape.Of(MarkerStyle.Cross).StyleOf(null, Colors.Blue, 2));
    }

    /// <summary>The edge matplotlib's <c>markeredgecolor='auto'</c> takes: a filled marker the theme's edge for filled markers where it names
    /// one, else the line's colour; a marker made of strokes always the line's colour.</summary>
    [Fact]
    public void TheAutoEdge_OfAFilledMarker_IsTheThemesFilledEdge_OfAStrokedOneTheLine()
    {
        Assert.Equal(Colors.Black, MarkerShape.Of(MarkerStyle.Square).AutoEdge(Colors.Red, Colors.Black));
        Assert.Equal(Colors.Red, MarkerShape.Of(MarkerStyle.Square).AutoEdge(Colors.Red, null));
        Assert.Equal(Colors.Red, MarkerShape.Of(MarkerStyle.Plus).AutoEdge(Colors.Red, Colors.Black));
    }

    // ── A polygon is its unit corners, scaled and moved: the very points the per-point arithmetic drew ──

    /// <summary>The corners of a regular polygon as they were worked out for every point before the corners were held.</summary>
    private static Point[] RegularPolygon(Point center, double radius, int sides, double rotationDeg)
    {
        var pts = new Point[sides];
        double rotRad = rotationDeg * Math.PI / 180.0;
        for (int i = 0; i < sides; i++)
        {
            double theta = -Math.PI / 2 + rotRad + i * (2 * Math.PI / sides);
            pts[i] = new Point(center.X + radius * Math.Cos(theta), center.Y + radius * Math.Sin(theta));
        }
        return pts;
    }

    /// <summary>The corners of the star as they were worked out for every point.</summary>
    private static Point[] StarPolygon(Point center, double outerRadius, int points, double innerRatio)
    {
        var pts = new Point[points * 2];
        double innerRadius = outerRadius * innerRatio;
        double step = Math.PI / points;
        for (int i = 0; i < points * 2; i++)
        {
            double theta = -Math.PI / 2 + i * step;
            double r = (i % 2 == 0) ? outerRadius : innerRadius;
            pts[i] = new Point(center.X + r * Math.Cos(theta), center.Y + r * Math.Sin(theta));
        }
        return pts;
    }

    public static TheoryData<MarkerStyle> Polygons =>
    [
        MarkerStyle.Triangle, MarkerStyle.TriangleDown, MarkerStyle.TriangleLeft, MarkerStyle.TriangleRight,
        MarkerStyle.Diamond, MarkerStyle.Pentagon, MarkerStyle.Hexagon, MarkerStyle.Star,
    ];

    private static Point[] Expected(MarkerStyle style, Point c, double r) => style switch
    {
        MarkerStyle.Triangle => RegularPolygon(c, r, 3, 0),
        MarkerStyle.TriangleDown => RegularPolygon(c, r, 3, 180),
        MarkerStyle.TriangleLeft => RegularPolygon(c, r, 3, -90),
        MarkerStyle.TriangleRight => RegularPolygon(c, r, 3, 90),
        MarkerStyle.Diamond => RegularPolygon(c, r, 4, 0),
        MarkerStyle.Pentagon => RegularPolygon(c, r, 5, 0),
        MarkerStyle.Hexagon => RegularPolygon(c, r, 6, 0),
        _ => StarPolygon(c, r, 5, 0.38),
    };

    [Theory]
    [MemberData(nameof(Polygons))]
    public void APolygon_IsDrawnAtTheSamePoints_ToTheLastBit(MarkerStyle style)
    {
        foreach (var (center, size) in new[] { (new Point(10, 20), 8.0), (new Point(-3.7, 1e5), 13.25), (new Point(0.1, 0.2), 0.3) })
        {
            var call = Assert.IsType<DrawPolygonCall>(Assert.Single(Drawn(style, center, size).Calls));
            var drawn = call.Points;
            var expected = Expected(style, center, size / 2.0);
            Assert.Equal(expected.Length, drawn.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(expected[i].X), BitConverter.DoubleToInt64Bits(drawn[i].X));
                Assert.Equal(BitConverter.DoubleToInt64Bits(expected[i].Y), BitConverter.DoubleToInt64Bits(drawn[i].Y));
            }
        }
    }
}
