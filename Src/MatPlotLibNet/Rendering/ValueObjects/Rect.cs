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

    /// <summary>Centre point of the rectangle.</summary>
    public Point Center => new(X + Width / 2.0, Y + Height / 2.0);

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
