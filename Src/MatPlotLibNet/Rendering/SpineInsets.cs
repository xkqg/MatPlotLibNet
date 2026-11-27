// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>How far each spine stands in from its edge of the plot, at the plot the axes is drawn at, in pixels: 0 for a spine on its
/// edge, less than 0 for one moved out past it, more for one moved into the plot (<see cref="EdgeTerms.InsetOf"/>). What hangs from a
/// spine (its tick labels and the title past them) stands where the spine stands, so what it reaches past the plot's edge, the room a
/// layout keeps for it, is its band from the spine less the inset, as matplotlib's tight bbox holds the labels wherever their spine
/// stands.</summary>
/// <param name="Left">The left spine's inset.</param>
/// <param name="Right">The right spine's inset.</param>
/// <param name="Top">The top spine's inset; 0 where a top X axis owns the edge, which stands at the plot's edge.</param>
/// <param name="Bottom">The bottom spine's inset.</param>
internal readonly record struct SpineInsets(double Left, double Right, double Top, double Bottom)
{
    /// <summary>What a band hung from the left spine reaches past the plot's left edge.</summary>
    /// <param name="band">From the spine to the far side of what hangs from it; 0 when nothing does.</param>
    /// <returns>The reach past the edge, in pixels; 0 when nothing stands past it.</returns>
    internal double PastLeft(double band) => Reach(band, Left);

    /// <summary>What a band hung from the right spine reaches past the plot's right edge.</summary>
    /// <param name="band">From the spine to the far side of what hangs from it; 0 when nothing does.</param>
    /// <returns>The reach past the edge, in pixels; 0 when nothing stands past it.</returns>
    internal double PastRight(double band) => Reach(band, Right);

    /// <summary>What a band hung from the top spine reaches past the plot's top edge.</summary>
    /// <param name="band">From the spine to the far side of what hangs from it; 0 when nothing does.</param>
    /// <returns>The reach past the edge, in pixels; 0 when nothing stands past it.</returns>
    internal double PastTop(double band) => Reach(band, Top);

    /// <summary>What a band hung from the bottom spine reaches past the plot's bottom edge.</summary>
    /// <param name="band">From the spine to the far side of what hangs from it; 0 when nothing does.</param>
    /// <returns>The reach past the edge, in pixels; 0 when nothing stands past it.</returns>
    internal double PastBottom(double band) => Reach(band, Bottom);

    private static double Reach(double band, double inset) => band > 0 ? Math.Max(0, band - inset) : 0;
}
