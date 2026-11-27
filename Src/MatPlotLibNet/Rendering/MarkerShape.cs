// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The shape a marker is drawn in: one sealed leaf for each <see cref="MarkerStyle"/>, chosen in one place
/// (<see cref="Of(MarkerStyle)"/>). A series resolves its shape once and draws every point through it, so the style is not decided again for
/// each point, and a polygon holds its corners for a radius of one, so a point only scales and moves them instead of working
/// out a sine and a cosine for every corner.</summary>
/// <remarks>The <c>size</c> a shape is drawn at is the full edge of its bounding box, or its diameter, in pixels. The line, step
/// and scatter renderers and the legend swatch all draw through these shapes, so a marker looks the same in the plot and in the
/// legend. <see cref="CrossMarker"/> and <see cref="PlusMarker"/> are outline-only (as in matplotlib's <c>markers.py</c>): they
/// stroke in the fill colour, and at an eighth of their size when the shape gives no width.</remarks>
internal abstract class MarkerShape
{
    private static readonly NoMarker NoShape = new();
    private static readonly CircleMarker Circle = new();
    private static readonly SquareMarker Square = new();
    private static readonly CrossMarker Cross = new();
    private static readonly PlusMarker Plus = new();
    private static readonly PolygonMarker Triangle = PolygonMarker.Regular(sides: 3, rotationDeg: 0);
    private static readonly PolygonMarker TriangleDown = PolygonMarker.Regular(sides: 3, rotationDeg: 180);
    private static readonly PolygonMarker TriangleLeft = PolygonMarker.Regular(sides: 3, rotationDeg: -90);
    private static readonly PolygonMarker TriangleRight = PolygonMarker.Regular(sides: 3, rotationDeg: 90);
    private static readonly PolygonMarker Diamond = PolygonMarker.Regular(sides: 4, rotationDeg: 0);
    private static readonly PolygonMarker Pentagon = PolygonMarker.Regular(sides: 5, rotationDeg: 0);
    private static readonly PolygonMarker Hexagon = PolygonMarker.Regular(sides: 6, rotationDeg: 0);
    // matplotlib.markers (3.11.2, printed at a marker size of 1): '^' is the isosceles triangle (0, 1), (-1, -1), (1, -1) scaled by 0.5, so it fills 1 x 1;
    // 'D' is the unit square turned 45 degrees, so it fills 1.4142 x 1.4142. In the screen's frame (y grows downward), the corners as a share of the size.
    private static readonly PolygonMarker BoxTriangle = PolygonMarker.InBox([0, 0.5, -0.5], [-0.5, 0.5, 0.5]);
    private static readonly PolygonMarker BoxTriangleDown = PolygonMarker.InBox([0, -0.5, 0.5], [0.5, -0.5, -0.5]);
    private static readonly PolygonMarker BoxTriangleLeft = PolygonMarker.InBox([-0.5, 0.5, 0.5], [0, 0.5, -0.5]);
    private static readonly PolygonMarker BoxTriangleRight = PolygonMarker.InBox([0.5, -0.5, -0.5], [0, -0.5, 0.5]);
    private static readonly PolygonMarker BoxDiamond = PolygonMarker.InBox([0, Math.Sqrt(0.5), 0, -Math.Sqrt(0.5)], [-Math.Sqrt(0.5), 0, Math.Sqrt(0.5), 0]);

    // 5-point star: 10 corners alternating the outer radius and 0.38 of it, matplotlib's star shape.
    private static readonly PolygonMarker Star = PolygonMarker.Star(points: 5, innerRatio: 0.38);

