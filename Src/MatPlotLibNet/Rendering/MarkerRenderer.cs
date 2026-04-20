// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>
/// Single source of truth for drawing marker shapes. Both
/// <c>LineSeriesRenderer</c> and <c>ScatterSeriesRenderer</c> delegate here
/// (Phase M.2 of the v1.7.2 plan). Pre-Phase-M, line renderers drew every
/// marker as a circle and scatter renderers honoured only Square — this
/// helper implements all 12 non-<see cref="MarkerStyle.None"/> shapes.
/// <para>Geometry only — no caching, no state. The <c>size</c> argument is the
/// full bounding-box edge / diameter in pixels; the helper converts to
/// per-shape radii internally.</para>
/// <para><b>Cross / Plus</b> are outline-only markers (matches matplotlib's
/// <c>markers.py</c>). They have no fill; the caller's <c>fill</c>
/// argument is used as the stroke colour and <c>strokeWidth</c>
/// as the line thickness (falling back to <c>size / 8</c> when zero so the
/// marker remains visible at small sizes).</para>
/// </summary>
internal static class MarkerRenderer
{
    /// <summary>Draws a single marker at <paramref name="center"/>.</summary>
    /// <param name="ctx">Target render context.</param>
    /// <param name="style">Shape to draw.</param>
    /// <param name="center">Centre of the marker in pixel space.</param>
    /// <param name="size">Full bounding-box edge / diameter in pixels.</param>
    /// <param name="fill">Fill colour. For Cross / Plus this is used as the
    /// stroke colour instead (those shapes have no fill).</param>
    /// <param name="stroke">Edge colour for filled shapes. Ignored for
    /// Cross / Plus.</param>
    /// <param name="strokeWidth">Edge thickness for filled shapes; line
    /// thickness for Cross / Plus.</param>
    public static void Draw(
        IRenderContext ctx,
        MarkerStyle style,
        Point center,
        double size,
        Color? fill,
        Color? stroke,
        double strokeWidth)
    {
        if (style == MarkerStyle.None || size <= 0) return;

        double r = size / 2.0;
        switch (style)
        {
            case MarkerStyle.Circle:
                ctx.DrawCircle(center, r, fill, stroke, strokeWidth);
                return;

            case MarkerStyle.Square:
                ctx.DrawRectangle(
                    new Rect(center.X - r, center.Y - r, size, size),
                    fill, stroke, strokeWidth);
                return;

            case MarkerStyle.Triangle:
                ctx.DrawPolygon(EquilateralTriangle(center, r, rotationDeg: 0),
                    fill, stroke, strokeWidth);
                return;

            case MarkerStyle.TriangleDown:
                ctx.DrawPolygon(EquilateralTriangle(center, r, rotationDeg: 180),
                    fill, stroke, strokeWidth);
                return;

            case MarkerStyle.TriangleLeft:
                ctx.DrawPolygon(EquilateralTriangle(center, r, rotationDeg: -90),
                    fill, stroke, strokeWidth);
                return;

            case MarkerStyle.TriangleRight:
                ctx.DrawPolygon(EquilateralTriangle(center, r, rotationDeg: 90),
                    fill, stroke, strokeWidth);
                return;

            case MarkerStyle.Diamond:
                ctx.DrawPolygon(RegularPolygon(center, r, sides: 4, rotationDeg: 0),
                    fill, stroke, strokeWidth);
                return;

            case MarkerStyle.Pentagon:
                ctx.DrawPolygon(RegularPolygon(center, r, sides: 5, rotationDeg: 0),
                    fill, stroke, strokeWidth);
                return;

            case MarkerStyle.Hexagon:
                ctx.DrawPolygon(RegularPolygon(center, r, sides: 6, rotationDeg: 0),
                    fill, stroke, strokeWidth);
                return;

            case MarkerStyle.Star:
                // 5-point star: 10 vertices alternating outer (r) and inner radius.
                // Ratio 0.38 ≈ matplotlib's `mpl.markers.py` star shape.
                ctx.DrawPolygon(StarPolygon(center, r, points: 5, innerRatio: 0.38),
                    fill, stroke, strokeWidth);
                return;

            case MarkerStyle.Cross:
            {
                // Diagonal "X" — two crossing lines from corner to corner.
                var color = fill ?? stroke ?? new Color(0, 0, 0);
                double thickness = strokeWidth > 0 ? strokeWidth : Math.Max(1.0, size / 8.0);
                ctx.DrawLine(new Point(center.X - r, center.Y - r),
                    new Point(center.X + r, center.Y + r), color, thickness, LineStyle.Solid);
                ctx.DrawLine(new Point(center.X - r, center.Y + r),
                    new Point(center.X + r, center.Y - r), color, thickness, LineStyle.Solid);
                return;
            }

            case MarkerStyle.Plus:
            {
                // Horizontal + vertical line through centre.
                var color = fill ?? stroke ?? new Color(0, 0, 0);
                double thickness = strokeWidth > 0 ? strokeWidth : Math.Max(1.0, size / 8.0);
                ctx.DrawLine(new Point(center.X - r, center.Y),
                    new Point(center.X + r, center.Y), color, thickness, LineStyle.Solid);
                ctx.DrawLine(new Point(center.X, center.Y - r),
                    new Point(center.X, center.Y + r), color, thickness, LineStyle.Solid);
                return;
            }
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
