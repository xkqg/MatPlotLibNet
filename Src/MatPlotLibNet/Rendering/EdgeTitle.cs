// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>How an axis title stands beside the edge its ticks hang from, the one difference between the titles of the four edges
/// (<see cref="EdgeTerms.Title"/>): upright and centred along a top or a bottom edge (<see cref="Upright"/>), turned a quarter and centred
/// along a left or a right one (<see cref="Turned"/>). Every axis title of a Cartesian axes is drawn through it, so the X title below the
/// plot, the top axis' title, a linked axis' title below the plot and every Y axis' title stand by the rule of their edge.</summary>
internal abstract class EdgeTitle
{
    /// <summary>The title of a top or a bottom edge: upright, centred along the plot's width.</summary>
    internal static EdgeTitle Upright { get; } = new UprightEdgeTitle();

    /// <summary>The title of a left or a right edge: turned a quarter, centred along the plot's height.</summary>
    internal static EdgeTitle Turned { get; } = new TurnedEdgeTitle();

    /// <summary>Draws the title of <paramref name="axis"/> beside <paramref name="edge"/>, clear of its tick labels, its style's pad on
    /// top; nothing when the axis has no title.</summary>
    /// <param name="ctx">The surface drawn on.</param>
    /// <param name="axes">The axes the axis belongs to.</param>
    /// <param name="axis">The axis whose title is drawn.</param>
    /// <param name="edge">The edge it stands beside.</param>
    /// <param name="theme">The active theme.</param>
    /// <param name="plot">The plot area.</param>
    /// <param name="edgeLine">The coordinate across the edge the axis' ticks hang from.</param>
    /// <param name="tallestTickLabel">How far the tick labels drawn there reach past the mark and the pad; 0 when none was drawn.</param>
    internal abstract void Draw(IRenderContext ctx, Axes axes, Axis axis, EdgeTerms edge, Theme theme, Rect plot, double edgeLine,
        double tallestTickLabel);
}
