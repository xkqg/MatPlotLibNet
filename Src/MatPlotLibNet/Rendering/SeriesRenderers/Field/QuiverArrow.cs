// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>The arrow of an autoscaled quiver: the polygon matplotlib's <c>Quiver._h_arrows</c> makes, measured in shaft widths (the
/// units <c>units='width'</c> measures an arrow in, a shaft being <see cref="QuiverSeries.AutoScaleWidth"/> of the axes' width across).
/// One owner for the outline, read by the quiver's renderer and by its key's, which draws through the quiver's own arrow.</summary>
internal static class QuiverArrow
{
    /// <summary>How many points an arrow has, the heptagon of a dot as well: <c>ii = [0, 1, 2, 3, 2, 1, 0, 0]</c>.</summary>
    internal const int Vertices = 8;

    /// <summary>How wide the head is, in shaft widths: matplotlib's <c>headwidth</c>.</summary>
    private const double HeadWidth = 3;

    /// <summary>How long the head is, in shaft widths: <c>headlength</c>.</summary>
    private const double HeadLength = 5;

    /// <summary>How far back from the tip the barbs join the shaft, in shaft widths: <c>headaxislength</c>.</summary>
    private const double HeadAxisLength = 4.5;

    /// <summary>The length below which an arrow is shrunk to its head, in shaft widths: <c>minshaft * headlength</c>.</summary>
    private const double ShrunkBelow = 1 * HeadLength;

    /// <summary>The length below which an arrow is a dot, in shaft widths: <c>minlength</c>.</summary>
    private const double DotBelow = 1;

    /// <summary>The longest an arrow is made, in shaft widths: <c>np.clip(length, 0, 2 ** 16)</c>, where Agg's pixel values overflow.</summary>
    private const double Longest = 65536;

    /// <summary>The outline of an arrow <paramref name="length"/> shaft widths long that points along +X from a tail at the origin, with up
    /// positive: the tail's two corners, the shaft's end, the barb, the tip and the other barb, as <c>_h_arrows</c> orders them. An arrow
    /// shorter than <see cref="ShrunkBelow"/> is the head alone, shrunk by what it falls short; one shorter than
    /// <see cref="DotBelow"/> is a heptagon one shaft width across (<c>th = arange(0, 8) * (pi / 3)</c>).</summary>
    /// <param name="length">The arrow's length in shaft widths.</param>
    /// <param name="outline">Receives <see cref="Vertices"/> points, in shaft widths.</param>
    internal static void Outline(double length, Span<Point> outline)
    {
        length = Math.Clamp(length, 0, Longest);
        if (length < DotBelow)
        {
            for (int k = 0; k < Vertices; k++)
            {
                double th = k * (Math.PI / 3);
                outline[k] = new Point(Math.Cos(th) * DotBelow * 0.5, Math.Sin(th) * DotBelow * 0.5);
            }
            return;
        }

        // x: the tail, the end of the shaft, the barb and the tip; y: half the shaft, half the shaft, half the head, none.
        double x1, x2, x3, y0 = 0.5, y2 = 0.5 * HeadWidth;
        if (length < ShrunkBelow)
        {
            double shrink = length / ShrunkBelow;
            x1 = (ShrunkBelow - HeadAxisLength) * shrink;
            x2 = (ShrunkBelow - HeadLength) * shrink;
            x3 = ShrunkBelow * shrink;
            y0 *= shrink;
            y2 *= shrink;
        }
        else
        {
            x1 = length - HeadAxisLength;
            x2 = length - HeadLength;
            x3 = length;
        }

        outline[0] = new Point(0, y0);
        outline[1] = new Point(x1, y0);
        outline[2] = new Point(x2, y2);
        outline[3] = new Point(x3, 0);
        outline[4] = new Point(x2, -y2);
        outline[5] = new Point(x1, -y0);
        outline[6] = new Point(0, -y0);
        outline[7] = new Point(0, y0);
    }

    /// <summary>The arrow in pixels: its outline (<see cref="Outline"/>) turned to point along (<paramref name="cos"/>,
    /// <paramref name="sin"/>) with up positive on the screen, scaled by the shaft's width in pixels and put with its tail on
    /// <paramref name="tail"/>.</summary>
    /// <param name="tail">The tail, in pixels.</param>
    /// <param name="length">The arrow's length in shaft widths.</param>
    /// <param name="width">The shaft's width in pixels.</param>
    /// <param name="cos">The cosine of the direction the arrow points in on the screen.</param>
    /// <param name="sin">The sine of that direction, positive up.</param>
    /// <returns>The arrow's points in pixels.</returns>
    internal static Point[] Place(Point tail, double length, double width, double cos, double sin)
    {
        Span<Point> outline = stackalloc Point[Vertices];
        Outline(length, outline);
        var points = new Point[Vertices];
        for (int i = 0; i < Vertices; i++)
        {
            double x = outline[i].X, y = outline[i].Y;
            points[i] = new Point(tail.X + (x * cos - y * sin) * width, tail.Y - (x * sin + y * cos) * width);
        }
        return points;
    }
}
