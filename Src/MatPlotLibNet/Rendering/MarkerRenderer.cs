// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>
/// Single source of truth for drawing marker shapes. The line, step and scatter
/// renderers and the legend swatch delegate here, so a marker looks the same in the
/// plot and in the legend. It implements all 12 non-<see cref="MarkerStyle.None"/> shapes.
/// <para>Geometry only — no caching, no state. The <c>size</c> argument is the
/// full bounding-box edge / diameter in pixels; the helper converts to
/// per-shape radii internally.</para>
/// <para><b>Cross / Plus</b> are outline-only markers (matches matplotlib's
/// <c>markers.py</c>). They have no fill; <paramref name="shape"/>'s <c>Fill</c>
/// is used as the stroke colour instead (those shapes have no fill), with
/// <c>StrokeThickness</c> falling back to <c>size / 8</c> when zero so the
/// marker remains visible at small sizes.</para>
/// </summary>
internal static class MarkerRenderer
{
    extension(IRenderContext ctx)
    {
        /// <summary>Draws a single marker at <paramref name="center"/>.</summary>
        /// <param name="style">Shape to draw.</param>
        /// <param name="center">Centre of the marker in pixel space.</param>
        /// <param name="size">Full bounding-box edge / diameter in pixels.</param>
        /// <param name="shape">Fill/stroke/stroke-width. For Cross / Plus, <c>Fill</c> is used as the stroke
        /// colour (those shapes have no fill) and a zero <c>StrokeThickness</c> falls back to <c>size / 8</c>.</param>
        public void DrawMarker(MarkerStyle style, Point center, double size, ShapeStyle shape)
        {
            if (style == MarkerStyle.None || size <= 0) return;

            double r = size / 2.0;
            switch (style)
            {
                case MarkerStyle.Circle:
                    ctx.DrawCircle(center, r, shape);
                    return;

                case MarkerStyle.Square:
                    ctx.DrawRectangle(new Rect(center.X - r, center.Y - r, size, size), shape);
                    return;

                case MarkerStyle.Cross:
                case MarkerStyle.Plus:
                    DrawCrossOrPlus(ctx, style, center, r, size, shape);
                    return;
            }

            // Every remaining shape is a polygon — one switch expression decides its points, one shared
            // DrawPolygon call draws them. A MarkerStyle value reaching the default arm is a shape this
            // method was never extended for; DrawMarker_HandlesEveryDeclaredMarkerStyle (MarkerRendererTests)
            // fails loudly on it, rather than the shape silently drawing nothing.
            Point[] polygon = style switch
            {
                MarkerStyle.Triangle => EquilateralTriangle(center, r, rotationDeg: 0),
                MarkerStyle.TriangleDown => EquilateralTriangle(center, r, rotationDeg: 180),
                MarkerStyle.TriangleLeft => EquilateralTriangle(center, r, rotationDeg: -90),
                MarkerStyle.TriangleRight => EquilateralTriangle(center, r, rotationDeg: 90),
                MarkerStyle.Diamond => RegularPolygon(center, r, sides: 4, rotationDeg: 0),
                MarkerStyle.Pentagon => RegularPolygon(center, r, sides: 5, rotationDeg: 0),
                MarkerStyle.Hexagon => RegularPolygon(center, r, sides: 6, rotationDeg: 0),
                // 5-point star: 10 vertices alternating outer (r) and inner radius.
                // Ratio 0.38 ≈ matplotlib's `mpl.markers.py` star shape.
                MarkerStyle.Star => StarPolygon(center, r, points: 5, innerRatio: 0.38),
                _ => throw new ArgumentOutOfRangeException(nameof(style), style, $"MarkerRenderer has no shape for {style}"),
            };
            ctx.DrawPolygon(polygon, shape);
        }
    }

    private static void DrawCrossOrPlus(IRenderContext ctx, MarkerStyle style, Point center, double r, double size, ShapeStyle shape)
    {
        var color = shape.Fill ?? shape.Stroke ?? new Color(0, 0, 0);
        double thickness = shape.StrokeThickness > 0 ? shape.StrokeThickness : Math.Max(1.0, size / 8.0);
        var stroke = new StrokeStyle(color, thickness, LineStyle.Solid);
        if (style == MarkerStyle.Cross)
        {
            // Diagonal "X" — two crossing lines from corner to corner.
            ctx.DrawLine(new Point(center.X - r, center.Y - r), new Point(center.X + r, center.Y + r), stroke);
            ctx.DrawLine(new Point(center.X - r, center.Y + r), new Point(center.X + r, center.Y - r), stroke);
        }
        else
        {
            // Horizontal + vertical line through centre.
            ctx.DrawLine(new Point(center.X - r, center.Y), new Point(center.X + r, center.Y), stroke);
            ctx.DrawLine(new Point(center.X, center.Y - r), new Point(center.X, center.Y + r), stroke);
        }
    }

    /// <summary>Regular N-gon inscribed in a circle of radius <paramref name="radius"/>,
    /// first vertex at 12 o'clock (adjusted by <paramref name="rotationDeg"/>).</summary>
    private static Point[] RegularPolygon(Point center, double radius, int sides, double rotationDeg)
    {
        var pts = new Point[sides];
        double rotRad = rotationDeg * Math.PI / 180.0;
        for (int i = 0; i < sides; i++)
        {
            double theta = -Math.PI / 2 + rotRad + i * (2 * Math.PI / sides);
            pts[i] = new Point(
                center.X + radius * Math.Cos(theta),
                center.Y + radius * Math.Sin(theta));
        }
        return pts;
    }

    /// <summary>Equilateral triangle. rotationDeg=0 → apex up, 180 → apex down.</summary>
    private static Point[] EquilateralTriangle(Point center, double radius, double rotationDeg) =>
        RegularPolygon(center, radius, sides: 3, rotationDeg);

    /// <summary>N-pointed star as a 2N-vertex polygon alternating outer and
    /// inner radii. Matches matplotlib's mpl-style 5-point star at innerRatio=0.38.</summary>
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
}
