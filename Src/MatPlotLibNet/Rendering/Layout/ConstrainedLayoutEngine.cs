// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>
/// Computes <see cref="SubPlotSpacing"/> margins based on actual text extents measured via
/// <see cref="IRenderContext.MeasureText"/>.  Called by <see cref="ChartRenderer"/> when
/// <c>TightLayout</c> or <c>ConstrainedLayout</c> is enabled; the default layout path calls its right-edge rule
/// (<see cref="ReserveEdgeBands"/>) only.
/// </summary>
/// <param name="ranges">The data ranges of the draw being laid out (<see cref="FigureRanges"/>): every renderer the layout
/// makes to measure a subplot reads the subplot's range there, the range the drawing reads too. <see langword="null"/> for a
/// layout outside a draw, whose renderers work the range out themselves.</param>
/// <param name="grid">The grid the figure being laid out puts its subplots on (<see cref="FigureGrid"/>), worked out once for the
/// draw; <see langword="null"/> for a layout outside a draw, which works it out from the figure it is handed.</param>
internal sealed partial class ConstrainedLayoutEngine(FigureRanges? ranges = null, FigureGrid? grid = null)
{
    // The library's pads (px) past what an axes needs on each edge (LayoutPads.Library; a tight layout under the matplotlib themes
    // keeps matplotlib's, LayoutPads.Of).
    internal const double PadLeft  = 5;
    internal const double PadBottom = 5;
    internal const double PadRight = 8;

    /// <summary>The pad kept above the title and the top band of the top row, and above the title of a lower row (which holds
    /// it off the tick labels of the row above): the buffer against the figure's edge and the figure's title that matplotlib's
    /// <c>tight_layout</c> keeps with its <c>pad</c>. A fixed layout under a theme that keeps none above the top row
    /// (<see cref="FigureLook.NoPadAboveTitle"/>) leaves it out there.</summary>
    internal const double PadTop = 8;

    /// <summary>Computes optimal <see cref="SubPlotSpacing"/> margins for the figure: the sides first
    /// (<see cref="ComputeSides"/>), what an axes draws past its left and right spines that does not depend on its plot's
    /// width (the Y tick labels, the secondary axes, the end labels); then the room the X tick labels need beside their plot,
    /// which does depend on it, since they turn when upright ones would collide at the width the plot is drawn at and a turned
    /// label stands out sideways (<see cref="SettleSides"/>); then the top and the bottom (<see cref="ComputeEnds"/>), measured
    /// at the width each plot is drawn at (<see cref="FigureGrid.PlotRects"/>), and how far the labels reach below the
    /// plot (and above it, mirrored) follows. Each margin is what the subplots need there and, past it, the room the figure keeps
    /// at that edge for what stands outside the axes (<see cref="FigureLegendExtensions.RoomFor"/>).</summary>
    /// <param name="figure">The figure to lay out.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="legends">The figure's legends as the draw placed them; <see langword="null"/> places them here.</param>
    /// <returns>The spacing that holds what the axes draws.</returns>
    internal SubPlotSpacing Compute(Figure figure, IRenderContext ctx, IReadOnlyList<PlacedFigureLegend>? legends = null)
    {
        var cells = grid ?? new FigureGrid(figure);
        var room = RoomAtTheEdges(figure, legends ?? figure.PlacedLegends(ctx));
        return SettleAtLaidOutPlots(figure, cells, new TightPass(this, figure, ctx, cells, room),
            twice: figure.Spacing.ConstrainedLayout && ranges is { Stretches: true });
    }

    /// <summary>One pass of <see cref="Compute"/>, the spines that stand off their edges read at <paramref name="drawn"/>.</summary>
    /// <param name="figure">The figure to lay out.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="cells">The grid the figure's subplots are laid out on.</param>
    /// <param name="room">The room the figure keeps at each edge past what the subplots need there.</param>
    /// <param name="drawn">The plots the last pass laid out; <see langword="null"/> on the first.</param>
    /// <returns>The spacing that holds what the axes draws.</returns>
    private SubPlotSpacing ComputeAt(Figure figure, IRenderContext ctx, FigureGrid cells, Sides<double> room, IReadOnlyList<Rect>? drawn)
    {
        var needs = new AxesEdgeNeeds(LayoutPads.Of(figure), ranges, drawn);
        var sides = SettleColorBars(needs, figure, ctx, cells, ComputeSides(needs, figure, ctx, cells, room), room, BarRoomStage.Sides);
        var ends = SettleColorBars(needs, figure, ctx, cells, ComputeEnds(needs, figure, ctx, cells, SettleSides(needs, figure, ctx, cells, sides, room), room), room, BarRoomStage.Ends);
        return SettleColorBars(needs, figure, ctx, cells, ends, room, BarRoomStage.Last);
    }

