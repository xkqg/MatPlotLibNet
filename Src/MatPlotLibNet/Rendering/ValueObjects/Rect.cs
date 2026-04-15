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
