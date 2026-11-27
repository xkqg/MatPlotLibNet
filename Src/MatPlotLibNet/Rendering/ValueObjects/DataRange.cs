// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Represents the computed data range for a set of axes.</summary>
/// <param name="XMin">Minimum X value.</param>
/// <param name="XMax">Maximum X value.</param>
/// <param name="YMin">Minimum Y value.</param>
/// <param name="YMax">Maximum Y value.</param>
public readonly record struct DataRange(double XMin, double XMax, double YMin, double YMax)
{
    /// <summary>The box two data corners span, whichever way they were drawn: each coordinate's smaller value its low end. What a zoom
    /// rectangle and a brush select, however the pointer was dragged.</summary>
    /// <param name="a">One corner.</param>
    /// <param name="b">The other corner.</param>
    /// <returns>The box, low ends first.</returns>
    internal static DataRange Between(Point a, Point b) =>
        new(Math.Min(a.X, b.X), Math.Max(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.Y, b.Y));

    /// <summary>The range from <paramref name="low"/> to <paramref name="high"/> as they are given, so a reversed axis stays reversed: what
    /// a zoom and a pan move the edges of a plot to.</summary>
    /// <param name="low">The corner at the plot's low ends.</param>
    /// <param name="high">The corner at the plot's high ends.</param>
    /// <returns>The range.</returns>
    internal static DataRange FromCorners(Point low, Point high) => new(low.X, high.X, low.Y, high.Y);
}