    /// <summary>The layout a pass gives once it stands at the plots it lays out. A spine moved off its edge (a fraction of the axes, a data
    /// value) hangs its tick labels where it stands, and where it stands is a share of the plot's width or height, which the layout
    /// decides: so while a spine stands off its edge the figure is laid out again with the insets read at the plots the last pass laid
    /// out (<see cref="EdgeBands.Insets"/>), until a pass gives back the spacing it was laid out at, to within <see cref="SettledWithin"/>.
    /// Each pass moves a margin by the share of the last move that the spine stands of the plot's side (a margin that widens narrows
    /// the plot, which pulls the spine back by that share), so after two passes the layout steps to where the margins head
    /// (<see cref="Headed"/>) and lays the figure out there, at most <see cref="MaxSettlePasses"/> times. That is the layout matplotlib's
    /// <c>tight_layout</c> comes to when it is run until it stands (one run reads the spine at the plot before the layout, and a second
    /// run moves it again). A figure whose spines stand on their edges is laid out once, or twice when <paramref name="twice"/> says
    /// so: matplotlib's constrained layout runs its algorithm two times (<c>do_constrained_layout</c>, _constrained_layout.py:108), the
    /// second at the axes the first placed, which moves only what keeps a share of its axes' height (a table's rows,
    /// <see cref="AxesEdgeNeeds.EdgeBandsOf"/>).</summary>
    /// <typeparam name="TPass">The pass of the layout path.</typeparam>
    /// <param name="figure">The figure to lay out.</param>
    /// <param name="cells">The grid the figure's subplots are laid out on.</param>
    /// <param name="pass">One pass of the layout.</param>
    /// <param name="twice">Whether a figure whose spines stand on their edges is laid out a second time at the plots of the first.</param>
    /// <returns>The spacing of the last pass.</returns>
    private static SubPlotSpacing SettleAtLaidOutPlots<TPass>(Figure figure, FigureGrid cells, TPass pass, bool twice = false) where TPass : struct, ILayoutPass
    {
        var given = pass.At(null);
        if (!AnySpineOffItsEdge(figure)) return twice ? pass.At(cells.PlotRects(given.MarginTop, given)) : given;
        var next = pass.At(cells.PlotRects(given.MarginTop, given));
        for (int i = 0; i < MaxSettlePasses && !Settled(next, given); i++)
        {
            var after = pass.At(cells.PlotRects(next.MarginTop, next));
            if (Settled(after, next)) return after;
            given = Headed(given, next, after);
            next = pass.At(cells.PlotRects(given.MarginTop, given));
        }
        return next;
    }

    /// <summary>Whether a pass gave back the spacing it was laid out at, every margin and gap to within <see cref="SettledWithin"/>.</summary>
    private static bool Settled(SubPlotSpacing laidOut, SubPlotSpacing given) =>
        Math.Abs(laidOut.MarginLeft - given.MarginLeft) < SettledWithin
        && Math.Abs(laidOut.MarginRight - given.MarginRight) < SettledWithin
        && Math.Abs(laidOut.MarginTop - given.MarginTop) < SettledWithin
        && Math.Abs(laidOut.MarginBottom - given.MarginBottom) < SettledWithin
        && Math.Abs(laidOut.HorizontalGap - given.HorizontalGap) < SettledWithin
        && Math.Abs(laidOut.VerticalGap - given.VerticalGap) < SettledWithin;

