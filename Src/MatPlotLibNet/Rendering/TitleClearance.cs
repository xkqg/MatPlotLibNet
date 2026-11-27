// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>What places an axis title beside the tick labels of its edge, and the one rule that places it on all four edges
/// (<see cref="TitleOffset"/>, and the room it takes, <see cref="TitleExtent"/>): the band the labels take, the blank gap wanted between
/// them and the title's ink, the title's font size and which part of its ink faces them. A clearance is made for an edge by
/// <see cref="Beside"/>, which reads the edge's terms.</summary>
/// <param name="TickLabelBand">From the edge to the far side of the tick labels drawn there
/// (<see cref="Models.TickConfig.TickLabelBand"/>); 0 when none is drawn.</param>
/// <param name="Gap">The blank gap between the far side of the tick labels and the title's ink, in pixels.</param>
/// <param name="LabelSize">The title's font size.</param>
/// <param name="Ink">Which part of the title's ink faces the tick labels.</param>
internal readonly record struct TitleClearance(double TickLabelBand, double Gap, double LabelSize, FacingInk Ink)
{
    /// <summary>The gap between the tick labels on a top or bottom edge and the horizontal axis title beside them, under a theme that
    /// names none (<see cref="AxesTitleExtensions"/>' <c>XLabelGapUnder</c>).</summary>
    internal const double XLabelGap = 6;

    /// <summary>Where the title is placed when no tick label is drawn on its edge (hidden ticks, axis-less series), or
    /// <see langword="null"/> when the edge has no fixed place and the rule holds with a band of 0.</summary>
    internal double? WithoutTickLabels { get; init; }

    /// <summary>The clearance of a title beside <paramref name="edge"/>: the ink of a title that faces its labels
    /// (<see cref="EdgeTerms.Ink"/>) and the place the title stands without labels (<see cref="EdgeTerms.TitleWithoutTickLabels"/>), the
    /// one rule the X titles below and above the plot and the title of every Y axis slot are placed by.</summary>
    /// <param name="edge">The edge the title stands beside.</param>
    /// <param name="tickLabelBand">From the edge to the far side of its tick labels (<see cref="Models.TickConfig.TickLabelBand"/>); 0
    /// when none is drawn.</param>
    /// <param name="gap">The blank gap between the labels and the title's ink.</param>
    /// <param name="labelSize">The title's font size.</param>
    /// <returns>The clearance.</returns>
    internal static TitleClearance Beside(EdgeTerms edge, double tickLabelBand, double gap, double labelSize) =>
        new(tickLabelBand, gap, labelSize, edge.Ink) { WithoutTickLabels = edge.TitleWithoutTickLabels };

    /// <summary>Where the title stands: its placing line (the baseline of a horizontal title, the anchor line of a turned Y title)
    /// stands past the tick label band by the gap and the part of the title's ink that faces the labels (<see cref="FacingInk.Facing"/>),
    /// so that ink edge stands exactly the gap past the band whichever way the title faces. When no band is drawn and the edge has a
    /// fixed place (<see cref="WithoutTickLabels"/>), the title stands there.</summary>
    /// <returns>The distance from the edge to the title's placing line.</returns>
    internal double TitleOffset() =>
        FixedPlace is { } fixedOffset ? fixedOffset : TickLabelBand + Gap + Ink.Facing * LabelSize;

    /// <summary>From the edge to the far side of a title of <paramref name="titleHeight"/> placed by <see cref="TitleOffset"/>, the room
    /// the layout keeps for it: past the band, the gap less the part of the title's box that dips into it (<see cref="FacingInk.Dip"/>),
    /// the title's <paramref name="pad"/> and its full height (the safe direction), or,
    /// at the fixed place, that place less the ink that faces the labels, then the pad and the height. The one band decides both, so the
    /// room kept is where the title is drawn.</summary>
    /// <param name="titleHeight">The measured height of the title in its font.</param>
    /// <param name="pad">The pad of the title's style (<see cref="Styling.TextStyle.Pad"/>), 0 when none.</param>
    /// <returns>The distance from the edge to the far side of the title.</returns>
    internal double TitleExtent(double titleHeight, double pad) =>
        FixedPlace is { } fixedOffset
            ? fixedOffset - Ink.Facing * LabelSize + pad + titleHeight
            : TickLabelBand + Gap - Ink.Dip * LabelSize + pad + titleHeight;

    /// <summary>The place the title stands at when no band is drawn and the edge has one (<see cref="WithoutTickLabels"/>);
    /// <see langword="null"/> when it stands past the band. The one decision <see cref="TitleOffset"/> and <see cref="TitleExtent"/>
    /// both read.</summary>
    private double? FixedPlace => TickLabelBand > 0 ? null : WithoutTickLabels;
}