    /// <summary>The shape of <paramref name="style"/>: the one place a marker style is mapped to its shape, the same instance on
    /// every call. <see cref="MarkerStyle.None"/> is a shape that draws nothing.</summary>
    /// <param name="style">The style a series or a legend entry asks for.</param>
    /// <exception cref="ArgumentOutOfRangeException">A value the enum does not declare: a style added to the enum and not here
    /// fails loudly instead of drawing nothing.</exception>
    internal static MarkerShape Of(MarkerStyle style) => style switch
    {
        MarkerStyle.None => NoShape,
        MarkerStyle.Circle => Circle,
        MarkerStyle.Square => Square,
        MarkerStyle.Cross => Cross,
        MarkerStyle.Plus => Plus,
        MarkerStyle.Triangle => Triangle,
        MarkerStyle.TriangleDown => TriangleDown,
        MarkerStyle.TriangleLeft => TriangleLeft,
        MarkerStyle.TriangleRight => TriangleRight,
        MarkerStyle.Diamond => Diamond,
        MarkerStyle.Pentagon => Pentagon,
        MarkerStyle.Hexagon => Hexagon,
        MarkerStyle.Star => Star,
        _ => throw style.NotAMember(nameof(style), $"MarkerShape has no shape for {style}"),
    };

    /// <summary>The shape of <paramref name="style"/> under <paramref name="theme"/>: matplotlib's, where the theme has its markers fill
    /// their box (<see cref="MarkerLook.FillTheirBox"/>: the triangles and the diamond; the other shapes are already matplotlib's), the
    /// library's own otherwise.</summary>
    /// <param name="style">The style a series or a legend entry asks for.</param>
    /// <param name="theme">The theme the marker is drawn under.</param>
    /// <returns>The shape of the style.</returns>
    internal static MarkerShape Of(MarkerStyle style, Theme theme) => !theme.Looks.Markers.FillTheirBox ? Of(style) : style switch
    {
        MarkerStyle.Triangle => BoxTriangle,
        MarkerStyle.TriangleDown => BoxTriangleDown,
        MarkerStyle.TriangleLeft => BoxTriangleLeft,
        MarkerStyle.TriangleRight => BoxTriangleRight,
        MarkerStyle.Diamond => BoxDiamond,
        _ => Of(style),
    };

    /// <summary>Makes a shape whose matplotlib marker joins its corners by <paramref name="join"/>.</summary>
    /// <param name="join">The join of matplotlib's marker of this shape.</param>
    private protected MarkerShape(JoinStyle join = JoinStyle.Miter) => Join = join;

    /// <summary>How matplotlib's marker of this shape joins its corners (<c>MarkerStyle.get_joinstyle()</c>): 'bevel' for the star, 'round'
    /// for the circle, the cross and the plus, 'miter' for every other shape. A line under the matplotlib themes draws its markers with it
    /// (<see cref="SeriesSlot.LineMarkerStyleOf"/>); the library's own themes mitre every marker.</summary>
    internal JoinStyle Join { get; }

    /// <summary>Whether a marker of this kind has a fill to edge, and so takes the edge its theme gives a filled marker (a scatter's, a line
    /// marker's): <see langword="true"/> for every shape but one made of strokes (<see cref="StrokedMarker"/>), which is strokes in its fill
    /// colour. The one fact of a kind the scatter, its 3-D form, the legend's handle, a line's markers and a box's fliers read.</summary>
    internal virtual bool TakesAnEdge => true;

    /// <summary>The style a marker of this kind is drawn with: <paramref name="face"/> edged by <paramref name="edge"/> at
    /// <paramref name="width"/>; a marker that takes no edge (<see cref="TakesAnEdge"/>) inks with its edge instead: the edge is the fill
    /// colour its strokes are drawn in, at the edge's width, with no outline.</summary>
    /// <param name="face">The colour of the fill; <see langword="null"/> for none.</param>
    /// <param name="edge">The colour of the outline; <see langword="null"/> for none.</param>
    /// <param name="width">The width of the outline, in pixels.</param>
    /// <returns>The style.</returns>
    internal ShapeStyle StyleOf(Color? face, Color? edge, double width) =>
        TakesAnEdge ? ShapeStyle.Edged(face, edge, width) : ShapeStyle.Edged(edge, null, width);

