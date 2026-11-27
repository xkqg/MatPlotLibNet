// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>What an axes draws past its four spines besides the title and the end labels, measured by the renderer that
/// draws it (<see cref="AxesRenderer.MeasureEdgeBands"/>).</summary>
/// <param name="Top">From the top spine to the top of what is drawn above it below the axes title: the mirrored X tick
/// labels, or the secondary X axis that owns the edge (its tick labels, then its title), or the rise of a polar axes' title
/// above its square. 0 when nothing is drawn there.</param>
/// <param name="Right">From the right spine to the far side of the mirrored Y tick labels; 0 when none are drawn there.
/// The secondary Y axis is measured on its own (<see cref="AxesEdgeNeeds.SecondaryAxisBands"/>) and
/// <see cref="RightBand.Axis"/> takes the wider.</param>
/// <param name="Left">From the left spine to the far side of the primary Y axis' tick labels, numbers or the category
/// names it carries (bars that lie down): the tick mark, the pad and the widest label. 0 when the Y axis draws no labels,
/// or the renderer measures none (polar, 3-D), whose labels the tight layout estimates.</param>
/// <param name="Bottom">From the bottom spine to the far side of the X tick labels, numbers, dates or the category names
/// the X axis carries (upright bars, a count plot, a candlestick's dates): the tick mark, the pad and the tallest label. 0
/// when the X axis draws no tick labels.</param>
/// <param name="BottomTitle">From the bottom spine to the far side of the X axis title drawn below the tick labels
/// (<see cref="AxesTitleExtensions"/>' <c>BottomTitleExtent</c>, the rule the title is placed by); 0 when the axes has no X
/// axis title or draws it elsewhere (a 3-D box draws it along its edge, a polar axes not at all).</param>
/// <param name="Overhang">How far the first and the last X tick label stand out past the left and the right edge of the plot
/// area (<see cref="SideOverhang"/>): a label hung from its end and turned up stands out to the left of the plot by its turned
/// width. The left and right margins hold it. Empty when no label stands out, and for the sides, which are measured before the
/// plot has a width.</param>
/// <param name="HungBelow">From the plot's bottom edge to the far side of the lowest thing hung below the plot past what the primary X
/// axis draws there: a linked X axis (<see cref="CartesianAxisPlacement.PlaceBottomAxes"/>), its tick labels or its title, or a table
/// below the axes (<see cref="SeriesRenderers.TableGrid.WithTablesOf"/>), which hangs from the plot's edge where the X axis draws its
/// labels, under them, as matplotlib's does; 0 when nothing hangs there.</param>
/// <param name="Insets">How far each spine stands in from its edge of the plot the axes is drawn at (<see cref="SpineInsets"/>): the
/// bands above hang from the spines, so what they reach past the plot's edges, the room a layout keeps, is read through these
/// (<see cref="TopPastEdge"/>, <see cref="RightPastEdge"/>, <see cref="BottomExtent"/>, the left stack's
/// <see cref="AxesLeftEdgeExtensions"/>). 0 on every edge when the spines stand on their edges.</param>
/// <param name="HungLeft">How far what hangs below the plot stands out past its left edge: the row headers of a table below the axes. 0
/// when nothing does.</param>
internal readonly record struct EdgeBands(double Top, double Right, double Left, double Bottom, double BottomTitle = 0,
    SideOverhang Overhang = default, double HungBelow = 0, SpineInsets Insets = default, double HungLeft = 0)
{
    /// <summary>What the top band (<see cref="Top"/>) reaches past the plot's top edge: the mirrored X tick labels stand where the top
    /// spine stands; a top X axis stands at the plot's edge (its inset is 0).</summary>
    internal double TopPastEdge => Insets.PastTop(Top);

    /// <summary>What the mirrored Y tick labels (<see cref="Right"/>) reach past the plot's right edge, where the right spine stands.</summary>
    internal double RightPastEdge => Insets.PastRight(Right);

    /// <summary>Whether the renderer measured anything it draws below the bottom spine: X tick labels (<see cref="Bottom"/>), an X
    /// axis title (<see cref="BottomTitle"/>), or a linked X axis or a table hung below the plot (<see cref="HungBelow"/>). Then both
    /// layout paths reserve the bottom edge by <see cref="BottomNeed"/>.</summary>
    internal bool DrawsBelow => Bottom > 0 || BottomTitle > 0 || HungBelow > 0;

    /// <summary>How far what is drawn below the plot stands out sideways past it: the X tick labels (<see cref="Overhang"/>) and, on the
    /// left, the row headers of a table below the axes (<see cref="HungLeft"/>), whichever reaches further. The margins hold it.</summary>
    internal SideOverhang Sideways => HungLeft > Overhang.Left ? Overhang with { Left = HungLeft } : Overhang;

    /// <summary>What the primary X axis draws below the bottom spine: its tick label band (<see cref="Bottom"/>) and its title
    /// (<see cref="BottomTitle"/>).</summary>
    internal BandsBelowAxis BelowPrimary => new(Bottom, BottomTitle);

    /// <summary>From the plot's bottom edge to the far side of the lowest thing the axes draws below it: the linked X axes below the
    /// plot or a table hung below it (<see cref="HungBelow"/>) when they reach lowest, else what the primary X axis
    /// reaches (<see cref="BandsBelowAxis.Extent"/>: its title when it has one, otherwise its tick labels) from the bottom spine wherever it
    /// stands (<see cref="Insets"/>); 0 when it draws none. What a colour bar below the axes stands under
    /// (<see cref="ColorBarRendering.ColorBarRoomExtensions"/>).</summary>
    internal double BottomExtent => Math.Max(HungBelow, Insets.PastBottom(BelowPrimary.Extent));

    /// <summary>The one rule for the room the bottom edge of an axes needs for what is drawn below its bottom spine: its
    /// <see cref="BottomExtent"/> and then the layout's pad below it (<see cref="LayoutPads.Edges"/>), or nothing when nothing is
    /// drawn there: the pad holds what is drawn off the edge, so a pad alone is no need. The mirror of the left edge's rule
    /// (<see cref="AxesLeftEdgeExtensions"/>).</summary>
    /// <param name="pad">The pad the layout keeps below what an axes needs.</param>
    /// <returns>The room the bottom edge needs, in pixels; 0 when nothing is drawn below the spine.</returns>
    internal double BottomNeed(double pad) => BottomExtent > 0 ? BottomExtent + pad : 0;
}

/// <summary>How far the labels on a horizontal edge stand out sideways past the plot area: the part of the first label that
/// lies left of the plot's left edge, and of the last that lies right of its right edge, in pixels; 0 when none does.</summary>
/// <param name="Left">Past the left edge.</param>
/// <param name="Right">Past the right edge.</param>
internal readonly record struct SideOverhang(double Left, double Right);