    /// <summary>Where the margins and gaps of three passes in a row head, each moving by a steady share of its last move (Aitken's
    /// step): the third and its last move times the share over one less the share, the share being the last move over the move before
    /// it. A value that did not move, or moved by the same amount twice, stands where the third pass put it.</summary>
    private static SubPlotSpacing Headed(SubPlotSpacing first, SubPlotSpacing second, SubPlotSpacing third) => third with
    {
        MarginLeft = HeadedValue(first.MarginLeft, second.MarginLeft, third.MarginLeft),
        MarginRight = HeadedValue(first.MarginRight, second.MarginRight, third.MarginRight),
        MarginTop = HeadedValue(first.MarginTop, second.MarginTop, third.MarginTop),
        MarginBottom = HeadedValue(first.MarginBottom, second.MarginBottom, third.MarginBottom),
        HorizontalGap = HeadedValue(first.HorizontalGap, second.HorizontalGap, third.HorizontalGap),
        VerticalGap = HeadedValue(first.VerticalGap, second.VerticalGap, third.VerticalGap),
    };

    private static double HeadedValue(double first, double second, double third)
    {
        double last = third - second, bend = last - (second - first);
        return Math.Abs(bend) < SettledWithin * SettledWithin ? third : third - last * last / bend;
    }

    /// <summary>Whether a subplot of <paramref name="figure"/> has a spine off its edge (<see cref="SpinesConfig.AnyOffItsEdge"/>).</summary>
    private static bool AnySpineOffItsEdge(Figure figure)
    {
        foreach (var axes in figure.SubPlots)
            if (axes.Spines.AnyOffItsEdge) return true;
        return false;
    }

    /// <summary>One pass of a layout path, at the plots the pass before it laid out (<see cref="SettleAtLaidOutPlots"/>).</summary>
    private interface ILayoutPass
    {
        /// <summary>The spacing of the pass.</summary>
        /// <param name="drawn">The plots the last pass laid out; <see langword="null"/> on the first.</param>
        SubPlotSpacing At(IReadOnlyList<Rect>? drawn);
    }

    /// <summary>A pass of the tight and the constrained layout (<see cref="ComputeAt"/>).</summary>
    private readonly struct TightPass(ConstrainedLayoutEngine engine, Figure figure, IRenderContext ctx, FigureGrid cells, Sides<double> room) : ILayoutPass
    {
        /// <inheritdoc />
        public SubPlotSpacing At(IReadOnlyList<Rect>? drawn) => engine.ComputeAt(figure, ctx, cells, room, drawn);
    }

    /// <summary>A pass of the default layout path (<see cref="ReserveEdgeBandsAt"/>).</summary>
    private readonly struct DefaultPass(ConstrainedLayoutEngine engine, Figure figure, SubPlotSpacing resolved, IRenderContext ctx, FigureGrid cells) : ILayoutPass
    {
        /// <inheritdoc />
        public SubPlotSpacing At(IReadOnlyList<Rect>? drawn) => engine.ReserveEdgeBandsAt(figure, resolved, ctx, cells, drawn);
    }

    /// <summary>The room the figure keeps free at each edge past what the subplots need there: the part of the figure outside
    /// <see cref="SubPlotSpacing.LayoutRect"/> (matplotlib's <c>rect</c>, kept on top of the margins as matplotlib keeps it, the
    /// title's band included), and on that the room of the figure's outside legends (<see cref="FigureLegendExtensions.RoomFor"/>).</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="legends">The figure's legends as the draw placed them.</param>
    /// <returns>The room on each edge, in pixels.</returns>
    private static Sides<double> RoomAtTheEdges(Figure figure, IReadOnlyList<PlacedFigureLegend> legends)
    {
        var room = figure.RoomFor(legends);
        if (figure.Spacing.LayoutRect is not { } rect) return room;
        return new Sides<double>(
            room.Left + rect.Left * figure.Width,
            room.Bottom + rect.Bottom * figure.Height,
            room.Top + (1 - rect.Top) * figure.Height,
            room.Right + (1 - rect.Right) * figure.Width);
    }

