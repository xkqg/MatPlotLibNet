// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Represents an axis-aligned rectangle defined by its position and dimensions.</summary>
/// <param name="X">The left edge coordinate.</param>
/// <param name="Y">The top edge coordinate.</param>
/// <param name="Width">The horizontal dimension.</param>
/// <param name="Height">The vertical dimension.</param>
public readonly record struct Rect(double X, double Y, double Width, double Height)
{
    /// <summary>Right edge X coordinate (X + Width).</summary>
    public double Right => X + Width;

    /// <summary>Bottom edge Y coordinate (Y + Height).</summary>
    public double Bottom => Y + Height;

    /// <summary>Centre point of the rectangle: its centre along X and along Y (the one place
    /// half a side is added to its start).</summary>
    public Point Center => new(this.CentreAlong(true), this.CentreAlong(false));

    /// <summary>The shorter of the two sides: what the circle of a round chart, the square a 3-D box is fitted in and the largest
    /// radius of a rounded corner are measured from. The one place the lesser of the width and the height is picked.</summary>
    internal double ShorterSide => Math.Min(Width, Height);

    /// <summary>The rectangle between two opposite corners, in either order: its left and top edges are the smaller
    /// coordinates, its width and height the distances between them, never negative. A mark placed by its data corners
    /// (a bar from its foot to its value, a box from one quartile to the other, a cell between its edges) is built here,
    /// so it covers the same area on an axis that runs backwards (limits set high to low, an inverted axis), where the
    /// corner that is the top left on an upright axis is the bottom right. Corners handed over top left first give the
    /// very numbers <c>new Rect(a.X, a.Y, b.X - a.X, b.Y - a.Y)</c> gives. A corner that is not finite (a value the axes
    /// cannot place) gives a rectangle that is not finite either.</summary>
    /// <param name="a">One corner, in pixels.</param>
    /// <param name="b">The opposite corner, in pixels.</param>
    /// <returns>The rectangle the two corners span.</returns>
    internal static Rect FromCorners(Point a, Point b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y));

    /// <summary>The smallest rectangle that holds every rectangle of <paramref name="rects"/> (matplotlib's <c>Bbox.union</c>).</summary>
    /// <param name="rects">The rectangles.</param>
    /// <returns>The enclosing rectangle; <see langword="null"/> when there is none.</returns>
    internal static Rect? Enclosing(IReadOnlyList<Rect> rects)
    {
        if (rects.Count == 0) return null;
        double left = double.MaxValue, top = double.MaxValue, right = double.MinValue, bottom = double.MinValue;
        for (int i = 0; i < rects.Count; i++)
        {
            var rect = rects[i];
            left = Math.Min(left, rect.X);
            top = Math.Min(top, rect.Y);
            right = Math.Max(right, rect.Right);
            bottom = Math.Max(bottom, rect.Bottom);
        }

        return new Rect(left, top, right - left, bottom - top);
    }

    /// <summary>This rectangle moved by the transform that maps the box <paramref name="from"/> onto the box
    /// <paramref name="to"/> (matplotlib's <c>BboxTransform(from, to)</c>): its position and its size are scaled by the ratio of the
    /// two boxes on each axis.</summary>
    /// <param name="from">The box the rectangle stands in.</param>
    /// <param name="to">The box it is mapped onto.</param>
    /// <returns>The rectangle in <paramref name="to"/>.</returns>
    internal Rect Remapped(Rect from, Rect to)
    {
        double scaleX = to.Width / from.Width, scaleY = to.Height / from.Height;
        return new Rect(to.X + (X - from.X) * scaleX, to.Y + (Y - from.Y) * scaleY, Width * scaleX, Height * scaleY);
    }

    /// <summary>Whether a plot area of this rectangle shows <paramref name="point"/>, the point an annotation in data coordinates
    /// annotates: inside it or within half a pixel past an edge, as matplotlib reads it (<c>Annotation._check_xy</c> asks
    /// <c>Axes.contains_point</c>, the axes patch with a radius of 1.0). An annotation whose point it does not show is not drawn: the
    /// label of a bar whose end lies past the limits, an annotation of a point past them.</summary>
    /// <param name="point">The annotated point, in pixels.</param>
    /// <returns>Whether the point is shown.</returns>
    internal bool Shows(Point point) =>
        point.X >= X - HalfAPixel && point.X <= Right + HalfAPixel && point.Y >= Y - HalfAPixel && point.Y <= Bottom + HalfAPixel;

    /// <summary>How far past an edge of the axes a point still stands in it for matplotlib (half of the radius of 1.0 its axes patch is
    /// asked with), in pixels.</summary>
    private const double HalfAPixel = 0.5;

    /// <summary>
    /// Returns <see langword="true"/> when this rectangle overlaps <paramref name="other"/>
    /// (standard AABB test — touching edges count as non-overlapping).
    /// </summary>
    public bool Intersects(Rect other) =>
        X < other.X + other.Width && other.X < X + Width &&
        Y < other.Y + other.Height && other.Y < Y + Height;

    /// <summary>Returns a new rectangle inflated outward by <paramref name="dx"/>/<paramref name="dy"/>.</summary>
    public Rect Inflate(double dx, double dy) =>
        new(X - dx, Y - dy, Width + 2 * dx, Height + 2 * dy);
}
