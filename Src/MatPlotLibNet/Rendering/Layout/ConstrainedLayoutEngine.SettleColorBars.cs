// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.ColorBarRendering;

namespace MatPlotLibNet.Rendering.Layout;

internal sealed partial class ConstrainedLayoutEngine
{
    /// <summary>The margins and the gaps widened for the colour bars whose room the layout keeps at <paramref name="stage"/>
    /// (<see cref="ColorBarRoom.SettlesAt"/>): each such bar's reach past the rectangle its room reads (<see cref="ColorBarRoom.AreasAt"/>:
    /// the axes' cell for matplotlib's slot, the plot for matplotlib's constrained margin), past what the axes draws on its side
    /// (<see cref="ColorBarRoom.ClearanceOf"/>), with its tick labels and its label (<see cref="ColorBarRoom.ReachOf"/>); an axes on the
    /// figure's edge widens the margin there to that reach and the pad, an interior one the gap beside it by it, and the figure's own bar
    /// widens the margin past the box of every subplot (<see cref="ColorBarRoom.FigureAreaAt"/>,
    /// <see cref="ColorBarRoomExtensions.FigureBarClearance"/>). The rectangles are shares of what the margins leave, so the reaches are
    /// measured again at the rectangles the widened spacing gives, a bounded number of times (<see cref="ConstrainedLayoutEngine.MaxSettlePasses"/>), until two
    /// measures agree to within <see cref="ConstrainedLayoutEngine.SettledWithin"/>; matplotlib's constrained layout keeps a bar's whole tight box and its pad
    /// in the margin of its axes the same way (<c>make_layout_margins</c>: <c>margin['rightcb'] += cbbbox.width + pad</c>). The
    /// spacing comes back as it was when no bar is settled at that stage, or none reaches past its rectangle. The library's margin keeps
    /// its room while the edges are measured (<see cref="AxesEdgeNeeds.BarNeedBeside"/>, <see cref="AxesEdgeNeeds.BarNeedBelow"/>).</summary>
    /// <param name="needs">What the axes need at their edges, and the pads and the ranges of the layout.</param>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="cells">The grid the figure's subplots are laid out on.</param>
    /// <param name="spacing">The spacing settled so far.</param>
    /// <param name="room">The room the figure keeps at each edge past what the subplots need there.</param>
    /// <param name="stage">The point of the layout reached: after the sides, after the top and the bottom, or last.</param>
    private static SubPlotSpacing SettleColorBars(AxesEdgeNeeds needs, Figure figure, IRenderContext ctx, FigureGrid cells, SubPlotSpacing spacing, Sides<double> room, BarRoomStage stage)
    {
        var bars = figure.BarRoom;
        if (!AnyBarSettlesAt(figure, bars, stage)) return spacing;
        var theme = figure.Theme;
        var current = spacing;
        // What each axes draws past its spines at the rectangles of a pass, measured once a pass and only where a room asks for it.
        var laidOut = new LaidOut(needs, figure, ctx);
        Func<int, LaidOutAxes>? bandsOf = null;
        for (int pass = 0; pass < ConstrainedLayoutEngine.MaxSettlePasses; pass++)
        {
            var areas = bars.AreasAt(cells, current);
            laidOut.MovedTo(areas);
            // Each pass widens the spacing it was handed, by what the bars reach at the rectangles of the pass.
            var across = AxisMargins.Across(spacing.MarginLeft, spacing.MarginRight, spacing.HorizontalGap);
            var down = AxisMargins.Down(spacing.MarginTop, spacing.MarginBottom, spacing.VerticalGap);
            for (int i = 0; i < figure.SubPlots.Count && i < areas.Count; i++)
            {
                var axes = figure.SubPlots[i];
                if (axes.ColorBar is not { Visible: true } bar || !bars.SettlesAt(bar, stage)) continue;
                double reach = bars.ReachOf(bar, areas[i], axes.Series, ctx, theme, bars.ClearanceOf(bar, laidOut.At(i)));
                if (reach <= 0) continue;
                // A bar on the end edge of the grid takes the margin there, with the pad, the room of an outside legend that stands past
                // it on that edge (AxesEdgeNeeds.DrawnPast) and the room past them; one inside the grid widens the gap past it by its reach.
                var at = cells.EdgeOf(i);
                if (bar.Beside)
                {
                    across.NeedAtEnd(at, AxesEdgeNeeds.RoomPastBar(axes, ctx, theme, bar, reach + needs.Pads.Edges.Right) + room.Right, 0);
                    across.Widen(at, 0, reach);
                }
                else
                {
                    down.NeedAtEnd(at, AxesEdgeNeeds.RoomPastBar(axes, ctx, theme, bar, reach + needs.Pads.Edges.Bottom) + room.Bottom, 0);
                    down.Widen(at, 0, reach);
                }
            }

            // The bar of the figure stands past the box that holds every subplot, which ends at the margin, and an outside legend of
            // an axes on its edge past the bar (AxesEdgeNeeds.RoomPastFigureBar).
            if (figure.FigureColorBar is { Visible: true } figureBar && bars.SettlesAt(figureBar, stage)
                && bars.FigureAreaAt(cells, current, areas) is { } whole
                && bars.ReachOf(figureBar, whole, figure.SubPlots.SelectMany(axes => axes.Series), ctx, theme,
                    figure.FigureBarClearance(areas, cells, bandsOf ??= laidOut.At)) is var figureReach and > 0)
            {
                if (figureBar.Beside) across.AtEnd(AxesEdgeNeeds.RoomPastFigureBar(figure, cells, ctx, figureReach + needs.Pads.Edges.Right) + room.Right);
                else down.AtEnd(AxesEdgeNeeds.RoomPastFigureBar(figure, cells, ctx, figureReach + needs.Pads.Edges.Bottom) + room.Bottom);
            }

            bool settled = Math.Abs(across.End - current.MarginRight) < ConstrainedLayoutEngine.SettledWithin && Math.Abs(down.End - current.MarginBottom) < ConstrainedLayoutEngine.SettledWithin
                && Math.Abs(across.Gap - current.HorizontalGap) < ConstrainedLayoutEngine.SettledWithin && Math.Abs(down.Gap - current.VerticalGap) < ConstrainedLayoutEngine.SettledWithin;
            current = current with { MarginRight = across.End, MarginBottom = down.End, HorizontalGap = across.Gap, VerticalGap = down.Gap };
            if (settled) break;
        }

        return current;
    }