    /// <summary>The sides together with the plot areas they give, once the room the X tick labels need beside their plot is
    /// settled in, and what each axes draws past its top and bottom spines when the last pass measured it at these plots.</summary>
    /// <param name="Sides">The spacing with its sides widened for the labels that stand out sideways.</param>
    /// <param name="Plots">The plot area of each axes under <paramref name="Sides"/>.</param>
    /// <param name="Ends">The end bands of each axes measured at <paramref name="Plots"/> by the pass that settled the sides;
    /// <see langword="null"/> when that pass moved the plots, so the ends are measured at <paramref name="Plots"/>.</param>
    private readonly record struct SettledSides(SubPlotSpacing Sides, List<Rect> Plots, EdgeBands[]? Ends);

    /// <summary>How many times <see cref="SettleSides"/> measures at the plots the last measure gave before it stops: the
    /// labels turn, never un-turn, as the plot narrows, so the width converges in two or three. Each pass leaves the share of the
    /// last change that the first tick stands in from the plot's edge (a tenth to a fifth for bars, whose margin pads the
    /// categories), so six reach within <see cref="SettledWithin"/> where four stopped a fiftieth of a pixel short.</summary>
    internal const int MaxSettlePasses = 6;

    /// <summary>How close two measures of a margin must come for the sides to be settled, in pixels.</summary>
    internal const double SettledWithin = 0.01;

    /// <summary>Settles the sides for what the X tick labels need beside their plot: the first label of a bottom axis, hung from
    /// its end and turned, stands out to the left of its plot, and the last stands out to the right of it, so the margin (or the
    /// gutter) holds that by the one rule (<see cref="WidenForOverhang"/>). Whether the labels turn depends on the width of the
    /// plot, and the plot's width on the sides, so the room is measured at the width the sides give and the sides given at first
    /// are widened by it; that narrows the plot, where a label that did not turn may now turn (the labels turn, never un-turn,
    /// as the plot narrows) and the ticks stand a little differently, so it is measured again at the width that remains, a
    /// bounded number of times (<see cref="MaxSettlePasses"/>), until two measures agree to within <see cref="SettledWithin"/>.
    /// The sides come back as they were when nothing stands out. A pass measures the end bands of every axes at the plots it
    /// stands on; when the plots it hands on are those, the bands go with them (<see cref="SettledSides.Ends"/>), so the ends
    /// are not measured again.</summary>
    /// <param name="needs">What the axes need at their edges, and the pads of the layout.</param>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="cells">The grid the figure's subplots are laid out on.</param>
    /// <param name="sides">The spacing with its sides settled for what does not depend on the plot's width.</param>
    /// <param name="room">The room the figure keeps at each edge past what the subplots need there.</param>
    private static SettledSides SettleSides(in AxesEdgeNeeds needs, Figure figure, IRenderContext ctx, FigureGrid cells, SubPlotSpacing sides, Sides<double> room)
    {
        var plots = cells.PlotRects(sides.MarginTop, sides);
        var current = sides;
        for (int pass = 0; pass < MaxSettlePasses; pass++)
        {
            var ends = needs.EndBandsAt(figure, ctx, cells, plots);
            var widened = WidenForOverhang(figure, cells, sides, ends, room, needs.Pads);
            // Nothing stands out past the sides: they stand, and on the first pass so do the plots just measured.
            if (widened == sides)
                return current == sides
                    ? new SettledSides(sides, plots, ends)
                    : new SettledSides(sides, cells.PlotRects(sides.MarginTop, sides), null);
            // The pass found the room it stands on: the plots measured are the plots handed on.
            if (widened == current) return new SettledSides(current, plots, ends);
            bool settled = Math.Abs(widened.MarginLeft - current.MarginLeft) < SettledWithin
                && Math.Abs(widened.MarginRight - current.MarginRight) < SettledWithin
                && Math.Abs(widened.HorizontalGap - current.HorizontalGap) < SettledWithin;
            current = widened;
            plots = cells.PlotRects(current.MarginTop, current);
            if (settled) break;
        }
        return new SettledSides(current, plots, null);
    }

