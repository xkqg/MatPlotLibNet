// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

internal sealed partial class ConstrainedLayoutEngine
{
    /// <summary>The margins at the two ends of one direction of the figure's grid (across it: the left and the right; down it: the top
    /// and the bottom) and the gap between its cells, worked out from what each axes needs there: the one owner of the rule that an
    /// axes on an edge of the grid widens the margin of that edge and an axes inside it the gap beside it, and of how the needs that
    /// meet in a gap stack. Both layout paths of <see cref="ConstrainedLayoutEngine"/> feed one per direction, axes by axes, and read its
    /// margins and gap once every axes has said what it needs.</summary>
    /// <remarks>A mutable value, held in a local for the one layout it works out: it allocates nothing.</remarks>
    internal struct AxisMargins
    {
        private readonly bool _alongX;
        private readonly double _restingGap;
        private readonly double _startLift;
        private double _startInGap, _endInGap, _besideEnd, _stacked;
        private double _startBand, _endBand, _bandInGap, _pastEnd;

        /// <summary>The left and the right margin and the gap between columns, starting from those the layout begins with.</summary>
        /// <param name="left">The left margin the layout begins with.</param>
        /// <param name="right">The right margin the layout begins with.</param>
        /// <param name="gap">The gap the layout begins with: the resting gap a band widens (<see cref="Rendering.Layout.ConstrainedLayoutEngine.AxisMargins.Widen"/>).</param>
        /// <returns>The margins across the grid.</returns>
        internal static AxisMargins Across(double left, double right, double gap) => new(true, left, right, gap, 0);

        /// <summary>The top and the bottom margin and the gap between rows, starting from those the layout begins with.</summary>
        /// <param name="top">The top margin the layout begins with.</param>
        /// <param name="bottom">The bottom margin the layout begins with.</param>
        /// <param name="gap">The gap the layout begins with: the resting gap a band widens (<see cref="Rendering.Layout.ConstrainedLayoutEngine.AxisMargins.Widen"/>).</param>
        /// <param name="aboveTopRow">What stands above every axes of the top row (the figure's title): an axes there needs its own room
        /// on top of it.</param>
        /// <returns>The margins down the grid.</returns>
        internal static AxisMargins Down(double top, double bottom, double gap, double aboveTopRow = 0) => new(false, top, bottom, gap, aboveTopRow);

        private AxisMargins(bool alongX, double start, double end, double gap, double startLift)
        {
            _alongX = alongX;
            _restingGap = gap;
            _startLift = startLift;
            Start = start;
            End = end;
            Gap = gap;
        }

        /// <summary>The margin at the start of the direction (left, top), in pixels.</summary>
        internal double Start { readonly get; private set; }

        /// <summary>The margin at the end of the direction (right, bottom), in pixels.</summary>
        internal double End { readonly get; private set; }

        /// <summary>The gap between two cells of the direction, in pixels.</summary>
        internal double Gap { readonly get; private set; }

        /// <summary>How far past their end spine the axes at the end of the grid draw (<see cref="AxisNeeds.PastEnd"/>), the farthest.</summary>
        internal readonly double PastEnd => _pastEnd;

        /// <summary>Takes in all that one axes needs (<see cref="AxisNeeds"/>): its need at each end (<see cref="Rendering.Layout.ConstrainedLayoutEngine.AxisMargins.NeedAtStart"/>,
        /// <see cref="Rendering.Layout.ConstrainedLayoutEngine.AxisMargins.NeedAtEnd"/>), the measured part of each, which lifts the cap of the margin or the gap there (<see cref="Rendering.Layout.ConstrainedLayoutEngine.AxisMargins.Bound"/>),
        /// and what it draws past its end spine when it stands at the end of the grid.</summary>
        /// <param name="cell">The edges of the grid the axes' cell touches.</param>
        /// <param name="needs">What the axes needs.</param>
        internal void Demand(Sides<bool> cell, AxisNeeds needs)
        {
            NeedAtStart(cell, needs.Start, needs.Start);
            NeedAtEnd(cell, needs.End, needs.End);
            if (needs.StartBand > 0)
            {
                if (cell.StartAlong(_alongX)) _startBand = Math.Max(_startBand, _startLift + needs.StartBand);
                else _bandInGap = Math.Max(_bandInGap, needs.StartBand);
            }
            if (cell.EndAlong(_alongX))
            {
                _endBand = Math.Max(_endBand, needs.EndBand);
                _pastEnd = Math.Max(_pastEnd, needs.PastEnd);
            }
            else _bandInGap = Math.Max(_bandInGap, needs.EndBand);
        }

        /// <summary>The need of an axes at the start of the direction: the start margin holds it, past what stands above every axes
        /// there, when the axes' cell starts the grid; otherwise it stands in the gap before the cell.</summary>
        /// <param name="cell">The edges of the grid the axes' cell touches.</param>
        /// <param name="atEdge">The need when the cell starts the grid.</param>
        /// <param name="inGap">The need when it stands in a gap.</param>
        internal void NeedAtStart(Sides<bool> cell, double atEdge, double inGap)
        {
            if (cell.StartAlong(_alongX)) Start = Math.Max(Start, _startLift + atEdge);
            else _startInGap = Math.Max(_startInGap, inGap);
        }

