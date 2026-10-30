// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>What one mark of a hatch tile is.</summary>
public enum HatchMarkKind
{
    /// <summary>A line from (X1, Y1) to (X2, Y2).</summary>
    Line = 0,

    /// <summary>A dot of radius one centred on (X1, Y1).</summary>
    Dot = 1,
}

/// <summary>One mark of a hatch tile, in tile units: a line from (<paramref name="X1"/>, <paramref name="Y1"/>) to
/// (<paramref name="X2"/>, <paramref name="Y2"/>), or a dot at (<paramref name="X1"/>, <paramref name="Y1"/>), whose end
/// point is zero.</summary>
/// <param name="Kind">Whether it is a line or a dot.</param>
/// <param name="X1">The line's start X, or the dot's centre X.</param>
/// <param name="Y1">The line's start Y, or the dot's centre Y.</param>
/// <param name="X2">The line's end X; zero for a dot.</param>
/// <param name="Y2">The line's end Y; zero for a dot.</param>
public readonly record struct HatchMark(HatchMarkKind Kind, double X1, double Y1, double X2, double Y2);

/// <summary>The marks of the tile a hatch repeats over a fill, written once. The SVG backend draws them as a pattern, the
/// raster backend as a shader and the MAUI backend as a pattern paint, so a hatch looks the same on every surface and a
/// pattern is changed in one place.</summary>
public static class HatchTile
{
    /// <summary>The side of the square tile in layout units. Six reads as a hatch at a glance without moiré at chart scale.</summary>
    public const double Size = 6.0;

    private const double T = Size;

    private static HatchMark Line(double x1, double y1, double x2, double y2) => new(HatchMarkKind.Line, x1, y1, x2, y2);

    private static HatchMark Dot() => new(HatchMarkKind.Dot, T / 2, T / 2, 0, 0);

    /// <summary>The marks of the tile for <paramref name="hatch"/>, in the order they are drawn. The diagonals are drawn
    /// with a short stroke at each end of the tile as well, so that the line meets itself where tiles abut. A plain fill, and
    /// a value that is not a pattern, have none.</summary>
    /// <param name="hatch">The pattern.</param>
    /// <returns>The marks of one tile; empty when there is nothing to draw.</returns>
    public static IReadOnlyList<HatchMark> MarksOf(HatchPattern hatch) => hatch switch
    {
        HatchPattern.ForwardDiagonal => [Line(0, T, T, 0), Line(-1, 1, 1, -1), Line(T - 1, T + 1, T + 1, T - 1)],
        HatchPattern.BackDiagonal => [Line(0, 0, T, T), Line(-1, T - 1, 1, T + 1), Line(T - 1, -1, T + 1, 1)],
        HatchPattern.Horizontal => [Line(0, T / 2, T, T / 2)],
        HatchPattern.Vertical => [Line(T / 2, 0, T / 2, T)],
        HatchPattern.Cross => [Line(0, T / 2, T, T / 2), Line(T / 2, 0, T / 2, T)],
        HatchPattern.DiagonalCross => [Line(0, T, T, 0), Line(0, 0, T, T)],
        HatchPattern.Dots => [Dot()],
        HatchPattern.Stars => [Dot(), Line(0, T, T, 0), Line(0, 0, T, T)],
        _ => [],
    };
}