    /// <summary>The sides widened for what stands out sideways past their plot below it, as the end bands
    /// <paramref name="ends"/> measured it (<see cref="EdgeBands.Sideways"/>: the X tick labels, and the row headers of a table below the
    /// axes): an axes in the first column widens the left margin to the overhang on its left and
    /// the pad, one in the last column the right margin to the overhang on its right and the pad, and an interior axes the gutter
    /// by the overhangs that meet in it (the worst case, as the gutter rule is). An overhang within the margin the sides already
    /// give changes nothing.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="cells">The grid the figure's subplots are laid out on.</param>
    /// <param name="sides">The sides to widen.</param>
    /// <param name="ends">The end bands of each axes, measured at the plots under the sides the pass stands on.</param>
    /// <param name="room">The room the figure keeps at each edge past what the subplots need there.</param>
    /// <param name="pads">The pads of the layout.</param>
    /// <returns>The sides, widened; <paramref name="sides"/> itself when nothing stands out past them.</returns>
    private static SubPlotSpacing WidenForOverhang(Figure figure, FigureGrid cells, SubPlotSpacing sides, EdgeBands[] ends, Sides<double> room, in LayoutPads pads)
    {
        var across = AxisMargins.Across(sides.MarginLeft, sides.MarginRight, sides.HorizontalGap);
        for (int i = 0; i < figure.SubPlots.Count; i++)
        {
            var overhang = ends[i].Sideways;
            if (overhang.Left <= 0 && overhang.Right <= 0) continue;
            var cell = cells.EdgeOf(i);
            across.NeedAtStart(cell, overhang.Left + pads.Edges.Left + room.Left, overhang.Left);
            across.NeedAtEnd(cell, overhang.Right + pads.Edges.Right + room.Right, overhang.Right);
        }
        // The overhangs carry no pad of their own, so the gap keeps none of them once: they add up.
        across.Stacked(pads with { Shared = 0 });
        return across.Start == sides.MarginLeft && across.End == sides.MarginRight && across.Gap == sides.HorizontalGap
            ? sides
            : sides with { MarginLeft = across.Start, MarginRight = across.End, HorizontalGap = across.Gap };
    }

    /// <summary>The left and right margins and the horizontal gutter of <see cref="Compute"/>: each axes' needs taken in across the
    /// grid (<see cref="Rendering.Layout.ConstrainedLayoutEngine.AxisMargins"/>: an edge axes' on its edge, an interior axes' in the gutter, the worst-case left and right needs
    /// stacked), the figure's colour bar past the last column, held in the bounds of the layout (<see cref="LayoutPads.Bounds"/>,
    /// the caps lifted by what is measured there), and the room the figure keeps at that edge past it.</summary>
    /// <param name="needs">What the axes need at their edges, and the pads of the layout.</param>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="cells">The grid the figure's subplots are laid out on.</param>
    /// <param name="room">The room the figure keeps at each edge past what the subplots need there.</param>
    /// <returns>The figure's spacing with <see cref="SubPlotSpacing.MarginLeft"/>, <see cref="SubPlotSpacing.MarginRight"/>
    /// and <see cref="SubPlotSpacing.HorizontalGap"/> settled.</returns>
    private static SubPlotSpacing ComputeSides(in AxesEdgeNeeds needs, Figure figure, IRenderContext ctx, FigureGrid cells, Sides<double> room)
    {
        var across = AxisMargins.Across(figure.Spacing.MarginLeft, figure.Spacing.MarginRight, figure.Spacing.HorizontalGap);
        for (int i = 0; i < figure.SubPlots.Count; i++)
            across.Demand(cells.EdgeOf(i), needs.MeasureSides(figure.SubPlots[i], figure, ctx, needs.EdgeBandsOf(figure, cells, i, ctx)));
        across.Stacked(needs.Pads);

        // The figure's colour bar in the margin beside the box of every plot, past what the last column draws past its right spines,
        // and an outside legend of the last column past the bar; drawn whatever the margin, so they lift the cap as a measured band does.
        across.AtEnd(AxesEdgeNeeds.RoomPastFigureBar(figure, cells, ctx, needs.BarNeedBeside(figure, figure.FigureColorBar, null, ctx, across.PastEnd)));
        across.Bound(needs.Pads.Bounds);

        return figure.Spacing with
        {
            MarginLeft    = across.Start + room.Left,
            MarginRight   = across.End + room.Right,
            HorizontalGap = across.Gap,
        };
    }