        /// <summary>The need of an axes at the end of the direction: the end margin holds it when the axes' cell ends the grid;
        /// otherwise it stands in the gap after the cell.</summary>
        /// <param name="cell">The edges of the grid the axes' cell touches.</param>
        /// <param name="atEdge">The need when the cell ends the grid.</param>
        /// <param name="inGap">The need when it stands in a gap.</param>
        internal void NeedAtEnd(Sides<bool> cell, double atEdge, double inGap)
        {
            if (cell.EndAlong(_alongX)) End = Math.Max(End, atEdge);
            else _endInGap = Math.Max(_endInGap, inGap);
        }

        /// <summary>The need of an axes at the start of the direction, measured only when the axes' cell starts the grid, where the start
        /// margin holds it; an axes inside the grid measures nothing for it.</summary>
        /// <typeparam name="TNeed">The need, measured when it is read.</typeparam>
        /// <param name="cell">The edges of the grid the axes' cell touches.</param>
        /// <param name="atEdge">The need.</param>
        internal void NeedAtStartEdge<TNeed>(Sides<bool> cell, TNeed atEdge) where TNeed : struct, ILayoutNeed
        {
            if (cell.StartAlong(_alongX)) Start = Math.Max(Start, _startLift + atEdge.Need());
        }

        /// <summary>A need at the start of an axes inside the grid that its gap holds only stacked on what the axes before the gap needs
        /// at its end (<see cref="Rendering.Layout.ConstrainedLayoutEngine.AxisMargins.Stacked"/>): the title of a lower row stands in the gap the theme keeps unless the row above draws its
        /// labels into it. Measured only for an axes whose start lies in a gap.</summary>
        /// <typeparam name="TNeed">The need, measured when it is read.</typeparam>
        /// <param name="cell">The edges of the grid the axes' cell touches.</param>
        /// <param name="need">The need.</param>
        internal void BesideEnd<TNeed>(Sides<bool> cell, TNeed need) where TNeed : struct, ILayoutNeed
        {
            if (!cell.StartAlong(_alongX)) _besideEnd = Math.Max(_besideEnd, need.Need());
        }

        /// <summary>A band an axes draws into the gap beside it that the resting gap does not hold: the gap widens past the resting gap by
        /// it. A band on a side of the cell that touches the edge of the grid widens nothing here.</summary>
        /// <param name="cell">The edges of the grid the axes' cell touches.</param>
        /// <param name="atStart">The band the axes draws past its start spine.</param>
        /// <param name="atEnd">The band the axes draws past its end spine.</param>
        internal void Widen(Sides<bool> cell, double atStart, double atEnd)
        {
            if (!cell.StartAlong(_alongX)) Gap = Math.Max(Gap, _restingGap + atStart);
            if (!cell.EndAlong(_alongX)) Gap = Math.Max(Gap, _restingGap + atEnd);
        }

        /// <summary>What stands at the end edge past every axes (the figure's own colour bar): the end margin holds it, and it is
        /// measured, so it lifts the cap there.</summary>
        /// <param name="need">The room it needs, in pixels.</param>
        internal void AtEnd(double need)
        {
            End = Math.Max(End, need);
            _endBand = Math.Max(_endBand, need);
        }

        /// <summary>Widens the gap to hold the needs that meet in it, stacked (<see cref="LayoutPads.Stacked"/>): the widest end need of
        /// an axes before a gap and the widest start need of one after it, which pair is neighbours not being known. A start need that
        /// stands in a gap only beside an end need (<see cref="Rendering.Layout.ConstrainedLayoutEngine.AxisMargins.BesideEnd"/>) is stacked only when an end need is there.</summary>
        /// <param name="pads">The pads of the layout: how much of two pads a gap keeps once.</param>
        internal void Stacked(in LayoutPads pads)
        {
            _stacked = pads.Stacked(_endInGap, _endInGap > 0 ? Math.Max(_startInGap, _besideEnd) : _startInGap);
            Gap = Math.Max(Gap, _stacked);
        }

        /// <summary>Holds the margins and the gap in <paramref name="bounds"/>: each between its floor and its cap, the cap of a margin
        /// lifted by what the axes on its edge are measured to draw there, the cap of the gap by the needs stacked in it when an axes is
        /// measured to draw into it (<see cref="MarginBounds.Clamp"/>).</summary>
        /// <param name="bounds">The bounds of the layout (<see cref="LayoutPads.Bounds"/>).</param>
        internal void Bound(LayoutBounds bounds)
        {
            Start = bounds.Margins.StartAlong(_alongX).Clamp(Start, _startBand);
            End = bounds.Margins.EndAlong(_alongX).Clamp(End, _endBand);
            Gap = bounds.GapAlong(_alongX).Clamp(Gap, _bandInGap > 0 ? _stacked : 0);
        }
    }
}