    /// <summary>The edge matplotlib's <c>markeredgecolor = 'auto'</c> gives a marker of this kind on a line of colour <paramref name="line"/>:
    /// the theme's edge of a filled marker where it names one (black in the classic style), else the line's colour; a marker that takes no
    /// edge always the line's.</summary>
    /// <param name="line">The colour of the line the marker sits on.</param>
    /// <param name="filledEdge">The edge the theme gives a filled marker; <see langword="null"/> when it names none.</param>
    /// <returns>The edge.</returns>
    internal Color AutoEdge(Color line, Color? filledEdge) => TakesAnEdge ? filledEdge ?? line : line;

    /// <summary>Draws one marker at <paramref name="center"/>; a marker of no size draws nothing.</summary>
    /// <param name="ctx">The context to draw on.</param>
    /// <param name="center">The centre of the marker, in pixels.</param>
    /// <param name="size">The full edge of the bounding box, or the diameter, in pixels.</param>
    /// <param name="shape">The fill, the outline and its width.</param>
    internal void Draw(IRenderContext ctx, Point center, double size, ShapeStyle shape)
    {
        if (size <= 0) return;
        DrawAt(ctx, center, size / 2.0, size, shape);
    }

    /// <summary>Draws the shape at <paramref name="center"/> with the half size <paramref name="r"/>.</summary>
    private protected abstract void DrawAt(IRenderContext ctx, Point center, double r, double size, ShapeStyle shape);
}

/// <summary>The shape of <see cref="MarkerStyle.None"/>: nothing.</summary>
internal sealed class NoMarker : MarkerShape
{
    private protected override void DrawAt(IRenderContext ctx, Point center, double r, double size, ShapeStyle shape) { }
}

/// <summary>A circle whose diameter is the size.</summary>
internal sealed class CircleMarker : MarkerShape
{
    /// <summary>matplotlib's <c>'o'</c> joins round.</summary>
    internal CircleMarker() : base(JoinStyle.Round) { }

    private protected override void DrawAt(IRenderContext ctx, Point center, double r, double size, ShapeStyle shape) =>
        ctx.DrawCircle(center, r, shape);
}

/// <summary>A square whose edge is the size.</summary>
internal sealed class SquareMarker : MarkerShape
{
    private protected override void DrawAt(IRenderContext ctx, Point center, double r, double size, ShapeStyle shape) =>
        ctx.DrawRectangle(new Rect(center.X - r, center.Y - r, size, size), shape);
}

/// <summary>A marker drawn as two strokes and no fill: it strokes in the fill colour (the outline when there is none, black when
/// neither), at the shape's width, or at an eighth of its size, at least one pixel, when the shape gives none.</summary>
internal abstract class StrokedMarker : MarkerShape
{
    /// <summary>matplotlib's <c>'x'</c> and <c>'+'</c> join round.</summary>
    private protected StrokedMarker() : base(JoinStyle.Round) { }

    /// <inheritdoc />
    /// <remarks>Strokes have no fill to edge: they are drawn in the fill colour.</remarks>
    internal sealed override bool TakesAnEdge => false;

    private protected sealed override void DrawAt(IRenderContext ctx, Point center, double r, double size, ShapeStyle shape)
    {
        var color = shape.Fill ?? shape.Stroke ?? Colors.Black;
        double thickness = shape.StrokeThickness > 0 ? shape.StrokeThickness : Math.Max(1.0, size / 8.0);
        DrawStrokes(ctx, center, r, StrokeStyle.Solid(color, thickness));
    }

    /// <summary>Draws the two strokes.</summary>
    private protected abstract void DrawStrokes(IRenderContext ctx, Point center, double r, StrokeStyle stroke);
}

/// <summary>A diagonal cross: two lines from corner to corner.</summary>
internal sealed class CrossMarker : StrokedMarker
{
    private protected override void DrawStrokes(IRenderContext ctx, Point center, double r, StrokeStyle stroke)
    {
        ctx.DrawLine(new Point(center.X - r, center.Y - r), new Point(center.X + r, center.Y + r), stroke);
        ctx.DrawLine(new Point(center.X - r, center.Y + r), new Point(center.X + r, center.Y - r), stroke);
    }
}