    /// <summary>The top and bottom margins and the row gap of <see cref="Compute"/>, measured with each axes' plot at the
    /// width it is drawn at (the plots of <paramref name="settled"/>): each axes' needs taken in down the grid
    /// (<see cref="Rendering.Layout.ConstrainedLayoutEngine.AxisMargins"/>: an edge axes' on its edge, the figure's title stacked above the top row, an interior axes' in the
    /// row gap, the worst-case upper bottom need and lower top need stacked), the figure's colour bar below the bottom row, held in
    /// the bounds of the layout, and the room the figure keeps at that edge past it.</summary>
    /// <param name="needs">What the axes need at their edges, and the pads of the layout.</param>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="cells">The grid the figure's subplots are laid out on.</param>
    /// <param name="settled">The sides settled (<see cref="ComputeSides"/>, <see cref="SettleSides"/>), their plots, and the
    /// end bands measured at those plots when the settling pass measured them there.</param>
    /// <param name="room">The room the figure keeps at each edge past what the subplots need there.</param>
    /// <returns>The settled sides with <see cref="SubPlotSpacing.MarginTop"/>, <see cref="SubPlotSpacing.MarginBottom"/> and
    /// <see cref="SubPlotSpacing.VerticalGap"/> settled.</returns>
    private static SubPlotSpacing ComputeEnds(in AxesEdgeNeeds needs, Figure figure, IRenderContext ctx, FigureGrid cells, SettledSides settled, Sides<double> room)
    {
        var ends = settled.Ends ?? needs.EndBandsAt(figure, ctx, cells, settled.Plots);

        // The figure's title stands above the top row: an axes there needs its own top room on top of the title's band, and the
        // band alone is the least the top margin is (a figure with a title and no axes title).
        double supReserved = figure.Title is not null ? figure.TitleBand(figure.TitleHeight(ctx)) : 0;
        var down = AxisMargins.Down(Math.Max(figure.Spacing.MarginTop, supReserved), figure.Spacing.MarginBottom,
            figure.Spacing.VerticalGap, aboveTopRow: supReserved);
        for (int i = 0; i < figure.SubPlots.Count; i++)
            down.Demand(cells.EdgeOf(i), needs.MeasureEnds(figure.SubPlots[i], figure, ctx, ends[i], settled.Plots[i]));
        down.Stacked(needs.Pads);

        // The figure's colour bar in the margin below the box of every plot, under what the bottom row draws there, and an outside
        // legend of the bottom row under the bar; they lift the cap as a measured band does.
        down.AtEnd(AxesEdgeNeeds.RoomPastFigureBar(figure, cells, ctx, needs.FigureBarNeedBelow(figure, cells, ends, settled.Plots)));
        down.Bound(needs.Pads.Bounds);

        return settled.Sides with
        {
            MarginBottom = down.End + room.Bottom,
            MarginTop    = down.Start + room.Top,
            VerticalGap  = down.Gap,
        };
    }

    /// <summary>The default layout path's share of the edge rules: <paramref name="resolved"/> (the figure's or the
    /// theme's spacing in pixels) widened so what is drawn past the right and the top spines, the category names measured
    /// past the left spine and the X tick labels and X axis title measured below the bottom spine, fit. An axes in the last
    /// column widens <see cref="SubPlotSpacing.MarginRight"/> to its <see cref="AxesEdgeNeeds.MeasureRightBand"/>; an axes left of
    /// another widens the gutter by its band, on top of the gutter the neighbour's Y tick labels already use. An axes with
    /// a top band
    /// (<see cref="EdgeBands.Top"/>: mirrored X tick labels or a drawn secondary X axis) in the top row widens
    /// <see cref="SubPlotSpacing.MarginTop"/> to the band and the title above it (the rule <see cref="Compute"/>
    /// applies); in a lower row it widens the row gap by the band, the distance its title rises. The left stack of the Y
    /// axis container (the primary's measured tick labels, <see cref="EdgeBands.Left"/>, its title and every slot past it)
    /// widens the left margin or the gutter to <see cref="AxesLeftEdgeExtensions.LeftNeed"/>; what is drawn
    /// below the bottom spine (<see cref="EdgeBands.Bottom"/>, <see cref="EdgeBands.BottomTitle"/>) widens
    /// <see cref="SubPlotSpacing.MarginBottom"/> of an axes in the last row to <see cref="EdgeBands.BottomNeed"/>, and the row gap
    /// below an axes in an upper row to that need plus the room the lower row's title takes (<see cref="AxesTitleExtensions"/>' <c>TitleBand</c>).
    /// A colour bar in the margin (the library's own themes) is kept room for by the rules <see cref="Compute"/> keeps it by: beside
    /// the plot its whole stack where it needs more than the right band, below it the stack under the X tick labels and title. The
    /// sides are settled first (<see cref="ReserveSideBands"/>) and the top and bottom bands measured with each plot at the
    /// width it is drawn at (<see cref="FigureGrid.PlotRects"/>), the order <see cref="Compute"/> follows: the X tick
    /// labels turn when upright ones would collide at that width. The end bands are those the pass that settled the sides
    /// measured, when it measured them at the plots it settled on. A figure whose resolved spacing already holds all of
    /// that gets <paramref name="resolved"/> back unchanged.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="resolved">The spacing the default path resolved, in pixels.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <returns>The spacing with room for the edge bands.</returns>
    internal SubPlotSpacing ReserveEdgeBands(Figure figure, SubPlotSpacing resolved, IRenderContext ctx)
    {
        var cells = grid ?? new FigureGrid(figure);
        return SettleAtLaidOutPlots(figure, cells, new DefaultPass(this, figure, resolved, ctx, cells));
    }