    /// <summary>Whether the figure or one of its subplots has a visible colour bar whose room is kept at <paramref name="stage"/>.</summary>
    private static bool AnyBarSettlesAt(Figure figure, ColorBarRoom bars, BarRoomStage stage)
    {
        if (figure.FigureColorBar is { Visible: true } figureBar && bars.SettlesAt(figureBar, stage)) return true;
        // Walked by index: a foreach over the list boxes its enumerator.
        for (int i = 0; i < figure.SubPlots.Count; i++)
            if (figure.SubPlots[i].ColorBar is { Visible: true } bar && bars.SettlesAt(bar, stage)) return true;
        return false;
    }

    /// <summary>What every axes draws past its spines with its plot at the rectangles of a settling pass, measured by the renderer it is
    /// drawn with when a colour bar's room first asks for it (<see cref="AxesRenderer.MeasureEdgeBands"/>), and what it draws past its
    /// right spine by the rule the layout reserves that band with (<see cref="AxesEdgeNeeds.MeasureRightBand"/>); kept for the pass, forgotten when
    /// the next pass moves the rectangles (<see cref="MovedTo"/>). One for a whole settling, so a pass allocates no more than its
    /// measures.</summary>
    private sealed class LaidOut(AxesEdgeNeeds needs, Figure figure, IRenderContext ctx)
    {
        private List<Rect> _areas = [];
        private EdgeBands?[]? _edges;
        private double?[]? _past;

        /// <summary>Starts a pass at <paramref name="areas"/>: what was measured at the last pass's rectangles is forgotten.</summary>
        internal void MovedTo(List<Rect> areas)
        {
            _areas = areas;
            if (_edges is { } edges) Array.Clear(edges);
            if (_past is { } past) Array.Clear(past);
        }

        /// <summary>The axes at <paramref name="index"/>, as a colour bar's room asks it.</summary>
        internal LaidOutAxes At(int index) => new(this, index);

        /// <summary>What the axes at <paramref name="index"/> draws past its four spines at this pass's rectangle.</summary>
        internal EdgeBands EdgesOf(int index) =>
            (_edges ??= new EdgeBands?[_areas.Count])[index]
                ??= AxesRenderer.Create(figure.SubPlots[index], _areas[index], ctx, figure.Theme, null, needs.Ranges).MeasureEdgeBands();

        /// <summary>What the axes at <paramref name="index"/> draws past its right spine at this pass's rectangle.</summary>
        internal double PastRightOf(int index) =>
            (_past ??= new double?[_areas.Count])[index] ??= needs.MeasureRightBand(figure.SubPlots[index], ctx, figure.Theme, EdgesOf(index).RightPastEdge).Past;
    }

    /// <summary>One axes of a settling pass as a colour bar's room asks it (<see cref="IAxesBands"/>): a view of
    /// <see cref="LaidOut"/>, which keeps what was measured.</summary>
    private readonly struct LaidOutAxes(LaidOut laidOut, int index) : IAxesBands
    {
        /// <inheritdoc />
        public EdgeBands Ends() => laidOut.EdgesOf(index);

        /// <inheritdoc />
        public EdgeBands Edges() => laidOut.EdgesOf(index);

        /// <inheritdoc />
        public double PastRightSpine() => laidOut.PastRightOf(index);
    }
}
