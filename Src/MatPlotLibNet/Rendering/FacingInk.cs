// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>How far an axis title's ink reaches on each side of the line it is placed by, as fractions of its font
/// size: the part that faces the tick labels of its edge and the part that faces away from them. It is the ink a reader sees
/// facing the labels, not the text's box (the box, its ascent and its descent, is <see cref="TextBox"/>'s over the measured
/// <see cref="TextMeasurement.TextExtent"/>), so the title rules read the rise of the letters, four fifths of the size
/// (<see cref="Ascent"/>), and a descent of one fifth (<see cref="Descent"/>).</summary>
/// <remarks>A horizontal title's ink runs from its ascent above the baseline to its descent below it, so below a bottom edge
/// its ascent faces the labels; above a top edge the title stands on its baseline, as matplotlib sets its top X label
/// (<c>va='baseline'</c>), and its descent dips into the gap (<see cref="Dip"/>). A Y title is drawn turned a quarter
/// (<c>rotate(-90)</c>) about its anchor with no <c>dy</c>: its up direction points left, so its ink spans
/// [x - ascent, x + descent]; that reach decides what a Y title takes away from the room beside it
/// (<see cref="Outward"/>). What decides where it stands is the ink the reader sees facing the labels, which is less: on
/// the right the body and capitals of the text (<see cref="TurnedRise"/>, not the full ascent of a bracket or a tall
/// letter), on the left the baseline itself (<see cref="TurnedSink"/>; a descender dips into the gap, as it does in
/// running text). With those, the 4 px gap puts the right title where 1.23.0 had it, and the left title at the same gap.
/// Which edge faces the labels is all that differs between the four edges: each edge's terms hold its ink
/// (<see cref="EdgeTerms.Ink"/>), and one rule (<see cref="TitleClearance.TitleOffset"/>) places the title by it.</remarks>
/// <param name="Facing">The part of the font size the ink reaches from the placing line towards the tick labels.</param>
/// <param name="Outward">The part of the font size the ink reaches from the placing line away from them.</param>
/// <param name="Dip">The part of the font size the title's box reaches past its placing line into the gap towards the labels: the
/// descent of a title placed on its baseline (matplotlib's top X label, <c>va='baseline'</c>); 0 for a title placed by its ink.</param>
internal readonly record struct FacingInk(double Facing, double Outward, double Dip = 0)
{
    /// <summary>The ascent of a title, as a fraction of its font size.</summary>
    internal const double Ascent = 0.8;

    /// <summary>The descent of a title, as a fraction of its font size.</summary>
    internal const double Descent = 0.2;

    /// <summary>How far above its baseline a turned title is read to rise towards the tick labels on its right, as a
    /// fraction of its font size: between the height of a lowercase body (about half) and that of a capital (about
    /// three quarters).</summary>
    internal const double TurnedRise = 0.6;

    /// <summary>How far below its baseline a turned title is read to reach towards the tick labels on its left, as a
    /// fraction of its font size: none, the baseline faces them.</summary>
    internal const double TurnedSink = 0;
}