    /// <summary>One pass of <see cref="ReserveEdgeBands"/>, the spines that stand off their edges read at <paramref name="drawn"/>.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="resolved">The spacing the default path resolved, in pixels.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="cells">The grid the figure's subplots are laid out on.</param>
    /// <param name="drawn">The plots the last pass laid out; <see langword="null"/> on the first.</param>
    /// <returns>The spacing with room for the edge bands.</returns>
    private SubPlotSpacing ReserveEdgeBandsAt(Figure figure, SubPlotSpacing resolved, IRenderContext ctx, FigureGrid cells, IReadOnlyList<Rect>? drawn)
    {
        var needs = new AxesEdgeNeeds(LayoutPads.Of(figure), ranges, drawn);
        // The default layout keeps no room at the figure's edges (matplotlib's subplot parameters keep none either).
        var settled = SettleSides(needs, figure, ctx, cells, ReserveSideBands(needs, figure, cells, resolved, ctx), room: default);
        var ends = settled.Ends ?? needs.EndBandsAt(figure, ctx, cells, settled.Plots);
        var theme = figure.Theme;
        double padAboveTopRow = theme.Looks.Figure.NoPadAboveTitle ? 0 : needs.Pads.Edges.Top;
        var down = AxisMargins.Down(resolved.MarginTop, resolved.MarginBottom, resolved.VerticalGap);
        for (int i = 0; i < figure.SubPlots.Count; i++)
        {
            var axes = figure.SubPlots[i];
            var cell = cells.EdgeOf(i);
            // Measured with the plot at the width it is drawn at: the X tick labels turn by it.
            var bands = ends[i];
            // Above the plot only the top band (the mirrored X tick labels where the top spine stands, or the secondary X axis) is new;
            // without it the resolved spacing stands: the top margin holds the band and the title above it, a row gap widens by the
            // band, the distance the title of a lower row rises.
            double top = bands.TopPastEdge;
            if (top > 0) down.NeedAtStartEdge(cell, new TitleAbove(axes, ctx, theme, top, padAboveTopRow));
            down.Widen(cell, top, 0);
            // Below the bottom spine the measured X tick labels and the X axis title past them, and a colour bar below them with
            // its text (the theme's margin is a fraction of the height and does not grow with them): the margin holds them, and a
            // row gap holds them above the lower row's title, which the gap the theme keeps holds on its own.
            double need = Math.Max(bands.BottomNeed(needs.Pads.Edges.Bottom), needs.BarNeedBelow(figure, axes.ColorBar, settled.Plots[i], bands));
            down.NeedAtEnd(cell, need, need);
            down.BesideEnd(cell, new TitleAbove(axes, ctx, theme, top, needs.Pads.Edges.Top));
        }
        down.Stacked(needs.Pads);
        down.AtEnd(needs.FigureBarNeedBelow(figure, cells, ends, settled.Plots));
        return settled.Sides with { MarginTop = down.Start, VerticalGap = down.Gap, MarginBottom = down.End };
    }

