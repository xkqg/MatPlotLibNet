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

/// <summary>What one axes needs at the edges of its plot, as the layout measures it (<see cref="ConstrainedLayoutEngine"/>): what it
/// draws past its four spines, measured by the renderer it is drawn with (<see cref="EdgeBandsOf"/>), the room its left and right
/// edges need before its plot has a width (<see cref="MeasureSides"/>), the room its top and bottom need at the width it is drawn at
/// (<see cref="MeasureEnds"/>), the band past its right spine (<see cref="MeasureRightBand"/>) and its Y slots, and the room a colour
/// bar in the library's margin and an outside legend keep there. Both layout paths read it; it holds the pads of the layout and the
/// data ranges of the draw, a value, so a layout allocates nothing for it.</summary>
/// <param name="pads">The pads of the layout being worked out (<see cref="LayoutPads.Of"/>).</param>
/// <param name="ranges">The data ranges of the draw being laid out (<see cref="FigureRanges"/>), which every renderer it makes reads;
/// <see langword="null"/> outside a draw.</param>
/// <param name="drawn">The plot of each axes as the last pass of the layout laid it out, which a spine moved off its edge stands in
/// (<see cref="EdgeBands.Insets"/>, measured at it); <see langword="null"/> on a layout's first pass, which reads every spine on its
/// edge.</param>
internal readonly struct AxesEdgeNeeds(LayoutPads pads, FigureRanges? ranges = null, IReadOnlyList<Rect>? drawn = null)
{
    /// <summary>The needs of a layout under the library's pads, outside a draw.</summary>
    public AxesEdgeNeeds() : this(LayoutPads.Library)
    {
    }

    /// <summary>The pads of the layout (<see cref="LayoutPads.Of"/>).</summary>
    internal LayoutPads Pads { get; } = pads;

    /// <summary>The data ranges of the draw being laid out; <see langword="null"/> outside a draw.</summary>
    internal FigureRanges? Ranges => ranges;

    // The tight layout's floor below drawn X tick labels: 15 px for the mark and the pad, a label's height in the theme's
    // tick font, the layout's bottom pad. It was the estimate of the band before the band was measured (EdgeBands.Bottom); it stays a
    // floor so a figure whose labels fit keeps its margin, numbers and category names alike, and it is the estimate a
    // renderer that measures nothing below its bottom spine (polar, 3-D) is still laid out by.
    private const double XTickBottomGap  = 15;

    /// <summary>What each axes draws past its top and bottom spines, measured with its plot at <paramref name="plots"/>.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="cells">The grid the figure's subplots are laid out on.</param>
    /// <param name="plots">The plot area of each axes; only its X and width are used.</param>
    /// <returns>The end bands of each axes, in <see cref="Figure.SubPlots"/> order.</returns>
    internal EdgeBands[] EndBandsAt(Figure figure, IRenderContext ctx, FigureGrid cells, List<Rect> plots)
    {
        var ends = new EdgeBands[figure.SubPlots.Count];
        for (int i = 0; i < ends.Length; i++) ends[i] = EdgeBandsOf(figure, cells, i, ctx, plots[i]);
        return ends;
    }

    /// <summary>What <paramref name="axes"/> needs on its left and right edges, from the side bands its renderer measured past
    /// its spines (<paramref name="bands"/>, <see cref="EdgeBandsOf"/> before the plot has a width) and the estimates, the
    /// colour bar and the legend box beside them. The side pass of <see cref="ConstrainedLayoutEngine.Compute"/>: nothing above or below the plot is
    /// measured here.</summary>
    /// <param name="axes">The axes to measure.</param>
    /// <param name="figure">The figure it belongs to.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="bands">The bands its renderer measured past the left and right spines.</param>
    /// <returns>The room the left and right edges need, and the measured bands that lift the caps.</returns>
    internal AxisNeeds MeasureSides(Axes axes, Figure figure, IRenderContext ctx, EdgeBands bands)
    {
        var theme = figure.Theme;

        // --- Left: the Y tick labels and the title past them (PrimaryYTickBand) ---
        // The whole left stack of the container: the primary, then every slot stacked past it, each charged even when it
        // draws nothing (see MeasureRightBand's remarks). What was measured there lifts the left cap.
        var leftSlots = LeftAxisBands(axes, ctx, theme);
        double leftNeeded = axes.LeftNeed(theme, PrimaryYTickBand(axes, ctx, theme, bands), bands.Insets, leftSlots, Pads.Edges.Left);
        double leftBand = bands.Left > 0 || leftSlots.Length > 0 ? leftNeeded : 0;

        // --- Right: the secondary Y axis or the mirrored Y tick labels, then the end labels, stacked (MeasureRightBand); a
        // vertical colour bar in the library's margin stands past them, so it takes the edge with them ---
        var right = MeasureRightBand(axes, ctx, theme, bands.RightPastEdge);
        double rightNeeded = RightNeed(figure, axes, ctx, right);

        // An outside legend is measured: on its edge the whole need lifts the cap. Past the right spine everything is measured (the
        // bands, a colour bar in the library's margin, the legend), so the right need lifts it whole. The box stands past the left
        // stack and past what is drawn beside the right spine, a colour bar in the library's margin with it (DrawnPast).
        var needs = ReserveOutsideLegend(axes, ctx, theme, new Sides<double>(leftNeeded, 0, 0, rightNeeded),
            new Sides<double>(leftNeeded, 0, 0, rightNeeded));
        return new AxisNeeds(needs.Left, needs.Right, needs.Left > leftNeeded ? needs.Left : leftBand, needs.Right, right.Past);
    }

    /// <summary>From the left spine to the far side of the primary Y axis' tick labels, as the layout reserves them: measured by the
    /// renderer that draws them (<see cref="EdgeBands.Left"/>, numbers and category names alike), or, by a renderer that measures none
    /// (polar, 3-D), estimated from a representative label (<see cref="AxesLeftEdgeExtensions"/>' <c>EstimateYTickLabel</c>) with the
    /// same band rule; 0 where the axis labels no ticks on its own side. Hidden major ticks draw nothing in this margin
    /// (CartesianAxesRenderer skips mark and label on TickConfig.Visible = false), so an axes that hid them is not charged for their
    /// width.</summary>
    private static double PrimaryYTickBand(Axes axes, IRenderContext ctx, Theme theme, EdgeBands bands)
    {
        var yMajor = axes.YAxis.MajorTicksUnder(theme);
        if (!yMajor.LabelsOwnSide) return 0;
        // All themed fonts come from ThemeFontExtensions, the one source the renderer and the legend measurer read too.
        return bands.Left > 0 ? bands.Left : yMajor.TickLabelBand(ctx.ExtentOf(TextLabel.Of(axes.EstimateYTickLabel()), theme.TickFont()).Width);
    }

    /// <summary>From the plot's bottom edge to the far side of what the axes draws below it: what its renderer measured
    /// (<see cref="EdgeBands.BottomExtent"/>), or, by a renderer that measures nothing there (polar, 3-D), the Cartesian rule on an
    /// estimated band: a tick label's height in the theme's tick font past the mark and the pad where the axis labels its own side, and
    /// the X axis title past it by the rule the title is placed by (at its fixed place when no tick label is drawn).</summary>
    private static double DrawnBelow(Axes axes, IRenderContext ctx, Theme theme, EdgeBands bands)
    {
        if (bands.DrawsBelow) return bands.BottomExtent;
        var xMajor = axes.XAxis.MajorTicksUnder(theme);
        double tickH = xMajor.LabelsOwnSide ? ctx.LineExtentOf(theme.TickFont()).Height : 0;
        double below = xMajor.LabelsOwnSide ? XTickBottomGap + tickH : 0;
        return axes.TitleOf(axes.XAxis, theme) is not { } title ? below : Math.Max(below, axes.XAxis.BottomTitleExtent(title, ctx, theme, tickH));
    }

    /// <summary>What <paramref name="axes"/> draws past each edge of its plot, by the rules its needs are made of without the pad
    /// (<see cref="MeasureSides"/>, <see cref="MeasureEnds"/>): the left stack of its Y axis container
    /// (<see cref="AxesLeftEdgeExtensions"/>' <c>LeftStackBand</c>) or the row headers of a table below it (<see cref="EdgeBands.HungLeft"/>)
    /// where they reach further, the band past the right spine (<see cref="RightBand.Past"/>), the top
    /// band and the axes title (<see cref="AxesTitleExtensions"/>' <c>TitleBand</c> with no pad above), what stands below the plot, and,
    /// under a layout that keeps room (<see cref="FigureRanges.KeepsRoom"/>), the axes' colour bar on its edge, with its tick labels and
    /// its label, as it is drawn (<see cref="ColorBarRoom.PastPlot"/>), and, for an axes on the edge of the grid the figure's own colour
    /// bar stands at, that bar with its text as it is drawn (<see cref="FigureBarStand.PastPlotOf"/>). A legend outside the plot stands
    /// its gap past it (<see cref="LegendPositionStrategy"/>), on the edge the layout kept its room; the default layout keeps none, and
    /// the legend stands at its plot's edge over a bar there, as matplotlib's does.</summary>
    /// <param name="axes">The axes.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="theme">The theme the axes is drawn under.</param>
    /// <param name="bands">What the axes' renderer measured past its spines, with their insets.</param>
    /// <param name="plot">The plot area the axes is drawn in.</param>
    /// <param name="cell">The cell the layout gave the axes, which a colour bar in matplotlib's slot stands in; <see langword="null"/>
    /// when it is not known.</param>
    /// <returns>The reach past each edge, in pixels; 0 where nothing is drawn.</returns>
    internal Sides<double> DrawnPast(Axes axes, IRenderContext ctx, Theme theme, EdgeBands bands, Rect plot, Rect? cell)
    {
        double right = MeasureRightBand(axes, ctx, theme, bands.RightPastEdge).Past;
        var past = new Sides<double>(
            Math.Max(axes.LeftStackBand(theme, PrimaryYTickBand(axes, ctx, theme, bands), bands.Insets, LeftAxisBands(axes, ctx, theme)), bands.HungLeft),
            DrawnBelow(axes, ctx, theme, bands),
            axes.TitleBand(ctx, theme, bands.TopPastEdge, 0),
            right);
        if (ranges is not { KeepsRoom: true }) return past;
        if (axes.ColorBar is { Visible: true } bar)
        {
            var room = ColorBarRoom.Of(ranges, theme);
            past = PastBar(past, bar, room.PastPlot(bar, plot, cell, plot, axes.Series, ctx, theme, room.ClearanceOf(bar, new MeasuredBands(bands, right))));
        }

        // The figure's bar stands past what the axes on its edge draw there, so a legend on that edge stands past it too.
        return ranges?.FigureBar is { Bar: { } figureBar } stand ? PastBar(past, figureBar, stand.PastPlotOf(axes, plot, ctx)) : past;
    }

    /// <summary><paramref name="past"/> with the edge <paramref name="bar"/> stands at reaching at least <paramref name="reach"/>.</summary>
    private static Sides<double> PastBar(Sides<double> past, ColorBar bar, double reach) =>
        past.WithEndAlong(bar.Beside, Math.Max(past.EndAlong(bar.Beside), reach));

    /// <summary>The one rule for the room the right edge of <paramref name="axes"/> needs, on both layout paths: what it draws past its
    /// right spine and the pad (<see cref="RightBand.WidthWith"/>), or its colour bar in the library's margin past that
    /// (<see cref="BarNeedBeside"/>) when the bar needs more.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="axes">The axes.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="right">What the axes draws past its right spine (<see cref="MeasureRightBand"/>).</param>
    /// <returns>The room, in pixels.</returns>
    internal double RightNeed(Figure figure, Axes axes, IRenderContext ctx, RightBand right) =>
        Math.Max(right.WidthWith(Pads.Edges.Right), BarNeedBeside(figure, axes.ColorBar, axes, ctx, right.Past));

    /// <summary>What <paramref name="axes"/> needs on its top and bottom edges, from the end bands its renderer measured past its
    /// spines (<paramref name="bands"/>, <see cref="EdgeBandsOf"/> at the width the plot is drawn at) and the estimates, the
    /// title and the legend box beside them. The end pass of <see cref="ConstrainedLayoutEngine.Compute"/>: nothing beside the plot is measured
    /// here.</summary>
    /// <param name="axes">The axes to measure.</param>
    /// <param name="figure">The figure it belongs to.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="bands">The bands its renderer measured past the top and bottom spines.</param>
    /// <param name="plot">The plot area the axes is drawn in; its width sets the thickness of a colour bar below it.
    /// <see langword="null"/> when it is not known.</param>
    /// <returns>The room the top and bottom edges need, and the measured bands that lift the caps.</returns>
    internal AxisNeeds MeasureEnds(Axes axes, Figure figure, IRenderContext ctx, EdgeBands bands, Rect? plot = null)
    {
        var theme = figure.Theme;

        // --- Bottom: the X tick labels and the X axis title ---
        // What the axes draws below its bottom spine is measured by the renderer that draws it and reserved by
        // EdgeBands.BottomNeed, the rule the default path applies too, never below the floor the tight layout has always kept
        // under drawn tick labels. A renderer that measures nothing there (polar, 3-D) keeps the estimate it was always laid
        // out by: the Cartesian rule on an estimated band. An axes that draws no tick labels below keeps no floor: the pad holds
        // what is drawn off the edge, and a pad alone is no need (EdgeBands.BottomNeed).
        var xMajor = axes.XAxis.MajorTicksUnder(theme);
        double floor = xMajor.LabelsOwnSide ? XTickBottomGap + ctx.LineExtentOf(theme.TickFont()).Height + Pads.Edges.Bottom : 0;
        double below = DrawnBelow(axes, ctx, theme, bands);
        double bottomNeeded = Math.Max(floor, below > 0 ? below + Pads.Edges.Bottom : 0);
        double bottomBand = bands.DrawsBelow ? bottomNeeded : 0;
        // --- A colour bar below the plot, under the library's own themes: it stands in the bottom margin under the tick labels and
        // the X axis title of its axes, its own tick labels and its label under it (ColorBarRoom.ReachBelow, the band cleared by
        // ColorBarRoom.ClearanceOf, as the bar is drawn). It lifts the caps like the measured bands above, as content that is
        // drawn whatever the margin must never be cut; under the matplotlib themes SettleBars holds the bar's room. ---
        if (BarNeedBelow(figure, axes.ColorBar, plot, bands) is var barNeed and > 0)
        {
            bottomNeeded = Math.Max(bottomNeeded, barNeed);
            bottomBand = Math.Max(bottomBand, barNeed);
        }

        // --- Top: the top band (the mirrored X tick labels, or the secondary X axis with its title) and the axes title above
        // it (AxesTitleExtensions.TitleBand) ---
        double titleBand = axes.TitleBand(ctx, theme, bands.TopPastEdge, Pads.Edges.Top);

        // An outside legend is measured: on its edge the whole need lifts the cap.
        // What an outside legend below the plot stands past: all of it, a colour bar in the library's margin with it (DrawnPast).
        var needs = ReserveOutsideLegend(axes, ctx, theme, new Sides<double>(0, bottomNeeded, titleBand, 0), new Sides<double>(0, bottomNeeded, titleBand, 0));
        return new AxisNeeds(needs.Top, needs.Bottom, needs.Top > titleBand ? needs.Top : bands.TopPastEdge > 0 ? titleBand : 0,
            needs.Bottom > bottomNeeded ? needs.Bottom : bottomBand);
    }

    /// <summary>The room <paramref name="bar"/> needs beside the plot when its room is kept while the edges are measured (the library's
    /// margin, <see cref="BarRoomStage.Measure"/>): what the axes draws past its right spine (<paramref name="pastRightSpine"/>), the
    /// gap, the bar and its text (<see cref="ColorBarRoom.ReachBeside"/>), and the pad; 0 for a bar below, a hidden bar, and a bar
    /// whose room another stage keeps (<see cref="Rendering.Layout.ConstrainedLayoutEngine.SettleColorBars"/>). The bar of an axes with the axes' series, and the figure's bar with
    /// every subplot's series past the last column: the one rule both layouts keep the right edge by, as the bar is drawn
    /// (<see cref="ColorBarRoom.ClearanceOf"/>).</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="bar">The bar of the axes, or of the figure.</param>
    /// <param name="owner">The axes whose series back the bar; <see langword="null"/> for the figure's bar, which every subplot's
    /// series back (gathered only when the bar needs room here).</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="pastRightSpine">What the axes, or the last column, draws past its right spine (<see cref="RightBand.Past"/>).</param>
    internal double BarNeedBeside(Figure figure, ColorBar? bar, Axes? owner, IRenderContext ctx, double pastRightSpine)
    {
        var bars = figure.BarRoom;
        if (bar is not { Visible: true } || !bar.Beside || !bars.SettlesAt(bar, BarRoomStage.Measure)) return 0;
        var series = owner?.Series ?? figure.SubPlots.SelectMany(axes => axes.Series);
        double reach = bars.ReachBeside(bar, null, series, ctx, figure.Theme, bars.ClearanceOf(bar, new MeasuredBands(default, pastRightSpine)));
        return reach > 0 ? reach + Pads.Edges.Right : 0;
    }

    /// <summary>The room the colour bar of an axes needs below its plot when its room is kept while the edges are measured (the
    /// library's margin): what the axes draws under its plot (<paramref name="bands"/>), the gap, the bar and its text
    /// (<see cref="ColorBarRoom.ReachBelow"/>), and the pad; 0 for a bar beside, a hidden bar, and a bar whose room another stage keeps.
    /// The one rule both layouts keep the bottom edge by.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="bar">The bar of the axes.</param>
    /// <param name="plot">Its plot area, whose width the bar's thickness reads; <see langword="null"/> when it is not known.</param>
    /// <param name="bands">What the axes draws past its spines.</param>
    internal double BarNeedBelow(Figure figure, ColorBar? bar, Rect? plot, EdgeBands bands)
    {
        var bars = figure.BarRoom;
        if (bar is not { Visible: true } || bar.Beside || !bars.SettlesAt(bar, BarRoomStage.Measure)) return 0;
        double reach = bars.ReachBelow(bar, plot, figure.Theme, bars.ClearanceOf(bar, new MeasuredBands(bands, 0)));
        return reach > 0 ? reach + Pads.Edges.Bottom : 0;
    }

    /// <summary>The room the figure's own colour bar needs below the box of every plot when its room is kept while the edges are
    /// measured (the library's margin): what the bottom row draws under its plots (<see cref="ColorBarRoomExtensions.FigureBarClearance"/>,
    /// the band the drawing clears too), the gap, the bar and its text (<see cref="ColorBarRoom.ReachBelow"/>) and the pad; 0 for a bar
    /// beside, a hidden bar, a bar whose room another stage keeps, and a figure with no subplot. The bottom margin of both layouts holds
    /// it.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="cells">The grid its subplots stand on.</param>
    /// <param name="ends">The end bands of each subplot, measured at <paramref name="plots"/>.</param>
    /// <param name="plots">The plot area of each subplot.</param>
    internal double FigureBarNeedBelow(Figure figure, FigureGrid cells, EdgeBands[] ends, List<Rect> plots)
    {
        var bars = figure.BarRoom;
        if (figure.FigureColorBar is not { Visible: true } bar || bar.Beside || !bars.SettlesAt(bar, BarRoomStage.Measure)
            || Rect.Enclosing(plots) is not { } box) return 0;
        double reach = bars.ReachBelow(bar, box, figure.Theme, figure.FigureBarClearance(plots, cells, i => new MeasuredBands(ends[i], 0)));
        return reach > 0 ? reach + Pads.Edges.Bottom : 0;
    }

    /// <summary>The room the edge <paramref name="bar"/> stands at keeps when the outside legend of <paramref name="axes"/> stands past
    /// the bar there (<see cref="DrawnPast"/>): what the edge needs for the bar (<paramref name="past"/>, its reach and the pad), then the
    /// legend's box and a gap on either side (<see cref="ReserveOutsideLegend"/>); <paramref name="past"/> alone when the legend stands
    /// inside the plot or on another edge. The one rule for the room past a bar in the layout's margin: an axes' own bar
    /// (<see cref="Rendering.Layout.ConstrainedLayoutEngine.SettleColorBars"/>) and the figure's (<see cref="RoomPastFigureBar"/>).</summary>
    /// <param name="axes">The axes whose legend may stand past the bar.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="theme">The theme the axes is drawn under.</param>
    /// <param name="bar">The bar, which says the edge.</param>
    /// <param name="past">What the edge needs for the bar.</param>
    /// <returns>The room, in pixels.</returns>
    internal static double RoomPastBar(Axes axes, IRenderContext ctx, Theme theme, ColorBar bar, double past)
    {
        var edge = default(Sides<double>).WithEndAlong(bar.Beside, past);
        return ReserveOutsideLegend(axes, ctx, theme, edge, edge).EndAlong(bar.Beside);
    }

    /// <summary>The room the edge the figure's own colour bar stands at keeps for the bar and for the outside legend of each axes on
    /// that edge of the grid, which stands past the bar (<see cref="RoomPastBar"/>, <see cref="FigureGrid.StandsAtBarEdge"/>): the
    /// widest; <paramref name="past"/> alone when no such legend stands there, the figure's bar is hidden, or it needs no room.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="cells">The grid its subplots stand on.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="past">What the edge needs for the figure's bar: its reach and the pad.</param>
    /// <returns>The room, in pixels.</returns>
    internal static double RoomPastFigureBar(Figure figure, FigureGrid cells, IRenderContext ctx, double past)
    {
        if (past <= 0 || figure.FigureColorBar is not { Visible: true } bar) return past;
        double room = past;
        for (int i = 0; i < figure.SubPlots.Count; i++)
            if (cells.StandsAtBarEdge(i, bar)) room = Math.Max(room, RoomPastBar(figure.SubPlots[i], ctx, figure.Theme, bar, past));
        return room;
    }

    /// <summary><paramref name="needs"/> with the room the axes' legend keeps past the edge it stands at, when it stands outside
    /// the plot: its position's strategy says what the box needs on that one edge (<see cref="LegendPositionStrategy.ReserveFor"/>),
    /// the box measured by <see cref="LegendLayoutCalculator.MeasureLegendBox"/>, by the formulas the legend is drawn by.</summary>
    /// <param name="axes">The axes.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="theme">The theme the axes is drawn under.</param>
    /// <param name="needs">What the edges need without the legend.</param>
    /// <param name="past">What the edges need for what the box stands past (<see cref="DrawnPast"/> and the pad).</param>
    private static Sides<double> ReserveOutsideLegend(Axes axes, IRenderContext ctx, Theme theme, Sides<double> needs, Sides<double> past)
    {
        var legendPosition = LegendPositionStrategyFactory.Create(axes.Legend.PositionUnder(theme));
        if (!axes.Legend.Visible || !legendPosition.StandsOutside) return needs;
        var legendBox = LegendLayoutCalculator.MeasureLegendBox(axes, ctx, theme);
        return legendBox.Width > 0 && legendBox.Height > 0 ? legendPosition.ReserveFor(legendBox, needs, past) : needs;
    }

    /// <summary>The one rule for what the right edge of <paramref name="axes"/> draws past its right spine, measured
    /// with the functions the drawing uses: the axis side (the secondary Y axis with its tick marks, pad, widest tick
    /// label and title, or the mirrored primary Y tick labels) and, stacked after it, the end-of-line labels
    /// (<see cref="DirectLabelMetrics"/>). Both layout paths reserve it: <see cref="ConstrainedLayoutEngine.Compute"/> for tight and
    /// constrained layout, and <see cref="ConstrainedLayoutEngine.ReserveEdgeBands"/> for the default path.</summary>
    /// <param name="axes">The axes to measure.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="theme">The active theme (fonts).</param>
    /// <param name="mirroredYTicks">The mirrored Y tick band the axes' renderer measured
    /// (<see cref="EdgeBands.Right"/>); 0 when none is drawn.</param>
    /// <returns>The bands; <see cref="RightBand.WidthWith"/> is the room the edge needs.</returns>
    /// <remarks>Every right slot is charged its band (<see cref="SecondaryAxisBands"/>), stacked by the one composition a render uses
    /// too (<see cref="RightBand.Of"/>). A slot without series draws nothing, but it is still charged: what it would draw is what the
    /// figure asked for, and a reservation that is too wide never clips. The end-label column (<c>CartesianDirectLabelsPart</c>)
    /// starts past the axis side, so labels never sit on the slots' ticks.</remarks>
    internal RightBand MeasureRightBand(Axes axes, IRenderContext ctx, Theme theme, double mirroredYTicks) =>
        RightBand.Of(axes, theme, SecondaryAxisBands(axes, ctx, theme), mirroredYTicks, new DirectLabelMetrics(axes, theme, ctx).Band());

    /// <summary>The band of every right slot of the container, nearest the plot first (<see cref="SlotBand"/>):
    /// <see cref="MeasureRightBand"/> stacks these for the layout's reservation, and the renderer stacks them
    /// (<see cref="AxisStackingExtensions.StackedOffsets"/>) to place each slot past every slot before it. Empty when the
    /// axes has no right slot.</summary>
    /// <param name="axes">The axes whose right slots are measured.</param>
    /// <param name="ctx">The context the bands are measured with.</param>
    /// <param name="theme">The theme the axes is drawn under.</param>
    /// <returns>The width of each band, in pixels, nearest the plot first.</returns>
    internal double[] SecondaryAxisBands(Axes axes, IRenderContext ctx, Theme theme) =>
        SlotBands(axes.YAxes.Right, axes, ctx, theme);

    /// <summary>The band of every left slot of the container past the primary, nearest the plot first
    /// (<see cref="SlotBand"/>): stacked after the primary's own band for the left margin (<see cref="AxesLeftEdgeExtensions.LeftNeed"/>) and for
    /// each slot's render-time offset. Empty when the axes has no left slot past the primary.</summary>
    internal double[] LeftAxisBands(Axes axes, IRenderContext ctx, Theme theme) =>
        SlotBands(axes.YAxes.Left.Skip(axes.YAxes.Left.OutwardOffset), axes, ctx, theme);

    private double[] SlotBands(IEnumerable<YAxisSlot> slots, Axes axes, IRenderContext ctx, Theme theme)
    {
        // A loop, not a projection: an axes without slots on a side, the usual one, allocates nothing here.
        List<double>? bands = null;
        foreach (var slot in slots) (bands ??= []).Add(SlotBand(axes, slot, ctx, theme));
        return bands is null ? [] : [.. bands];
    }

    /// <summary>From one stacked slot's edge to the far side of what it draws, as its part draws it: the slot placed where
    /// the render places it (<see cref="CartesianAxisPlacement.PlaceSlot"/>: its range, its ticks), beside the primary's range
    /// (the draw's, which a linked slot lays its ticks out over; any other slot reads only its X extent, which no band depends
    /// on), and measured by the band rule the render stacks it by (<see cref="CartesianAxisPlacement.SlotBand"/>). The plot's
    /// height is not known yet: the slot is placed beside a plot as high as its densest ticks need
    /// (<see cref="AxesTickSourceExtensions"/>' <c>DensestTickLength</c>), so what is reserved holds the labels of a shorter plot,
    /// whose ticks are a coarser set of the same values.</summary>
    /// <param name="axes">The axes the slot belongs to.</param>
    /// <param name="slot">The slot.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="theme">The active theme (fonts).</param>
    /// <returns>The slot's band.</returns>
    private double SlotBand(Axes axes, YAxisSlot slot, IRenderContext ctx, Theme theme)
    {
        var placement = new CartesianAxisPlacement(axes, new Rect(0, 0, 0, slot.Axis.DensestTickLength(theme, isX: false)), ctx, theme, ranges);
        return placement.SlotBand(placement.PlaceSlot(slot, placement.ComputeDataRanges()));
    }

    /// <summary>What the axes at <paramref name="index"/> draws past its four spines, measured by the renderer the axes is drawn with
    /// (<see cref="AxesRenderer.MeasureEdgeBands"/>), the owner that draws it. The plot's height is not laid out yet, so
    /// the renderer is handed the height of the axes' grid cell, the upper bound of its plot height: the number of Y ticks
    /// depends on that height only below the length of the axis' densest ticks (<see cref="AxesTickSourceExtensions"/>'
    /// <c>DensestTickLength</c>), and a taller plot never has fewer. Its width is the drawn one once the sides are settled
    /// (<paramref name="plot"/>, from <see cref="FigureGrid.PlotRects"/>): the X tick labels turn when upright ones
    /// would collide at that width, so the top and bottom bands are read from this measurement. Before that (no
    /// <paramref name="plot"/>) the plot has no width, and only the left and right bands, which do not depend on it, are
    /// read; with it, the sideways overhang of the X tick labels (<see cref="EdgeBands.Overhang"/>) is read too, which the sides
    /// are then widened for (the settling of the sides (<c>ConstrainedLayoutEngine.SettleSides</c>)).</summary>
    /// <param name="figure">The figure the axes belongs to.</param>
    /// <param name="cells">The grid the figure's subplots are laid out on.</param>
    /// <param name="index">The axes' index in <see cref="Figure.SubPlots"/>.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="plot">The plot area the axes is drawn in under the settled sides; only its X and width are used.</param>
    /// <remarks>A spine moved off its edge hangs its bands where it stands at the plot the axes is drawn at, which only the layout's
    /// result says: the insets are read at the plot the last pass laid out (<see cref="ConstrainedLayoutEngine"/> lays the figure out
    /// again at those plots until they stand), none on the first.</remarks>
    internal EdgeBands EdgeBandsOf(Figure figure, FigureGrid cells, int index, IRenderContext ctx, Rect? plot = null)
    {
        var axes = figure.SubPlots[index];
        double cellHeight = cells.CellHeight(index);
        var area = plot is { } drawnWidth ? new Rect(drawnWidth.X, 0, drawnWidth.Width, cellHeight) : new Rect(0, 0, 0, cellHeight);
        var renderer = AxesRenderer.Create(axes, area, ctx, figure.Theme, null, ranges);
        // Each pass measures only the edges it reads: the sides before the plot has a width, the ends once it has one. The insets are
        // the laid-out plot's, which the linked X axes below the plot are stacked past too; and what keeps a share of the axes' height
        // (a table's rows) is measured at the axes the last pass laid out, as matplotlib's layouts measure it at the axes as they stand
        // when they run: a first pass measures it at the axes the subplot parameters place.
        var insets = default(SpineInsets);
        renderer.Stretch = 1;
        if (drawn is { } laidOut)
        {
            insets = renderer.SpineInsetsAt(laidOut[index]);
            renderer.Stretch = ranges?.StretchOf(axes, laidOut[index].Height) ?? 1;
        }
        return plot is null ? renderer.MeasureSideBands() with { Insets = insets } : renderer.EndBands(insets);
    }
}