/// <summary>A plus: a horizontal and a vertical line through the centre.</summary>
internal sealed class PlusMarker : StrokedMarker
{
    private protected override void DrawStrokes(IRenderContext ctx, Point center, double r, StrokeStyle stroke)
    {
        ctx.DrawLine(new Point(center.X - r, center.Y), new Point(center.X + r, center.Y), stroke);
        ctx.DrawLine(new Point(center.X, center.Y - r), new Point(center.X, center.Y + r), stroke);
    }
}

/// <summary>A filled polygon, its corners held for a radius of one: each corner's cosine and sine and its share of the radius
/// (1 for a regular polygon; the star's inner corners a smaller one). A point is drawn at <c>centre + (r × share) × (cos, sin)</c>,
/// the very arithmetic that worked a corner out for every point, so the corners land on the same pixels to the last bit.</summary>
internal sealed class PolygonMarker : MarkerShape
{
    /// <summary>One corner of the polygon for a radius of one.</summary>
    /// <param name="Cos">The cosine of the corner's angle.</param>
    /// <param name="Sin">The sine of the corner's angle.</param>
    /// <param name="Share">The share of the radius the corner stands at.</param>
    private readonly record struct UnitCorner(double Cos, double Sin, double Share);

    private readonly UnitCorner[] _corners;

    private PolygonMarker(UnitCorner[] corners, JoinStyle join = JoinStyle.Miter) : base(join) => _corners = corners;

    /// <summary>A regular polygon inscribed in the circle of the radius, its first corner at 12 o'clock turned by
    /// <paramref name="rotationDeg"/> (a triangle with 0 points up, with 180 down).</summary>
    internal static PolygonMarker Regular(int sides, double rotationDeg)
    {
        var corners = new UnitCorner[sides];
        double rotRad = rotationDeg.ToRadians();
        for (int i = 0; i < sides; i++)
        {
            double theta = -Math.PI / 2 + rotRad + i * (2 * Math.PI / sides);
            corners[i] = new UnitCorner(Math.Cos(theta), Math.Sin(theta), 1.0);
        }
        return new PolygonMarker(corners);
    }

    /// <summary>A polygon whose corners are given as shares of the marker's size from its centre, in the screen's frame: the box a
    /// matplotlib marker path fills.</summary>
    /// <param name="xs">The shares of the size to the right of the centre.</param>
    /// <param name="ys">The shares of the size below the centre.</param>
    /// <returns>The marker with those corners.</returns>
    internal static PolygonMarker InBox(double[] xs, double[] ys)
    {
        // a corner is drawn at centre + (r * share) * (cos, sin) with r half the size, so a share of the size s is cos = 2 s
        var corners = new UnitCorner[xs.Length];
        for (int i = 0; i < corners.Length; i++) corners[i] = new UnitCorner(2 * xs[i], 2 * ys[i], 1.0);
        return new PolygonMarker(corners);
    }

    /// <summary>A star of <paramref name="points"/> points: twice as many corners, alternating the radius and
    /// <paramref name="innerRatio"/> of it. matplotlib's <c>'*'</c> joins them bevelled.</summary>
    internal static PolygonMarker Star(int points, double innerRatio)
    {
        var corners = new UnitCorner[points * 2];
        double step = Math.PI / points;
        for (int i = 0; i < points * 2; i++)
        {
            double theta = -Math.PI / 2 + i * step;
            corners[i] = new UnitCorner(Math.Cos(theta), Math.Sin(theta), i % 2 == 0 ? 1.0 : innerRatio);
        }
        return new PolygonMarker(corners, JoinStyle.Bevel);
    }

    /// <inheritdoc />
    /// <remarks>Every point gets corners of its own: a context may keep the list it is handed (a kept layer, a recording).</remarks>
    private protected override void DrawAt(IRenderContext ctx, Point center, double r, double size, ShapeStyle shape)
    {
        var points = new Point[_corners.Length];
        for (int i = 0; i < points.Length; i++)
        {
            var corner = _corners[i];
            double radius = r * corner.Share;
            points[i] = new Point(center.X + radius * corner.Cos, center.Y + radius * corner.Sin);
        }
        ctx.DrawPolygon(points, shape);
    }
}