    /// <summary>The room an axes' title takes above its top band, the pad above it included (<see cref="AxesTitleExtensions"/>'
    /// <c>TitleBand</c>), measured only when the layout reads it (<see cref="Rendering.Layout.ConstrainedLayoutEngine.ILayoutNeed"/>).</summary>
    /// <param name="Axes">The axes.</param>
    /// <param name="Ctx">The context text is measured with.</param>
    /// <param name="Theme">The theme the axes is drawn under.</param>
    /// <param name="TopBand">What the axes draws past its top spine below the title.</param>
    /// <param name="PadAbove">The pad kept above the title.</param>
    private readonly record struct TitleAbove(Axes Axes, IRenderContext Ctx, Theme Theme, double TopBand, double PadAbove) : ILayoutNeed
    {
        /// <inheritdoc />
        public double Need() => Axes.TitleBand(Ctx, Theme, TopBand, PadAbove);
    }

    /// <summary>The sides of <see cref="ReserveEdgeBands"/>, settled before its top and bottom: what an axes draws past its
    /// left and right spines does not depend on its plot's width (the room the X tick labels need beside their plot does, and
    /// is settled after it, <see cref="SettleSides"/>). An axes in the last column widens
    /// <see cref="SubPlotSpacing.MarginRight"/> to its <see cref="AxesEdgeNeeds.MeasureRightBand"/>, one left of another the gutter by
    /// it; the left stack of the Y axis container widens the left margin or the gutter to
    /// <see cref="AxesLeftEdgeExtensions.LeftNeed"/>.</summary>
    /// <returns><paramref name="resolved"/> with its left and right margins and its horizontal gutter settled.</returns>
    private static SubPlotSpacing ReserveSideBands(in AxesEdgeNeeds needs, Figure figure, FigureGrid cells, SubPlotSpacing resolved, IRenderContext ctx)
    {
        var across = AxisMargins.Across(resolved.MarginLeft, resolved.MarginRight, resolved.HorizontalGap);
        for (int i = 0; i < figure.SubPlots.Count; i++)
        {
            var axes = figure.SubPlots[i];
            var cell = cells.EdgeOf(i);
            var bands = needs.EdgeBandsOf(figure, cells, i, ctx);
            // What the axes draws past its right spine, and its colour bar in the margin past that: the margin holds it, and a
            // gutter widens past the gap the theme keeps by it.
            var rightBand = needs.MeasureRightBand(axes, ctx, figure.Theme, bands.RightPastEdge);
            double band = needs.RightNeed(figure, axes, ctx, rightBand);
            across.Widen(cell, 0, band);
            // Left of the left spine the whole left stack of the container as the renderer draws it (the primary's tick
            // labels measured by its renderer, its title, every slot past it): the margin holds it, and a gutter holds it
            // right of its neighbour's band, by the one rule for the left edge's need (AxesLeftEdgeExtensions.LeftNeed). Nothing
            // drawn there needs nothing, not even the pad (the rule's own): the pad holds the outermost ink off the figure's edge,
            // and a figure that draws none (an icon that fills its square, a row of tiles with no axes) keeps the margin and the
            // gutter its caller asked for.
            double need = axes.LeftNeed(figure.Theme, bands.Left, bands.Insets, needs.LeftAxisBands(axes, ctx, figure.Theme), needs.Pads.Edges.Left);
            across.Demand(cell, new AxisNeeds(need, band, 0, 0, rightBand.Past));
        }
        across.Stacked(needs.Pads);
        across.AtEnd(needs.BarNeedBeside(figure, figure.FigureColorBar, null, ctx, across.PastEnd));
        return resolved with { MarginLeft = across.Start, MarginRight = across.End, HorizontalGap = across.Gap };
    }

    /// <summary>A need an axes has that is measured only when <see cref="Rendering.Layout.ConstrainedLayoutEngine.AxisMargins"/> takes it: where the cell of the axes stands
    /// decides whether it is read at all, so an axes whose need goes nowhere measures nothing for it.</summary>
    internal interface ILayoutNeed
    {
        /// <summary>Measures the need.</summary>
        /// <returns>The room, in pixels.</returns>
        double Need();
    }
}
