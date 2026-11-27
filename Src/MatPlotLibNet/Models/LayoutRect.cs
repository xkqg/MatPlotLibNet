// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Models;

/// <summary>A rectangle in fractions of the figure, counted from its bottom-left corner: matplotlib's <c>rect</c> of
/// <c>tight_layout</c>, <c>(left, bottom, right, top)</c>. The tight and the constrained layout fit the subplots, their labels and
/// the figure's title into it (<see cref="SubPlotSpacing.LayoutRect"/>). A class, so the spacing record that holds it, copied a
/// few times a draw, carries a reference rather than four numbers.</summary>
public sealed record LayoutRect
{
    /// <summary>Creates the rectangle from its four edges.</summary>
    /// <param name="left">The left edge, as a fraction of the figure's width from its left edge.</param>
    /// <param name="bottom">The bottom edge, as a fraction of the figure's height from its bottom edge.</param>
    /// <param name="right">The right edge, as a fraction of the figure's width from its left edge.</param>
    /// <param name="top">The top edge, as a fraction of the figure's height from its bottom edge.</param>
    /// <exception cref="ArgumentOutOfRangeException">An edge lies outside 0 to 1, or is not a number, or the left edge is not
    /// left of the right one, or the bottom edge not below the top one.</exception>
    public LayoutRect(double left, double bottom, double right, double top)
    {
        left.ThrowIfNot(IsRectangle(left, bottom, right, top), nameof(left),
            "must be the left edge of a rectangle in the figure: each edge is a fraction from 0 to 1, left below right and bottom below top");
        Left = left;
        Bottom = bottom;
        Right = right;
        Top = top;
    }

    /// <summary>The left edge, as a fraction of the figure's width from its left edge.</summary>
    public double Left { get; }

    /// <summary>The bottom edge, as a fraction of the figure's height from its bottom edge.</summary>
    public double Bottom { get; }

    /// <summary>The right edge, as a fraction of the figure's width from its left edge.</summary>
    public double Right { get; }

    /// <summary>The top edge, as a fraction of the figure's height from its bottom edge.</summary>
    public double Top { get; }

    /// <summary>Whether the four edges make a rectangle in the figure: each from 0 to 1, left of right, below top.</summary>
    /// <param name="left">The left edge, as a share of the figure's width.</param>
    /// <param name="bottom">The bottom edge, as a share of the figure's height.</param>
    /// <param name="right">The right edge, as a share of the figure's width.</param>
    /// <param name="top">The top edge, as a share of the figure's height.</param>
    /// <returns><see langword="true"/> when the edges make a rectangle in the figure.</returns>
    internal static bool IsRectangle(double left, double bottom, double right, double top) =>
        left >= 0 && left < right && right <= 1 && bottom >= 0 && bottom < top && top <= 1;
}
