// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>The edge of the axes a spine stands on. It keys the one rule that colours an axis with the spine on its edge
/// (<see cref="AxisVerbs"/>) and the one that edits a spine (<see cref="SpineEdgeExtensions"/>): the left Y axis stands on
/// <see cref="Left"/>, the first right Y axis on <see cref="Right"/>, the bottom X axis on <see cref="Bottom"/>, the secondary X axis
/// on <see cref="Top"/>.</summary>
internal enum SpineEdge
{
    /// <summary>The top edge: the secondary X axis.</summary>
    Top,

    /// <summary>The bottom edge: the X axis.</summary>
    Bottom,

    /// <summary>The left edge: the primary Y axis.</summary>
    Left,

    /// <summary>The right edge: the first right Y axis.</summary>
    Right,
}

/// <summary>The one rule that edits the spines of an axes by their edge: the spine on an edge, one spine changed, some spines hidden.
/// A colour verb, a hide verb and the renderer all go through it; none copies the spines and the edge with a <c>with</c> of its own.</summary>
internal static class SpineEdgeExtensions
{
    extension(SpinesConfig spines)
    {
        /// <summary>The spine on <paramref name="edge"/>.</summary>
        /// <param name="edge">Which spine.</param>
        /// <returns>The spine.</returns>
        internal SpineConfig On(SpineEdge edge) => edge switch
        {
            SpineEdge.Top => spines.Top,
            SpineEdge.Bottom => spines.Bottom,
            SpineEdge.Left => spines.Left,
            _ => spines.Right,
        };

        /// <summary>These spines with the one on <paramref name="edge"/> changed, the others as they are.</summary>
        /// <param name="edge">Which spine.</param>
        /// <param name="change">What becomes of that spine.</param>
        /// <returns>The new spines.</returns>
        internal SpinesConfig WithEdge(SpineEdge edge, Func<SpineConfig, SpineConfig> change) => edge switch
        {
            SpineEdge.Top => spines with { Top = change(spines.Top) },
            SpineEdge.Bottom => spines with { Bottom = change(spines.Bottom) },
            SpineEdge.Left => spines with { Left = change(spines.Left) },
            _ => spines with { Right = change(spines.Right) },
        };

        /// <summary>These spines with the ones on <paramref name="edges"/> not drawn, the others as they are: one copy of the spines
        /// whatever the number of edges named (a copy for each edge was most of what hiding the four spines of a tile cost).</summary>
        /// <param name="edges">The edges whose spine is hidden.</param>
        /// <returns>The new spines.</returns>
        internal SpinesConfig Hiding(params ReadOnlySpan<SpineEdge> edges) => edges.IsEmpty ? spines : spines with
        {
            Top = Hidden(spines.Top, SpineEdge.Top, edges),
            Bottom = Hidden(spines.Bottom, SpineEdge.Bottom, edges),
            Left = Hidden(spines.Left, SpineEdge.Left, edges),
            Right = Hidden(spines.Right, SpineEdge.Right, edges),
        };
    }

    /// <summary>The spine on <paramref name="edge"/>, not drawn when <paramref name="edges"/> names that edge.</summary>
    private static SpineConfig Hidden(SpineConfig spine, SpineEdge edge, ReadOnlySpan<SpineEdge> edges)
    {
        foreach (var named in edges)
        {
            if (named == edge) return spine with { Visible = false };
        }

        return spine;
    }
}
